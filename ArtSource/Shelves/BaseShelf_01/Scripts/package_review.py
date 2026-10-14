from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[4]; SRC=ROOT/'ArtSource/Shelves/BaseShelf_01'
images=[Image.open('C:/Users/User/Downloads/Modular Shelf Unit Product Board.png').convert('RGB'),Image.open(SRC/'Renders/B_Hero_EmissionOn.png').convert('RGB'),Image.open(SRC/'Renders/D_Unity_Filled.png').convert('RGB')]
w,h=images[0].size; images[0]=images[0].crop((int(w*.015),int(h*.07),int(w*.545),int(h*.72)))
board=Image.new('RGB',(2160,910),'#edf0f5');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',25)
for i,(im,title) in enumerate(zip(images,['SUPPLIED CONCEPT — LARGE VIEW','EDITABLE BLENDER SHELF','ACTUAL UNITY / EXISTING CARS'])):
    d.text((i*720+24,22),title,font=font,fill='#182436')
    im.thumbnail((696,790));board.paste(im,(i*720+(720-im.width)//2,68+(790-im.height)//2))
d.text((24,867),'3 rows / 6 spaces / solid base / blank header / blank badge / uniform 3x scale',font=font,fill='#182436')
board.save(SRC/'Renders/Reference_Comparison.png')
before=json.loads((SRC/'source_car_hashes.json').read_text());changed=[]
for p,digest in before.items():
    path=ROOT/p
    if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest()!=digest:changed.append(p)
(SRC/'source_car_integrity.json').write_text(json.dumps({'files_checked':len(before),'changed_files':changed,'unchanged':not changed},indent=2))
shutil.copy2(ROOT/'Assets/TinyRiders/Editor/TRBaseShelfArtBuilder.cs',SRC/'Scripts/TRBaseShelfArtBuilder.cs')
# Remove only task-generated intermediate geometry dump and Blender backup copies.
for name in ['preview_car_geometry.json','TR_Shelf_Base_01.blend1','TR_Shelf_Base_01_Export.blend1']:
    p=SRC/name
    if p.exists():p.unlink()
print('Comparison saved; source integrity:',len(before),'files,',len(changed),'changes')
