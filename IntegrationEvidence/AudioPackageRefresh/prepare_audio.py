"""Inspect and prepare working copies of the user's imported audio; never edits pack originals."""
from pathlib import Path
import struct, json, wave, hashlib, sys
import numpy as np

PROJECT=Path(__file__).resolve().parents[2]
REPORT=Path(__file__).resolve().parent
MOBILE=PROJECT/'Assets/Mobile Games Bundle'
FX=PROJECT/'Assets/2000 Game Sound FX'

def source(folder, name):
    found=list(folder.rglob(name+'.wav'))
    if len(found)!=1: raise ValueError(f'{name}: expected one source, found {len(found)}')
    return found[0]

def read_audio(path):
    data=path.read_bytes()
    if data[:4]!=b'RIFF' or data[8:12]!=b'WAVE': raise ValueError(f'Not RIFF/WAVE: {path}')
    pos=12;fmt=raw=None
    while pos+8<=len(data):
        kind,size=struct.unpack_from('<4sI',data,pos);pos+=8
        if kind==b'fmt ':fmt=data[pos:pos+size]
        elif kind==b'data':raw=data[pos:pos+size]
        pos+=size+(size%2)
    if fmt is None or raw is None:raise ValueError(f'Missing WAV chunks: {path}')
    encoding,channels,rate,_,_,bits=struct.unpack_from('<HHIIHH',fmt)
    if encoding==65534:encoding=struct.unpack_from('<H',fmt,24)[0]
    if encoding==3:
        x=np.frombuffer(raw,dtype='<f4' if bits==32 else '<f8').astype(np.float64)
    elif encoding==1 and bits==24:
        b=np.frombuffer(raw,dtype=np.uint8).reshape(-1,3).astype(np.int32)
        n=b[:,0]|(b[:,1]<<8)|(b[:,2]<<16);n=(n^0x800000)-0x800000;x=n.astype(np.float64)/8388608
    elif encoding==1 and bits in (16,32):x=np.frombuffer(raw,dtype='<i2' if bits==16 else '<i4').astype(np.float64)/(2**(bits-1))
    else:raise ValueError(f'Unsupported encoding {encoding}/{bits}: {path}')
    return x.reshape(-1,channels),rate,bits

def describe(path):
    x,rate,bits=read_audio(path)
    active=np.flatnonzero(np.max(np.abs(x),axis=1)>.0001)
    return dict(source=path.relative_to(PROJECT).as_posix(),rate=rate,bits=bits,channels=x.shape[1],frames=len(x),seconds=len(x)/rate,peak=float(np.max(np.abs(x))),rms=float(np.sqrt(np.mean(x*x))),leadingSilence=(int(active[0])/rate if len(active) else len(x)/rate),trailingSilence=((len(x)-int(active[-1])-1)/rate if len(active) else len(x)/rate))

def selections():
    match=MOBILE/'3D Match Games Sound Effects and Music Pack'
    ui=MOBILE/'Cute UI _ Interact Sound Effects Pack'
    zen=MOBILE/'Zen Games Sound Effects and Music Pack'
    mini=MOBILE/'Mini Games Music and Sound Effects Pack'
    return {
      **{f'Footstep_{i}':source(mini,f'SFX_Footstep_Concrete_{i}') for i in range(1,5)},
      **{f'Pickup_{i}':source(MOBILE/'Runner Games Sound Effects and Music Pack Vol.1',f'SFX_Collect_Stack_Plastic_{i}') for i in range(1,4)},
      'Place_1':source(match,'SFX_Object_Drop_Plastic_1'),
      'Place_2':source(match,'SFX_Object_Drop_Plastic_2'),
      'Place_3':source(match,'SFX_Object_Place_Generic_2'),
      **{f'Throw_{i}':source(zen,f'SFX_Swoosh_{i}') for i in range(1,4)},
      'Jump':source(mini,'SFX_Jump'),
      'Refuse':source(MOBILE/'Match 3 Games Sound Effects and Music Pack','SFX_UI_Click_Locked_1'),
      'WrongSection':source(ui,'SFX_UI_Error'),
      'Complete':source(ui,'SFX_UI_Success_Bright_Pop_1'),
      'Hurt':source(FX,'IMPACT Thump Thud 01'),
      'Sweep':source(ui,'SFX_UI_Swipe_Swoosh_Medium_1'),
      'Shake':source(match,'SFX_Object_Rattle_Pop_1'),
      'UIClick':source(ui,'SFX_UI_Button_Click_Select_1'),
      'Menu_LifeIsSimple':source(MOBILE/'Casual Fun Music Pack','MUSC_Life_Is_Simple_MenuTheme_CMajor_120BPM'),
      'Gameplay_LifeIsSimple':source(MOBILE/'Casual Fun Music Pack','MUSC_Life_Is_Simple_GameplayTheme_CMajor_123BPM'),
    }

