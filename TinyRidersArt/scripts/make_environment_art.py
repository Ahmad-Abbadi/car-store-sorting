"""Flat family signage for physical panels in the art-only sorting room."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
FAMILIES=json.loads((KIT/"catalog"/"family_specs.json").read_text(encoding="utf-8"))
OUT=ROOT/"textures"/"environment"/"signs"; OUT.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",57)
small=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",21)
for fam in FAMILIES:
    image=Image.new("RGB",(512,128),fam["primary"]); d=ImageDraw.Draw(image)
    d.rounded_rectangle((8,8,504,120),radius=18,outline=fam["secondary"],width=8)
    d.polygon([(375,0),(425,0),(510,128),(460,128)],fill=fam["accent"])
    d.text((28,19),fam["name"].upper(),font=font,fill="white",stroke_width=2,stroke_fill=fam["accent"])
    d.text((31,89),"TINY RIDERS  /  COLLECTION",font=small,fill=fam["secondary"])
    image.save(OUT/(fam["code"]+".png"),optimize=True)
print("environment signs",len(FAMILIES))
