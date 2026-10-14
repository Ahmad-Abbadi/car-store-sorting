from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import shutil

ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'ArtSource/Shelves/BaseShelf_01/Artwork'
DEST = ROOT / 'Assets/TinyRiders/Art/Shelves/BaseShelf_01/Textures'
# All graphics are printed into existing physical surfaces. No decal planes.
FONT = 'C:/Windows/Fonts/ARIALNBI.TTF'
def save(name, size, bg, elements):
    im = Image.new('RGB', size, bg); d = ImageDraw.Draw(im)
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{size[0]}" height="{size[1]}" viewBox="0 0 {size[0]} {size[1]}">',f'<rect width="100%" height="100%" fill="{bg}"/>']
    for e in elements:
        if e[0]=='rect':
            _,box,c=e; d.rectangle(box,fill=c); x,y,x2,y2=box
            svg.append(f'<rect x="{x}" y="{y}" width="{x2-x}" height="{y2-y}" fill="{c}"/>')
        elif e[0]=='ellipse':
            _,box,c,outline,w=e; d.ellipse(box,fill=c,outline=outline,width=w); x,y,x2,y2=box
            svg.append(f'<ellipse cx="{(x+x2)/2}" cy="{(y+y2)/2}" rx="{(x2-x)/2}" ry="{(y2-y)/2}" fill="{c}" stroke="{outline}" stroke-width="{w}"/>')
        else:
            _,xy,t,s,c=e; d.text(xy,t,font=ImageFont.truetype(FONT,s),fill=c)
            svg.append(f'<text x="{xy[0]}" y="{xy[1]+s*.86}" font-family="Arial Narrow" font-weight="bold" font-style="italic" font-size="{s}" fill="{c}">{t}</text>')
    im.save(OUT/(name+'.png')); (OUT/(name+'.svg')).write_text('\n'.join(svg+['</svg>']))
    shutil.copy2(OUT/(name+'.png'),DEST/(name+'.png'))
def checks(x,y,s,cols=3,rows=3,c='#eef0f3'):
    return [('rect',(x+i*s,y+j*s,x+(i+1)*s,y+(j+1)*s),c) for j in range(rows) for i in range(cols) if (i+j)%2==0]
save('TEX_Header_Blank',(2048,256),'#f2f3f5',checks(48,30,64,c='#171d29')+checks(1808,30,64,c='#171d29'))
save('TEX_SidePanel',(512,2048),'#ffffff',[('ellipse',(111,360,401,650),'#191d28','#d4d7df',13)]+checks(122,990,90,c='#dfe3eb'))
side=Image.open(OUT/'TEX_SidePanel.png').convert('RGBA')
side.putdata([(r,g,b,0 if (r,g,b)==(255,255,255) else 255) for r,g,b,a in side.getdata()])
side.save(OUT/'TEX_SidePanel.png'); shutil.copy2(OUT/'TEX_SidePanel.png',DEST/'TEX_SidePanel.png')
svgpath=OUT/'TEX_SidePanel.svg'; svgpath.write_text(svgpath.read_text().replace('<rect width="100%" height="100%" fill="#ffffff"/>',''))
save('TEX_Printed_Left',(512,1024),'#242936',[('text',(65,140),'DRIVE',98,'#f0f1f4'),('text',(65,260),'SORT',98,'#f0f1f4'),('text',(65,380),'COLLECT',98,'#f0f1f4')]+checks(170,720,56))
save('TEX_Printed_Right',(512,1024),'#242936',[('text',(60,130),'SMALL',92,'#f0f1f4'),('text',(60,240),'CARS',92,'#f0f1f4'),('text',(60,350),'BRIGHTER',80,'#f0f1f4'),('text',(60,460),'DAYS',92,'#f0f1f4')]+checks(170,740,56))
print('Artwork saved. Font: Arial Narrow Bold Italic (system font, not bundled).')
