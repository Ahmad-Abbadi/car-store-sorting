"""Editable skyline atlas and seamless, physically scaled plaster/acoustic surface maps.
Run with Python + Pillow + numpy. SVG is the editable master; exports share its primitives.
Atlas: four 8 m x .75 m modules, arranged 2 x 2. No fonts or external image dependencies.
"""
from pathlib import Path
from math import sin, cos, pi
from PIL import Image, ImageDraw
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'ArtSource/ShopInterior/Artwork'
ART = ROOT / 'Assets/TinyRiders/Art/ShopInterior/Textures'
SOURCE.mkdir(parents=True, exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)
W, H = 8192, 2048
image = Image.new('RGB', (W, H), '#E5DFCF')
draw = ImageDraw.Draw(image)
svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="8192" height="2048" viewBox="0 0 1600 400">',
       '<title>Tiny Riders — Four Toy Town Skyline Modules</title>',
       '<desc>Four 8 m by 0.75 m bands occupy the top 75/200 of each quadrant. Remaining sky is compression-safe padding. Constant 512 source pixels/m.</desc>']
S = W / 1600
ox = oy = 0

def rect(x,y,w,h,fill,r=0):
    x+=ox;y+=oy
    svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"/>')
    draw.rounded_rectangle((x*S,y*S,(x+w)*S,(y+h)*S),radius=r*S,fill=fill)

def ellipse(x,y,rx,ry,fill):
    x+=ox;y+=oy
    svg.append(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="{fill}"/>')
    draw.ellipse(((x-rx)*S,(y-ry)*S,(x+rx)*S,(y+ry)*S),fill=fill)

def poly(points,fill):
    points=[(x+ox,y+oy) for x,y in points]
    svg.append('<polygon points="'+' '.join(f'{x},{y}' for x,y in points)+f'" fill="{fill}"/>')
    draw.polygon([(x*S,y*S) for x,y in points],fill=fill)

def house(x,w,h,body,roof,style):
    # Large, calm forms; deliberately no tiny text, cars, or baked painted shadows.
    y=70-h
    rect(x,y,w,h,body,3)
    if style==0: poly([(x-2,y+3),(x+w*.5,y-13),(x+w+2,y+3)],roof)
    elif style==1: rect(x-2,y-5,w+4,10,roof,5)
    else:
        rect(x+3,y-9,w-6,13,roof,6)
        rect(x-2,y+1,w+4,4,roof,2)
    for a in (.24,.65):
        rect(x+w*a,y+10,7,10,'#DDE2D2',3)
        if h>41: rect(x+w*a,y+28,7,10,'#DDE2D2',3)
    rect(x+w*.43,57,9,13,'#89A6A0',4)

def tree(x,h,fill):
    rect(x-1,69-h/2,2,h/2,'#A6A38B',1)
    ellipse(x,69-h*.65,h*.23,h*.35,fill)

for module in range(4):
    ox=(module%2)*800;oy=(module//2)*200
    svg.append(f'<g id="skyline-module-{module+1}">')
    rect(0,0,800,200,'#E5DFCF')
    # Each module begins/ends on the same quiet, continuous horizon.
    rect(0,66,800,9,'#C0C8AD',3)
    rect(0,72,800,3,'#A9BBA3')
    for x,y in ((72,14),(350,12),(654,17)):
        rect(x,y,37,5,'#EEEADE',2.5)
        rect(x+8,y-4,17,8,'#EEEADE',4)
    ellipse(715,15,8,8,'#D4BF91')
    shift=(module*23)%47
    house(115+shift,56,37,'#B4C7BA','#7E9F9B',module%3)
    house(185+shift,44,45,'#CCB9A5','#B68D78',(module+1)%3)
    house(430-shift,60,42,'#A8BDB9','#779895',(module+2)%3)
    house(505-shift,40,32,'#CDC7AF','#A9AF96',module%3)
    for x,h in ((48,37),(290,42),(324,31),(608,42),(639,33),(760,34)):
        tree(x,h,'#9BB396' if int(x)%2 else '#ACC0A1')
    # Small broad park paths integrate the same sage/ivory language as the floor.
    rect(271,68,68,3,'#E0D7BC',1.5)
    rect(591,68,67,3,'#E0D7BC',1.5)
    svg.append('</g>')
svg.append('</svg>')
(SOURCE/'Toy Town Skyline.svg').write_text('\n'.join(svg),encoding='utf-8')
image.save(SOURCE/'Toy Town Skyline 8192.png')
image.resize((4096,1024),Image.Resampling.LANCZOS).save(ART/'Toy Town Skyline.png')

N=2048
rng=np.random.default_rng(14829)
def periodic_noise(cells):
    a=rng.integers(0,256,(cells,cells),dtype=np.uint8)
    # Repeat 3 x 3 before bicubic resizing, then crop the middle tile.
    im=Image.fromarray(np.tile(a,(3,3))).resize((N*3,N*3),Image.Resampling.BICUBIC)
    a=np.asarray(im,dtype=np.float32)[N:N*2,N:N*2]/255-.5
    a[-1,:]=a[0,:];a[:,-1]=a[:,0]
    return a

for name,tint,strength,smooth in [('Wall Plaster',(229,222,205),1.6,.19),('Ceiling Acoustic',(234,232,218),1.15,.16)]:
    noise=periodic_noise(16)*.4+periodic_noise(128)*.35+periodic_noise(512)*.25
    base=np.clip(np.array(tint)[None,None,:]+noise[:,:,None]*strength*3,0,255).astype(np.uint8)
    Image.fromarray(base).save(ART/f'{name} BaseColor.png')
    dx=(np.roll(noise,-1,axis=1)-np.roll(noise,1,axis=1))*.12
    dy=(np.roll(noise,-1,axis=0)-np.roll(noise,1,axis=0))*.12
    normal=np.stack((-dx,dy,np.ones_like(dx)),axis=2)
    normal/=np.linalg.norm(normal,axis=2)[:,:,None]
    encoded=np.clip((normal*.5+.5)*255,0,255).astype(np.uint8)
    encoded[-1,:]=encoded[0,:];encoded[:,-1]=encoded[:,0]
    Image.fromarray(encoded).save(ART/f'{name} Normal.png')
    mask=np.zeros((N,N,4),dtype=np.uint8)
    mask[:,:,3]=np.clip((smooth+noise*.025)*255,0,255).astype(np.uint8)
    Image.fromarray(mask).save(ART/f'{name} Smoothness.png')
print('Exported editable SVG, 8192px source, 4096px runtime atlas and six seamless 2048px surface maps.')
