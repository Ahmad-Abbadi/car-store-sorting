"""Inspect saved editable Blender sources; --pack-surface also embeds Unity's surface atlas."""
import bpy,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/ThemedShelves'
reports=[]
for path in sorted((SRC/'Blends').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    if '--pack-surface' in sys.argv:
        name=path.stem+'_Surface.png'
        image=bpy.data.images.get(name) or bpy.data.images.load(str(SRC/'Textures'/name))
        image.colorspace_settings.name='Non-Color';image.use_fake_user=True;image.pack()
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(path))
    markers=[o for o in bpy.data.objects if o.name.startswith('Slot_R')]
    assert len(markers)==15,path.stem
    assert len([o for o in bpy.data.objects if o.name.startswith('Divider_')])==12,path.stem
    for suffix in ('_BaseColor.png','_Surface.png'):
        image=bpy.data.images[path.stem+suffix]
        assert image.packed_file is not None,path.stem+suffix
        assert tuple(image.size)==(1024,1024),path.stem+suffix
    geo=bpy.data.objects['Geometry']
    assert all(o.type not in {'LIGHT','CAMERA'} for o in geo.children_recursive)
    reports.append({'theme':path.stem,'slots':15,'packedAtlases':2,'sourceChecked':True})
assert len(reports)==40
(ROOT/'IntegrationEvidence/ThemedShelves/blender_source_audit.json').write_text(json.dumps(reports,indent=2))
print('SOURCE_AUDIT_PASSED: 40 editable sources, 15 markers and 2 packed atlases each')
