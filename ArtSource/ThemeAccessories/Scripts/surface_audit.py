import json,pathlib,numpy as np
P=pathlib.Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass'
data=json.loads((D/'baseline/base_geometry.json').read_text())
def hits(e,x,z,body_only=True):
    result=[]
    for r in e['renderers']:
        if body_only and r['name']!='Car_'+e['baseCarId'][2:]:continue
        v=np.array(r['vertices'])
        for sub,ix in enumerate(r['submeshes']):
            tri=v[np.array(ix).reshape(-1,3)];a,b,c=tri[:,0],tri[:,1],tri[:,2]
            den=(b[:,2]-c[:,2])*(a[:,0]-c[:,0])+(c[:,0]-b[:,0])*(a[:,2]-c[:,2]);valid=abs(den)>1e-9
            den=np.where(valid,den,1)
            u=((b[:,2]-c[:,2])*(x-c[:,0])+(c[:,0]-b[:,0])*(z-c[:,2]))/den
            w=((c[:,2]-a[:,2])*(x-c[:,0])+(a[:,0]-c[:,0])*(z-c[:,2]))/den;f=1-u-w
            for idx in np.where(valid&(u>=-1e-5)&(w>=-1e-5)&(f>=-1e-5))[0]:
                n=np.cross(b[idx]-a[idx],c[idx]-a[idx]);n=n/max(np.linalg.norm(n),1e-9)
                result.append({'y':float(u[idx]*a[idx,1]+w[idx]*b[idx,1]+f[idx]*c[idx,1]),'normal':n.tolist(),'renderer':r['path'],'material':r['materials'][sub],'triangle':int(idx),'submesh':sub})
    return sorted(result,key=lambda h:h['y'],reverse=True)
if __name__=='__main__':
    report=[]
    for e in data:
        row=[]
        for z in [-1.6,-1.3,-1,-.7,-.4,-.1,.2,.5,.8,1.1]:
            h=hits(e,0,z)
            row.append((z,round(h[0]['y'],3),pathlib.Path(h[0]['material']).stem) if h else (z,None))
        report.append({'baseCarId':e['baseCarId'],'centerlineSurfaces':row})
    (D/'baseline/surface_samples.json').write_text(json.dumps(report,indent=2))
    for e in report:print(e['baseCarId'],e['centerlineSurfaces'])
