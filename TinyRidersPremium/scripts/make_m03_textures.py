"""Original local artwork for the M03 modeling candidate; no downloaded art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Textures'/'Candidates'; OUT.mkdir(parents=True,exist_ok=True)
BLUE='#0862ed'; NAVY='#092755'; WHITE='#f6f8f1'; ORANGE='#ff8a16'
FONT=Path('C:/Windows/Fonts/arialbd.ttf')

def number(im,xy,size=180,angle=0):
    layer=Image.new('RGBA',(size*3,size*2),(0,0,0,0))
    d=ImageDraw.Draw(layer); f=ImageFont.truetype(str(FONT),size)
    d.text((size*1.5,size), '23',font=f,fill=NAVY,anchor='mm',stroke_width=1)
    if angle: layer=layer.rotate(angle,expand=True,resample=Image.Resampling.BICUBIC)
    im.paste(layer,(int(xy[0]-layer.width/2),int(xy[1]-layer.height/2)),layer)

body=Image.new('RGB',(2048,2048),BLUE); d=ImageDraw.Draw(body)
# Atlas top is longitudinal left-to-right, with a centered broad white stripe.
d.rectangle((0,293,1024,475),fill=WHITE)
d.rounded_rectangle((125,210,365,560),radius=28,fill=WHITE)
number(body,(240,385),140,-90)
for x0 in (0,1024):
    d=ImageDraw.Draw(body)
    d.rounded_rectangle((x0+365,925,x0+640,1190),radius=15,fill=WHITE)
    number(body,(x0+501,1065),150)
    d.rectangle((x0+385,1204,x0+635,1242),fill=ORANGE)
# Front and rear graphics are printed directly on their shaped body faces.
d=ImageDraw.Draw(body)
for x in (140,728):
    d.rounded_rectangle((x,1540,x+156,1745),radius=23,fill='#06152a')
    d.rounded_rectangle((x+15,1555,x+141,1730),radius=18,fill='#fff6cf')
d.rounded_rectangle((325,1715,699,1928),radius=16,fill='#101c2a')
d.line((338,1770,686,1770),fill='#334251',width=8)
d.line((338,1845,686,1845),fill='#334251',width=8)
for x in (1110,1770):
    d.rounded_rectangle((x,1515,x+160,1760),radius=18,fill='#2e1630')
    d.rounded_rectangle((x+12,1535,x+148,1670),radius=12,fill='#f14a39')
    d.rectangle((x+12,1690,x+148,1725),fill='#ffbd62')
d.rounded_rectangle((1360,1740,1710,1870),radius=12,fill='#092755')
body.save(OUT/'M03_body.png')

cab=Image.new('RGB',(2048,1024),BLUE); d=ImageDraw.Draw(cab)
for i in range(4):
    x=i*512
    if i in (1,2):
        points=[(34,470),(164,43),(370,43),(475,470)]
        if i==2: points=[(512-a,b) for a,b in points]
        points=[(x+a,b) for a,b in points]
        d.polygon(points,fill='#263345'); d.line(points+[points[0]],fill='#081324',width=12,joint='curve')
    else:
        d.rounded_rectangle((x+39,38,x+473,458),radius=20,fill='#081324')
        d.rounded_rectangle((x+52,52,x+460,445),radius=14,fill='#263345')
d.rectangle((0,682,1024,854),fill=WHITE)
cab.save(OUT/'M03_cabin.png')
# Full editable vector companion. Python above remains the pixel-exact raster source.
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="2048" height="2048" viewBox="0 0 2048 2048">']
def rect(box,color,r=0):
    x,y,x1,y1=box
    svg.append(f'<rect x="{x}" y="{y}" width="{x1-x}" height="{y1-y}" rx="{r}" fill="{color}"/>')
def num(x,y,size,angle=0):
    svg.append(f'<text x="{x}" y="{y}" font-family="Arial" font-weight="bold" font-size="{size}" fill="{NAVY}" text-anchor="middle" dominant-baseline="central" transform="rotate({angle} {x} {y})">23</text>')
rect((0,0,2048,2048),BLUE);rect((0,293,1024,475),WHITE)
rect((125,210,365,560),WHITE,28);num(240,385,140,90)
for x in [0,1024]:
    rect((x+365,925,x+640,1190),WHITE,15);num(x+501,1065,150)
    rect((x+385,1204,x+635,1242),ORANGE)
for x in [140,728]:
    rect((x,1540,x+156,1745),'#06152a',23);rect((x+15,1555,x+141,1730),'#fff6cf',18)
rect((325,1715,699,1928),'#101c2a',16)
for y in [1770,1845]:rect((338,y-4,686,y+4),'#334251')
for x in [1110,1770]:
    rect((x,1515,x+160,1760),'#2e1630',18);rect((x+12,1535,x+148,1670),'#f14a39',12)
    rect((x+12,1690,x+148,1725),'#ffbd62')
rect((1360,1740,1710,1870),NAVY,12)
svg.append('</svg>');(OUT/'M03_body.svg').write_text('\n'.join(svg),encoding='utf8')
print('M03 candidate body/cabin PNG artwork written')
