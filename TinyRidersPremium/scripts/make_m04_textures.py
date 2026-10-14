"""Locally authored orange/ivory Wedge graphics; geometry carries no printed details."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[1]/'Textures/Candidates'
ORANGE='#f87816';WHITE='#fff4df';INK='#152331'
im=Image.new('RGB',(2048,2048),ORANGE);d=ImageDraw.Draw(im)
# Hood stripe and embedded upper-surface lamp art, with no decal-plane geometry.
d.rectangle((0,317,1024,451),fill=WHITE)
for y in [105,568]:
    d.rounded_rectangle((52,y,142,y+95),radius=9,fill=INK)
    d.rounded_rectangle((62,y+10,132,y+85),radius=5,fill=WHITE)
for offset in [0,1024]:
    d.polygon([(offset+260,1240),(offset+790,1040),(offset+820,1080),(offset+330,1255)],fill=WHITE)
    d.rounded_rectangle((offset+615,973,offset+735,1002),radius=5,fill=INK)
    for j in range(3):d.rectangle((offset+710+j*29,1080,offset+725+j*29,1150),fill=INK)
# Low front intake, three separate graphic cooling apertures.
for x0,x1 in [(92,280),(352,672),(744,932)]:
    d.rounded_rectangle((x0,1620,x1,1880),radius=14,fill=INK)
    d.line((x0+14,1765,x1-14,1765),fill='#3c4a52',width=11)
for x in [1100,1780]:
    d.rounded_rectangle((x,1460,x+170,1580),radius=16,fill='#842523')
    d.rounded_rectangle((x+10,1470,x+160,1540),radius=9,fill='#ff6f48')
d.rounded_rectangle((1394,1600,1680,1745),radius=10,fill=INK)
d.text((1537,1673),'WEDGE',font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',48),fill=WHITE,anchor='mm')
im.save(OUT/'M04_body.png')
im=Image.new('RGB',(2048,1024),ORANGE);d=ImageDraw.Draw(im)
for i in range(4):
    x=i*512
    if i in (1,2):
        pts=[(25,475),(216,35),(401,35),(490,475)]
        if i==2:pts=[(512-a,b) for a,b in pts]
        pts=[(x+a,b) for a,b in pts];d.polygon(pts,fill='#203242');d.line(pts+[pts[0]],fill=INK,width=12,joint='curve')
    else:
        d.rounded_rectangle((x+25,30,x+487,480),radius=16,fill=INK)
        d.rounded_rectangle((x+38,43,x+474,467),radius=10,fill='#203242')
# Roof UV is x longitudinal and y transverse, with the same central stripe width.
d.rectangle((0,691,1024,845),fill=WHITE)
im.save(OUT/'M04_cabin.png')
print('M04 local artwork saved')
