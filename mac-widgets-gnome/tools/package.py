#!/usr/bin/env python3
"""Build an installable GNOME extension with system GLib; no pip dependencies."""
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
compiler = shutil.which('glib-compile-schemas')
if not compiler:
    # Debian may ship the binary in the GLib runtime package without a /usr/bin link.
    candidates = list(Path('/usr/lib').glob('*/glib-2.0/glib-compile-schemas'))
    compiler = str(candidates[0]) if candidates else None
if not compiler:
    raise SystemExit('需要 glib-compile-schemas。Debian: sudo apt install libglib2.0-bin，然后重新运行。')
output = ROOT / 'dist' / 'tile-widgets@tile.local.shell-extension.zip'
output.parent.mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix='tile-pack-') as directory:
    staging = Path(directory)
    for name in ['metadata.json', 'extension.js', 'prefs.js', 'core.js', 'network.js', 'photoPicker.js', 'stylesheet.css', 'LICENSE']:
        shutil.copy2(ROOT / name, staging / name)
    for name in ['schemas', 'assets']:
        shutil.copytree(ROOT / name, staging / name)
    subprocess.run([compiler, '--strict', str(staging / 'schemas')], check=True)
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
        for file in staging.rglob('*'):
            if file.is_file():
                archive.write(file, file.relative_to(staging))
print(output)
