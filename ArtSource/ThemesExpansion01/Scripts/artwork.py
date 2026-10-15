"""Original expansion artwork. Deterministic, local, editable vectors; no external art."""
import pathlib,json,sys,math
import numpy as np
from PIL import Image,ImageDraw,ImageColor,ImageFilter
P=pathlib.Path(__file__).resolve().parents[3]
S=P/'ArtSource/ThemesExpansion01'; D=P/'TinyRidersProduction/ThemesExpansion01'
O=P/'Assets/TinyRiders/ThemedCollection'; A=P/'Assets/TinyRiders/Art/ThemeAccessories'
sys.path.insert(0,str(P/'ArtSource/ThemedCars/Scripts'))
from theme_art import Art
from refine_atlases import layout_groups
THEMES=json.loads((D/'texture_recipes.json').read_text())['themes']

def emblem(t,secondary=False):
    a=Art(); k=t['id'][3:]; p,b,c,d=t['palette']; dark='#25303A'; cream='#FFF2D8'
    if secondary and k=='BUTTERFLY_GARDEN':
        for j in range(6):q=j*math.tau/6;x=64+28*math.cos(q);y=64+28*math.sin(q);a.ellipse((x-21,y-21,x+21,y+21),c)
        a.ellipse((43,43,85,85),d);return a
    if k=='WILD_WEST':
        if secondary:
            a.line([(65,111),(65,22)],c,13);a.line([(65,67),(32,67),(32,42)],c,11);a.line([(65,82),(93,82),(93,55)],c,11)
        else:
            a.star(64,64,53,d);a.star(64,64,34,p);a.ellipse((49,49,79,79),b)
            for j in range(5):
                q=j*math.tau/5;a.ellipse((59+50*math.sin(q),59-50*math.cos(q),69+50*math.sin(q),69-50*math.cos(q)),b)
    elif k=='DETECTIVE':
        if secondary:
            for x,y in [(40,22),(78,64)]:a.ellipse((x,y,x+21,y+34),b);a.ellipse((x,y+40,x+18,y+53),b)
        else:
            a.line([(79,80),(109,110)],b,17);a.ellipse((13,12,96,95),b);a.ellipse((24,23,85,84),c)
            for r in [12,20,28]:a.line([(55+r*math.cos(q*.12),55+r*.8*math.sin(q*.12)) for q in range(43)],p,3)
    elif k=='NINJA':
        a.poly([(64,8),(75,46),(116,31),(85,65),(117,100),(76,85),(64,121),(51,84),(12,99),(41,65),(12,30),(52,46)],c)
        a.poly([(64,42),(86,64),(64,86),(42,64)],b);a.ellipse((57,57,71,71),p)
        if secondary:a.line([(18,42),(52,32),(95,40)],b,9)
    elif k=='VIKING_LEGENDS':
        a.ellipse((10,10,118,118),c);a.ellipse((21,21,107,107),b)
        for x in [38,64,90]:a.line([(x,27),(x,101)],p,4)
        a.ellipse((47,47,81,81),p);a.ellipse((55,55,73,73),d)
        for j in range(8):q=j*math.pi/4;a.ellipse((60+46*math.cos(q),60+46*math.sin(q),68+46*math.cos(q),68+46*math.sin(q)),d)
    elif k=='ANCIENT_EGYPT':
        if secondary:
            a.poly([(10,76),(38,52),(64,44),(91,53),(118,76),(91,90),(64,95),(37,89)],b);a.ellipse((48,51,80,88),c);a.ellipse((58,55,71,85),d);a.line([(82,88),(70,112),(54,112)],b,6)
        else:
            a.poly([(64,60),(7,30),(19,76),(53,100)],b);a.poly([(64,60),(121,30),(109,76),(75,100)],b)
            for j in range(3):a.line([(16+j*9,49+j*12),(54,76+j*8)],d,5);a.line([(112-j*9,49+j*12),(74,76+j*8)],d,5)
            a.ellipse((44,41,84,105),c);a.ellipse((49,24,79,52),d);a.line([(64,47),(64,99)],b,5)
    elif k=='STEAMPUNK':
        for j in range(12):
            q=j*math.tau/12;u=(math.cos(q),math.sin(q));v=(-u[1],u[0]);a.poly([(64+u[0]*r+v[0]*s,64+u[1]*r+v[1]*s) for r,s in [(36,-10),(55,-10),(55,10),(36,10)]],b)
        a.ellipse((24,24,104,104),b);a.ellipse((41,41,87,87),d);a.ellipse((51,51,77,77),p)
    elif k=='PREHISTORIC':
        if secondary:
            a.line([(20,98),(107,31)],c,19)
            for x,y in [(20,98),(107,31)]:a.ellipse((x-13,y-13,x+13,y+13),c)
        else:a.poly([(20,41),(34,16),(52,50),(64,7),(76,50),(100,19),(111,47),(93,69),(91,98),(67,117),(38,98),(37,70)],c);a.poly([(44,76),(65,61),(88,77),(66,101)],b)
    elif k=='HONEY_BEE':
        a.ellipse((20,13,63,61),b);a.ellipse((65,13,108,61),b);a.ellipse((25,40,103,107),p)
        for y in [54,77]:a.rect((29,y,99,y+12),c,5)
        a.ellipse((40,33,88,65),c);a.line([(48,40),(39,21)],c,6);a.line([(80,40),(89,21)],c,6)
    elif k=='BUTTERFLY_GARDEN':
        for flip in [-1,1]:
            a.ellipse((64+min(0,flip*56),14,64+max(0,flip*56),74),p)
            a.ellipse((64+min(0,flip*44),62,64+max(0,flip*44),112),b)
            for x,y,r in [(32,40,15),(40,84,11)]:xx=64+flip*(64-x);a.ellipse((xx-r,y-r,xx+r,y+r),c)
        a.line([(64,36),(64,105)],'#655079',10);a.line([(64,39),(50,21)],'#655079',4);a.line([(64,39),(78,21)],'#655079',4)
    elif k=='MUSHROOM_GROVE':
        a.rect((49,62,79,113),b,10);a.ellipse((12,14,116,100),p);a.rect((9,58,119,80),p,11)
        for x,y,r in [(35,49,10),(63,32,12),(91,49,10)]:a.ellipse((x-r,y-r,x+r,y+r),b)
        a.line([(35,117),(27,101),(18,110)],c,5);a.line([(91,117),(103,98),(112,110)],c,5)
    elif k=='THUNDERSTORM':
        for box in [(12,30,72,82),(38,12,99,80),(72,32,119,82)]:a.ellipse(box,b)
        a.poly([(65,54),(39,91),(61,90),(48,123),(96,77),(72,78),(86,54)],c)
        for x in [24,106]:a.line([(x,90),(x-6,104)],d,6)
    elif k=='WINTER_HOLIDAYS':
        a.rect((20,46,108,116),b,7);a.rect((14,41,114,59),p,4);a.rect((56,44,72,116),d)
        a.line([(64,43),(24,30),(23,13),(42,12),(64,43),(85,12),(105,14),(104,31),(64,43)],d,10)
    elif k=='RAILWAY':
        a.ellipse((11,11,117,117),c);a.ellipse((23,23,105,105),d)
        for j in range(8):q=j*math.pi/4;a.line([(64,64),(64+38*math.cos(q),64+38*math.sin(q))],c,7)
        a.line([(26,80),(91,57)],b,13);a.ellipse((54,54,74,74),c)
    elif k=='AVIATION':
        a.poly([(61,10),(70,10),(77,55),(119,84),(116,97),(76,78),(76,104),(92,115),(91,122),(64,115),(37,122),(36,115),(52,104),(52,78),(12,97),(9,84),(51,55)],b)
        a.poly([(60,29),(68,29),(71,60),(57,60)],c);a.line([(62,80),(66,105)],d,7)
    elif k=='CINEMA':
        a.ellipse((13,13,115,115),b)
        for j in range(5):q=j*math.tau/5;a.ellipse((53+31*math.sin(q),53-31*math.cos(q),75+31*math.sin(q),75-31*math.cos(q)),p)
        a.ellipse((57,57,71,71),c);a.line([(98,98),(116,114),(116,125)],b,8)
    elif k=='PHOTOGRAPHY':
        a.ellipse((9,9,119,119),b);a.ellipse((20,20,108,108),p)
        for j in range(6):
            q=j*math.pi/3;pts=[(math.cos(q)*r-math.sin(q)*s+64,math.sin(q)*r+math.cos(q)*s+64) for r,s in [(0,19),(28,37),(41,10),(18,-5)]];a.poly(pts,d if j%2 else c)
    elif k=='ART_STUDIO':
        a.ellipse((12,18,116,111),b);a.ellipse((43,71,67,99),p)
        for x,y,col in [(34,45,c),(66,33,d),(94,50,'#62A887'),(94,80,p)]:a.ellipse((x-10,y-10,x+10,y+10),col)
        a.line([(24,116),(91,32)],c,9);a.poly([(84,37),(93,17),(111,11),(105,30),(95,42)],dark)
    elif k=='POST_OFFICE':
        a.rect((9,25,119,106),c,7);a.line([(15,32),(64,75),(113,32)],b,8);a.line([(14,98),(44,69)],b,5);a.line([(114,98),(84,69)],b,5)
        for x in range(14,114,22):a.line([(x,16),(x+10,9)],p if x%44 else d,6)
    elif k=='BARBERSHOP':
        for x,y in [(32,94),(96,94)]:a.ellipse((x-20,y-20,x+20,y+20),c);a.ellipse((x-10,y-10,x+10,y+10),p)
        a.line([(39,82),(92,14)],b,10);a.line([(89,82),(36,14)],c,10);a.ellipse((57,52,71,66),d)
    elif k=='SCIENCE_LAB':
        a.poly([(44,12),(84,12),(84,26),(77,26),(77,53),(113,106),(107,118),(21,118),(15,106),(51,53),(51,26),(44,26)],d)
        a.poly([(42,75),(86,75),(102,106),(98,109),(30,109),(26,106)],b)
        for x,y,r in [(54,90,6),(78,101,5),(66,65,5),(64,39,5)]:a.ellipse((x-r,y-r,x+r,y+r),c)
    return a

