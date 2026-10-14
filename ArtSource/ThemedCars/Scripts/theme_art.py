"""Original, deterministic vector icons rasterized into supplier UV layouts. No downloaded art."""
from PIL import Image, ImageDraw
import pathlib, json, math
P=pathlib.Path(__file__).resolve().parents[3]
OUT=P/'Assets/TinyRiders/ThemedCollection'
SRC=P/'ArtSource/ThemedCars'
# Each selection is deliberate; 20 families x 7/8 cars = 150 collectibles.
ROWS=[
('PIRATE','Treasure Crew','#8F2940','#F4C458','#FFF1CA','skull',[1,3,5,6,8,9,12,15]),
('ROYAL','Royal Guard','#5537A5','#F4C85A','#FCF2D4','crown',[1,2,4,5,9,10,13,15]),
('SPACE','Orbit Club','#D9E9F1','#183456','#3CE4EF','planet',[1,2,3,7,10,11,13,14]),
('ALIEN','Cosmic Visitors','#83D844','#663CB7','#E9FFB2','alien',[2,3,4,7,10,11,14,15]),
('BEACH','Sunny Coast','#FFB577','#28B8BD','#FFF1B5','sun',[1,3,5,6,8,9,12,15]),
('JUNGLE','Wild Canopy','#3D9B65','#EDD35A','#DBF5BC','leaf',[3,5,6,8,9,12,14,15]),
('SPOOKY','Boo Crew','#6F42AF','#FF9A38','#F5F1DD','ghost',[1,2,4,5,7,10,13,15]),
('CANDY','Sugar Swirl','#F5AFCF','#8F6DD2','#FFF4CE','candy',[1,2,3,4,7,10,11,13]),
('FOOD','Snack Stop','#F7BD52','#E65C49','#FFF0BD','burger',[3,4,5,6,8,9,12,15]),
('FIRE','Lava Flow','#E85E36','#642D3A','#FFD85B','flame',[1,2,6,7,10,11,13,14]),
('ICE','Snow Patrol','#A3DCF0','#557AC7','#FFFFFF','snow',[2,3,4,6,9,12,14]),
('OCEAN','Reef Friends','#2CBAB6','#276AAB','#FFE3A4','fish',[1,3,5,8,9,12,15]),
('CONSTRUCTION','Little Builders','#F4BD37','#435364','#FFF1B5','helmet',[4,5,6,8,9,12,15]),
('RESCUE','Helping Hands','#F6F0DA','#DF6752','#55BECA','heart',[2,4,5,6,8,9,12]),
('WIZARD','Moon Magic','#283F78','#AF91DF','#FFE4A7','hat',[1,2,4,7,10,13,15]),
('ROBOT','Circuit Buddies','#6DB9D1','#344A68','#F8D95E','robot',[1,2,7,10,11,13,14]),
('MUSIC','Rhythm Ride','#DA69B1','#534488','#F5DE73','note',[1,2,3,7,10,11,13]),
('SPORT','Play Ball','#59B8D7','#EF8651','#FFFFFF','ball',[1,2,3,7,10,11,14]),
('FARM','Country Patch','#B7D669','#BF5746','#FFF0BA','apple',[3,5,6,8,9,12,15]),
('PARTY','Confetti Club','#AA8BE2','#EF83B4','#F9DB6C','balloon',[1,2,4,7,10,11,13]),
]

class Art:
    def __init__(self):
        self.im=Image.new('RGBA',(512,512));self.d=ImageDraw.Draw(self.im);self.svg=[]
    def ellipse(self,box,fill):
        self.d.ellipse(tuple(v*4 for v in box),fill=fill)
        x,y,X,Y=box;self.svg.append(f'<ellipse cx="{(x+X)/2}" cy="{(y+Y)/2}" rx="{(X-x)/2}" ry="{(Y-y)/2}" fill="{fill}"/>')
    def poly(self,pts,fill):
        self.d.polygon([(x*4,y*4) for x,y in pts],fill=fill)
        self.svg.append(f'<polygon points="'+ ' '.join(f'{x},{y}' for x,y in pts)+f'" fill="{fill}"/>')
    def line(self,pts,fill,width=5):
        self.d.line([(x*4,y*4) for x,y in pts],fill=fill,width=width*4,joint='curve')
        self.svg.append(f'<polyline points="'+ ' '.join(f'{x},{y}' for x,y in pts)+f'" fill="none" stroke="{fill}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round"/>')
    def rect(self,box,fill,r=0):
        self.d.rounded_rectangle(tuple(v*4 for v in box),radius=r*4,fill=fill)
        x,y,X,Y=box;self.svg.append(f'<rect x="{x}" y="{y}" width="{X-x}" height="{Y-y}" rx="{r}" fill="{fill}"/>')
    def star(self,x,y,r,fill):
        self.poly([(x+math.sin(i*math.pi/5)*r*(1 if i%2==0 else .43),y-math.cos(i*math.pi/5)*r*(1 if i%2==0 else .43)) for i in range(10)],fill)

