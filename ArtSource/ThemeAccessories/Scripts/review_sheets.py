"""Make review sheets from actual Unity renders without changing car crops between revisions."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
import json,math
P=Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass';C=P/'Assets/TinyRiders/ThemedCollection'
cat=json.loads((C/'Data/Themes/variant_catalog.json').read_text())['entries']
OUT=D/'contact_sheets';OUT.mkdir(exist_ok=True)
BG=(232,237,242)
def sheet(items,path,cols=5,cell=256,labels=True):
    h=cell+22 if labels else cell
    out=Image.new('RGB',(cols*cell,math.ceil(len(items)/cols)*h),BG);d=ImageDraw.Draw(out)
    for j,(image,label) in enumerate(items):
        if isinstance(image,(str,Path)):image=Image.open(image).convert('RGB')
        image=image.copy();image.thumbnail((cell,cell),Image.Resampling.LANCZOS)
        x=(j%cols)*cell;y=(j//cols)*h;out.paste(image,(x+(cell-image.width)//2,y))
        if labels:d.text((x+5,y+cell+3),label,fill='#233448')
    path.parent.mkdir(parents=True,exist_ok=True);out.save(path)
def main():
    completed=[e for e in cat if (D/'before_after/after'/(e['collectibleId']+'.png')).exists()]
    reps=[];tiny128=[];tiny64=[]
    for theme in dict.fromkeys(e['themeId'] for e in completed):
        rows=[e for e in completed if e['themeId']==theme];reps.append(rows[0])
        for view in ['Front','Side','Elevated','Clay']:
            paths=[(D/('before_after/after' if view=='Front' else 'views/'+view)/(e['collectibleId']+'.png'),e['baseCarId']) for e in rows]
            sheet(paths,OUT/(theme+'_'+view+'.png'),cols=4,cell=256)
        pairs=[]
        for e in rows:
            for rev in ['before','after']:pairs.append((D/'before_after'/rev/(e['collectibleId']+'.png'),e['baseCarId']+' '+rev))
        sheet(pairs,OUT/(theme+'_BeforeAfter.png'),cols=4,cell=256)
    for e in completed:
        ident=e['collectibleId'];before=Image.open(D/'before_after/before'/(ident+'.png')).convert('RGB');after=Image.open(D/'before_after/after'/(ident+'.png')).convert('RGB')
        # Car width is measured from the before image. Same scale is retained in after.
        bg=Image.new('RGB',before.size,before.getpixel((0,0)))
        mask=ImageChops.difference(before,bg).convert('L').point(lambda x:255 if x>12 else 0);box=mask.getbbox();width=box[2]-box[0]
        for target in [128,64]:
            factor=target/width;dim=(round(after.width*factor),round(after.height*factor));im=after.resize(dim,Image.Resampling.LANCZOS)
            folder=D/'thumbnails'/str(target);folder.mkdir(parents=True,exist_ok=True);im.save(folder/(ident+'.png'))
            if e in reps:(tiny128 if target==128 else tiny64).append((im,''))
    for view in ['Front','Clay']:
        sheet([(D/('before_after/after' if view=='Front' else 'views/'+view)/(e['collectibleId']+'.png'),e['themeId']) for e in reps],OUT/('AllThemes_'+view+'.png'),cols=5,cell=256)
    sheet([(D/'before_after/after'/(e['collectibleId']+'.png'),'') for e in reps],OUT/'MixedNamesHidden.png',cols=5,cell=256,labels=False)
    sheet(tiny128,OUT/'Native128.png',cols=5,cell=256,labels=False);sheet(tiny64,OUT/'Native64.png',cols=5,cell=128,labels=False)
    for j in range(0,len(completed),25):
        group=completed[j:j+25]
        sheet([(D/'before_after/after'/(e['collectibleId']+'.png'),e['collectibleId']) for e in group],OUT/('AllVariants_%02d.png'%(j//25+1)),cols=5,cell=220)
    print('Rendered review sheets for',len(completed),'variants,',len(reps),'themes')
if __name__=='__main__':main()
