"""One locked 1200x1800 package art layout for all Tiny Riders SKUs."""
import argparse
import json
import os
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))
FAMS={f["code"]:f for f in json.loads((KIT/"catalog"/"family_specs.json").read_text(encoding="utf-8"))}
LOGO=KIT/"design"/"brand"/"tiny_riders_logo.png"
FONT="C:/Windows/Fonts/arialbd.ttf"
def font(sz): return ImageFont.truetype(FONT,sz)
def img_ref(path,from_dir): return os.path.relpath(path,from_dir).replace("\\","/")

def draw_common(im,p,front,source_dir):
    fam=FAMS[p["familyCode"]]; d=ImageDraw.Draw(im)
    bg=fam["primary"]; dark=fam["accent"]; accent=fam["secondary"]
    d.rectangle((0,0,1200,1800),fill=bg)
    # family scenery remains secondary to the locked zones
    for j in range(5):
        offset=j*150
        d.polygon([(0,515+offset),(1200,270+offset),(1200,315+offset),(0,560+offset)],fill=accent if j%2 else dark)
    d.rounded_rectangle((58,80,1142,1738),radius=32,outline="#F8FBFF",width=8)
    logo=Image.open(LOGO).convert("RGBA")
    logo.thumbnail((980,205),Image.Resampling.LANCZOS)
    im.paste(logo,(100+(980-logo.width)//2,135+(205-logo.height)//2),logo)
    d.rounded_rectangle((100,350,620,430),radius=10,fill=accent)
    d.text((127,362),fam["name"].upper(),font=font(50),fill=dark)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1800" viewBox="0 0 1200 1800">',
         f'<rect width="1200" height="1800" fill="{bg}"/>',
         f'<path d="M0 515 L1200 270 L1200 315 L0 560Z" fill="{dark}"/>',
         f'<rect x="58" y="80" width="1084" height="1658" rx="32" fill="none" stroke="#F8FBFF" stroke-width="8"/>',
         f'<image href="{img_ref(LOGO,source_dir)}" x="100" y="135" width="980" height="205" preserveAspectRatio="xMidYMid meet"/>',
         f'<rect x="100" y="350" width="520" height="80" rx="10" fill="{accent}"/>',
         f'<text x="127" y="413" font-family="Arial,sans-serif" font-weight="bold" font-size="50" fill="{dark}">{fam["name"].upper()}</text>']
    return d,svg,bg,dark,accent

def compose(p,force=False):
    sku=p["sku"]; hero=ROOT/"previews"/"products"/(sku+".png")
    if not hero.exists(): return "missing_render"
    front=ROOT/"textures"/"packages"/"front"/(sku+".png")
    back=ROOT/"textures"/"packages"/"back"/(sku+".png")
    fs=ROOT/"textures"/"source_artwork"/"packages"/"front"/(sku+".svg")
    bs=ROOT/"textures"/"source_artwork"/"packages"/"back"/(sku+".svg")
    if all(x.exists() for x in (front,back,fs,bs)) and not force: return "skipped"
    for path in (front,back,fs,bs): path.parent.mkdir(parents=True,exist_ok=True)
    car=Image.open(hero).convert("RGBA")
    car.thumbnail((980,495),Image.Resampling.LANCZOS)
    # Front: exact car render in the hero region and NO car illustration behind blister.
    im=Image.new("RGBA",(1200,1800)); d,svg,bg,dark,accent=draw_common(im,p,True,fs.parent)
    d.rounded_rectangle((90,460,1110,965),radius=26,fill=FAMS[p["familyCode"]]["background"])
    d.polygon([(90,887),(365,675),(575,850),(880,650),(1110,840),(1110,965),(90,965)],fill=accent)
    x=600-car.width//2; y=715-car.height//2
    im.alpha_composite(car,(x,y))
    svg += [f'<rect x="90" y="460" width="1020" height="505" rx="26" fill="{FAMS[p["familyCode"]]["background"]}"/>',
            f'<image href="{img_ref(hero,fs.parent)}" x="{x}" y="{y}" width="{car.width}" height="{car.height}"/>']
    d.rounded_rectangle((86,975,1114,1515),radius=28,fill=FAMS[p["familyCode"]]["background"])
    d.rectangle((86,975,1114,1003),fill=accent)
    svg += [f'<rect x="86" y="975" width="1028" height="540" rx="28" fill="{FAMS[p["familyCode"]]["background"]}"/>',
            f'<rect x="86" y="975" width="1028" height="28" fill="{accent}"/>']
    d.rounded_rectangle((90,1542,950,1700),radius=20,fill=dark)
    d.rounded_rectangle((955,1560,1138,1700),radius=16,fill=accent)
    model=p["modelName"].upper(); title=model if len(model)<16 else model[:16]
    d.text((120,1561),title,font=font(70),fill="white")
    d.text((120,1641),p["buildId"].upper()+"  /  "+p["liveryName"],font=font(33),fill=accent)
    d.text((976,1590),p["displayNumber"],font=font(37),fill=dark)
    d.text((975,1644),p["rarity"].upper(),font=font(25),fill=dark)
    svg += [f'<rect x="90" y="1542" width="860" height="158" rx="20" fill="{dark}"/>',
            f'<rect x="955" y="1560" width="183" height="140" rx="16" fill="{accent}"/>',
            f'<text x="120" y="1635" font-family="Arial,sans-serif" font-weight="bold" font-size="70" fill="white">{title}</text>',
            f'<text x="120" y="1680" font-family="Arial,sans-serif" font-weight="bold" font-size="33" fill="{accent}">{p["buildId"].upper()} / {p["liveryName"]}</text>',
            f'<text x="976" y="1635" font-family="Arial,sans-serif" font-weight="bold" font-size="37" fill="{dark}">{p["displayNumber"]}</text>',
            f'<text x="975" y="1677" font-family="Arial,sans-serif" font-weight="bold" font-size="25" fill="{dark}">{p["rarity"].upper()}</text>']
    im.convert("RGB").save(front,optimize=True)
    fs.write_text("\n".join(svg+["</svg>"]),encoding="utf-8")
    # Back uses the same logo/header anchors, plus exact product and family lineup renders.
    im=Image.new("RGBA",(1200,1800)); d,svg,bg,dark,accent=draw_common(im,p,False,bs.parent)
    d.rounded_rectangle((90,455,1110,910),radius=30,fill=FAMS[p["familyCode"]]["background"])
    small=car.copy(); small.thumbnail((800,390),Image.Resampling.LANCZOS)
    im.alpha_composite(small,(600-small.width//2,682-small.height//2))
    svg += [f'<rect x="90" y="455" width="1020" height="455" rx="30" fill="{FAMS[p["familyCode"]]["background"]}"/>',
            f'<image href="{img_ref(hero,bs.parent)}" x="{600-small.width//2}" y="{682-small.height//2}" width="{small.width}" height="{small.height}"/>']
    d.rounded_rectangle((90,945,1110,1290),radius=24,fill="white")
    lines=[p["modelName"].upper()+"  /  "+p["displayNumber"],p["familyName"].upper(),
           "EDITION: "+p["buildId"].upper()+"  /  "+p["liveryName"],
           "RARITY: "+p["rarity"].upper(),"SMALL CARS. BIG STORIES."]
    for j,line in enumerate(lines):
        y=975+j*60; d.text((130,y),line,font=font(41 if j==0 else 32),fill=dark)
        svg.append(f'<text x="130" y="{y+44}" font-family="Arial,sans-serif" font-weight="bold" font-size="{41 if j==0 else 32}" fill="{dark}">{line}</text>')
    d.text((100,1330),"MORE FROM "+p["familyName"].upper(),font=font(45),fill="white")
    svg.append(f'<text x="100" y="1375" font-family="Arial,sans-serif" font-weight="bold" font-size="45" fill="white">MORE FROM {p["familyName"].upper()}</text>')
    for j in range(5):
        bid=f'{p["familyCode"]}-{j+1:02d}'
        lookup=next(q for q in PRODUCTS if q["bodyId"]==bid and q["buildId"]=="standard" and q["liveryIndex"]==1)
        path=ROOT/"previews"/"products"/(lookup["sku"]+".png")
        if path.exists():
            thumb=Image.open(path).convert("RGBA"); thumb.thumbnail((180,130),Image.Resampling.LANCZOS)
            tx=100+j*205+(180-thumb.width)//2; ty=1425+(130-thumb.height)//2
            im.alpha_composite(thumb,(tx,ty))
            svg.append(f'<image href="{img_ref(path,bs.parent)}" x="{tx}" y="{ty}" width="{thumb.width}" height="{thumb.height}"/>')
        d.rounded_rectangle((100+j*205,1410,280+j*205,1560),radius=15,outline="white",width=4)
        d.text((114+j*205,1573),bid,font=font(25),fill="white")
    d.rounded_rectangle((90,1640,1110,1720),radius=16,fill=accent)
    d.text((115,1654),"TINY RIDERS  /  COLLECT ALL 500",font=font(42),fill=dark)
    svg.append(f'<rect x="90" y="1640" width="1020" height="80" rx="16" fill="{accent}"/>')
    svg.append(f'<text x="115" y="1700" font-family="Arial,sans-serif" font-weight="bold" font-size="42" fill="{dark}">TINY RIDERS / COLLECT ALL 500</text>')
    im.convert("RGB").save(back,optimize=True)
    bs.write_text("\n".join(svg+["</svg>"]),encoding="utf-8")
    return "created"

if __name__=="__main__":
    ap=argparse.ArgumentParser(); ap.add_argument("--body-id"); ap.add_argument("--body-ids"); ap.add_argument("--family"); ap.add_argument("--limit",type=int); ap.add_argument("--force",action="store_true")
    args=ap.parse_args()
    selected_ids=set(args.body_ids.split(",")) if args.body_ids else None
    selected=[p for p in PRODUCTS if (not args.body_id or p["bodyId"]==args.body_id) and (selected_ids is None or p["bodyId"] in selected_ids) and (not args.family or p["familyCode"]==args.family)]
    if args.limit: selected=selected[:args.limit]
    counts={}
    for p in selected:
        try: k=compose(p,args.force)
        except Exception as e: k="failed:"+repr(e)
        counts[k]=counts.get(k,0)+1
    print(counts)
