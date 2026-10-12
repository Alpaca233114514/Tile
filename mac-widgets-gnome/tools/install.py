#!/usr/bin/env python3
"""Graphical first-install helper; does not replace an existing extension."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
try:
    import gi
    gi.require_version('Gtk', '4.0')
    from gi.repository import Gtk
except (ImportError, ValueError):
    raise SystemExit('需要 Debian python3-gi 和 gir1.2-gtk-4.0，或使用 README 中的命令行安装。')

class Installer(Gtk.Application):
    def do_activate(self):
        window = Gtk.ApplicationWindow(application=self, title='安装 Tile 小组件', default_width=440)
        box = Gtk.Box(orientation=Gtk.Orientation.VERTICAL, spacing=18, margin_top=28, margin_bottom=28, margin_start=28, margin_end=28)
        label = Gtk.Label(label='Tile · 原生桌面小组件\n安装到当前用户，无需管理员权限。', wrap=True)
        box.append(label)
        button = Gtk.Button(label='安装 demo')
        box.append(button)
        def install(_button):
            button.set_sensitive(False)
            try:
                subprocess.run([sys.executable, str(ROOT / 'tools/package.py')], check=True, capture_output=True, text=True)
                subprocess.run(['gnome-extensions', 'install', str(ROOT / 'dist/tile-widgets@tile.local.shell-extension.zip')], check=True, capture_output=True, text=True)
                label.set_label('安装完成。\n请注销再登录，在“扩展”应用中启用 Tile。')
            except (OSError, subprocess.CalledProcessError) as error:
                label.set_label('安装未完成，已有版本不会被覆盖。\n' + (getattr(error, 'stderr', None) or str(error)))
                button.set_sensitive(True)
        button.connect('clicked', install)
        window.set_child(box)
        window.present()

Installer(application_id='local.tile.Installer').run()