def icon(kind,accent,cream):
    a=Art(); ink='#263447'; f=cream; c=accent
    if kind=='skull':
        a.line([(30,92),(98,111)],c,9);a.line([(30,111),(98,92)],c,9)
        a.ellipse((25,15,103,84),f);a.rect((43,65,85,94),f,5)
        a.ellipse((40,41,58,63),ink);a.ellipse((70,41,88,63),ink);a.poly([(64,64),(57,75),(71,75)],ink)
        a.line([(57,81),(57,93)],ink,3);a.line([(71,81),(71,93)],ink,3)
        a.poly([(28,33),(37,13),(91,13),(101,33)],c)
    elif kind=='crown':
        a.poly([(21,35),(45,56),(64,22),(83,56),(107,35),(98,94),(30,94)],c)
        a.rect((30,92,98,104),f,4)
        for x,y in [(21,32),(64,19),(107,32)]:a.ellipse((x-7,y-7,x+7,y+7),f)
        a.poly([(64,56),(73,70),(64,84),(55,70)],ink)
    elif kind=='planet':
        a.ellipse((32,28,101,97),c);a.ellipse((44,37,62,55),f);a.ellipse((69,64,88,83),f)
        a.line([(17,85),(34,88),(74,75),(110,48),(114,38),(103,35)],f,8)
        a.star(22,24,12,f);a.star(109,100,9,c)
    elif kind=='alien':
        a.ellipse((24,17,104,92),f);a.poly([(29,65),(99,65),(64,111)],f)
        a.poly([(36,46),(58,55),(57,74),(43,68)],ink);a.poly([(92,46),(70,55),(71,74),(85,68)],ink)
        a.line([(57,88),(71,88)],ink,4);a.line([(46,19),(37,8)],c,5);a.line([(82,19),(91,8)],c,5)
    elif kind=='sun':
        for t in range(10):
            q=t*math.tau/10;a.line([(64+41*math.cos(q),59+41*math.sin(q)),(64+52*math.cos(q),59+52*math.sin(q))],c,7)
        a.ellipse((32,27,96,91),f);a.ellipse((46,50,53,59),ink);a.ellipse((75,50,82,59),ink)
        a.line([(50,70),(64,76),(78,70)],ink,4)
        a.line([(17,111),(40,104),(64,111),(88,104),(111,111)],c,6)
    elif kind=='leaf':
        a.poly([(64,12),(94,23),(110,51),(103,87),(65,113),(29,93),(17,58),(32,29)],f)
        a.line([(64,109),(64,27)],ink,6)
        for y in [43,64,85]:
            a.line([(38,y-8),(64,y+7),(91,y-8)],c,7)
    elif kind=='ghost':
        a.ellipse((28,15,100,88),f);a.poly([(28,47),(100,47),(108,111),(88,98),(74,112),(57,99),(40,112),(20,100)],f)
        a.ellipse((43,45,55,65),ink);a.ellipse((73,45,85,65),ink);a.ellipse((58,72,70,87),c)
    elif kind=='candy':
        a.poly([(34,47),(12,30),(12,94),(34,80)],c);a.poly([(94,47),(116,30),(116,94),(94,80)],c)
        a.ellipse((29,29,99,99),f)
        pts=[(64+(.5+t/6)*math.cos(t*.14),64+(.5+t/6)*math.sin(t*.14)) for t in range(165)]
        a.line(pts,c,4)
    elif kind=='burger':
        a.ellipse((20,23,108,89),c);a.rect((15,57,113,70),f,7);a.rect((18,72,110,86),ink,7);a.rect((22,89,106,105),c,7)
        for x,y in [(42,40),(65,33),(85,43)]:a.line([(x,y),(x+6,y)],f,3)
        a.poly([(22,68),(47,82),(68,69),(91,81),(108,68)],'#8AC85E')
    elif kind=='flame':
        a.poly([(65,10),(87,43),(92,62),(109,45),(111,82),(96,106),(65,118),(32,107),(17,83),(22,58),(39,70),(40,42)],c)
        a.poly([(64,53),(85,84),(78,104),(63,111),(46,102),(42,87)],f)
    elif kind=='snow':
        for k in range(6):
            t=k*math.pi/3;u=(math.cos(t),math.sin(t));v=(-u[1],u[0]);a.line([(64,64),(64+u[0]*49,64+u[1]*49)],f,7)
            for sign in [-1,1]:a.line([(64+u[0]*30,64+u[1]*30),(64+u[0]*40+v[0]*13*sign,64+u[1]*40+v[1]*13*sign)],f,5)
        a.ellipse((54,54,74,74),c)
    elif kind=='fish':
        a.poly([(36,58),(12,36),(12,94),(36,78)],c);a.ellipse((31,35,115,98),f)
        a.poly([(59,39),(79,16),(89,41)],c);a.ellipse((88,53,100,65),ink)
        a.line([(62,45),(54,64),(62,88)],c,7);a.ellipse((106,14,119,27),c)
    elif kind=='helmet':
        a.ellipse((23,29,105,103),c);a.rect((16,76,112,94),f,6);a.rect((56,20,72,78),f,5)
        a.rect((19,94,109,110),ink,5)
    elif kind=='heart':
        a.ellipse((20,24,69,76),c);a.ellipse((59,24,108,76),c);a.poly([(24,63),(104,63),(64,110)],c)
        a.line([(29,65),(46,65),(54,49),(68,83),(78,65),(100,65)],f,6)
    elif kind=='hat':
        a.poly([(67,9),(91,91),(30,91)],f);a.ellipse((14,83,114,110),c);a.poly([(39,67),(84,67),(89,84),(33,84)],c);a.star(66,49,12,c)
        a.star(104,27,12,f)
    elif kind=='robot':
        a.line([(64,26),(64,12)],c,6);a.ellipse((56,5,72,21),f);a.rect((22,28,106,106),f,14)
        a.rect((12,52,25,85),c,5);a.rect((103,52,116,85),c,5);a.rect((35,44,93,72),ink,7)
        a.ellipse((43,51,54,64),c);a.ellipse((74,51,85,64),c);a.rect((44,84,84,92),ink,3)
    elif kind=='note':
        a.line([(48,91),(48,33),(98,22),(98,79)],f,10);a.line([(48,47),(98,36)],f,8)
        a.ellipse((20,82,53,106),c);a.ellipse((70,70,103,94),c);a.star(20,33,12,f)
    elif kind=='ball':
        a.ellipse((17,17,111,111),f);a.poly([(64,41),(85,55),(77,80),(51,80),(43,55)],ink)
        for k in range(5):
            q=k*math.tau/5-math.pi/2;a.line([(64+28*math.cos(q),64+28*math.sin(q)),(64+46*math.cos(q),64+46*math.sin(q))],c,7)
    elif kind=='apple':
        a.line([(64,40),(68,17)],ink,7);a.poly([(69,26),(81,9),(103,14),(84,32)],c)
        a.ellipse((20,35,77,106),f);a.ellipse((51,35,108,106),f);a.poly([(31,78),(97,78),(87,115),(64,110),(41,115)],f)
        a.line([(37,54),(32,66)],c,5)
    elif kind=='balloon':
        a.ellipse((20,15,73,81),f);a.poly([(46,76),(39,90),(55,90)],f);a.line([(46,91),(53,111)],f,3)
        a.ellipse((65,30,111,90),c);a.poly([(88,84),(82,99),(96,99)],c);a.line([(88,100),(78,118)],c,3)
        a.star(97,12,10,f);a.star(15,97,9,c)
    return a

