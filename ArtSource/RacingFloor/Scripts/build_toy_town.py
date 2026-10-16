"""Author the editable town SVG and matching supersampled Unity texture.

All coordinates are room-local meters (X right, Z toward the back of the shop).
The SVG and PNG are drawn from the same vector primitives; Pillow is the only dependency.
"""
from pathlib import Path
from math import sin, cos, pi
from html import escape
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'ArtSource/RacingFloor/ToyTown'
RUNTIME = ROOT / 'Assets/TinyRiders/Art/RacingFloor/Toy Town Background.png'
SOURCE.mkdir(parents=True, exist_ok=True)
SIZE = 4096
image = Image.new('RGB', (SIZE, SIZE))
draw = ImageDraw.Draw(image)
svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="2460" height="2660" viewBox="0 0 2460 2660">',
       '<title>Tiny Riders — Toy Town Playmat</title>',
       '<desc>24.6 by 26.6 meter floor. North is the back wall. Track and shelves are separate Unity geometry.</desc>']

P = dict(pave='#9DAEAD', seam='#899F9F', walk='#CCD0BF', sand='#D6CCB0', edge='#B8BAA5',
         lawn='#9CB79B', lawn2='#ADC2A4', lawn3='#8FAB91', tree='#6F957B', tree2='#8EAD86',
         trunk='#ACA17E', ivory='#E3DECA', teal='#739C9B', water='#84AFB1', water2='#A3C4BD',
         roof='#C39B86', roof2='#D1AC93', roofdark='#AB8574', outline='#80998D')

def coord(x, z): return ((x + 12.3) * 100, (13.3 - z) * 100)
def pix(p): return (p[0] * SIZE / 2460, p[1] * SIZE / 2660)
def color(c): return P.get(c, c)
def group(name): svg.append(f'<g id="{escape(name)}">')
def end(): svg.append('</g>')

def poly(points, fill, stroke=None, width=.035):
    pts = [coord(*p) for p in points]
    attr = f'fill="{color(fill)}"'
    if stroke: attr += f' stroke="{color(stroke)}" stroke-width="{width*100}" stroke-linejoin="round"'
    svg.append('<polygon points="'+' '.join(f'{x:.3f},{y:.3f}' for x,y in pts)+f'" {attr}/>')
    draw.polygon([pix(p) for p in pts], fill=color(fill))
    if stroke: draw.line([pix(p) for p in pts+[pts[0]]], fill=color(stroke), width=max(1,round(width*SIZE/25.6)), joint='curve')

def line(points, fill, width=.035):
    pts = [coord(*p) for p in points]
    svg.append('<polyline points="'+' '.join(f'{x:.3f},{y:.3f}' for x,y in pts)
               +f'" fill="none" stroke="{color(fill)}" stroke-width="{width*100}" stroke-linecap="round" stroke-linejoin="round"/>')
    draw.line([pix(p) for p in pts], fill=color(fill), width=max(1,round(width*SIZE/25.6)), joint='curve')
    r=width*SIZE/25.6/2
    for p in [pts[0],pts[-1]]:
        x,y=pix(p); draw.ellipse((x-r,y-r,x+r,y+r),fill=color(fill))

def ellipse(x,z,rx,rz,fill,stroke=None,width=.035):
    cx,cy=coord(x,z)
    attr=f'fill="{color(fill)}"'
    if stroke: attr+=f' stroke="{color(stroke)}" stroke-width="{width*100}"'
    svg.append(f'<ellipse cx="{cx:.3f}" cy="{cy:.3f}" rx="{rx*100:.3f}" ry="{rz*100:.3f}" {attr}/>')
    a=pix((cx-rx*100,cy-rz*100));b=pix((cx+rx*100,cy+rz*100))
    draw.ellipse((*a,*b), fill=color(fill), outline=color(stroke) if stroke else None, width=max(1,round(width*SIZE/25.6)))

