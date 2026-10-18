from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[1]/'Renders'
names=['car_hero','car_front','car_side','car_rear','car_top','car_clay','car_wireframe','display_empty','display_front','display_side','display_rear','display_filled','fit_check_canopy_hidden','detail_crown','detail_checker','detail_wheel','detail_wing','detail_cover','detail_light_ring','low_effects','unity_empty_display','unity_filled_display','unity_car','unity_scale_check']
sheet=Image.new('RGB',(1600,6*290),(25,29,36));draw=ImageDraw.Draw(sheet)
for i,n in enumerate(names):
 p=R/(n+'.png')
 if not p.exists():continue
 im=Image.open(p).convert('RGB');im.thumbnail((394,260));x=(i%4)*400;y=(i//4)*290
 sheet.paste(im,(x+(400-im.width)//2,y));draw.text((x+10,y+265),n,fill=(235,235,235))
sheet.save(R/'QA_ContactSheet.jpg',quality=92)
print(R/'QA_ContactSheet.jpg')
