"""Original painted shelf atlases; no downloaded imagery or supplier assets are modified."""
from pathlib import Path
import json, math, shutil
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'ArtSource/Shelves/ThemedShelves/Textures'
DEST = ROOT / 'Assets/TinyRiders/Art/Shelves/Themed/Textures'
RECIPES = ROOT / 'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json'
PATTERNS = {
 'PIRATE':'wood','ROYAL':'diamond','SPACE':'orbit','ALIEN':'orbit','BEACH':'waves','JUNGLE':'leaves',
 'SPOOKY':'stars','CANDY':'stripes','FOOD':'checker','FIRE':'cracks','ICE':'crystal','OCEAN':'waves',
 'CONSTRUCTION':'hazard','RESCUE':'cross','WIZARD':'stars','ROBOT':'circuit','MUSIC':'rhythm','SPORT':'court',
 'FARM':'wood','PARTY':'confetti','WILD_WEST':'wood','DETECTIVE':'plaid','NINJA':'chevron',
 'VIKING_LEGENDS':'knot','ANCIENT_EGYPT':'egypt','STEAMPUNK':'gears','PREHISTORIC':'scales',
 'HONEY_BEE':'honeycomb','BUTTERFLY_GARDEN':'petals','MUSHROOM_GROVE':'spots','THUNDERSTORM':'bolts',
 'WINTER_HOLIDAYS':'ribbon','RAILWAY':'tracks','AVIATION':'wings','CINEMA':'film','PHOTOGRAPHY':'lens',
 'ART_STUDIO':'brush','POST_OFFICE':'airmail','BARBERSHOP':'barber','SCIENCE_LAB':'molecules'}

def rgb(s): return tuple(int(s.lstrip('#')[i:i+2],16) for i in (0,2,4))
def mix(a,b,t): return tuple(round(x*(1-t)+y*t) for x,y in zip(a,b))
def polygon(d,cx,cy,r,n,color,angle=0):
    d.polygon([(cx+math.cos(angle+2*math.pi*i/n)*r,cy+math.sin(angle+2*math.pi*i/n)*r) for i in range(n)], fill=color)
def star(d,x,y,r,color):
    d.polygon([(x+math.cos(-math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .43),y+math.sin(-math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .43)) for i in range(10)],fill=color)

