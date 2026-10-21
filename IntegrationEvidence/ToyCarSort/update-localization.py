import csv
import io
from pathlib import Path

path = Path('Localization.csv')
raw = path.read_bytes()
text = raw.decode('utf-8-sig')
lines = text.splitlines(keepends=True)
reader = csv.reader(io.StringIO(text, newline=''))
patches = []
previous = 0
for row in reader:
    start, end = previous, reader.line_num
    previous = end
    if row and row[0] in ('menu.title', 'sign.series'):
        value = 'Toy Car Sort' if row[0] == 'menu.title' else 'TOY CAR SORT  /  MK.02'
        out = io.StringIO(newline='')
        csv.writer(out, lineterminator='\r\n' if '\r\n' in text else '\n').writerow([row[0]] + [value] * (len(row)-1))
        patches.append((start, end, out.getvalue()))
assert len(patches) == 2
for start, end, replacement in reversed(patches):
    lines[start:end] = [replacement]
path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'') + ''.join(lines).encode('utf-8'))
print('Updated only menu.title and sign.series across all languages.')
