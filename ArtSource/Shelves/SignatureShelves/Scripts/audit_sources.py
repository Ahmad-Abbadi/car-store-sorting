"""Read all delivered Blender sources and verify the saved asset contract."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/SignatureShelves'
reports=[]
for path in sorted((SRC/'Blends').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    assert len([o for o in bpy.data.objects if o.name.startswith('Slot_R')])==15,path.stem
    assert len([o for o in bpy.data.objects if o.name.startswith('Divider_')])==12,path.stem
    assert bpy.data.objects.get('ThemeTopper') is not None,path.stem
    assert bpy.data.objects.get('TopperPost') is not None,path.stem
    assert len([o for o in bpy.data.objects if o.name.startswith('SignatureIcon')])==2,path.stem
    geo=bpy.data.objects['Geometry'];assert all(o.type not in {'LIGHT','CAMERA'} for o in geo.children_recursive)
    for kind in ('BaseColor','Normal','Surface'):
        images=[i for i in bpy.data.images if i.name.startswith(path.stem+'_Signature_'+kind)]
        assert images and all(i.packed_file is not None and tuple(i.size)==(2048,2048) for i in images),path.stem+kind
        master=bpy.data.images.load(str(SRC/'Textures'/f'{path.stem}_Signature_{kind}_4K.png'))
        assert tuple(master.size)==(4096,4096);bpy.data.images.remove(master)
    report=json.loads((SRC/'Reports'/(path.stem+'.json')).read_text())
    assert report['decorationTriangles']<=8000
    reports.append({'theme':path.stem,'slots':15,'topper':True,'headerSymbols':2,'packedSignatureMaps':3,'masterSize':4096,'runtimeSize':2048})
assert len(reports)==40,len(reports)
(ROOT/'IntegrationEvidence/SignatureShelves/blender_source_audit.json').write_text(json.dumps(reports,indent=2))
print('SOURCE_AUDIT_COMPLETE: 40 sources, 80 header symbols, 40 toppers, 120 packed signature maps')
