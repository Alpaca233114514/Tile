import Clutter from 'gi://Clutter';
import Gio from 'gi://Gio';
import GLib from 'gi://GLib';
import St from 'gi://St';
import {Extension} from 'resource:///org/gnome/shell/extensions/extension.js';
import * as Main from 'resource:///org/gnome/shell/ui/main.js';
import * as PanelMenu from 'resource:///org/gnome/shell/ui/panelMenu.js';
import * as PopupMenu from 'resource:///org/gnome/shell/ui/popupMenu.js';
import {dimensions, parseWidgets, position, dragPosition, weatherDescription, TITLES, KINDS} from './core.js';
import {Network} from './network.js';
import {PhotoPicker} from './photoPicker.js';

class Card {
    constructor(extension, model) {
        this.extension = extension;
        this.model = model;
        this.alive = true;
        this.network = new Network();
        this.nextWeather = 0;
        this.actor = new St.BoxLayout({vertical: true, reactive: true,
            style_class: `tile-card tile-${model.kind} ${extension.isDark() ? 'tile-dark' : 'tile-light'}`,
            ...dimensions(model.size)});
        this.title = new St.Label({text: TITLES[model.kind], style_class: 'tile-title'});
        this.body = new St.BoxLayout({vertical: true, x_expand: true, y_expand: true, style_class: 'tile-body'});
        this.actor.add_child(this.title); this.actor.add_child(this.body);
        extension.layer.add_child(this.actor);
        const area = extension.areaFor(model.x, model.y);
        const p = position(model.x, model.y, model.size, area);
        this.actor.set_position(p.x, p.y);
        this.menu = new PopupMenu.PopupMenu(this.actor, .5, St.Side.TOP);
        Main.uiGroup.add_child(this.menu.actor); this.menu.actor.hide();
        this.menuManager = new PopupMenu.PopupMenuManager(this.actor); this.menuManager.addMenu(this.menu);
        this.menu.addAction('配置小组件…', () => extension.openPreferences());
        if (model.kind === 'photo') this.menu.addAction('选择本地照片…', () => extension.choosePhoto(model.id));
        this.menu.addAction('突出显示拖动区域', () => extension.setEditing(true));
        this.menu.addAction('完成编辑', () => extension.setEditing(false));
        for (const [size, label] of [['small', '小'], ['medium', '中'], ['large', '大']])
            this.menu.addAction(`尺寸 · ${label}`, () => extension.mutate(items => { items.find(w => w.id === model.id).size = size; }));
        this.menu.addAction('移除小组件', () => extension.mutate(items => items.splice(items.findIndex(w => w.id === model.id), 1)));
        this.actor.connect('button-press-event', (_actor, event) => {
            if (event.get_button() === Clutter.BUTTON_SECONDARY) { this.menu.toggle(); return Clutter.EVENT_STOP; }
            return this.startDrag(event);
        });
        this.updateEditing();
        this.timer = GLib.timeout_add_seconds(GLib.PRIORITY_DEFAULT, 30, () => { this.refresh(); return GLib.SOURCE_CONTINUE; });
        this.refresh();
    }
    startDrag(event) {
        if (event.get_button() !== Clutter.BUTTON_PRIMARY) return Clutter.EVENT_PROPAGATE;
        for (let source = event.get_source(); source && source !== this.actor; source = source.get_parent())
            if (source instanceof St.Button) return Clutter.EVENT_PROPAGATE;
        const [startX, startY] = event.get_coords();
        const originX = this.actor.x, originY = this.actor.y;
        this.endDrag();
        this.dragOrigin = {x: originX, y: originY};
        this.dragMoved = false;
        this.grab = global.stage.grab(this.actor);
        this.capture = global.stage.connect('captured-event', (_stage, ev) => {
            if (ev.type() === Clutter.EventType.MOTION) {
                const [x, y] = ev.get_coords();
                if (!this.dragMoved && Math.abs(x - startX) < 4 && Math.abs(y - startY) < 4) return Clutter.EVENT_STOP;
                this.dragMoved = true;
                this.actor.add_style_class_name('tile-editing');
                const p = dragPosition({x: originX, y: originY}, {x: startX, y: startY}, {x, y});
                this.actor.set_position(p.x, p.y);
                return Clutter.EVENT_STOP;
            }
            if (ev.type() === Clutter.EventType.BUTTON_RELEASE) {
                if (!this.dragMoved) { this.endDrag(); return Clutter.EVENT_STOP; }
                const area = this.extension.areaFor(this.actor.x, this.actor.y);
                const p = position(this.actor.x, this.actor.y, this.model.size, area);
                this.dragOrigin = null; this.endDrag();
                this.extension.mutate(items => Object.assign(items.find(w => w.id === this.model.id), p));
                return Clutter.EVENT_STOP;
            }
            if (ev.type() === Clutter.EventType.KEY_PRESS && ev.get_key_symbol() === Clutter.KEY_Escape) {
                this.actor.set_position(originX, originY); this.endDrag(); return Clutter.EVENT_STOP;
            }
            return Clutter.EVENT_PROPAGATE;
        });
        return Clutter.EVENT_STOP;
    }
    endDrag() {
        if (this.capture) { global.stage.disconnect(this.capture); this.capture = 0; }
        this.grab?.dismiss(); this.grab = null;
        if (this.dragOrigin) { this.actor.set_position(this.dragOrigin.x, this.dragOrigin.y); this.dragOrigin = null; }
        if (!this.extension.editing) this.actor.remove_style_class_name('tile-editing');
    }
    updateEditing() {
        if (!this.extension.editing) this.endDrag();
        this.title.text = TITLES[this.model.kind] + (this.extension.editing ? ' · 拖动排列' : '');
        if (this.extension.editing) this.actor.add_style_class_name('tile-editing');
        else this.actor.remove_style_class_name('tile-editing');
    }
    label(text, style = 'tile-detail') {
        const label = new St.Label({text, style_class: style});
        this.body.add_child(label); return label;
    }
    draw() {
        this.body.destroy_all_children();
        const now = GLib.DateTime.new_now_local();
        if (this.model.kind === 'clock') {
            this.label(now.format('%H:%M'), 'tile-time');
            this.label(now.format('%m月%d日 · %A'));
            if (this.model.size === 'large') this.label('把时间，留给生活。', 'tile-message');
        } else if (this.model.kind === 'weather') {
            this.label(this.model.city?.split('·')[0] || '选择你的城市');
            this.label(this.weather ? `${Math.round(this.weather.temperature)}° ${this.weather.symbol}` : '—°', 'tile-temperature');
            this.label(this.status || (this.weather ? `${this.weather.description} · ${this.weather.at}` : '右键配置城市'));
            if (this.model.size !== 'small') this.label('Open-Meteo', 'tile-credit');
        } else if (this.model.kind === 'calendar') {
            this.label(now.format('%A'));
            this.label(now.format('%-d'), 'tile-time');
            this.label(now.format('%Y年 %m月'));
            if (this.model.size === 'large') {
                const date = new Date();
                const first = new Date(date.getFullYear(), date.getMonth(), 1);
                const offset = (first.getDay() + 6) % 7;
                const days = new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
                let calendar = ' 一  二  三  四  五  六  日\n';
                const cells = Array(offset).fill('   ');
                for (let i = 1; i <= days; i++) cells.push(String(i).padStart(3));
                for (let i = 0; i < cells.length; i += 7) calendar += cells.slice(i, i + 7).join(' ') + '\n';
                this.label(calendar, 'tile-calendar');
            }
        } else {
            const file = this.model.photo ? Gio.File.new_for_path(this.model.photo) : null;
            const hasPhoto = file?.query_exists(null);
            if (hasPhoto) {
                const photo = new St.Widget({x_expand: true, y_expand: true, style_class: 'tile-photo-image'});
                photo.set_style(`background-image: url("${file.get_uri().replaceAll('"', '%22')}"); background-size: cover;`);
                this.body.add_child(photo);
            } else {
                this.label('▧', 'tile-photo-empty');
                this.label(this.model.photo ? '照片不可用，请重新选择' : '留住喜欢的瞬间');
            }
            const choose = new St.Button({label: hasPhoto ? '更换照片' : '选择本地照片', style_class: 'tile-photo-choose', can_focus: true});
            choose.connect('clicked', () => this.extension.choosePhoto(this.model.id));
            this.body.add_child(choose);
        }
    }
    async refresh() {
        if (!this.alive || this.busy) return;
        this.busy = true;
        try {
            this.draw();
            if (this.model.kind !== 'weather' || !this.model.city || Date.now() < this.nextWeather) return;
            this.status = '正在更新…'; this.draw();
            try {
                const m = this.model;
                const json = await this.network.get(`https://api.open-meteo.com/v1/forecast?latitude=${encodeURIComponent(m.latitude)}&longitude=${encodeURIComponent(m.longitude)}&current=temperature_2m,weather_code&timezone=auto`);
                if (!Number.isFinite(json.current?.temperature_2m)) throw new Error('无有效温度');
                const [description, symbol] = weatherDescription(json.current.weather_code);
                this.weather = {temperature: json.current.temperature_2m, description, symbol, at: GLib.DateTime.new_now_local().format('%H:%M')};
                this.status = ''; this.nextWeather = Date.now() + 900000;
            } catch (_error) {
                this.status = this.weather ? `离线 · 上次 ${this.weather.at}` : '暂时无法获取天气';
                this.nextWeather = Date.now() + 120000;
            }
            if (this.alive) this.draw();
        } finally { this.busy = false; }
    }
    destroy() {
        this.alive = false; this.endDrag();
        if (this.timer) GLib.Source.remove(this.timer);
        this.network.destroy(); this.menu.destroy(); this.actor.destroy();
    }
}

