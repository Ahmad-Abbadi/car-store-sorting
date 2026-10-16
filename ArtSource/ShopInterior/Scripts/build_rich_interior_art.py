"""Pack image_gen masters into proportional, editable wall layouts and shared atlases.

This script performs technical cropping, layout, feather-mask composition and packing.
It does not draw replacement illustrations. Original generated pixels remain in Masters/.
The corresponding SVG layouts retain linked raster layers, clipping and editable masks.
"""
from pathlib import Path
from html import escape
import json
import math
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'ArtSource/ShopInterior/RichArtwork'
RUNTIME = ROOT / 'Assets/TinyRiders/Art/ShopInterior/Textures'
LANCZOS = Image.Resampling.LANCZOS


def main():
    config = json.loads((SOURCE / 'composition.json').read_text())
    size = config['sourceAtlasSize']
    ppm = config['sourcePixelsPerMeter']
    height = round(config['bandHeightMeters'] * ppm)
    gutter = config['gutterPixels']
    for folder in ['Layouts', 'Masks', 'Panoramas']:
        (SOURCE / folder).mkdir(exist_ok=True)
    RUNTIME.mkdir(parents=True, exist_ok=True)
    bridge_master = Image.open(SOURCE / 'Masters/09 Connecting Landscapes.png').convert('RGB')
    tiles = []
    records = []
    for cat in config['categories']:
        master = Image.open(SOURCE / 'Masters' / cat['file']).convert('RGB')
        crop = cat['crop']
        art = master.crop(crop)
        width = round(cat['lengthMeters'] * ppm)
        canvas = Image.new('RGB', (width, height), '#FFF7E5')
        svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
               f'<rect width="{width}" height="{height}" fill="#FFF7E5"/>']
        if cat['bridge'] >= 0:
            # The low-detail scenery may repeat; each of the five landmark scenes appears once.
            n = cat['bridge']
            by0 = round(n * bridge_master.height / 3)
            by1 = round((n+1) * bridge_master.height / 3)
            bridge = bridge_master.crop((0, by0, bridge_master.width, by1))
            scale = height / bridge.height
            bw = round(bridge.width * scale)
            bridge = bridge.resize((bw, height), LANCZOS)
            # Overlapping scenery tiles use actual painted pixels, never stretched edge columns.
            overlap = round(.35 * ppm)
            step = bw - overlap
            for k, x in enumerate(range(-bw//3, width, step)):
                bm = Image.new('L', (bw, height), 255)
                bd = ImageDraw.Draw(bm)
                if k:
                    for xx in range(overlap): bd.line((xx,0,xx,height),fill=round(255*xx/overlap))
                canvas.paste(bridge, (x,0), bm)
                mask_name = f'{cat["id"]:02d}-bridge-{k}.png'
                bm.save(SOURCE/'Masks'/mask_name)
                clip = f'bridge-{k}'
                svg += [f'<defs><clipPath id="{clip}"><rect x="{x}" y="0" width="{bw}" height="{height}"/></clipPath>',
                        f'<mask id="{clip}-mask" maskUnits="userSpaceOnUse" x="{x}" y="0" width="{bw}" height="{height}"><image href="../Masks/{mask_name}" x="{x}" width="{bw}" height="{height}"/></mask></defs>',
                        f'<g clip-path="url(#{clip})" mask="url(#{clip}-mask)"><image href="../Masters/09 Connecting Landscapes.png" x="{x}" y="{-by0*scale}" width="{bridge_master.width*scale}" height="{bridge_master.height*scale}"/></g>']
            scale = height / art.height
            for theme in range(5):
                left = round(theme * art.width / 5)
                right = round((theme+1) * art.width / 5)
                scene = art.crop((left,0,right,art.height))
                sw = round(scene.width * scale)
                scene = scene.resize((sw,height),LANCZOS)
                x = round((theme+.5)*width/5-sw/2)
                feather = min(round(config['featherMeters']*ppm),sw//8)
                mask = Image.new('L',(sw,height),255)
                md = ImageDraw.Draw(mask)
                for xx in range(feather):
                    v = round(255*xx/feather)
                    md.line((xx,0,xx,height),fill=v)
                    md.line((sw-1-xx,0,sw-1-xx,height),fill=v)
                canvas.paste(scene,(x,0),mask)
                mask_name=f'{cat["id"]:02d}-theme-{theme}.png'
                mask.save(SOURCE/'Masks'/mask_name)
                clip=f'theme-{theme}'
                svg += [f'<defs><clipPath id="{clip}"><rect x="{x}" y="0" width="{sw}" height="{height}"/></clipPath>',
                        f'<mask id="{clip}-mask" maskUnits="userSpaceOnUse" x="{x}" y="0" width="{sw}" height="{height}"><image href="../Masks/{mask_name}" x="{x}" width="{sw}" height="{height}"/></mask></defs>',
                        f'<g id="theme-{theme+1}-scene" clip-path="url(#{clip})" mask="url(#{clip}-mask)"><image href="../Masters/{escape(cat["file"])}" x="{x-(left+crop[0])*scale}" y="{-crop[1]*scale}" width="{master.width*scale}" height="{master.height*scale}"/></g>']
        else:
            # Entrance compositions retain all five environments with quiet ivory matting.
            scale = min(width/art.width, height/art.height)
            sw, sh = round(art.width*scale), round(art.height*scale)
            x,y = (width-sw)//2,(height-sh)//2
            canvas.paste(art.resize((sw,sh),LANCZOS),(x,y))
            svg += [f'<defs><clipPath id="art"><rect x="{x}" y="{y}" width="{sw}" height="{sh}"/></clipPath></defs>',
                    f'<g clip-path="url(#art)"><image href="../Masters/{escape(cat["file"])}" x="{x-crop[0]*scale}" y="{y-crop[1]*scale}" width="{master.width*scale}" height="{master.height*scale}"/></g>']
        svg += ['</svg>']
        (SOURCE/'Layouts'/(cat['name']+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
        canvas.save(SOURCE/'Panoramas'/(cat['name']+'.png'))
        records.append({'id':cat['id'],'name':cat['name'],'lengthMeters':cat['lengthMeters'],'tiles':[]})
        # Five contiguous UV tiles per panorama improve atlas utilization without changing scale.
        for n in range(5):
            x0,x1=round(n*width/5),round((n+1)*width/5)
            tiles.append({'category':cat['id'],'order':n,'image':canvas.crop((x0,0,x1,height)),
                          'startMeters':cat['lengthMeters']*n/5,'endMeters':cat['lengthMeters']*(n+1)/5})
    atlas=Image.new('RGB',(size,size),'#F2E5C8')
    rows=[]
    for tile in sorted(tiles,key=lambda t:t['image'].width,reverse=True):
        im=tile['image']; w=im.width+2*gutter; h=height+2*gutter
        row=next((r for r in rows if r['x']+w<=size),None)
        if row is None:
            row={'x':0,'y':len(rows)*h};rows.append(row)
        x,y=row['x']+gutter,row['y']+gutter
        if y+height+gutter>size-96:raise RuntimeError('Atlas capacity exceeded')
        atlas.paste(im,(x,y))
        # Mip-safe extruded gutters are outside visible UVs, not stretched visible scenery.
        atlas.paste(im.crop((0,0,1,height)).resize((gutter,height)),(x-gutter,y))
        atlas.paste(im.crop((im.width-1,0,im.width,height)).resize((gutter,height)),(x+im.width,y))
        atlas.paste(atlas.crop((x-gutter,y,x+im.width+gutter,y+1)).resize((w,gutter)),(x-gutter,y-gutter))
        atlas.paste(atlas.crop((x-gutter,y+height-1,x+im.width+gutter,y+height)).resize((w,gutter)),(x-gutter,y+height))
        row['x']+=w
        records[tile['category']]['tiles'].append({'startMeters':tile['startMeters'],'endMeters':tile['endMeters'],
            'u0':x/size,'u1':(x+im.width)/size,'v0':1-(y+height)/size,'v1':1-y/size})
    d=ImageDraw.Draw(atlas)
    # Shared palette for existing frames, cap rail and checker trim.
    for i,c in enumerate(config['palette']):d.rectangle((64+i*128,size-80,144+i*128,size-16),fill=c)
    atlas.save(SOURCE/'Rich Category Atlas Source.png')
    atlas.resize((config['runtimeAtlasSize'],)*2,LANCZOS).save(RUNTIME/'Category Murals.png')
    for r in records:r['tiles'].sort(key=lambda t:t['startMeters'])
    manifest={'version':1,'bandHeightMeters':config['bandHeightMeters'],'categories':records,
              'palette':[{'u':(104+i*128)/size,'v':48/size} for i in range(8)]}
    (RUNTIME/'Rich Mural Layout.json').write_text(json.dumps(manifest,indent=2))
    (SOURCE/'atlas-layout.json').write_text(json.dumps(manifest,indent=2))
    # The ceiling print now contains only palette colors; no hidden logo can reappear on rebuild.
    ceiling=Image.new('RGB',(2048,1024),'#438B8B');cd=ImageDraw.Draw(ceiling)
    svg=['<svg xmlns="http://www.w3.org/2000/svg" width="4096" height="2048" viewBox="0 0 1000 500"><rect width="1000" height="500" fill="#438B8B"/>']
    for i,c in enumerate(config['palette']):
        cd.rectangle((round((30+i*120)*2.048),901,round((120+i*120)*2.048),993),fill=c)
        svg.append(f'<rect x="{30+i*120}" y="440" width="90" height="45" fill="{c}"/>')
    svg.append('</svg>')
    ceiling.save(RUNTIME/'Racing Ceiling Graphics.png')
    (SOURCE/'Ceiling Palette.svg').write_text('\n'.join(svg))
    print(f'Packed 8 panoramas / 40 unique theme tiles into {size}px source and {config["runtimeAtlasSize"]}px runtime atlas; {len(rows)} rows. No branding.')


if __name__ == '__main__': main()