def pattern(t,w=768,h=768,accessory=False):
    k=t['id'][3:];p,b,c,d=t['palette'];im=Image.new('RGBA',(w,h),p);dr=ImageDraw.Draw(im)
    sx=w/768;sy=h/768
    def poly(pts,col):dr.polygon([(x*sx,y*sy) for x,y in pts],fill=col)
    def line(pts,col,th=14):dr.line([(x*sx,y*sy) for x,y in pts],fill=col,width=max(1,round(th*min(sx,sy))),joint='curve')
    if k=='HONEY_BEE' and not accessory:
        for row in range(7):
            for col in range(7):
                x=col*140+(70 if row%2 else 0);y=row*121;pts=[(x+70*math.cos(j*math.pi/3),y+70*math.sin(j*math.pi/3)) for j in range(7)];line(pts,d,16)
        line([(0,35),(768,35)],c,34);line([(0,733),(768,733)],c,34)
    elif k in ['BARBERSHOP','CINEMA','ANCIENT_EGYPT','NINJA','HONEY_BEE']:
        for j in range(-8,12):
            if k=='BARBERSHOP':poly([(j*150,0),(j*150+65,0),(j*150+833,768),(j*150+768,768)],[b,c][j%2])
            elif k=='ANCIENT_EGYPT':poly([(0,j*90),(768,j*90),(768,j*90+42),(0,j*90+42)],b)
            elif k=='HONEY_BEE':poly([(0,j*160),(768,j*160),(768,j*160+68),(0,j*160+68)],c)
            else:poly([(j*180,0),(j*180+75,0),(j*180-230,768),(j*180-305,768)],b)
    elif k=='DETECTIVE':
        for j in range(0,769,128):
            line([(j,0),(j,768)],b,45);line([(0,j),(768,j)],b,45);line([(j+43,0),(j+43,768)],d,8);line([(0,j+43),(768,j+43)],d,8)
    elif k=='MUSHROOM_GROVE':
        for x,y,r in [(120,150,63),(440,100,72),(670,270,52),(300,370,65),(100,610,67),(570,590,76)]:dr.ellipse(((x-r)*sx,(y-r)*sy,(x+r)*sx,(y+r)*sy),fill=b)
    elif k=='BUTTERFLY_GARDEN':
        poly([(25,710),(90,100),(340,45),(700,230),(590,680)],b)
        for x,y in [(150,170),(340,150),(530,290),(380,530),(150,520)]:dr.ellipse(((x-55)*sx,(y-75)*sy,(x+55)*sx,(y+75)*sy),fill=c)
        for x,y in [(150,170),(340,150),(530,290),(380,530)]:line([(90,710),(x,y)],d,14)
    elif k=='ART_STUDIO':
        for j,col in enumerate([b,c,d,'#72AC8B']):poly([(40+j*130,80),(115+j*130,45),(225+j*130,690),(155+j*130,718)],col)
        if accessory:
            im=Image.new('RGBA',(w,h),p);dr=ImageDraw.Draw(im)
            for x,y,col in [(170,170,b),(440,110,c),(620,300,d),(540,570,'#72AC8B')]:dr.ellipse(((x-70)*sx,(y-70)*sy,(x+70)*sx,(y+70)*sy),fill=col)
    elif k=='WINTER_HOLIDAYS':
        for j in [160,540]:line([(j,0),(j,768)],b,90);line([(0,j),(768,j)],b,90)
        line([(360,0),(360,768)],d,60);line([(0,380),(768,380)],d,60)
    elif k=='WILD_WEST':
        im=Image.new('RGBA',(w,h),c if not accessory else p);dr=ImageDraw.Draw(im)
        for y in range(80,769,190):
            for x in range(80,769,190):poly([(x,y-30),(x+22,y),(x,y+30),(x-22,y)],b)
        for x in range(20,768,55):line([(x,25),(x+22,25)],b,8);line([(x,743),(x+22,743)],b,8)
    elif k in ['VIKING_LEGENDS','STEAMPUNK','RAILWAY','PHOTOGRAPHY']:
        for y in [35,733]:line([(20,y),(748,y)],c,20)
        for x in range(55,740,95):
            for y in [38,730]:dr.ellipse(((x-9)*sx,(y-9)*sy,(x+9)*sx,(y+9)*sy),fill=d)
        if k=='VIKING_LEGENDS':
            for j in range(8):line([(0,j*105+20),(260,j*105+8),(530,j*105+33),(768,j*105+20)],b,13)
    elif k=='POST_OFFICE':
        im=Image.new('RGBA',(w,h),c);dr=ImageDraw.Draw(im)
        for x in range(-60,800,160):
            for y in [0,690]:poly([(x,y+78),(x+65,y+78),(x+130,y),(x+65,y)],p);poly([(x+80,y+78),(x+130,y+78),(x+195,y),(x+145,y)],b)
    elif k=='THUNDERSTORM':
        for x,y in [(140,160),(410,100),(630,360),(270,650)]:line([(x,y),(x-60,y+115)],d,20)
    elif k=='PREHISTORIC':
        for j in range(5):line([(0,j*180),(160,j*180+65),(330,j*180+5),(530,j*180+85),(768,j*180+20)],b,18)
    elif k=='SCIENCE_LAB':
        for pts in [[(80,100),(220,180),(220,340)],[(560,540),(680,620),(680,760)]]:
            line(pts,b,18)
            for x,y in pts:dr.ellipse(((x-28)*sx,(y-28)*sy,(x+28)*sx,(y+28)*sy),fill=c)
    else:
        for y in [100,570]:line([(0,y+40),(200,y),(430,y+50),(768,y-10)],b,65)
    return im