def prepare(selected):
    staged=REPORT/'Prepared'
    records=[]
    for role,path in selected.items():
        x,rate,bits=read_audio(path)
        music=role.startswith(('Menu_','Gameplay_'))
        original_frames=len(x)
        start=0;end=len(x)
        if not music:
            x=x.mean(axis=1,keepdims=True)
            active=np.flatnonzero(np.max(np.abs(x),axis=1)>.0001)
            if len(active):start=max(0,int(active[0])-(rate*3//1000));end=min(len(x),int(active[-1])+rate*25//1000+1)
            x=x[start:end]
        original_rms=float(np.sqrt(np.mean(x*x)))
        peak=float(np.max(np.abs(x)))
        target=.1 if music else (.063 if role.startswith('Footstep_') else .126)
        gain=min(target/max(original_rms,1e-9),(.7 if music else .5)/max(peak,1e-9))
        x*=gain
        ramp=min(int(rate*(.002 if music else .001)),len(x)//2)
        x[:ramp]*=np.linspace(0,1,ramp)[:,None];x[-ramp:]*=np.linspace(1,0,ramp)[:,None]
        folder='Music' if music else 'SFX'
        dest=staged/folder/(role+'.wav');dest.parent.mkdir(parents=True,exist_ok=True)
        pcm=np.rint(np.clip(x,-1,1-1/32768)*32768).astype('<i2')
        with wave.open(str(dest),'wb') as w:w.setnchannels(x.shape[1]);w.setsampwidth(2);w.setframerate(rate);w.writeframes(pcm.tobytes())
        records.append(dict(role=role,asset='Assets/00_Project/Audio/PackageSelection/'+folder+'/'+role+'.wav',source=path.relative_to(PROJECT).as_posix(),sourceBits=bits,sourceFrames=original_frames,sampleRate=rate,channels=x.shape[1],frames=len(x),duration=len(x)/rate,trimStartFrames=start,trimEndFrames=original_frames-end,gainDb=float(20*np.log10(gain)),peakDb=float(20*np.log10(np.max(np.abs(x)))),rmsDb=float(20*np.log10(np.sqrt(np.mean(x*x)))),clippedSamples=int(np.sum(np.abs(pcm.astype(np.int32))>=32767)),boundaryMax=int(np.max(np.abs(pcm[[0,-1]].astype(np.int32)))),sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
    manifest=REPORT/'package-selection.json'
    if '--effects-only' in sys.argv and manifest.exists():
        records += [r for r in json.loads(manifest.read_text(encoding='utf-8')) if r['role'].startswith(('Menu_','Gameplay_'))]
    manifest.write_text(json.dumps(records,indent=2),encoding='utf-8')
    print(f'Prepared {len(records)} working copies; {sum(r["clippedSamples"] for r in records)} clipped samples; originals unchanged.')

if __name__=='__main__':
    REPORT.mkdir(exist_ok=True)
    selected=selections()
    if '--effects-only' in sys.argv:selected={k:v for k,v in selected.items() if not k.startswith(('Menu_','Gameplay_'))}
    rows=[dict(role=role,**describe(path)) for role,path in selected.items()]
    (REPORT/'source-analysis.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    for r in rows:print(f"{r['role']}: {r['seconds']:.3f}s {r['rate']}Hz {r['bits']}bit {r['channels']}ch peak={r['peak']:.3f} rms={r['rms']:.3f} silence={r['leadingSilence']:.3f}/{r['trailingSilence']:.3f}s")
    if '--prepare' in sys.argv:prepare(selected)
