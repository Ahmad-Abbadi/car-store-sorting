"""Create reproducible 2D UV print artwork for each catalog SKU."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from palette import CORE_COLORS

ROOT=Path(__file__).resolve().parents[1]
KIT=ROOT.parent/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
PRODUCTS=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))
FAMILIES={f["code"]:f for f in json.loads((KIT/"catalog"/"family_specs.json").read_text(encoding="utf-8"))}
LIVERIES={
    "CORE":(None,"#F5F7FF","#142B55"),
    "SUNSET":("#FF7A28","#FFD45A","#713C2B"),
    "CLOUD":("#E5EFF9","#50A8E4","#214775"),
    "NIGHT":("#283044","#83D3E0","#EDEEF6"),
    "CHASE":("#F7C648","#FFF5D1","#744C17"),
}
FONT_PATHS=[Path("C:/Windows/Fonts/arialbd.ttf"),Path("C:/Windows/Fonts/Arial.ttf")]
def font(size):
    for p in FONT_PATHS:
        if p.exists(): return ImageFont.truetype(str(p),size)
    return ImageFont.load_default()

class Art:
    def __init__(self,w,h,bg):
        self.w=w; self.h=h; self.im=Image.new("RGB",(w,h),bg); self.draw=ImageDraw.Draw(self.im)
        self.svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="{bg}"/>']
    def rect(self,box,fill,r=0):
        self.draw.rounded_rectangle(box,radius=r,fill=fill)
        x0,y0,x1,y1=box
        self.svg.append(f'<rect x="{x0}" y="{y0}" width="{x1-x0}" height="{y1-y0}" rx="{r}" fill="{fill}"/>')
    def poly(self,points,fill):
        self.draw.polygon(points,fill=fill)
        pts=" ".join(f"{x},{y}" for x,y in points)
        self.svg.append(f'<polygon points="{pts}" fill="{fill}"/>')
    def circle(self,center,radius,fill):
        x,y=center
        self.draw.ellipse((x-radius,y-radius,x+radius,y+radius),fill=fill)
        self.svg.append(f'<circle cx="{x}" cy="{y}" r="{radius}" fill="{fill}"/>')
    def text(self,xy,s,size,fill):
        self.draw.text(xy,s,font=font(size),fill=fill)
        import html
        self.svg.append(f'<text x="{xy[0]}" y="{xy[1]+size}" font-family="Arial,sans-serif" font-weight="bold" font-size="{size}" fill="{fill}">{html.escape(s)}</text>')
    def save(self,png,svg):
        png.parent.mkdir(parents=True,exist_ok=True); svg.parent.mkdir(parents=True,exist_ok=True)
        self.im.save(png,optimize=True); svg.write_text("\n".join(self.svg+["</svg>"]),encoding="utf-8")

def make(p,force=False):
    sku=p["sku"]; png=ROOT/"textures"/"cars"/(sku+".png")
    svg=ROOT/"textures"/"source_artwork"/"cars"/(sku+".svg")
    if png.exists() and svg.exists() and not force: return "skipped"
    fam=FAMILIES[p["familyCode"]]
    base,bright,ink=LIVERIES[p["liveryName"]]
    base=base or CORE_COLORS[p["bodyId"]]
    a=Art(1024,1024,base)
    window="#1C2940"; white="#F7F8EF"; accent=bright
    # All printed areas correspond to UV islands in production_factory.py.
    a.rect((12,10,500,245),base); a.rect((524,10,1012,245),base)
    for x0 in (12,524):
        if p["liveryName"]=="CORE":
            a.poly([(x0+22,143),(x0+270,143),(x0+424,63),(x0+464,63),(x0+296,181),(x0+22,181)],accent)
        elif p["liveryName"]=="SUNSET":
            a.poly([(x0+25,182),(x0+180,40),(x0+213,40),(x0+61,188)],accent)
            a.poly([(x0+75,192),(x0+235,40),(x0+251,40),(x0+106,192)],ink)
            a.rect((x0+22,199,x0+469,223),accent)
        elif p["liveryName"]=="CLOUD":
            a.rect((x0+24,105,x0+466,135),accent,10)
            a.rect((x0+24,147,x0+466,159),ink,5)
            a.rect((x0+24,169,x0+466,180),accent,5)
        elif p["liveryName"]=="NIGHT":
            a.poly([(x0+25,91),(x0+451,91),(x0+451,114),(x0+25,154)],accent)
            for j in range(10):
                if j%2==0: a.rect((x0+24+j*44,177,x0+68+j*44,219),accent)
                else: a.rect((x0+24+j*44,177,x0+68+j*44,219),ink)
        else:
            a.poly([(x0+23,172),(x0+202,68),(x0+248,96),(x0+435,44),(x0+327,153),(x0+283,133),(x0+123,212)],accent)
            a.poly([(x0+34,207),(x0+224,105),(x0+250,121),(x0+124,229)],ink)
        if p["familyCode"] in ("TC","FF","RR","DP","BW"):
            race="23" if p["bodyId"]=="TC-01" else f'{int(p["bodyId"][-2:]):02d}'
            a.rect((x0+272,104,x0+415,200),white,10)
            a.text((x0+290,112),race,62,ink)
        elif p["familyCode"]=="CR":
            # The face is printed on both shell sides, mirrored toward the nose.
            front=x0+(137 if x0==12 else 351)
            a.circle((front,121),27,white)
            a.circle((front+(7 if x0==12 else -7),120),14,ink)
            a.circle((front+(9 if x0==12 else -9),115),5,white)
            if p["bodyId"] in ("CR-01","CR-03","CR-05"):
                left=front-66
                a.poly([(left,160),(left+134,160),(left+115,188),(left+95,172),
                        (left+75,194),(left+56,173),(left+38,190),(left+18,170)],white)
                a.poly([(left+9,159),(left+126,159),(left+115,169),(left+9,169)],ink)
            else:
                a.poly([(front-57,164),(front-20,181),(front+22,181),(front+58,164),
                        (front+34,185),(front-31,185)],ink)
            label_x=x0+(252 if x0==12 else 25)
            a.rect((label_x,66,label_x+207,105),bright,7)
            a.text((label_x+12,68),p["modelName"].upper(),29,ink)
        elif p["familyCode"]=="RC":
            a.rect((x0+78,96,x0+204,178),white,13)
            if p["bodyId"]=="RC-02":
                a.rect((x0+132,104,x0+151,170),"#E43B3C",3)
                a.rect((x0+109,126,x0+173,145),"#E43B3C",3)
            elif p["bodyId"]=="RC-05":
                a.circle((x0+141,128),28,"#2368B7")
                a.rect((x0+137,92,x0+145,163),white,4)
                a.poly([(x0+115,144),(x0+141,169),(x0+167,144)],white)
            else:
                a.text((x0+93,108),p["modelName"].upper()[:5],23,ink)
        else:
            a.rect((x0+75,97,x0+208,167),bright,8)
            label=p["modelName"].upper()[:6]
            a.text((x0+84,111),label,24 if len(label)>5 else 29,ink)
        if p["buildId"]=="custom":
            a.rect((x0+30,31,x0+93,66),bright,6)
            a.text((x0+47,33),"C",29,ink)
        a.rect((x0+304,19,x0+465,55),fam["secondary"],5)
        a.text((x0+326,20),p["familyCode"]+" "+p["bodyId"][-2:],26,ink)
    a.rect((12,264,500,490),base)
    if p["liveryName"]=="CORE":
        a.poly([(40,264),(141,264),(365,490),(282,490)],bright)
        a.poly([(176,264),(202,264),(443,490),(411,490)],ink)
    elif p["liveryName"]=="SUNSET":
        for j in range(3): a.poly([(35+j*115,264),(77+j*115,264),(285+j*80,490),(244+j*80,490)],bright if j!=1 else ink)
    elif p["liveryName"]=="CLOUD":
        a.rect((25,334,488,381),bright,16)
        a.rect((25,397,488,414),ink,8)
    elif p["liveryName"]=="NIGHT":
        for j in range(8):
            for k in range(3):
                if (j+k)%2==0: a.rect((20+j*59,300+k*58,79+j*59,358+k*58),bright)
    else:
        a.poly([(35,430),(205,264),(299,264),(201,360),(468,360),(276,490),(212,410)],bright)
        a.poly([(49,455),(202,317),(223,335),(87,468)],ink)
    if p["familyCode"]=="CR":
        for y in (327,425):
            a.circle((111,y),30,white)
            a.circle((116,y),16,ink)
            a.circle((120,y-6),6,white)
    a.rect((524,264,760,490),base); a.rect((776,264,1012,490),base)
    for x0 in (524,776):
        if p["familyCode"]!="CR" or x0==776:
            a.rect((x0+25,345,x0+94,415),white,6); a.rect((x0+142,345,x0+211,415),white,6)
        a.rect((x0+17,435,x0+219,458),ink,5)
    if p["familyCode"]=="CR":
        a.rect((540,359,744,423),ink,14)
        for j in range(6):
            x=547+j*32
            a.poly([(x,366),(x+22,366),(x+12,396)],white)
            a.poly([(x+3,416),(x+27,416),(x+15,390)],white)
    # Opaque windows printed directly onto the cabin facets.
    for x0 in (12,524):
        a.rect((x0,510,x0+488,740),base)
        if p["bodyId"] in ("CC-03","RC-02","SS-04"):
            a.poly([(x0+38,562),(x0+185,551),(x0+185,686),(x0+38,687)],window)
            a.rect((x0+230,565,x0+405,650),bright,9)
            a.text((x0+250,576),p["familyCode"],44,ink)
        elif p["bodyId"] in ("CC-05","DP-04","RR-02"):
            for j in range(4): a.rect((x0+30+j*108,561,x0+112+j*108,685),window,8)
        else:
            a.poly([(x0+38,562),(x0+235,548),(x0+202,686),(x0+38,687)],window)
            a.poly([(x0+253,548),(x0+447,561),(x0+447,687),(x0+218,686)],window)
        a.poly([(x0+63,575),(x0+129,570),(x0+83,672)],"#2B3B59")
    for x0 in (12,255):
        a.rect((x0,760,x0+228,1008),base)
        a.poly([(x0+19,800),(x0+209,800),(x0+194,958),(x0+34,958)],window)
    a.rect((500,760,1012,1008),base)
    a.poly([(610,760),(684,760),(856,1008),(786,1008)],bright)
    a.save(png,svg)
    return "created"

if __name__=="__main__":
    ap=argparse.ArgumentParser(); ap.add_argument("--body-id"); ap.add_argument("--family"); ap.add_argument("--limit",type=int); ap.add_argument("--force",action="store_true")
    args=ap.parse_args()
    selected=[p for p in PRODUCTS if (not args.body_id or p["bodyId"]==args.body_id) and (not args.family or p["familyCode"]==args.family)]
    if args.limit: selected=selected[:args.limit]
    done=0
    for p in selected:
        if make(p,args.force)=="created": done+=1
    print(f"2D car artwork created: {done}; selected: {len(selected)}")
