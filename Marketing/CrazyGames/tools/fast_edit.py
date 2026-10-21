"""12-second alternate trailer: fast pickups, readable landings, punch-in cuts."""
import json, subprocess
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
FF=next((ROOT/'tools/python/imageio_ffmpeg/binaries').glob('ffmpeg*.exe'))
OUT=ROOT/'FastCut';OUT.mkdir(exist_ok=True)
def run(args):
    subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y',*map(str,args)],check=True)

report={}
for orientation,(w,h) in {'landscape':(1920,1080),'portrait':(1080,1620)}.items():
    source=ROOT/'SourceClips'/f'{orientation}-gameplay.mp4'
    cover=ROOT/'Deliverables'/f'cover-{orientation}.png'
    work=OUT/orientation;work.mkdir(exist_ok=True)
    # start, end, output frames, initial zoom, final zoom, focus X, focus Y
    shots=[('cover',0,6,1,1,.5,.5),
           (2.1,2.5,6,1.38,1.43,1,.64),
           (7.1,7.5,6,1.38,1.43,1,.64),
           (12.1,12.5,6,1.38,1.43,1,.64)]
    for base in (0,5,10):
        shots.extend([(base,base+.8,9,1.03,1.08,.5,.58),
                      (base+.8,base+1.6,15,1.08,1.13,.55,.58),
                      (base+1.6,base+2.7,18,1.13,1.17,1,.60),
                      (base+2.7,base+3.6,27,1.17,1.06,1,.53),
                      (base+3.6,base+4.8,15,1.06,1.11,.5,.5)])
    shots.extend([(15,18.5,54,1.04,1.18,.5,.52),
                  (18.5,19.6,18,1.3,1.36,.5,.53),
                  ('cover',0,12,1,1,.5,.5)])
    for i,(start,end,frames,z0,z1,cx,cy) in enumerate(shots):
        segment=work/f'{i:02}.mp4'
        if start=='cover':
            args=['-loop','1','-framerate','30','-i',cover]
            vf=f'scale={w}:{h},setsar=1'
        else:
            args=['-ss',start,'-t',end-start,'-i',source]
            factor=(frames/30)/(end-start)
            # Time remap only the lead-in and recovery; landing gets 27 frames
            # from 0.9 seconds of gameplay, retaining its actual timing.
            vf=(f'setpts={factor}*(PTS-STARTPTS),fps=30,'
                f'zoompan=z=\'{z0}+({z1-z0})*min(on/{max(1,frames-1)},1)\':'
                f'x=\'(iw-iw/zoom)*{cx}\':y=\'(ih-ih/zoom)*{cy}\':'
                f'd=1:s={w}x{h}:fps=30,'
                'eq=contrast=1.035:saturation=1.12:brightness=0.008,'
                'unsharp=5:5:0.25:3:3:0,tpad=stop_mode=clone:stop_duration=0.1,setsar=1')
        run([*args,'-vf',vf,'-frames:v',frames,'-an','-c:v','libx264','-preset','fast','-crf','17','-pix_fmt','yuv420p',segment])
    playlist=work/'concat.txt'
    playlist.write_text('\n'.join(f"file '{i:02}.mp4'" for i in range(len(shots))))
    dest=OUT/f'toy-car-sort-{orientation}-fast.mp4'
    run(['-f','concat','-safe','0','-i',playlist,'-an','-c:v','copy','-movflags','+faststart',dest])
    check=subprocess.run([str(FF),'-hide_banner','-i',str(dest),'-vf','blackdetect=d=0.03:pix_th=0.02:pic_th=0.98','-f','null','-'],capture_output=True,text=True,check=True)
    assert 'Duration: 00:00:12.00' in check.stderr,check.stderr
    assert 'Audio:' not in check.stderr and 'black_start:' not in check.stderr
    assert dest.stat().st_size<50_000_000
    report[orientation]={'seconds':12,'resolution':[w,h],'fps':30,'bytes':dest.stat().st_size,'silent':True,'fullDecodePassed':True,'blackFrames':False}
    sheet=Image.new('RGB',(1600,1000),(18,23,32));draw=ImageDraw.Draw(sheet)
    for j,t in enumerate([0,.4,.65,1.3,2,3.3,4.2,5.5,6.8,8.2,10,11.7]):
        frame=work/f'qa-{j:02}.jpg';run(['-ss',t,'-i',dest,'-frames:v','1','-update','1',frame])
        im=Image.open(frame);im.thumbnail((390,300));x=(j%4)*400;y=(j//4)*333
        sheet.paste(im,(x+(400-im.width)//2,y+22));draw.text((x+10,y+4),f'{t:.2f}s',fill='white')
    sheet.save(OUT/f'{orientation}-review.jpg')
    print(orientation,report[orientation],flush=True)
(OUT/'validation.json').write_text(json.dumps(report,indent=2))
(OUT/'README.txt').write_text('Fast alternate edits: 12 seconds, silent, speed ramps, punch-in cuts, normal-speed landings. CrazyGames advises against fast-forwarding; retain the original Deliverables versions for that requirement. Source footage and reproducible edit script remain in the parent package.\n')
