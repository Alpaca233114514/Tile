import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parseWidgets, dimensions, position, dragPosition, weatherDescription} from '../core.js';

test('empty desktop stays empty after restart; malformed configuration is rejected', () => {
    assert.deepEqual(parseWidgets('[]'), []);
    assert.throws(() => parseWidgets('{broken'));
    assert.throws(() => parseWidgets('[{"kind":"unknown","id":"a"}]'));
    assert.throws(() => parseWidgets('[{"kind":"clock","id":"a"},{"kind":"clock","id":"a"}]'));
});
test('normalize old fields without dropping instance configuration', () => {
    const [w] = parseWidgets('[{"id":"1","kind":"photo","size":"old","x":null,"photo":"/tmp/a.jpg"}]');
    assert.equal(w.size, 'small'); assert.equal(w.x, 64); assert.equal(w.photo, '/tmp/a.jpg');
    assert.deepEqual(parseWidgets(JSON.stringify([w])), [w]);
});
test('snap and clamp across negative monitor origins and small work areas', () => {
    assert.deepEqual(position(-1801, 41, 'small', {x: -1920, y: 32, width: 1920, height: 1048}), {x: -1800, y: 40});
    assert.deepEqual(position(900, 900, 'large', {x: 0, y: 32, width: 600, height: 700}), {x: 200, y: 332});
    assert.deepEqual(position(-800, -100, 'large', {x: 10, y: 20, width: 300, height: 300}), {x: 10, y: 20});
});
test('card size contract', () => {
    assert.deepEqual(dimensions('small'), {width: 192, height: 192});
    assert.deepEqual(dimensions('medium'), {width: 400, height: 192});
    assert.deepEqual(dimensions('large'), {width: 400, height: 400});
});
test('WMO weather categories preserve snow, rain, fog and thunder distinctions', () => {
    for (const [code, label] of [[0, '晴朗'], [48, '雾'], [65, '降雨'], [75, '降雪'], [85, '降雪'], [95, '雷雨'], [999, '天气']]) assert.equal(weatherDescription(code)[0], label);
});
test('schema starter data uses valid instances', () => {
    const xml = readFileSync(new URL('../schemas/org.gnome.shell.extensions.tile-widgets.gschema.xml', import.meta.url), 'utf8');
    const raw = xml.match(/<default>'(\[.*?\])'<\/default>/s)[1];
    const widgets = parseWidgets(raw);
    assert.equal(widgets.length, 4);
    assert.equal(new Set(widgets.map(w => w.id)).size, 4);
});
test('whole-card drag preserves pointer offset, then persists snapped position and local photo', () => {
    const origin = {x: -1680, y: 120};
    const moved = dragPosition(origin, {x: -1600, y: 200}, {x: -1400, y: 260});
    assert.deepEqual(moved, {x: -1480, y: 180});
    assert.deepEqual(origin, {x: -1680, y: 120});
    const snapped = position(moved.x, moved.y, 'small', {x: -1920, y: 32, width: 1920, height: 1048});
    const photo = '/home/user/Pictures/本地 照片.png';
    const [saved] = parseWidgets(JSON.stringify([{id: 'photo', kind: 'photo', photo, ...snapped}]));
    assert.equal(saved.photo, photo); assert.equal(saved.x, -1480); assert.equal(saved.y, 184);
});
