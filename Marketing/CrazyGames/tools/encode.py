"""Encode and inspect the Unity captures. Run with the bundled Python runtime."""
import json, subprocess, sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FFMPEG = next((ROOT / 'tools/python/imageio_ffmpeg/binaries').glob('ffmpeg*.exe'))

def run(args):
    subprocess.run([str(FFMPEG), '-hide_banner', '-y', *map(str,args)], check=True)

def encode():
    (ROOT/'Deliverables').mkdir(exist_ok=True)
    (ROOT/'SourceClips').mkdir(exist_ok=True)
    report={}
    for name, size in [('landscape',(1920,1080)),('portrait',(1080,1620))]:
        frames=ROOT/'Frames'/name
        expected=[frames/f'{i:05}.png' for i in range(600)]
        assert all(p.exists() for p in expected), f'Missing frames: {name}'
        assert all(Image.open(p).size==size for p in expected), f'Wrong capture size: {name}'
        source=ROOT/'SourceClips'/f'{name}-gameplay.mp4'
        run(['-framerate','30','-i',frames/'%05d.png','-frames:v','600','-an','-c:v','libx264','-preset','slow','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',source])
        cover=ROOT/'Deliverables'/f'cover-{name}.png'
        final=ROOT/'Deliverables'/f'toy-car-sort-{name}.mp4'
        # Full frame cover dissolves into the first pickup; a matching closing
        # cover makes the hover loop join cleanly without a black/logo-only card.
        filters=(f'[1:v]scale={size[0]}:{size[1]},setsar=1,format=rgba,split=2[a][b];'
                 '[a]fade=t=out:st=0.15:d=0.15:alpha=1[opening];'
                 '[b]fade=t=in:st=19.7:d=0.3:alpha=1[closing];'
                 '[0:v][opening]overlay=0:0:enable=lt(t\\,0.3)[intro];'
                 '[intro][closing]overlay=0:0:enable=gte(t\\,19.7),format=yuv420p[out]')
        run(['-i',source,'-loop','1','-framerate','30','-i',cover,'-filter_complex',filters,'-map','[out]','-t','20','-r','30','-an','-c:v','libx264','-preset','slow','-crf','18','-movflags','+faststart',final])
        # Full decode verifies the delivered stream, including its final frames.
        result=subprocess.run([str(FFMPEG),'-hide_banner','-i',str(final),'-f','null','-'],capture_output=True,text=True)
        assert result.returncode==0, result.stderr
        assert 'Audio:' not in result.stderr
        assert final.stat().st_size<50_000_000
        assert 'Duration: 00:00:20.00' in result.stderr
        report[name]={'resolution':size,'fps':30,'frames':600,'seconds':20,'bytes':final.stat().st_size,'audio':False,'fullDecodePassed':True}
        (ROOT/f'{name}-decode.txt').write_text(result.stderr)
        contact(name, final)
    (ROOT/'validation.json').write_text(json.dumps(report,indent=2))

def contact(name, video=None):
    qa=ROOT/'QA';qa.mkdir(exist_ok=True)
    times=[0,1,2,3.3,4.5,5.5,7,8.3,10.5,12,13.3,14.5,15.5,17,18.5,19.9]
    thumbs=[]
    for i,t in enumerate(times):
        if video:
            path=qa/f'{name}-{i:02}.jpg'
            run(['-ss',t,'-i',video,'-frames:v','1','-q:v','2',path])
        else:
            path=ROOT/'Frames'/name/f'{int(t*30)//30*30:05}.png'
        im=Image.open(path).convert('RGB');im.thumbnail((384,300))
        thumbs.append((im,t))
    sheet=Image.new('RGB',(4*400,4*330),(20,25,35));draw=ImageDraw.Draw(sheet)
    for i,(im,t) in enumerate(thumbs):
        x=(i%4)*400;y=(i//4)*330
        sheet.paste(im,(x+(400-im.width)//2,y+20));draw.text((x+12,y+4),f'{t:.1f}s',fill='white')
    sheet.save(qa/f'{name}-contact.jpg',quality=93)

if __name__=='__main__':
    if len(sys.argv)>1 and sys.argv[1]=='contact':contact(sys.argv[2])
    else:encode()
