import json,numpy as np,pathlib
from PIL import Image
p=pathlib.Path(__file__).resolve().parents[4]
d=json.loads((p/'ArtSource/ThemedCars/uv_audit.json').read_text())
palette=np.array(Image.open(p/'Assets/ithappy/Modular_Racing_Cars/Textures/Textures1.png').convert('RGB'))
report=[]
for c in d:
 r=next(r for r in c['renderers'] if r['name']==f"Car_{c['car']:02d}")
 uv=np.array(r['uv']);v=np.array(r['vertices']);t=np.array(r['submeshes'][0]).reshape(-1,3);u=uv[t]
 a=u[:,1]-u[:,0];b=u[:,2]-u[:,0];area=np.abs(a[:,0]*b[:,1]-a[:,1]*b[:,0])
 item={'car':c['car'],'uvTrianglePercentiles':np.percentile(area,[0,50,90,100]).tolist(),'collapsedTrianglesFraction':float(np.mean(area<1e-7))}
 centers=u.mean(1);world=v[t].mean(1)
 cells={}
 for cell,pos in zip(np.round(centers,3),world):cells.setdefault(str(cell.tolist()),[]).append(pos.tolist())
 item['uniquePalettePositions']=len(cells)
 weights=np.linalg.norm(np.cross(v[t][:,1]-v[t][:,0],v[t][:,2]-v[t][:,0]),axis=1)/2
 colors=palette[np.clip(((1-centers[:,1])*1023).astype(int),0,1023),np.clip((centers[:,0]*1023).astype(int),0,1023)]
 buckets={}
 for rgb,weight in zip(colors,weights):
  key=tuple((rgb//32).tolist());buckets[key]=buckets.get(key,0)+weight
 item['dominantColorArea']=[{'rgbApprox':(np.array(k)*32+16).tolist(),'area':round(a,2)} for k,a in sorted(buckets.items(),key=lambda x:-x[1])[:8]]
 report.append(item);print(item)
(p/'ArtSource/ThemedCars/Revisions/R02/body_uv_audit.json').write_text(json.dumps(report,indent=2))
