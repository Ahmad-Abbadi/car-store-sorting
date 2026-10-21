import hashlib,json,re,subprocess,zipfile
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]
ffmpeg=next((root/'tools/python/imageio_ffmpeg/binaries').glob('ffmpeg*.exe'))
report=json.loads((root/'validation.json').read_text())
for name in ['landscape','portrait']:
    video=root/'Deliverables'/f'toy-car-sort-{name}.mp4'
    cmd=[str(ffmpeg),'-hide_banner','-i',str(video),'-vf','blackdetect=d=0.03:pix_th=0.02:pic_th=0.98','-f','null','-']
    result=subprocess.run(cmd,capture_output=True,text=True,check=True)
    assert 'black_start:' not in result.stderr
    assert re.search(r'frame=\s*600\b',result.stderr)
    assert 'Audio:' not in result.stderr
    report[name]['decodedFrames']=600
    report[name]['blackFramesDetected']=False
    report[name]['sha256']=hashlib.sha256(video.read_bytes()).hexdigest()
    # Distinct consecutive captures prove that the player loop advanced.
    hashes=[hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((root/'Frames'/name).glob('*.png'))]
    report[name]['distinctCapturedFrames']=len(set(hashes))
    assert len(hashes)==600 and len(set(hashes))>550
report['covers']={}
for name,size in [('landscape',(1920,1080)),('portrait',(800,1200)),('square',(800,800))]:
    im=Image.open(root/'Deliverables'/f'cover-{name}.png');assert im.size==size
    report['covers'][name]=list(size)
report['restoration']=json.loads((root/'Backup/restoration.json').read_text())
assert report['restoration']['playerPrefsRestored']
(root/'validation.json').write_text(json.dumps(report,indent=2))
with zipfile.ZipFile(root/'Toy-Car-Sort-CrazyGames.zip','w',zipfile.ZIP_DEFLATED) as z:
    for path in (root/'Deliverables').iterdir():z.write(path,path.name)
    z.write(root/'validation.json','validation.json')
print(json.dumps(report,indent=2))
