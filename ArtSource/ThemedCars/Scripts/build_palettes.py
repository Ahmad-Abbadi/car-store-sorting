from theme_art import P,OUT,themes
from PIL import Image,ImageColor
import numpy as np
src=np.array(Image.open(P/'Assets/ithappy/Modular_Racing_Cars/Textures/Textures1.png').convert('RGBA'))
rgb=src[:,:,:3].astype(float)/255
value=rgb.max(2);sat=(value-rgb.min(2))/np.maximum(value,.0001)
for t in themes():
    folder=OUT/'Textures/Themes'/t['id'];folder.mkdir(parents=True,exist_ok=True)
    for mode in ['Paint','Neutral','LowNeutral','BodyMetal','Trim','Secondary']:
        out=src.copy();target=np.array(ImageColor.getrgb(t['trim'] if mode=='Trim' else t['secondary'] if mode=='Secondary' else t['paint']))/255
        mask=(sat>=.18)&(value>=.08)
        if mode in ['Neutral','LowNeutral']:mask|=(value>=(.16 if mode=='LowNeutral' else .4))
        if mode=='BodyMetal':mask=value>=.035
        # Preserve supplier light/dark swatch relationships, with stronger toy-like base paint.
        shade=.64+.36*value
        new=target[None,None,:]*shade[:,:,None]
        out[:,:,:3][mask]=(np.clip(new,0,1)*255).astype('uint8')[mask]
        if mode not in ['Trim','Secondary']:
            panels=(sat<.18)&(value>=.12)&(value<.36)
            second=np.array(ImageColor.getrgb(t['secondary']))/255
            panelcolor=second[None,None,:]*(.80+.20*value)[:,:,None]
            out[:,:,:3][panels]=(np.clip(panelcolor,0,1)*255).astype('uint8')[panels]
        Image.fromarray(out).save(folder/(mode+'Palette.png'))
print('120 safe derived palette atlases written')
