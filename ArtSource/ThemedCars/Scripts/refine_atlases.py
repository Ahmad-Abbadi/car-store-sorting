"""R02: larger emblems and broad, family-specific motifs in existing supplier decal UVs."""
import math,json,numpy as np
from PIL import Image,ImageDraw
from theme_art import SRC,OUT,themes,icon

def patch(t,ratio):
    H=512;W=max(260,min(2200,round(H*ratio)));im=Image.new('RGBA',(W,H));d=ImageDraw.Draw(im)
    a,b,c,ink=t['secondary'],t['trim'],t['light'],t['ink'];k=t['id'][3:]
    margin=14;unit=min(W,H)
    def line(points,col,width=12):d.line(points,fill=col,width=width,joint='curve')
    def star(x,y,r,col):d.polygon([(x+math.sin(i*math.pi/5)*r*(1 if i%2==0 else .43),y-math.cos(i*math.pi/5)*r*(1 if i%2==0 else .43)) for i in range(10)],fill=col)
    def band(y,col,width=20):line([(margin,y),(W-margin,y)],col,width)
    if k=='PIRATE':
        d.polygon([(10,25),(W-15,8),(W-32,H*.49),(W-10,H-22),(22,H-7)],fill=ink)
        line([(25,44),(W-40,26)],b,10);line([(30,H-33),(W-30,H-48)],b,10)
        for x in [W*.12,W*.88]:line([(x-20,H*.42),(x+20,H*.58)],b,10);line([(x+20,H*.42),(x-20,H*.58)],b,10)
    elif k=='ROYAL':
        d.polygon([(18,20),(W-18,20),(W-18,H*.73),(W/2,H-10),(18,H*.73)],fill=b)
        d.polygon([(35,38),(W-35,38),(W-35,H*.70),(W/2,H-36),(35,H*.70)],fill=ink)
        for x in [W*.13,W*.87]:star(x,H*.30,23,c)
    elif k in ['SPACE','ROBOT','MUSIC','WIZARD']:
        d.rounded_rectangle((10,10,W-10,H-10),radius=65,fill=ink,outline=b,width=12)
        if k=='SPACE':
            d.arc((W*.05,H*.15,W*.95,H*.85),10,170,fill=b,width=12)
            for x,y in [(W*.12,H*.20),(W*.87,H*.23),(W*.80,H*.80)]:star(x,y,28,c)
        elif k=='ROBOT':
            for flip in [False,True]:
                points=[(W*.08,H*.22),(W*.22,H*.22),(W*.22,H*.37),(W*.36,H*.37)]
                if flip:points=[(W-x,H-y) for x,y in points]
                line(points,b,16)
                x,y=points[0];d.ellipse((x-17,y-17,x+17,y+17),fill=c)
            band(H*.87,b,12)
        elif k=='MUSIC':
            for j in range(9):
                x=W*(j+1)/10;ht=[35,55,85,50,70,95,50,65,35][j]
                d.rounded_rectangle((x-10,H-34-ht,x+10,H-34),radius=8,fill=b if j%2 else c)
        else:
            d.ellipse((W*.06,30,W*.06+75,105),fill=b);d.ellipse((W*.06+26,18,W*.06+91,86),fill=ink)
            for x,y in [(W*.84,H*.22),(W*.13,H*.73),(W*.81,H*.80)]:star(x,y,24,c)
    elif k=='ALIEN':
        d.ellipse((8,12,W-8,H-12),fill=a,outline=b,width=15)
        for x,y in [(W*.13,H*.37),(W*.85,H*.65)]:star(x,y,27,c)
    elif k in ['BEACH','OCEAN']:
        d.rounded_rectangle((10,15,W-10,H-15),radius=90,fill=ink)
        for row,col in [(0,b),(1,a),(2,b)]:
            y=H*.72+row*35
            pts=[(x,y+20*math.sin(x/W*math.tau*2+row*.6)) for x in range(12,W-11,4)]
            line(pts,col,26)
        if k=='OCEAN':
            for x,y,r in [(W*.12,H*.25,22),(W*.88,H*.25,30),(W*.85,H*.42,16)]:d.ellipse((x-r,y-r,x+r,y+r),outline=c,width=8)
    elif k=='JUNGLE':
        d.polygon([(W*.5,8),(W-14,70),(W-8,H*.68),(W*.54,H-8),(15,H*.80),(8,90)],fill=ink)
        for side in [1,-1]:
            for y in [.25,.5,.75]:
                x=W*(.14 if side==1 else .86);d.ellipse((x-22,H*y-35,x+22,H*y+35),fill=b)
                line([(x,H*y-27),(x,H*y+27)],a,7)
    elif k=='SPOOKY':
        d.rounded_rectangle((10,15,W-10,H-15),radius=80,fill=ink)
        d.polygon([(15,H-30)]+[(W*j/8,H-(65 if j%2 else 30)) for j in range(9)]+[(W-15,H-15),(15,H-15)],fill=a)
        for x in [W*.14,W*.86]:star(x,H*.23,27,b)
    elif k=='CANDY':
        d.rounded_rectangle((12,15,W-12,H-15),radius=80,fill=a)
        for j in range(5):
            x=W*(j+.5)/5;d.ellipse((x-32,8,x+32,72),fill=c);d.ellipse((x-32,H-72,x+32,H-8),fill=c)
        for x,y,col in [(W*.13,H*.25,b),(W*.87,H*.32,t['paint']),(W*.12,H*.70,t['paint']),(W*.88,H*.73,b)]:line([(x-16,y-12),(x+16,y+12)],col,18)
    elif k=='FOOD':
        d.rounded_rectangle((12,18,W-12,H-18),radius=60,fill=c,outline=ink,width=12)
        for x in range(20,W-18,52):d.rectangle((x,35,min(x+26,W-20),70),fill=a);d.rectangle((x,H-70,min(x+26,W-20),H-35),fill=b)
    elif k=='FIRE':
        d.polygon([(12,35),(W*.22,9),(W*.50,35),(W*.72,12),(W-12,30),(W-20,H-20),(W*.60,H-9),(W*.25,H-30),(10,H-16)],fill=ink)
        for flip in [False,True]:
            points=[(W*.15,35),(W*.22,H*.30),(W*.12,H*.49),(W*.24,H*.70),(W*.13,H-25)]
            if flip:points=[(W-x,y) for x,y in points]
            line(points,b,22);line(points,c,7)
    elif k=='ICE':
        pts=[(W*.25,12),(W*.75,12),(W-10,H*.5),(W*.75,H-12),(W*.25,H-12),(10,H*.5)]
        d.polygon(pts,fill=b);d.polygon([(W*.27,32),(W*.73,32),(W-36,H*.5),(W*.73,H-32),(W*.27,H-32),(36,H*.5)],fill=a)
    elif k=='CONSTRUCTION':
        d.rounded_rectangle((12,16,W-12,H-16),radius=35,fill=ink)
        for y in [22,H-95]:
            for x in range(-50,W,110):d.polygon([(x,y+68),(x+48,y+68),(x+105,y),(x+57,y)],fill=t['paint'])
    elif k=='RESCUE':
        d.polygon([(W*.18,12),(W*.82,12),(W-12,H*.26),(W-30,H*.75),(W*.5,H-10),(30,H*.75),(12,H*.26)],fill=c)
        line([(W*.12,H*.82),(W*.29,H*.82),(W*.36,H*.69),(W*.45,H*.94),(W*.56,H*.80),(W*.85,H*.80)],ink,12)
    elif k=='SPORT':
        d.rounded_rectangle((12,16,W-12,H-16),radius=70,fill=a,outline=b,width=16)
        for x in [W*.12,W*.88]:line([(x,H*.25),(x,H*.75)],t['paint'],16)
    elif k=='FARM':
        d.rounded_rectangle((12,18,W-12,H-18),radius=50,fill=a)
        for x in range(20,W-18,65):
            d.rectangle((x,30,min(x+32,W-20),70),fill=b);d.rectangle((x,H-70,min(x+32,W-20),H-30),fill=b)
        for x in [W*.12,W*.88]:line([(x,H*.28),(x,H*.73)],b,12)
    else:
        d.rounded_rectangle((12,16,W-12,H-16),radius=70,fill=c)
        for x,y,col in [(W*.15,H*.24,b),(W*.85,H*.22,t['paint']),(W*.12,H*.68,a),(W*.87,H*.78,b)]:star(x,y,25,col)
        line([(W*.25,H*.86),(W*.45,H*.91),(W*.7,H*.84)],t['paint'],14)
    hero=icon(t['icon'],b,t['paint'] if k=='FARM' else c).im
    bbox=hero.getbbox();hero=hero.crop(bbox)
    maxw=W*.79;maxh=H*(.78 if k in ['MUSIC','BEACH','OCEAN','CONSTRUCTION'] else .86)
    scale=min(maxw/hero.width,maxh/hero.height);hero=hero.resize((round(hero.width*scale),round(hero.height*scale)),Image.Resampling.LANCZOS)
    # A few pixels of separation protect the silhouette from the supporting pattern.
    alpha=hero.getchannel('A');from PIL import ImageFilter
    outline=Image.new('RGBA',hero.size,t['ink'] if k in ['CANDY','FARM','PARTY','SPORT','RESCUE','ICE','FOOD'] else t['secondary'])
    outline.putalpha(alpha.filter(ImageFilter.MaxFilter(13)))
    px=(W-hero.width)//2;py=(H-hero.height)//2-(20 if k in ['MUSIC','BEACH','OCEAN','RESCUE'] else 0)
    im.alpha_composite(outline,(px,py));im.alpha_composite(hero,(px,py))
    return im