export default class TileExtension extends Extension {
    enable() {
        this.editing = false;
        this.photoPicker = new PhotoPicker();
        this.settings = this.getSettings(); this.cards = []; this.backgroundItems = [];
        this.interfaceSettings = new Gio.Settings({schema_id: 'org.gnome.desktop.interface'});
        this.layer = new St.Widget({name: 'tile-desktop-layer', reactive: false,
            layout_manager: new Clutter.FixedLayout(), width: global.stage.width, height: global.stage.height});
        Main.layoutManager._backgroundGroup.add_child(this.layer);
        this.panel = new PanelMenu.Button(0, 'Tile');
        this.panel.add_child(new St.Icon({icon_name: 'view-grid-symbolic', style_class: 'system-status-icon'}));
        this.panel.menu.addAction('Tile · 小组件库…', () => this.openPreferences());
        this.editItem = new PopupMenu.PopupSwitchMenuItem('编辑小组件', false);
        this.editItem.connect('toggled', (_item, enabled) => this.setEditing(enabled));
        this.panel.menu.addMenuItem(this.editItem);
        for (const kind of KINDS) this.panel.menu.addAction(`添加${TITLES[kind]}`, () => this.add(kind));
        Main.panel.addToStatusArea(this.uuid, this.panel);
        this.changed = this.settings.connect('changed', () => this.queueRebuild());
        this.themeChanged = this.interfaceSettings.connect('changed::color-scheme', () => this.queueRebuild());
        this.monitorsChanged = Main.layoutManager.connect('monitors-changed', () => this.queueRebuild());
        this.overviewShow = Main.overview.connect('showing', () => { this.setEditing(false); this.layer.hide(); });
        this.overviewHide = Main.overview.connect('hidden', () => this.layer.show());
        this.rebuild();
    }
    setEditing(enabled) { this.editing = enabled; this.editItem.setToggleState(enabled); for (const card of this.cards) card.updateEditing(); }
    async choosePhoto(id) {
        if (this.choosingPhoto) return;
        this.choosingPhoto = true;
        try {
            const path = await this.photoPicker.choose();
            if (path && this.settings) this.mutate(items => { const item = items.find(w => w.id === id); if (item) item.photo = path; });
        } catch (error) { if (this.settings) Main.notify('Tile', `无法选择照片：${error.message}`); }
        finally { this.choosingPhoto = false; }
    }
    isDark() { const mode = this.settings.get_string('theme'); return mode === 'dark' || mode === 'system' && this.interfaceSettings.get_string('color-scheme') === 'prefer-dark'; }
    areaFor(x, y) {
        const monitors = Main.layoutManager.monitors;
        const index = monitors.findIndex(m => x >= m.x && x < m.x + m.width && y >= m.y && y < m.y + m.height);
        return Main.layoutManager.getWorkAreaForMonitor(index < 0 ? Main.layoutManager.primaryIndex : index);
    }
    mutate(action) {
        try { const items = parseWidgets(this.settings.get_string('widgets')); action(items); this.settings.set_string('widgets', JSON.stringify(items)); }
        catch (error) { console.error(`Tile: 配置未改写: ${error.message}`); Main.notify('Tile', '无法读取组件配置，原数据已保留。'); }
    }
    add(kind) { this.mutate(items => items.push({id: GLib.uuid_string_random(), kind, size: 'small', x: 64 + items.length % 4 * 208, y: 80 + Math.floor(items.length / 4) * 208})); }
    queueRebuild() {
        if (this.idle) return;
        this.idle = GLib.idle_add(GLib.PRIORITY_DEFAULT_IDLE, () => { this.idle = 0; this.rebuild(); return GLib.SOURCE_REMOVE; });
    }
    rebuild() {
        this.layer.set_size(global.stage.width, global.stage.height);
        for (const card of this.cards) card.destroy(); this.cards = [];
        try { for (const model of parseWidgets(this.settings.get_string('widgets'))) this.cards.push(new Card(this, model)); }
        catch (error) { console.error(`Tile: ${error.message}`); }
        // Preserve the existing background menu and remove only our own entry on disable.
        for (const item of this.backgroundItems) item.destroy(); this.backgroundItems = [];
        for (const manager of Main.layoutManager._bgManagers ?? []) {
            const menu = manager.backgroundActor?._backgroundMenu;
            if (menu) {
                this.backgroundItems.push(menu.addAction('添加 Tile 小组件…', () => this.openPreferences()));
                this.backgroundItems.push(menu.addAction('编辑 Tile 小组件', () => this.setEditing(true)));
            }
        }
    }
    disable() {
        this.photoPicker.destroy();
        if (this.idle) { GLib.Source.remove(this.idle); this.idle = 0; }
        this.settings.disconnect(this.changed); this.interfaceSettings.disconnect(this.themeChanged);
        Main.layoutManager.disconnect(this.monitorsChanged);
        Main.overview.disconnect(this.overviewShow); Main.overview.disconnect(this.overviewHide);
        for (const card of this.cards) card.destroy(); this.cards = [];
        for (const item of this.backgroundItems) item.destroy(); this.backgroundItems = [];
        this.panel.destroy(); this.layer.destroy(); this.settings = null; this.interfaceSettings = null;
    }
}
