"""Editable Parcel artwork. All lettering, lamps and cargo marks are printed UV art."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
OUT=Path(__file__).resolve().parents[1]/'Textures/Candidates'
YELLOW='#ffc62d'; DARK='#242c31'; INK='#101e28'; WHITE='#fff7db'
im=Image.new('RGB',(2048,2048),YELLOW); d=ImageDraw.Draw(im)
def font(size): return ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',size)
# Atlas side rectangles use global x -1.8..1.85 and z .25..2.7.
for side,offset in [('left',0),('right',1024)]:
    def pt(x,z):
        a=(x+1.8)/3.65
        if side=='right': a=1-a
        return (offset+a*1024,800+(2.7-z)/2.45*500)
    def line(points,fill,width): d.line([pt(*p) for p in points],fill=fill,width=width,joint='curve')
    d.rectangle((offset,800+(2.7-1.23)/2.45*500,offset+1023,1300),fill=DARK)
    line([(-1.65,.37),(1.72,.37)],YELLOW,7)
    # Door shut line and inset handle are printing, not raised geometry.
    line([(-.27,1.27),(-.27,2.48)],'#df9f19',5)
    line([(-1.53,1.25),(-1.53,.84),(-.30,.84),(-.30,1.25)],'#0f171d',4)
    hx,hz=pt(-.5,1.36);d.rounded_rectangle((hx-25,hz-5,hx+25,hz+5),radius=4,fill=INK)
    nx,nz=pt(.39,1.05);d.text((nx,nz),'03',fill=WHITE,font=font(86),anchor='mm')
    # Original flat cube pictogram on cargo panel.
    cx,cy=pt(.69,2.07)
    pts=[(cx,cy-59),(cx+61,cy-27),(cx+61,cy+43),(cx,cy+77),(cx-61,cy+43),(cx-61,cy-27)]
    d.polygon(pts,fill=INK);d.line([pts[1],(cx,cy+6)],fill=YELLOW,width=6)
    d.line([pts[5],(cx,cy+6),(cx,cy+72)],fill=YELLOW,width=6)
    d.line([(cx-30,cy-44),(cx+30,cy-12)],fill=YELLOW,width=5)
# Front mapped z .25..1.45, full width.
for x in [108,736]:
    d.rounded_rectangle((x,1435,x+180,1630),radius=22,fill=INK)
    d.rounded_rectangle((x+12,1447,x+168,1618),radius=14,fill=WHITE)
    d.rectangle((x+22,1585,x+158,1604),fill='#ffd378')
d.rounded_rectangle((326,1610,698,1770),radius=24,fill=INK)
for y in [1645,1688,1731]:d.line((354,y,670,y),fill='#46535c',width=12)
d.rounded_rectangle((411,1800,613,1895),radius=12,fill=WHITE)
d.text((512,1848),'CC 03',font=font(48),fill=INK,anchor='mm')
# Rear upper cargo doors; lower lamps and number plate use full-height UVs.
def back(x,z): return (1024+(x+1.02)/2.04*1024,1350+(2.7-z)/2.45*650)
d.line([back(0,.47),back(0,2.51)],fill='#c38d18',width=6)
for y in [-.77,.77]:
    a,b=back(y,1.02),back(y+.12,.62)
    d.rounded_rectangle((*a,*b),radius=10,fill='#842124')
    a,b=back(y+.02,.96),back(y+.10,.82);d.rectangle((*a,*b),fill='#ff7852')
for x in [-.11,.11]:
    a,b=back(x,1.58),back(x+.055,1.29);d.rounded_rectangle((*a,*b),radius=5,fill=INK)
px,py=back(0,.82);d.rounded_rectangle((px-118,py-29,px+118,py+29),radius=8,fill=WHITE)
d.text((px,py),'PARCEL',font=font(36),fill=INK,anchor='mm')
im.save(OUT/'M02_body.png')
im=Image.new('RGB',(2048,1024),YELLOW);d=ImageDraw.Draw(im)
# Front and sides, explicitly mapped so the windows follow the short cab roofline.
d.rounded_rectangle((30,25,484,485),radius=25,fill=INK)
d.rounded_rectangle((42,39,472,470),radius=18,fill='#243340')
d.line((65,446,286,446),fill='#0c161f',width=10)
for i in [1,2]:
    pts=[(36,476),(134,32),(480,32),(480,476)]
    if i==2:pts=[(512-x,y) for x,y in pts]
    pts=[(i*512+x,y) for x,y in pts]
    d.polygon(pts,fill='#243340');d.line(pts+[pts[0]],fill=INK,width=12,joint='curve')
im.save(OUT/'M02_cabin.png')
print('M02 local artwork saved')
