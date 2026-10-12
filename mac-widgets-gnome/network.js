import Gio from 'gi://Gio';
import Soup from 'gi://Soup?version=3.0';

export class Network {
    constructor() {
        this._session = new Soup.Session({timeout: 12, user_agent: 'Tile-GNOME/0.1'});
        this._cancel = new Gio.Cancellable();
    }
    get(url) {
        const message = Soup.Message.new('GET', url);
        return new Promise((resolve, reject) => {
            this._session.send_and_read_async(message, 0, this._cancel, (session, result) => {
                try {
                    const bytes = session.send_and_read_finish(result);
                    if (message.status_code !== 200) throw new Error(`HTTP ${message.status_code}`);
                    resolve(JSON.parse(new TextDecoder().decode(bytes.get_data())));
                } catch (error) { reject(error); }
            });
        });
    }
    destroy() { this._cancel.cancel(); this._session.abort(); }
}