def rect(x,z,w,h,r,fill,stroke=None,width=.035):
    # Rounded corners are sampled to keep both editable SVG and PNG geometry identical.
    r=min(r,w/2,h/2)
    points=[]
    for cx,cz,angle in [(x+w/2-r,z+h/2-r,0),(x-w/2+r,z+h/2-r,90),
                        (x-w/2+r,z-h/2+r,180),(x+w/2-r,z-h/2+r,270)]:
        for i in range(13):
            t=(angle+i*90/12)*pi/180
            points.append((cx+r*cos(t),cz+r*sin(t)))
    poly(points,fill,stroke,width)

def curve(points, fill, width):
    """Cubic Bezier: continuous printed paths, emitted as editable vector polylines."""
    a,b,c,d=points
    sampled=[]
    for i in range(81):
        t=i/80;s=1-t
        sampled.append((s**3*a[0]+3*s*s*t*b[0]+3*s*t*t*c[0]+t**3*d[0],
                        s**3*a[1]+3*s*s*t*b[1]+3*s*t*t*c[1]+t**3*d[1]))
    line(sampled,fill,width)

def tree(x,z,size=1):
    ellipse(x,z,.48*size,.51*size,'lawn3')
    for dx,dz,r,c in [(-.17,.08,.26,'tree'),(.16,.13,.29,'tree2'),(.04,-.17,.27,'tree')]:
        ellipse(x+dx*size,z+dz*size,r*size,r*size,c)
    line([(x-.06*size,z-.24*size),(x+.04*size,z+.16*size)],'lawn2',.035*size)
    line([(x+.01*size,z-.04*size),(x+.16*size,z+.08*size)],'lawn2',.025*size)

def bench(x,z,vertical=False):
    if vertical:
        rect(x,z,.22,.7,.045,'trunk');line([(x-.04,z-.26),(x-.04,z+.26)],'sand',.035)
    else:
        rect(x,z,.7,.22,.045,'trunk');line([(x-.26,z-.04),(x+.26,z-.04)],'sand',.035)

def building(x,z,w=1.5,h=1.8,flip=False):
    rect(x,z,w+.3,h+.3,.2,'walk')
    rect(x,z,w,h,.12,'roofdark')
    poly([(x-w/2+.08,z-h/2+.08),(x,z-h/2+.18),(x,z+h/2-.18),(x-w/2+.08,z+h/2-.08)],'roof2')
    poly([(x,z-h/2+.18),(x+w/2-.08,z-h/2+.08),(x+w/2-.08,z+h/2-.08),(x,z+h/2-.18)],'roof')
    line([(x,z-h/2+.17),(x,z+h/2-.17)],'sand',.045)
    for dz in [-.42,0,.42]:
        line([(x-w/2+.13,z+dz),(x-.09,z+dz+.08)],'roof',.025)
        line([(x+.09,z+dz+.08),(x+w/2-.13,z+dz)],'roofdark',.025)
    rect(x+w*.24,z+h*.22,.25,.32,.03,'sand')
    rect(x+w*.24,z+h*.22,.13,.2,.02,'teal')

group('town-paving')
rect(0,0,24.6,26.6,0,'pave')
# Long perimeter promenades and broad expansion joints avoid empty flat color.
rect(0,0,23.8,25.8,.35,'walk')
rect(0,0,22.7,24.7,.45,'pave')
for side in [-1,1]:
    rect(side*10.6,0,1.9,20.2,.5,'walk')
    for z in [-8.6,-6.4,-4.2,-2,0.2,2.4,4.6,6.8,9]:
        line([(side*9.7,z),(side*11.5,z)],'edge',.025)
    line([(side*11.55,-11.9),(side*11.55,11.9)],'ivory',.045)
for z in [-11.4,11.4]:
    rect(0,z,19.2,1.9,.55,'walk')
    for x in [-7.3,-5.1,-2.9,2.9,5.1,7.3]:
        line([(x,z-.8),(x,z+.8)],'edge',.025)
end()

