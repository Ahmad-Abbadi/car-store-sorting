import json, pathlib, numpy as np
from PIL import Image,ImageDraw
from theme_art import P,OUT,SRC,themes,icon

def islands(r):
    uv=np.array(r['uv']);v=np.array(r['vertices']);tri=np.array(r['triangles']).reshape(-1,3)
    # Weld split FBX vertices only where both UV and position agree.
    keys=[tuple(np.round(np.r_[p,q],5)) for p,q in zip(v,uv)]
    par=list(range(len(v)))
    def find(x):
        while par[x]!=x:par[x]=par[par[x]];x=par[x]
        return x
    def join(a,b):par[find(a)]=find(b)
    seen={}
    for i,k in enumerate(keys):
        if k in seen:join(i,seen[k])
        seen[k]=i
    for a,b,c in tri:join(a,b);join(a,c)
    groups={}
    for i in range(len(v)):groups.setdefault(find(i),[]).append(i)
    result=[]
    for ids in groups.values():
        sub=uv[ids];lo=sub.min(0);hi=sub.max(0);extent=hi-lo
        if min(extent)<.00001:continue
        xyz=v[ids];coef=np.linalg.lstsq(np.c_[sub,np.ones(len(sub))],xyz,rcond=None)[0]
        axes=np.linalg.norm(coef[:2],axis=1);size=extent*axes
        tris=[t for t in tri if int(t[0]) in ids]
        area=sum(np.linalg.norm(np.cross(v[t[1]]-v[t[0]],v[t[2]]-v[t[0]]))/2 for t in tris)
        result.append(dict(uv=[*lo.tolist(),*hi.tolist()],size=size.tolist(),ratio=float(size[0]/size[1]),area=float(area),center=xyz.mean(0).tolist(),du=coef[0].tolist(),dv=coef[1].tolist()))
    return sorted(result,key=lambda x:-x['area'])

def main():
    audit=json.loads((SRC/'uv_audit.json').read_text()); layouts={}
    for car in audit:
        r=next(r for r in car['renderers'] if 'Decals' in r['name']);layout=islands(r);layouts[str(car['car'])]=layout
        print(car['car'],[(round(i['area'],2),round(i['ratio'],2),[round(x,3) for x in i['uv']]) for i in layout])
    (SRC/'decal_layouts.json').write_text(json.dumps(layouts,indent=2))
    N=2048
    for t in themes():
        folder=OUT/'Textures/Themes'/t['id'];folder.mkdir(parents=True,exist_ok=True)
        artwork=icon(t['icon'],t['trim'],t['light']).im
        for car in t['cars']:
            atlas=Image.new('RGBA',(N,N));d=ImageDraw.Draw(atlas);used=[];decorated=[]
            layout=layouts[str(car)]
            for i in layout:
                x,y,X,Y=i['uv'];box=(round(x*N),round((1-Y)*N),round(X*N),round((1-y)*N))
                if any(max(box[0],b[0])<min(box[2],b[2]) and max(box[1],b[1])<min(box[3],b[3]) for b in used):continue
                used.append(box)
                w,h=box[2]-box[0],box[3]-box[1]
                if min(w,h)<8:continue
                ratio=i['ratio']
                if .45<ratio<3.8 and i['area']>layout[0]['area']*.13:
                    # Correct physical aspect without changing the supplier mesh or UVs.
                    fw,fh=(.85/ratio,.85) if ratio>1 else (.85,.85*ratio)
                    iw,ih=max(4,int(w*fw)),max(4,int(h*fh))
                    stamp=artwork.resize((iw,ih),Image.Resampling.LANCZOS)
                    # Source UV V mostly follows physical up on sides; top icons align with +Z.
                    dv=np.array(i['dv']);up=dv[1] if abs(dv[1])>abs(dv[2])*.5 else -dv[2]
                    if up<0:stamp=stamp.transpose(Image.Transpose.ROTATE_180)
                    px,py=box[0]+(w-iw)//2,box[1]+(h-ih)//2
                    for dx in [-N,0,N]:
                        for dy in [-N,0,N]:
                            if px+dx<N and py+dy<N and px+dx+iw>0 and py+dy+ih>0:atlas.alpha_composite(stamp,(px+dx,py+dy))
                    decorated.append(box)
                elif ratio>=2.4 and i['area']>layout[0]['area']*.10:
                    # Quiet family motifs replace text strips; never reproduce racing typography.
                    for j in range(5):
                        cx=box[0]+w*(j+1)/6;cy=box[1]+h*.5;rad=min(h*.25,w/18)
                        col=t['light'] if j%2==0 else t['trim']
                        if t['icon']=='candy' or t['icon']=='balloon':d.rounded_rectangle((cx-rad,cy-rad*.4,cx+rad,cy+rad*.4),radius=rad*.4,fill=col)
                        else:d.ellipse((cx-rad,cy-rad,cx+rad,cy+rad),fill=col)
            atlas.save(folder/f'C{car:03d}_ThemeDecals.png')
            if not decorated:print('WARNING no main icon',t['id'],car)
    print('Atlas variants complete')
if __name__=='__main__':main()
