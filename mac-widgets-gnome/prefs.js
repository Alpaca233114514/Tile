import Adw from 'gi://Adw';
import Gtk from 'gi://Gtk';
import GLib from 'gi://GLib';
import {ExtensionPreferences} from 'resource:///org/gnome/Shell/Extensions/js/extensions/prefs.js';
import {parseWidgets, KINDS, TITLES, SIZES} from './core.js';
import {Network} from './network.js';

export default class TilePreferences extends ExtensionPreferences {
    fillPreferencesWindow(window) {
        const settings = this.getSettings();
        const network = new Network();
        window.set_default_size(700, 780);
        const page = new Adw.PreferencesPage({title: 'Tile', icon_name: 'view-grid-symbolic'});
        window.add(page);
        const intro = new Adw.PreferencesGroup({title: '一眼之间，生活在这里。', description: '直接按住卡片拖动，右键配置。照片卡片可直接选择本地图片。'});
        page.add(intro);
        const theme = new Adw.ComboRow({title: '外观', model: Gtk.StringList.new(['跟随系统', '浅色', '深色'])});
        theme.selected = Math.max(0, ['system', 'light', 'dark'].indexOf(settings.get_string('theme')));
        theme.connect('notify::selected', () => settings.set_string('theme', ['system', 'light', 'dark'][theme.selected]));
        intro.add(theme);
        const library = new Adw.PreferencesGroup({title: '小组件库', description: '选一个喜欢的，添加到桌面。'}); page.add(library);
        let active;
        let closed = false;
        function mutate(action) {
            try { const items = parseWidgets(settings.get_string('widgets')); action(items); settings.set_string('widgets', JSON.stringify(items)); }
            catch (error) { window.add_toast(new Adw.Toast({title: `配置未改写：${error.message}`})); }
        }
        for (const kind of KINDS) {
            const row = new Adw.ActionRow({title: TITLES[kind], subtitle: {clock: '本地时间与日期', weather: '城市天气 · Open-Meteo', photo: '你的本地照片', calendar: '今天与月历'}[kind]});
            const add = new Gtk.Button({label: '添加', valign: Gtk.Align.CENTER});
            add.add_css_class('suggested-action'); row.add_suffix(add); library.add(row);
            add.connect('clicked', () => { mutate(items => items.push({id: GLib.uuid_string_random(), kind, size: 'small', x: 64 + items.length % 4 * 208, y: 80 + Math.floor(items.length / 4) * 208})); rebuild(); });
        }
        function update(id, values) { mutate(items => { const item = items.find(w => w.id === id); if (item) Object.assign(item, values); }); }
        function rebuild() {
            if (active) page.remove(active);
            active = new Adw.PreferencesGroup({title: '已在桌面'}); page.add(active);
            let items;
            try { items = parseWidgets(settings.get_string('widgets')); }
            catch (_error) { active.add(new Adw.ActionRow({title: '配置无法读取', subtitle: '原数据保留。请先备份 dconf 后再排查。'})); return; }
            if (!items.length) active.add(new Adw.ActionRow({title: '桌面还是空的', subtitle: '添加第一个小组件吧。'}));
            for (const item of items) {
                const group = new Adw.ExpanderRow({title: TITLES[item.kind], expanded: item.kind === 'photo', subtitle: item.city || (item.photo ? GLib.path_get_basename(item.photo) : '展开配置')});
                active.add(group);
                const remove = new Gtk.Button({icon_name: 'user-trash-symbolic', valign: Gtk.Align.CENTER, tooltip_text: '移除小组件'});
                group.add_action(remove); remove.connect('clicked', () => { mutate(list => list.splice(list.findIndex(w => w.id === item.id), 1)); rebuild(); });
                const size = new Adw.ComboRow({title: '尺寸', model: Gtk.StringList.new(['小 · 192 × 192', '中 · 400 × 192', '大 · 400 × 400']), selected: SIZES.indexOf(item.size)});
                size.connect('notify::selected', () => update(item.id, {size: SIZES[size.selected]})); group.add_row(size);
                if (item.kind === 'photo') {
                    const row = new Adw.ActionRow({title: '本地照片', subtitle: item.photo ? GLib.path_get_basename(item.photo) : '未选择'});
                    const choose = new Gtk.Button({label: '选择本地照片…', valign: Gtk.Align.CENTER}); row.add_suffix(choose); group.add_row(row);
                    choose.connect('clicked', () => {
                        const dialog = new Gtk.FileChooserNative({title: '选择照片', transient_for: window, action: Gtk.FileChooserAction.OPEN, accept_label: '选择', cancel_label: '取消'});
                        const filter = new Gtk.FileFilter({name: '照片'}); for (const mime of ['image/jpeg', 'image/png', 'image/webp']) filter.add_mime_type(mime); dialog.add_filter(filter);
                        dialog.connect('response', (_dialog, response) => { if (response === Gtk.ResponseType.ACCEPT && !closed) { const path = dialog.get_file()?.get_path(); if (path) { update(item.id, {photo: path}); row.subtitle = GLib.path_get_basename(path); } } dialog.destroy(); });
                        dialog.show();
                    });
                }
                if (item.kind === 'weather') {
                    const query = new Adw.EntryRow({title: '城市名称', text: item.city?.split('·')[0]?.trim() || ''}); group.add_row(query);
                    const search = new Gtk.Button({label: '搜索', valign: Gtk.Align.CENTER}); query.add_suffix(search);
                    const results = new Adw.ComboRow({title: '地点', model: Gtk.StringList.new(['先搜索城市'])}); group.add_row(results);
                    const use = new Gtk.Button({label: '使用此地点', valign: Gtk.Align.CENTER, sensitive: false});
                    const status = new Adw.ActionRow({title: '搜索后选择地点', subtitle: '天气数据由 Open-Meteo 提供'}); status.add_suffix(use); group.add_row(status);
                    let places = [];
                    search.connect('clicked', async () => {
                        if (query.text.trim().length < 2) { status.title = '请输入至少两个字符'; return; }
                        search.sensitive = false; use.sensitive = false; status.title = '搜索中…';
                        try {
                            const data = await network.get(`https://geocoding-api.open-meteo.com/v1/search?count=8&language=zh&name=${encodeURIComponent(query.text.trim())}`);
                            if (closed) return;
                            places = data.results || [];
                            results.model = Gtk.StringList.new(places.map(p => `${p.name} · ${p.admin1 || ''} ${p.country || ''}`));
                            results.selected = 0; use.sensitive = places.length > 0;
                            status.title = places.length ? '请选择地点并应用' : '未找到，试试拼音或英文名称';
                        } catch (_error) { if (!closed) status.title = '网络不可用，请重试'; }
                        finally { if (!closed) search.sensitive = true; }
                    });
                    use.connect('clicked', () => {
                        const place = places[results.selected]; if (!place) return;
                        const city = `${place.name} · ${place.admin1 || ''} ${place.country || ''}`;
                        update(item.id, {city, latitude: place.latitude, longitude: place.longitude}); group.subtitle = city; status.title = '已更新城市';
                    });
                }
            }
        }
        rebuild();
        window.connect('close-request', () => { closed = true; network.destroy(); return false; });
    }
}