group('island-parks')
# The track overlays this park boundary; the center straight divides it into two islands.
rect(0,0,12.6,15.6,1.2,'lawn','outline',.06)
for side in [-1,1]:
    rect(side*3.85,0,4.65,14.9,.95,'lawn2')
    for z in [-4.5,0,4.5]:
        rect(side*2.05,z,1.25,2.85,.55,'lawn')
        rect(side*5.65,z+(.35 if side==1 else -.35),.9,2.25,.4,'lawn')
    curve([(side*2.55,-7.5),(side*1.55,-3.8),(side*2.95,1.8),(side*2.15,7.5)],'edge',.49)
    curve([(side*2.55,-7.5),(side*1.55,-3.8),(side*2.95,1.8),(side*2.15,7.5)],'sand',.38)
    curve([(side*5.4,-7.1),(side*5.2,-2.2),(side*5.5,2.4),(side*5.4,7.1)],'sand',.24)
    for z in [-5.9,-2.5,1.1,4.8]:
        tree(side*1.75,z,.66)
    for z in [-5.2,-.8,3.6]:
        tree(side*5.9,z,.62)
    bench(side*2.68,-3.7,True);bench(side*2.6,3.1,True)
    # Two small garden plazas at each island end are visible beyond the shelf footprints.
    for z in [-7.15,7.15]:
        ellipse(side*4,z,.85,.43,'sand')
        ellipse(side*4,z,.65,.29,'lawn3')
        for dx in [-.4,0,.4]: ellipse(side*4+dx,z,.13,.13,'roof2')
end()

group('pocket-gardens-and-parking')
for side in [-1,1]:
    for z in [-7.2,0,7.2]:
        rect(side*10.35,z,1.75,2.0,.5,'edge')
        rect(side*10.35,z,1.58,1.83,.43,'lawn')
        tree(side*10.3,z+.28,.95)
        bench(side*10.35,z-.58)
    for z in [-4.2,3.7]:
        rect(side*10.15,z,1.8,2.6,.19,'teal')
        for step in range(4):
            zz=z-1.2+step*.8
            line([(side*9.35,zz),(side*10.95,zz)],'walk',.045)
        line([(side*10.95,z-1.2),(side*10.95,z+1.2)],'walk',.045)
        for step in range(3):
            zz=z-.8+step*.8
            line([(side*10.65,zz-.2),(side*10.65,zz+.2)],'sand',.05)
end()

group('corner-landmarks')
for side in [-1,1]:
    for z in [-10.9,10.9]:
        building(side*10.05,z,1.55,1.65)
        tree(side*8.62,z+(.4 if z>0 else -.4),.8)
        rect(side*7.25,z+(.5 if z>0 else -.5),1.35,.65,.28,'lawn')
        tree(side*7.25,z+(.5 if z>0 else -.5),.58)
end()

group('back-courtyard')
rect(0,11.35,3.5,2.0,.65,'sand','edge',.05)
ellipse(0,11.35,.73,.73,'teal')
ellipse(0,11.35,.59,.59,'water2')
ellipse(0,11.35,.35,.35,'water')
ellipse(0,11.35,.12,.12,'ivory')
bench(-1.2,11.35,True);bench(1.2,11.35,True)
for x in [-4.2,4.2]:
    rect(x,11.35,1.55,1.5,.45,'lawn'); tree(x,11.35,.95)
end()

group('entrance-compass-plaza')
rect(0,-11.35,3.35,2.15,.55,'sand','edge',.055)
ellipse(0,-11.35,.91,.91,'walk','teal',.045)
ellipse(0,-11.35,.72,.72,'sand')
for i in range(4):
    a=i*pi/2
    tip=(.63*sin(a),-11.35+.63*cos(a))
    left=(.17*sin(a-.7),-11.35+.17*cos(a-.7))
    right=(.17*sin(a+.7),-11.35+.17*cos(a+.7))
    poly([(0,-11.35),left,tip],'teal')
    poly([(0,-11.35),tip,right],'roof')
ellipse(0,-11.35,.08,.08,'ivory')
for side in [-1,1]:
    for x in [3.2,5.3]:
        rect(side*x,-11.5,1.35,1.05,.3,'lawn')
        tree(side*x,-11.5,.67)
end()

svg.append('</svg>')
(SOURCE/'Toy Town Playmat.svg').write_text('\n'.join(svg),encoding='utf-8')
image.save(SOURCE/'Toy Town Playmat 4K.png')
image.resize((2048,2048),Image.Resampling.LANCZOS).save(RUNTIME)
print(f'Created editable SVG, 4096px source, and {RUNTIME.relative_to(ROOT)}')
