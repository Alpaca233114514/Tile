// Platform-free rules; the only module imported by Node tests.
export const KINDS = ['clock', 'weather', 'photo', 'calendar'];
export const TITLES = {clock: '时钟', weather: '天气', photo: '照片', calendar: '日历'};
export const SIZES = ['small', 'medium', 'large'];
export function dimensions(size) {
    return {width: size === 'small' ? 192 : 400, height: size === 'large' ? 400 : 192};
}
export function parseWidgets(raw) {
    const items = JSON.parse(raw);
    if (!Array.isArray(items) || items.length > 100)
        throw new Error('组件配置格式不兼容');
    const ids = new Set();
    return items.map(item => {
        if (!item || !KINDS.includes(item.kind) || typeof item.id !== 'string' || ids.has(item.id))
            throw new Error('组件类型或标识无效');
        ids.add(item.id);
        return {...item, size: SIZES.includes(item.size) ? item.size : 'small',
            x: Number.isFinite(item.x) ? item.x : 64,
            y: Number.isFinite(item.y) ? item.y : 80};
    });
}
export function position(x, y, size, area) {
    const {width, height} = dimensions(size);
    return {
        x: Math.max(area.x, Math.min(Math.round(x / 8) * 8, area.x + Math.max(0, area.width - width))),
        y: Math.max(area.y, Math.min(Math.round(y / 8) * 8, area.y + Math.max(0, area.height - height))),
    };
}
export function dragPosition(origin, pointerStart, pointerNow) {
    return {x: origin.x + pointerNow.x - pointerStart.x, y: origin.y + pointerNow.y - pointerStart.y};
}
export function weatherDescription(code) {
    if (code === 0) return ['晴朗', '☀'];
    if (code === 1 || code === 2) return ['晴间多云', '☀'];
    if (code === 3) return ['多云', '☁'];
    if (code === 45 || code === 48) return ['雾', '≋'];
    if (code >= 71 && code <= 77 || code === 85 || code === 86) return ['降雪', '❄'];
    if (code >= 95 && code <= 99) return ['雷雨', 'ϟ'];
    if (code >= 51 && code <= 67 || code >= 80 && code <= 82) return ['降雨', '☂'];
    return ['天气', '◌'];
}
