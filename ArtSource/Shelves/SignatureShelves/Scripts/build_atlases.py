"""Original 4K signature masters and 2K runtime atlases, with model-rendered header art."""
import json,sys,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/SignatureShelves'
DEST=ROOT/'Assets/TinyRiders/Art/Shelves/Themed/Textures'
TILES=[(2,3),(3,3),(2,2),(3,2),(0,1),(1,1),(2,1),(3,1),(0,0),(1,0),(2,0),(3,0)]
def rgb(s):return np.array([int(s.lstrip('#')[i:i+2],16) for i in (0,2,4)],dtype=float)
def build(theme,icon=None):
    colors=[rgb(theme[k]) for k in ('paint','secondary','trim','ink')]+[rgb(c) for c in ('#F6EAD5','#CEDAE1','#FFFFFF','#474F58','#69AD64','#E87056','#64C7CF','#CAA1E0')]
    base=Image.new('RGB',(4096,4096),'#F4F0E4');normal=Image.new('RGB',(4096,4096),(128,128,255));surface=Image.new('RGBA',(4096,4096),(0,0,0,110))
    rng=np.random.default_rng(sum(map(ord,theme['id'])))
    yy,xx=np.mgrid[0:1024,0:1024]
    for i,(tx,ty) in enumerate(TILES):
        # Fine painted/cloth grain and gentle broad variation, never baked directional highlights.
        grain=rng.normal(0,.55,(1024,1024));weave=np.sin(xx*.9)*np.sin(yy*.9)*.7
        variation=grain+weave+(np.sin(xx/110)+np.sin(yy/173))*.6
        patch=np.clip(colors[i][None,None,:]+variation[:,:,None],0,255).astype('uint8')
        xy=(tx*1024,(3-ty)*1024);base.paste(Image.fromarray(patch),xy)
        dy,dx=np.gradient(variation)
        normals=np.stack((128-dx*.7,128+dy*.7,np.full_like(dx,255)),axis=2).clip(0,255).astype('uint8')
        normal.paste(Image.fromarray(normals),xy)
        metal=round((theme.get('trimMetallic',.5) if i==2 else .03)*255)
        gloss=round((theme.get('trimSmoothness',.5) if i==2 else .42 if i in (0,1) else .35)*255)
        surface.paste(Image.new('RGBA',(1024,1024),(metal,0,0,gloss)),xy)
    if icon:
        render=Image.open(icon).convert('RGBA');render.thumbnail((1770,1770),Image.Resampling.LANCZOS)
        # High-resolution framed icon shares the object's palette and physical shape.
        # White/bright objects need contrast at actual player distance, not just in studio close-ups.
        bright=theme['id'] in {'TH_PIRATE','TH_SPOOKY','TH_SPORT'} or float(np.mean(colors[0]))>145
        backing=tuple((colors[0]*.35+colors[3]*.65).astype(int)) if bright else '#F4F0E4'
        plate=Image.new('RGB',(2048,2048),backing);draw=ImageDraw.Draw(plate)
        trim=tuple(map(int,colors[2]));draw.rounded_rectangle((24,24,2024,2024),radius=205,outline=trim,width=30)
        plate.paste(render,((2048-render.width)//2,(2048-render.height)//2),render)
        base.paste(plate,(0,0))
    for label,im in [('BaseColor',base),('Normal',normal),('Surface',surface)]:
        path=SRC/'Textures'/f"{theme['id']}_Signature_{label}_4K.png";path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
        runtime=im.resize((2048,2048),Image.Resampling.LANCZOS);DEST.mkdir(parents=True,exist_ok=True)
        runtime.save(DEST/f"{theme['id']}_Signature_{label}.png")
if __name__=='__main__':
    recipes=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text(encoding='utf-8-sig'))['themes']
    t=next(t for t in recipes if t['id']==sys.argv[1]);build(t,sys.argv[2] if len(sys.argv)>2 else None)
