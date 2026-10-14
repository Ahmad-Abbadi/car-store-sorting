"""Editable, deterministic local source artwork for the Pocket candidate."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[1]/'Textures/Candidates'
RED='#ef352b';WHITE='#faf0d6';DARK='#152431'
im=Image.new('RGB',(2048,2048),RED);d=ImageDraw.Draw(im)
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',190)
for x in [0,1024]:
    d.text((x+500,1080),'01',font=font,fill=WHITE,anchor='mm')
    d.rectangle((x+380,1185,x+650,1215),fill=WHITE)
    d.rectangle((x+380,1230,x+650,1250),fill=WHITE)
    d.rounded_rectangle((x+660,1010,x+736,1032),radius=8,fill=DARK)
# Round headlamp artwork mapped into the broad square nose.
for x in [115,720]:
    d.ellipse((x,1550,x+190,1780),fill=DARK)
    d.ellipse((x+16,1566,x+174,1764),fill='#fff3d2')
d.rounded_rectangle((340,1700,684,1885),radius=28,fill='#10232e')
for y in [1750,1805,1850]:d.line((366,y,658,y),fill='#384b58',width=12)
# Distinct rear lighting, hatch seam, and printed license panel.
for x in [1100,1830]:
    d.rounded_rectangle((x,1510,x+115,1780),radius=20,fill='#751920')
    d.rounded_rectangle((x+12,1522,x+103,1660),radius=12,fill='#ffb577')
    d.rectangle((x+12,1680,x+103,1712),fill='#ffecb3')
d.rounded_rectangle((1340,1700,1730,1830),radius=14,fill=WHITE)
d.text((1535,1765),'POCKET',font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',65),fill=DARK,anchor='mm')
im.save(OUT/'M01_body.png')
im=Image.new('RGB',(2048,1024),RED);d=ImageDraw.Draw(im)
for i in range(4):
    x=i*512
    if i in (1,2):
        points=[(22,470),(76,40),(449,40),(494,470)]
        if i==2:points=[(512-a,b) for a,b in points]
        points=[(x+a,b) for a,b in points]
        d.polygon(points,fill='#23323f');d.line(points+[points[0]],fill='#08151e',width=10,joint='curve')
    else:
        d.rounded_rectangle((x+22,34,x+490,471),radius=18,fill='#08151e')
        d.rounded_rectangle((x+32,46,x+478,458),radius=12,fill='#23323f')
    if i in (1,2):
        split=x+370 if i==1 else x+142
        d.rectangle((split-6,36,split+6,470),fill='#07131b')
    if i in (0,3): d.line((x+70,435,x+300,435),fill='#09131a',width=10)
im.save(OUT/'M01_cabin.png')
print('M01 original source artwork saved')
