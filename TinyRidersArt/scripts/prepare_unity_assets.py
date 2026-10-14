"""Stage generated art in the Unity project without touching gameplay content."""
import json
import shutil
from pathlib import Path
from PIL import Image
from palette import CORE_COLORS, ACCESSORY_COLORS

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parent
KIT=PROJECT/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
DEST=PROJECT/"Assets"/"TinyRidersArt"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))

def copy(src,dst):
    if not src.exists() or src.stat().st_size==0: raise FileNotFoundError(src)
    dst.parent.mkdir(parents=True,exist_ok=True)
    if not dst.exists() or src.stat().st_mtime_ns>dst.stat().st_mtime_ns: shutil.copy2(src,dst)

def resize(src,dst,size):
    if not src.exists(): raise FileNotFoundError(src)
    dst.parent.mkdir(parents=True,exist_ok=True)
    if dst.exists() and dst.stat().st_mtime_ns>=src.stat().st_mtime_ns: return
    with Image.open(src) as im:
        im.convert("RGB").resize(size,Image.Resampling.LANCZOS).save(dst,optimize=True)

def run():
    for path in sorted((ROOT/"exports"/"builds").glob("*.fbx")):
        copy(path,DEST/"Builds"/path.name)
    for path in sorted((ROOT/"exports"/"products").glob("*.fbx")):
        copy(path,DEST/"Cars"/"Products"/path.name)
    copy(ROOT/"exports"/"packages"/"PackagingMaster.fbx",DEST/"Packaging"/"PackagingMaster.fbx")
    for path in sorted((ROOT/"exports"/"packages").glob("TR-*.fbx")):
        copy(path,DEST/"Packages"/"Products"/path.name)
    for path in sorted((ROOT/"exports"/"environment").glob("*.fbx")):
        copy(path,DEST/"Environment"/"Models"/path.name)
    for path in sorted((ROOT/"textures"/"environment"/"signs").glob("*.png")):
        copy(path,DEST/"Environment"/"Textures"/"Signs"/path.name)
    for p in PRODUCTS:
        sku=p["sku"]
        copy(ROOT/"textures"/"cars"/(sku+".png"),DEST/"Textures"/"Cars"/(sku+".png"))
        resize(ROOT/"textures"/"packages"/"front"/(sku+".png"),DEST/"Textures"/"Packages"/"Front"/(sku+".png"),(600,900))
        resize(ROOT/"textures"/"packages"/"back"/(sku+".png"),DEST/"Textures"/"Packages"/"Back"/(sku+".png"),(600,900))
    copy(KIT/"catalog"/"collectibles_500.json",DEST/"Catalog"/"collectibles_500.json")
    copy(KIT/"catalog"/"family_specs.json",DEST/"Catalog"/"family_specs.json")
    (DEST/"Catalog").mkdir(parents=True,exist_ok=True)
    (DEST/"Catalog"/"core_palette.json").write_text(json.dumps([
        {"bodyId":bid,"core":core,"accessory":ACCESSORY_COLORS[bid]}
        for bid,core in CORE_COLORS.items()],indent=2),encoding="utf-8")
    print("Unity art staged:",len(list((DEST/"Builds").glob("*.fbx"))),"builds,",
          len(list((DEST/"Textures"/"Cars").glob("*.png"))),"car textures,",
          len(list((DEST/"Textures"/"Packages"/"Front").glob("*.png"))),"front cards,",
          len(list((DEST/"Textures"/"Packages"/"Back").glob("*.png"))),"back cards")

if __name__=="__main__": run()