def pattern(kind, size, paint, secondary, trim, ink):
    im=Image.new('RGB',size,paint);d=ImageDraw.Draw(im);w,h=size
    muted=mix(paint,secondary,.52)
    for y in range(0,h,112):
      for x in range(0,w,112):
        cx=x+56;cy=y+56
        if kind in ('wood','tracks'):
            d.line((x,y+15,x+112,y+15),fill=secondary,width=4)
            d.arc((x-30,y+27,x+85,y+62),0,160,fill=muted,width=3)
            d.ellipse((x+42,y+54,x+62,y+64),outline=trim,width=2)
            if kind=='tracks':
                for xx in (x+24,x+87):d.line((xx,y,xx,y+112),fill=trim,width=6)
                for yy in range(y,y+112,23):d.line((x+14,yy,x+97,yy),fill=secondary,width=5)
        elif kind in ('stripes','hazard','barber','airmail'):
            colors=[secondary,trim,paint] if kind!='barber' else [rgb('#CE414B'),rgb('#F4EEE2'),rgb('#2B5795'),rgb('#F4EEE2')]
            for k in range(-2,4):
                xx=x+k*36;d.polygon([(xx,y),(xx+18,y),(xx+112+18,y+112),(xx+112,y+112)],fill=colors[(k+(y//112))%len(colors)])
        elif kind in ('waves','rhythm'):
            for k in range(3):
                points=[(x+i,y+20+k*30+math.sin(i/112*math.pi*2)*12) for i in range(113)]
                d.line(points,fill=[secondary,trim,muted][k],width=7)
        elif kind in ('honeycomb','scales','crystal','diamond'):
            if kind=='honeycomb':
                pts=[(cx+math.cos(i*math.pi/3)*44,cy+math.sin(i*math.pi/3)*44) for i in range(6)];d.line(pts+[pts[0]],fill=trim,width=7)
            elif kind=='scales':d.arc((x,y,x+110,y+126),180,360,fill=secondary,width=8)
            else: polygon(d,cx,cy,40,4,secondary);polygon(d,cx,cy,26,4,trim)
        elif kind in ('orbit','lens','gears'):
            d.ellipse((cx-38,cy-38,cx+38,cy+38),outline=secondary,width=7)
            d.ellipse((cx-20,cy-20,cx+20,cy+20),outline=trim,width=4)
            if kind=='orbit':d.ellipse((cx+22,cy-38,cx+39,cy-21),fill=trim)
            if kind=='gears':
                for i in range(8):
                    a=i*math.pi/4;px=cx+math.cos(a)*39;py=cy+math.sin(a)*39;d.rectangle((px-5,py-5,px+5,py+5),fill=trim)
        elif kind in ('leaves','petals'):
            for a in (0,math.pi/2,math.pi,math.pi*1.5):
                px=cx+math.cos(a)*21;py=cy+math.sin(a)*21;d.ellipse((px-17,py-21,px+17,py+21),fill=secondary)
            d.ellipse((cx-10,cy-10,cx+10,cy+10),fill=trim)
        elif kind in ('stars','confetti'):
            star(d,cx,cy,27,trim)
            d.ellipse((x+12,y+17,x+22,y+27),fill=secondary)
            if kind=='confetti':d.line((x+80,y+12,x+93,y+25),fill=secondary,width=9)
        elif kind=='checker':
            d.rectangle((x,y,x+54,y+54),fill=secondary);d.rectangle((x+56,y+56,x+111,y+111),fill=secondary)
        elif kind=='plaid':
            d.rectangle((x+23,y,x+47,y+112),fill=secondary);d.rectangle((x,y+33,x+112,y+53),fill=secondary)
            d.line((x+76,y,x+76,y+112),fill=trim,width=3);d.line((x,y+81,x+112,y+81),fill=trim,width=3)
        elif kind in ('chevron','wings','knot','egypt'):
            d.line((x+10,y+25,cx,y+63,x+102,y+25),fill=secondary,width=9)
            d.line((x+10,y+48,cx,y+86,x+102,y+48),fill=trim,width=6)
            if kind=='egypt':
                d.polygon([(cx,y+4),(cx-18,y+24),(cx+18,y+24)],fill=trim)
            if kind=='knot':d.rectangle((x+20,y+13,x+90,y+96),outline=muted,width=4)
        elif kind in ('cracks','bolts'):
            d.polygon([(cx+15,y+8),(cx-24,y+58),(cx-1,y+58),(cx-13,y+105),(cx+34,y+43),(cx+10,y+43)],fill=trim)
        elif kind=='cross':
            d.rectangle((cx-9,cy-30,cx+9,cy+30),fill=secondary);d.rectangle((cx-30,cy-9,cx+30,cy+9),fill=secondary)
        elif kind in ('circuit','molecules'):
            points=[(x+18,y+20),(cx,y+20),(cx,y+80),(x+94,y+80)]
            d.line(points,fill=secondary,width=5)
            for px,py in points:d.ellipse((px-9,py-9,px+9,py+9),fill=trim)
        elif kind=='court':
            d.rectangle((x+12,y+14,x+100,y+100),outline=secondary,width=5);d.ellipse((cx-20,cy-20,cx+20,cy+20),outline=trim,width=4)
        elif kind=='spots':
            d.ellipse((cx-25,cy-25,cx+25,cy+25),fill=secondary);d.ellipse((x+10,y+11,x+22,y+23),fill=trim)
        elif kind=='ribbon':
            d.rectangle((cx-10,y,cx+10,y+112),fill=trim);d.rectangle((x,cy-9,x+112,cy+9),fill=secondary)
        elif kind=='film':
            d.rectangle((x+8,y,x+104,y+112),fill=secondary);d.rectangle((x+27,y+16,x+86,y+98),fill=paint)
            for yy in range(y+8,y+112,23):
                d.rectangle((x+12,yy,x+21,yy+12),fill=paint);d.rectangle((x+92,yy,x+100,yy+12),fill=paint)
        elif kind=='brush':
            d.line((x+18,y+80,x+83,y+22),fill=secondary,width=20);d.line((x+42,y+94,x+99,y+48),fill=trim,width=14)
    return im

def build(t):
    paint,secondary,trim,ink=[rgb(t[k]) for k in ('paint','secondary','trim','ink')]
    key=t['id'][3:];kind=PATTERNS[key]
    atlas=Image.new('RGB',(1024,1024),paint);d=ImageDraw.Draw(atlas)
    # Artwork regions: side print, lower-front panel, header, narrow trim, then flat-color swatches.
    atlas.paste(pattern(kind,(512,768),paint,secondary,trim,ink),(0,0))
    panel=pattern(kind,(512,384),mix(paint,ink,.25),secondary,trim,ink)
    # Calm center and bold perimeter keep large motifs readable without a wall of tiny detail.
    pd=ImageDraw.Draw(panel);pd.rounded_rectangle((55,54,457,330),radius=35,fill=paint,outline=trim,width=10)
    pd.line((90,192,422,192),fill=secondary,width=12)
    atlas.paste(panel,(512,0))
    d.rounded_rectangle((512,384,1023,639),radius=24,fill=rgb('#F4F0E4'),outline=trim,width=16)
    d.rectangle((528,610,1007,622),fill=secondary)
    atlas.paste(pattern(kind,(512,128),paint,secondary,trim,ink),(512,640))
    swatches=[paint,secondary,trim,ink,rgb('#F4F0E4'),rgb('#263447'),rgb(t['light']),mix(paint,ink,.4)]
    for i,c in enumerate(swatches):d.rectangle((i*128,768,i*128+127,1023),fill=c)
    atlas.save(OUT/(t['id']+'_BaseColor.png'))
    # Metallic/smoothness follow the car recipes; raised badges and borders get the trim response.
    metal=round(t.get('metallic',.1)*255);smooth=round(t.get('smoothness',.35)*255)
    mask=Image.new('RGBA',(1024,1024),(metal,0,0,smooth));md=ImageDraw.Draw(mask)
    md.rectangle((256,768,383,1023),fill=(round(t.get('trimMetallic',.4)*255),0,0,round(t.get('trimSmoothness',.4)*255)))
    mask.save(OUT/(t['id']+'_Surface.png'))
    for suffix in ('_BaseColor.png','_Surface.png'):shutil.copy2(OUT/(t['id']+suffix),DEST/(t['id']+suffix))

if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);DEST.mkdir(parents=True,exist_ok=True)
    for theme in json.loads(RECIPES.read_text(encoding='utf-8-sig'))['themes']:build(theme)
    print('Created 40 original 1024px painted atlases and surface maps.')
