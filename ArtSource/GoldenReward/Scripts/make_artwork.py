from PIL import Image, ImageDraw
from pathlib import Path
import json
root=Path(__file__).resolve().parents[3]
art=root/'ArtSource/GoldenReward/Artwork'; tex=root/'Assets/TinyRiders/Art/GoldenReward/Textures'
tex.mkdir(parents=True,exist_ok=True)
# Seamless square motif: UVs control repeats, with no baked reflections.
svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><rect width="512" height="512" fill="#ece8df"/><path d="M0 0h256v256H0z M256 256h256v256H256z" fill="#171a1e"/></svg>'
(art/'Checker_Master.svg').write_text(svg)
im=Image.new('RGB',(512,512),(236,232,223));d=ImageDraw.Draw(im);d.rectangle((0,0,255,255),fill=(23,26,30));d.rectangle((256,256,511,511),fill=(23,26,30));im.save(tex/'TR_Checker_BaseColor.png');im.save(art/'Checker_Master.png')
points=[(-.48,0),(-.6,.64),(-.28,.40),(0,1),(.28,.40),(.6,.64),(.48,0)]
(art/'Crown_Master.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="-70 -110 140 125"><path d="M-48 0 L-60 -64 L-28 -40 L0 -100 L28 -40 L60 -64 L48 0 Z" fill="#eab842"/><g fill="#eab842"><circle cx="-60" cy="-64" r="5"/><circle cx="0" cy="-100" r="5"/><circle cx="60" cy="-64" r="5"/></g></svg>')
(art/'Crown_Profile.json').write_text(json.dumps(points))
im=Image.new('RGB',(512,512),(12,15,18));d=ImageDraw.Draw(im)
for y in range(12,512,24):d.line((0,y,512,y),fill=(45,48,49),width=4)
im.save(tex/'TR_Grille_BaseColor.png')
(art/'Grille_Master.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512"><defs><pattern id="g" width="512" height="24" patternUnits="userSpaceOnUse"><rect width="512" height="24" fill="#0c0f12"/><path d="M0 12H512" stroke="#2d3031" stroke-width="4"/></pattern></defs><rect width="512" height="512" fill="url(#g)"/></svg>')
print(tex)