def patch(t,ratio):
    H=512;W=max(256,round(H*ratio));p,b,c,d=t['palette'];im=pattern(t,W,H);draw=ImageDraw.Draw(im)
    # Broad quiet central body area; rich borders remain visible without competing with the hero.
    quiet=p if t['id'] not in ['TH_MUSHROOM_GROVE','TH_BARBERSHOP','TH_WILD_WEST','TH_BUTTERFLY_GARDEN'] else '#F5E7CC'
    draw.rounded_rectangle((W*.17,H*.10,W*.83,H*.90),radius=H*.14,fill=quiet)
    hero=emblem(t).im;hero=hero.crop(hero.getbbox());s=min(W*.63/hero.width,H*.76/hero.height);hero=hero.resize((round(hero.width*s),round(hero.height*s)),Image.Resampling.LANCZOS);im.alpha_composite(hero,((W-hero.width)//2,(H-hero.height)//2))
    if ratio>1.8:
        support=emblem(t,True).im;support.thumbnail((round(H*.30),round(H*.30)),Image.Resampling.LANCZOS)
        for x in [int(W*.04),int(W*.90-support.width/2)]:im.alpha_composite(support,(x,round(H*.38)))
    return im

def build(t):
    folder=O/'Textures/Themes'/t['id'];folder.mkdir(parents=True,exist_ok=True);srcdir=S/'Artwork'/t['id'];srcdir.mkdir(parents=True,exist_ok=True)
    for support in [False,True]:
        a=emblem(t,support);name='Secondary' if support else 'Emblem';a.im.save(srcdir/(name+'.png'));(srcdir/(name+'.svg')).write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">'+''.join(a.svg)+'</svg>')
    patch(t,1.7).save(srcdir/'Composition.png');pattern(t).save(srcdir/'Pattern.png')
    src=np.array(Image.open(P/'Assets/ithappy/Modular_Racing_Cars/Textures/Textures1.png').convert('RGBA'));rgb=src[:,:,:3].astype(float)/255;value=rgb.max(2);sat=(value-rgb.min(2))/np.maximum(value,.0001)
    for mode in ['Paint','Neutral','LowNeutral','BodyMetal','Trim','Secondary']:
        out=src.copy();target=np.array(ImageColor.getrgb(t['trim'] if mode=='Trim' else t['secondary'] if mode=='Secondary' else t['paint']))/255;mask=(sat>=.18)&(value>=.08)
        if mode in ['Neutral','LowNeutral']:mask|=value>=(.16 if mode=='LowNeutral' else .4)
        if mode=='BodyMetal':mask=value>=.035
        out[:,:,:3][mask]=(target[None,None,:]*(.64+.36*value)[:,:,None]*255).astype('uint8')[mask]
        if mode not in ['Trim','Secondary']:
            panels=(sat<.18)&(value>=.12)&(value<.36);col=np.array(ImageColor.getrgb(t['secondary']))/255;out[:,:,:3][panels]=(col[None,None,:]*(.8+.2*value)[:,:,None]*255).astype('uint8')[panels]
        Image.fromarray(out).save(folder/(mode+'Palette.png'))
    layouts=json.loads((P/'ArtSource/ThemedCars/decal_layouts.json').read_text());N=2048
    for car in t['cars']:
        atlas=Image.new('RGBA',(N,N));used=[];groups=layout_groups(layouts[str(car)])
        for idx,i in enumerate(groups):
            x,y,X,Y=i['uv'];ox,oy,oX,oY=x,y,X,Y
            for j,other in enumerate(groups):
                if j==idx:continue
                u,v,U,V=other['uv']
                if max(x,u)>=min(X,U) or max(y,v)>=min(Y,V):continue
                gap=3/N;options=[(x,y,min(X,u-gap),Y),(max(x,U+gap),y,X,Y),(x,y,X,min(Y,v-gap)),(x,max(y,V+gap),X,Y)];options=[r for r in options if r[2]>r[0] and r[3]>r[1]]
                if options:x,y,X,Y=max(options,key=lambda r:(r[2]-r[0])*(r[3]-r[1]))
            box=(round(x*N),round((1-Y)*N),round(X*N),round((1-y)*N));w,h=box[2]-box[0],box[3]-box[1]
            if min(w,h)<8 or any(max(box[0],b[0])<min(box[2],b[2]) and max(box[1],b[1])<min(box[3],b[3]) for b in used):continue
            used.append(box);ratio=i['ratio']*((X-x)/(oX-ox))/((Y-y)/(oY-oy));art=patch(t,max(.5,min(4,ratio))).resize((w,h),Image.Resampling.LANCZOS);dv=np.array(i['dv']);up=dv[1] if abs(dv[1])>abs(dv[2])*.5 else -dv[2]
            if up<0:art=art.transpose(Image.Transpose.ROTATE_180)
            for dx in [-N,0,N]:
                for dy in [-N,0,N]:
                    px,py=box[0]+dx,box[1]+dy
                    if px<N and py<N and px+w>0 and py+h>0:atlas.alpha_composite(art,(px,py))
        atlas.save(folder/f'C{car:03d}_ThemeDecals.png')
    atlas=Image.new('RGBA',(1024,1024),t['paint']);atlas.alpha_composite(pattern(t,768,768,True));dr=ImageDraw.Draw(atlas)
    if t['id']=='TH_CINEMA':
        dr.rectangle((0,0,767,767),fill=t['paint']);hero=emblem(t).im.resize((460,460),Image.Resampling.LANCZOS);atlas.alpha_composite(hero,(154,160))
        for x in range(25,740,95):dr.rectangle((x,40,x+45,90),fill=t['secondary']);dr.rectangle((x,675,x+45,725),fill=t['secondary'])
        for j in range(-4,9):dr.polygon([(j*160,768),(j*160+80,768),(j*160-176,1024),(j*160-256,1024)],fill=t['secondary'])
    colors=t['palette']+['#FFF2DB','#25303A',t['paint'],t['secondary']]
    for j,col in enumerate(colors):x=768+(j%2)*128;y=(j//2)*128;dr.rectangle((x,y,x+127,y+127),fill=col)
    atlas.save(srcdir/'AccessoryAtlas.png');dest=A/'Textures'/t['id'];dest.mkdir(parents=True,exist_ok=True);atlas.save(dest/'AccessoryAtlas.png')
    print('ART',t['id'],len(t['cars']),flush=True)

if __name__=='__main__':
    chosen=sys.argv[1:] or [t['id'] for t in THEMES]
    for t in THEMES:
        if t['id'] in chosen:build(t)
