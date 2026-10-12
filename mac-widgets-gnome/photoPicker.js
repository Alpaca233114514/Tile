import Gio from 'gi://Gio';
import GLib from 'gi://GLib';

// Use the desktop portal: GTK must not be imported into the Shell process.
export class PhotoPicker {
    constructor() { this.pending = new Set(); }
    choose() {
        return new Promise((resolve, reject) => {
            const bus = Gio.DBus.session;
            const token = `tile_${GLib.uuid_string_random().replaceAll('-', '_')}`;
            const sender = bus.get_unique_name().slice(1).replaceAll('.', '_');
            let path = `/org/freedesktop/portal/desktop/request/${sender}/${token}`;
            let subscription = 0;
            let finished = false;
            const cancel = () => {
                bus.call('org.freedesktop.portal.Desktop', path, 'org.freedesktop.portal.Request', 'Close', null, null, Gio.DBusCallFlags.NONE, 3000, null, null);
                finish(null);
            };
            const finish = (value, error = null) => {
                if (finished) return;
                finished = true;
                if (subscription) bus.signal_unsubscribe(subscription);
                this.pending.delete(cancel);
                if (error) reject(error); else resolve(value);
            };
            const subscribe = () => bus.signal_subscribe('org.freedesktop.portal.Desktop', 'org.freedesktop.portal.Request', 'Response', path, null, Gio.DBusSignalFlags.NONE,
                (_bus, _sender, _path, _interface, _signal, parameters) => {
                    const [response, results] = parameters.deep_unpack();
                    if (response !== 0) { finish(null); return; }
                    const uris = results.uris instanceof GLib.Variant ? results.uris.deep_unpack() : results.uris;
                    const uri = uris?.[0];
                    if (!uri?.startsWith('file://')) { finish(null, new Error('请选择本地照片')); return; }
                    finish(Gio.File.new_for_uri(uri).get_path());
                });
            subscription = subscribe();
            this.pending.add(cancel);
            const options = {
                handle_token: new GLib.Variant('s', token),
                multiple: new GLib.Variant('b', false),
                modal: new GLib.Variant('b', false),
                filters: new GLib.Variant('a(sa(us))', [['照片', [[1, 'image/jpeg'], [1, 'image/png'], [1, 'image/webp'], [1, 'image/bmp'], [1, 'image/gif']]]]),
            };
            bus.call('org.freedesktop.portal.Desktop', '/org/freedesktop/portal/desktop', 'org.freedesktop.portal.FileChooser', 'OpenFile',
                new GLib.Variant('(ssa{sv})', ['', '选择本地照片', options]), new GLib.VariantType('(o)'), Gio.DBusCallFlags.NONE, 15000, null,
                (connection, result) => {
                    try {
                        const [returned] = connection.call_finish(result).deep_unpack();
                        if (finished) { bus.call('org.freedesktop.portal.Desktop', returned, 'org.freedesktop.portal.Request', 'Close', null, null, Gio.DBusCallFlags.NONE, 3000, null, null); return; }
                        if (!finished && returned !== path) { bus.signal_unsubscribe(subscription); path = returned; subscription = subscribe(); }
                    } catch (error) { finish(null, error); }
                });
        });
    }
    destroy() { for (const cancel of [...this.pending]) cancel(); }
}
