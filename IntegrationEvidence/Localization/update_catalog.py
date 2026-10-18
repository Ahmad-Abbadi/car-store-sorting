"""Merge reviewed translations and stable catalog names into the CSV master."""
import csv,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
codes=['en','es','pt-BR','id','tr','pl','vi','ar']
path=ROOT/'Localization.csv'
with path.open(encoding='utf-8-sig',newline='') as f:
    rows={r['key']:r for r in csv.DictReader(f)}
recipes=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text(encoding='utf-8-sig'))['themes']
theme_ids={t['name']:t['id'] for t in recipes}
for line in (Path(__file__).parent/'translations.tsv').read_text(encoding='utf-8').splitlines():
    cells=line.split('|')
    assert len(cells)==9,(len(cells),line)
    key=cells[0]
    if key.startswith('theme.'): key='theme.'+theme_ids[cells[1]]+'.name'
    rows[key]={'key':key,**{c:v.replace('\\n','\n') for c,v in zip(codes,cells[1:])}}
entries=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json').read_text(encoding='utf-8-sig'))['entries']
for e in entries:
    theme=rows['theme.'+e['themeId']+'.name']
    key='item.'+e['collectibleId']+'.name'
    rows[key]={'key':key,**{c:theme[c]+' · '+e['baseCarId'] for c in codes}}
# Refusal keys are derived from the Inspector's English strings by the legacy interaction API.
for target,source in [('feedback.collectible_unavailable','feedback.unavailable'),('feedback.this_accessory_needs_more_display_clearance','feedback.clearance')]:
    rows[target]={**rows[source],'key':target}
key='feedback.choose_an_empty_slot'
values=['Choose an empty slot','Elige un espacio vacío','Escolha um espaço vazio','Pilih slot kosong','Boş bir yer seç','Wybierz wolne miejsce','Chọn ô trống','اختر مكانا فارغا']
rows[key]={'key':key,**dict(zip(codes,values))}
with path.open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=['key']+codes);w.writeheader();w.writerows(rows.values())
problems=[]
for key,r in rows.items():
    placeholders=sorted(re.findall(r'\{[^}]+\}',r['en']))
    for c in codes:
        if not r[c].strip(): problems.append(key+': empty '+c)
        if sorted(re.findall(r'\{[^}]+\}',r[c]))!=placeholders: problems.append(key+': placeholders '+c)
report={'keys':len(rows),'languages':codes,'themes':len(recipes),'collectibles':len(entries),'problems':problems}
(Path(__file__).parent/'translation-coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
assert not problems,problems
print(json.dumps(report))
