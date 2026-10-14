"""Make deterministic QC galleries from actual Blender renders."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))
BODIES=json.loads((KIT/"catalog"/"bodies_50.json").read_text(encoding="utf-8"))
OUT=ROOT/"previews"/"contact_sheets"; OUT.mkdir(parents=True,exist_ok=True)
FONT=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",19)
TITLE=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",32)

def sheet(rows,cols,thumb,mode,path,title):
    w,h=thumb; cellh=h+27
    out=Image.new("RGB",(cols*w,80+((len(rows)+cols-1)//cols)*cellh),"#e6edf6")
    d=ImageDraw.Draw(out); d.text((18,17),title,font=TITLE,fill="#122D54")
    for i,(label,src) in enumerate(rows):
        if not src.exists(): continue
        with Image.open(src) as im:
            if mode=="car":
                img=Image.new("RGBA",im.size,"#D8E2EF"); img.alpha_composite(im.convert("RGBA"))
                img=img.convert("RGB").resize((w,h),Image.Resampling.LANCZOS)
            else:
                img=im.convert("RGB").resize((w,h),Image.Resampling.LANCZOS)
        x=(i%cols)*w; y=80+(i//cols)*cellh
        out.paste(img,(x,y)); d.text((x+5,y+h+3),label,font=FONT,fill="#18365F")
    out.save(path,optimize=True)

base=[(b["bodyId"]+" "+b["name"],ROOT/"previews"/"builds"/(b["bodyId"]+"-S.png")) for b in BODIES]
sheet(base,10,(200,150),"car",OUT/"all_50_bases.png","TINY RIDERS  /  50 BASE VEHICLES")
for code in ("CC","TC","RR","DP","BW","RC","WY","SS","FF","CR"):
    family=[p for p in PRODUCTS if p["familyCode"]==code]
    cars=[(p["sku"].replace("TR-",""),ROOT/"previews"/"products"/(p["sku"]+".png")) for p in family]
    packages=[(p["sku"].replace("TR-",""),ROOT/"previews"/"packages"/(p["sku"]+".png")) for p in family]
    builds=[(f'{code}-{i:02d}-{v}',ROOT/"previews"/"builds"/(f'{code}-{i:02d}-{v}.png')) for i in range(1,6) for v in ("S","C")]
    sheet(cars,10,(160,100),"car",OUT/(code+"_50_cars.png"),code+"  /  50 COLLECTIBLES")
    sheet(packages,10,(120,180),"package",OUT/(code+"_50_packages.png"),code+"  /  50 PACKAGES")
    sheet(builds,5,(300,225),"car",OUT/(code+"_build_comparison.png"),code+"  /  STANDARD AND CUSTOM BUILDS")
allcars=[(p["displayNumber"],ROOT/"previews"/"products"/(p["sku"]+".png")) for p in PRODUCTS]
allpackages=[(p["displayNumber"],ROOT/"previews"/"packages"/(p["sku"]+".png")) for p in PRODUCTS]
sheet(allcars,25,(120,75),"car",OUT/"collection_500_cars.png","TINY RIDERS  /  ALL 500 CARS")
sheet(allpackages,25,(80,120),"package",OUT/"collection_500_packages.png","TINY RIDERS  /  ALL 500 PACKAGES")
print("contact sheets",len(list(OUT.glob("*.png"))))