def layout_groups(layout):
    groups=[]
    for a in layout:
        target=None
        for g in groups:
            b=g[0];x,y,X,Y=a['uv'];u,v,U,V=b['uv'];intersection=max(0,min(X,U)-max(x,u))*max(0,min(Y,V)-max(y,v))
            if intersection/max(1e-8,min((X-x)*(Y-y),(U-u)*(V-v)))>.78:target=g;break
        if target is None:groups.append([a])
        else:target.append(a)
    result=[]
    for g in groups:
        a=dict(g[0]);a['uv']=[max(i['uv'][0] for i in g),max(i['uv'][1] for i in g),min(i['uv'][2] for i in g),min(i['uv'][3] for i in g)]
        result.append(a)
    return result

def main():
    layouts=json.loads((SRC/'decal_layouts.json').read_text());N=2048;stats=[]
    for t in themes():
        folder=OUT/'Textures/Themes'/t['id'];folder.mkdir(parents=True,exist_ok=True)
        (SRC/'Revisions/R02/BadgeArtwork').mkdir(parents=True,exist_ok=True)
        patch(t,1.4).save(SRC/'Revisions/R02/BadgeArtwork'/f"{t['id']}.png")
        for car in t['cars']:
            atlas=Image.new('RGBA',(N,N));used=[]
            groups=layout_groups(layouts[str(car)])
            for idx,i in enumerate(groups):
                x,y,X,Y=i['uv'];ox,oy,oX,oY=x,y,X,Y
                # Overlapping supplier islands otherwise reveal fragments of another badge.
                # Keep each emblem inside the largest part exclusive to its own UV island.
                for j,other in enumerate(groups):
                    if j==idx:continue
                    u,v,U,V=other['uv']
                    if max(x,u)>=min(X,U) or max(y,v)>=min(Y,V):continue
                    gap=3/N
                    candidates=[(x,y,min(X,u-gap),Y),(max(x,U+gap),y,X,Y),(x,y,X,min(Y,v-gap)),(x,max(y,V+gap),X,Y)]
                    candidates=[r for r in candidates if r[2]>r[0] and r[3]>r[1]]
                    if candidates:x,y,X,Y=max(candidates,key=lambda r:(r[2]-r[0])*(r[3]-r[1]))
                box=(round(x*N),round((1-Y)*N),round(X*N),round((1-y)*N));w,h=box[2]-box[0],box[3]-box[1]
                if min(w,h)<8:continue
                if any(max(box[0],b[0])<min(box[2],b[2]) and max(box[1],b[1])<min(box[3],b[3]) for b in used):continue
                used.append(box)
                # Fill the supported surface instead of leaving most of the decal plane empty.
                ratio=i['ratio']*((X-x)/(oX-ox))/((Y-y)/(oY-oy))
                art=patch(t,max(.5,min(4.0,ratio))).resize((w,h),Image.Resampling.LANCZOS)
                dv=np.array(i['dv']);up=dv[1] if abs(dv[1])>abs(dv[2])*.5 else -dv[2]
                if up<0:art=art.transpose(Image.Transpose.ROTATE_180)
                for dx in [-N,0,N]:
                    for dy in [-N,0,N]:
                        px,py=box[0]+dx,box[1]+dy
                        if px<N and py<N and px+w>0 and py+h>0:atlas.alpha_composite(art,(px,py))
            target=folder/f'C{car:03d}_ThemeDecals.png';atlas.save(target)
            before=SRC/'Revisions/R01/Collection/Textures/Themes'/t['id']/target.name
            old=np.array(Image.open(before).getchannel('A'));new=np.array(atlas.getchannel('A'))
            stats.append({'id':f"TR-C{car:03d}-{t['id']}-V01",'oldOpaquePixels':int((old>128).sum()),'newOpaquePixels':int((new>128).sum()),'coverageMultiplier':round(float((new>128).sum()/max(1,(old>128).sum())),2)})
    (SRC/'Revisions/R02/decal_coverage.json').write_text(json.dumps(stats,indent=2))
    print('Refined',len(stats),'atlases; minimum coverage gain',min(s['coverageMultiplier'] for s in stats))
if __name__=='__main__':main()
