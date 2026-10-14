import json,pathlib,numpy as np
from surface_audit import data
P=pathlib.Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass'
assign={r['variantId']:r for r in json.loads((D/'variant_assignments.json').read_text())}
geo={r['baseCarId']:r for r in data}
rows=[]
for row in json.loads((D/'accessory_world_vertices.json').read_text()):
    a=assign[row['id']];e=geo[a['baseCarId']];vs=np.unique(np.round(row['vertices'],6),axis=0)
    max_pen=0;glass_pen=0;contact=999;flags=[]
    for r in e['renderers']:
        if r['name']!='Car_'+e['baseCarId'][2:]:continue
        v=np.array(r['vertices'])
        for sub,ix in enumerate(r['submeshes']):
            tri=v[np.array(ix).reshape(-1,3)];p,b,c=tri[:,0],tri[:,1],tri[:,2]
            den=(b[:,2]-c[:,2])*(p[:,0]-c[:,0])+(c[:,0]-b[:,0])*(p[:,2]-c[:,2]);valid=abs(den)>1e-9;den=np.where(valid,den,1)
            for vv in vs:
                u=((b[:,2]-c[:,2])*(vv[0]-c[:,0])+(c[:,0]-b[:,0])*(vv[2]-c[:,2]))/den
                w=((c[:,2]-p[:,2])*(vv[0]-c[:,0])+(p[:,0]-c[:,0])*(vv[2]-c[:,2]))/den;f=1-u-w
                ids=np.where(valid&(u>=-1e-5)&(w>=-1e-5)&(f>=-1e-5))[0]
                if len(ids):
                    yy=u[ids]*p[ids,1]+w[ids]*b[ids,1]+f[ids]*c[ids,1];gap=vv[1]-max(yy)
                    max_pen=max(max_pen,-gap);contact=min(contact,abs(gap))
                    if '/Glass' in r['materials'][sub]:glass_pen=max(glass_pen,-gap)
    if max_pen>.10:flags.append('Review body penetration')
    if glass_pen>.015:flags.append('Review glass penetration')
    if contact>.045:flags.append('Review mount contact')
    lo=np.array(a['boundsMin']);hi=np.array(a['boundsMax']);size=hi-lo
    rows.append({'variantId':row['id'],'maxVerticalBodyPenetration':round(max_pen,5),'maxGlassPenetration':round(glass_pen,5),'closestVertexSurfaceDistance':round(contact,5),'flags':flags,'assembledSize':size.tolist(),'existingShelfCellSize':[.464,.54,.648],'rawPrefabShelfCompatible':bool(np.all(size<np.array([.464,.54,.648]))),'shelfNote':'The raw themed car was already larger than this separate shelf-ready collection; no car/shelf rescaling performed.'})
(D/'geometric_fit_checks.json').write_text(json.dumps(rows,indent=2))
print('Checked',len(rows),'fits. Flagged',sum(bool(r['flags']) for r in rows))
for r in rows:
    if r['flags']:print(r['variantId'],r['flags'],r['maxVerticalBodyPenetration'],r['closestVertexSurfaceDistance'])