def themes():
    # C013 only provides a very thin bumper-strip decal: no readable family badge.
    replacements={'ROYAL':3,'SPACE':4,'SPOOKY':3,'CANDY':5,'FIRE':3,'WIZARD':3,'ROBOT':4,'MUSIC':4,'PARTY':3}
    narrow={'PIRATE':2,'BEACH':4,'JUNGLE':4,'FOOD':1,'FIRE':4,'ICE':5,'CONSTRUCTION':3,'RESCUE':3,'FARM':4}
    from revision2_styles import STYLES
    result=[]
    for k,name,paint,trim,light,kind,cars in ROWS:
        t=dict(id='TH_'+k,name=name,paint=paint,trim=trim,light=light,icon=kind,cars=[replacements[k] if c==13 else narrow[k] if c==6 else c for c in cars])
        t.update(STYLES[k]);t['contentRevision']=2;result.append(t)
    return result

def main():
    (SRC/'Icons').mkdir(parents=True,exist_ok=True);(OUT/'Data/Themes').mkdir(parents=True,exist_ok=True)
    for t in themes():
        a=icon(t['icon'],t['trim'],t['light'])
        (SRC/'Icons'/f"{t['id']}.svg").write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">'+''.join(a.svg)+'</svg>')
        a.im.save(SRC/'Icons'/f"{t['id']}.png")
    (OUT/'Data/Themes/theme_recipes.json').write_text(json.dumps({'schemaVersion':1,'themes':themes()},indent=2))
    print('Authored',len(ROWS),'original vector families;',sum(len(t[-1]) for t in ROWS),'variants')
if __name__=='__main__':main()
