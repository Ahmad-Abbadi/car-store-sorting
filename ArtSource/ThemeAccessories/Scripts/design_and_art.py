"""Original accessory briefs and 1024px shared theme atlases; no external artwork."""
import pathlib,json,sys,math,shutil
from PIL import Image,ImageDraw
P=pathlib.Path(__file__).resolve().parents[3];S=P/'ArtSource/ThemeAccessories';O=P/'Assets/TinyRiders/Art/ThemeAccessories';D=P/'TinyRidersProduction/ThemeAccessoryPass'
sys.path.insert(0,str(P/'ArtSource/ThemedCars/Scripts'))
from theme_art import icon
themes=json.loads((P/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text())['themes']
DESIGNS={
'PIRATE':('Tricorn','Three upward folded brim corners surrounding a low crown. Printed skull on the forward folded brim. Dark burgundy felt, gold edging.','roof or rear deck'),
'ROYAL':('Crown','Thick open crown with five broad points, rounded band and printed jewel panels.','roof or rear deck'),
'SPACE':('RingedPlanet','Rounded planet with broad tilted orbital ring and short integrated foot; horizontal printed atmosphere bands.','roof or rear deck'),
'ALIEN':('Saucer','Low lens-shaped saucer, translucent-looking opaque dome, two short sturdy eye-stalk forms and printed lights.','roof or rear deck'),
'BEACH':('Surfboard','Thick rounded pointed surfboard secured diagonally close to the roof on two short attached rails. Printed sun and broad stripes.','roof or rear deck'),
'JUNGLE':('LeafCanopy','Three thick curved tropical leaves arranged as a low canopy. Broad painted veins; no thin branches.','roof or rear deck'),
'SPOOKY':('Pumpkin','Squat pumpkin with eight broad rounded ribs, chunky curved stem and painted jack-o-lantern face.','roof or rear deck'),
'CANDY':('WrappedSweet','Chunky horizontal sweet with solid pleated wrapper ends and printed swirls/stripes.','roof or rear deck'),
'FOOD':('Burger','Toy burger with domed bun, broad patty and lettuce silhouette; sesame seeds and sauce decoration printed.','roof or rear deck'),
'FIRE':('MoldedFlame','Rounded solid flame with three broad swept lobes and painted warm inner-flame shading.','roof or rear deck'),
'ICE':('CrystalCluster','Three stout faceted crystals of staggered height with opaque frosted coloring and printed streaks.','roof or rear deck'),
'OCEAN':('DorsalFin','Thick swept dorsal fin with rounded leading edge and broad fitted root; printed gill/wave detail.','roof or rear deck'),
'CONSTRUCTION':('HardHat','Rounded hard-hat crown, real projecting brim and broad longitudinal reinforcement ridge. Printed hazard emblem.','roof or rear deck'),
'RESCUE':('RescueBeacon','Low rounded medical rescue lightbar with two domed caps and a sculpted central heart-shaped housing; pulse graphic printed.','roof or rear deck'),
'WIZARD':('BentWizardHat','Broad low brim and tapered bent pointed crown, with printed stars. Distinct from three-corner Pirate tricorn.','roof or rear deck'),
'ROBOT':('SensorHead','Chamfered angular sensor housing, recessed dark visor, side modules and one stout antenna. Printed eye pixels and circuits.','roof or rear deck'),
'MUSIC':('Headphones','Thick arched headphone band joining two rounded earcups, a small lower bridge and printed speaker grilles.','roof or rear deck'),
'SPORT':('Football','Recognizable round football silhouette on a low integrated cradle; pentagonal panels are printed.','roof or rear deck'),
'FARM':('FarmHat','Broad gently curved straw-hat brim and rounded pinched crown, with green band and printed straw weave.','roof or rear deck'),
'PARTY':('BalloonCluster','Three rounded balloons on short thick converging supports, with printed confetti and tied bases.','roof or rear deck')}
def main():
    briefs=[]
    for t in themes:
        k=t['id'][3:];name,brief,mount=DESIGNS[k];aid='ACC-'+t['id']+'-PRIMARY'
        briefs.append({'accessoryId':aid,'themeId':t['id'],'themeName':t['name'],'role':'primary','name':name,'designBrief':brief,'mountIntent':mount,'supportingModels':[],'targetTriangles':[400,1500],'status':'designed_not_yet_modeled','paletteSource':'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json'})
        folder=S/'Artwork'/t['id'];folder.mkdir(parents=True,exist_ok=True)
        im=Image.new('RGB',(1024,1024),t['paint']);d=ImageDraw.Draw(im)
        colors=[t['paint'],t['secondary'],t['trim'],t['light'],t['ink'],'#FFFFFF','#181E28','#FFD976']
        for j,c in enumerate(colors):d.rectangle((768+(j%2)*128,(j//2)*128,895+(j%2)*128,127+(j//2)*128),fill=c)
        # Main printed field occupies [0,.75] x [.25,1] in UV space.
        d.rectangle((0,0,767,767),fill=t['paint'])
        if k=='SPACE':
            for y,h,c in [(140,75,t['secondary']),(235,26,t['trim']),(390,95,t['trim']),(510,35,t['secondary']),(640,45,t['light'])]:d.rectangle((0,y,767,y+h),fill=c)
        elif k in ['SPOOKY','ROBOT']:
            bg='#ED8B33' if k=='SPOOKY' else t['ink'];d.rectangle((0,0,767,767),fill=bg)
            if k=='SPOOKY':
                d.polygon([(145,350),(260,205),(320,350)],fill=t['ink']);d.polygon([(450,350),(510,205),(625,350)],fill=t['ink'])
                d.polygon([(150,455),(290,490),(320,455),(380,490),(445,450),(490,490),(625,450),(570,585),(220,585)],fill=t['ink'])
            else:
                d.rounded_rectangle((125,275,315,470),radius=45,fill=t['trim']);d.rounded_rectangle((455,275,645,470),radius=45,fill=t['trim'])
                d.rounded_rectangle((245,550,530,585),radius=14,fill=t['light'])
        elif k=='JUNGLE':
            d.rectangle((0,0,767,767),fill='#369B64');d.line((380,0,380,768),fill='#B5D56E',width=18)
            for y in range(90,750,130):
                d.line((380,y,70,y-110),fill='#79C377',width=12);d.line((380,y,690,y-110),fill='#79C377',width=12)
        elif k=='SPORT':
            d.rectangle((0,0,767,767),fill=t['light'])
            for x,y in [(180,200),(570,200),(380,555),(-10,600),(780,600)]:
                d.polygon([(x+105*math.sin(j*math.tau/5),y-105*math.cos(j*math.tau/5)) for j in range(5)],fill=t['ink'])
        elif k=='FARM':
            d.rectangle((0,0,767,767),fill='#E8C17B')
            for y in range(0,768,36):d.line([(0,y),(767,y+35)],fill='#B78C4D',width=6)
            for x in range(0,768,60):d.line([(x,0),(x+100,767)],fill='#F8DF9E',width=8)
        elif k=='FOOD':
            d.rectangle((0,0,767,767),fill='#E5AD56')
            for j in range(22):
                x=(j*167+53)%740;y=(j*233+41)%740;d.ellipse((x,y,x+24,y+10),fill=t['light'])
        else:
            d.rectangle((0,0,767,767),fill=t['ink'] if k in ['PIRATE','WIZARD','CONSTRUCTION','MUSIC'] else t['secondary'])
            hero=icon(t['icon'],t['trim'],t['light']).im
            hero=hero.crop(hero.getbbox());hero.thumbnail((570,570),Image.Resampling.LANCZOS)
            im.paste(hero,((768-hero.width)//2,(768-hero.height)//2),hero)
            if k in ['CANDY','BEACH','PARTY']:
                for j in range(9):d.line([(j*95,25),(j*95+30,55)],fill=t['trim'],width=15)
        # Broad band tile, used only on physical surfaces, never extra decal planes.
        d.rectangle((0,768,767,1023),fill=t['secondary'])
        for x in range(-150,768,180):d.polygon([(x,1023),(x+70,1023),(x+230,768),(x+160,768)],fill=t['trim'])
        im.save(folder/'AccessoryAtlas.png');target=O/'Textures'/t['id'];target.mkdir(parents=True,exist_ok=True);shutil.copy2(folder/'AccessoryAtlas.png',target/'AccessoryAtlas.png')
    (D/'design_briefs.json').write_text(json.dumps(briefs,indent=2))
    print('Authored',len(briefs),'design briefs and original accessory atlases.')
if __name__=='__main__':main()
