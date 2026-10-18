import csv,io,subprocess
from pathlib import Path
p=Path('Localization.csv')
rows=list(csv.reader(io.StringIO(p.read_text(encoding='utf-8-sig'))))
base=list(csv.reader(io.StringIO(subprocess.check_output(['git','show','HEAD:Localization.csv']).decode('utf-8-sig'))))
original={r[0]:r for r in base if r}
restored=0
for r in rows:
 old=original.get(r[0])
 if not old: continue
 for i in range(1,min(len(r),len(old))):
  if '\n' in old[i] and old[i].replace('\n','').replace('\r','')==r[i]:r[i]=old[i];restored+=1
output=io.StringIO(newline='')
csv.writer(output,lineterminator='\n').writerows(rows)
tmp=Path('Localization.scrapbook.tmp.csv')
tmp.write_text(output.getvalue(),encoding='utf-8-sig',newline='')
tmp.replace(p)
print('Restored original line breaks in',restored,'fields')
