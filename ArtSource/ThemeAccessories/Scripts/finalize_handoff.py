"""Finalize metadata only after actual Unity validation and render publication complete."""
import json,pathlib,hashlib,csv,shutil,sys,collections
import numpy as np
from PIL import Image
from review_sheets import sheet
P=pathlib.Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemeAccessoryPass';C=P/'Assets/TinyRiders/ThemedCollection';A=P/'Assets/TinyRiders/Art/ThemeAccessories'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,x):p.write_text(json.dumps(x,indent=2),encoding='utf-8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    first=read(D/'unity_validation_first.json');second=read(D/'unity_validation_second.json');protection=read(D/'source_protection.json')
    assert not first['errors'] and not second['errors'] and all(not x['changed'] for x in protection.values())
    assert first['variantCount']==second['variantCount']==150 and first['variants']==second['variants']
    assert first['materialCount']==second['materialCount']==20 and first['kitCount']==second['kitCount']==20
    beforeguids=read(D/'regeneration_guids_before.json');changed=[f for f,h in beforeguids.items() if sha(P/f)!=h];assert not changed,changed
    regeneration={'status':'passed','beforeValidation':'unity_validation_first.json','afterValidation':'unity_validation_second.json','variants':150,'roots':150,'kitInstances':150,'kitAssets':20,'materialAssets':20,'checkedMetaFiles':len(beforeguids),'changedMetaFiles':changed,'duplicateIds':0}
    write(D/'regeneration_validation.json',regeneration)
    cat=read(C/'Data/Themes/variant_catalog.json');entries=cat['entries'];assign=read(D/'variant_assignments.json');profiles=read(D/'fitting_profiles.json');lib=read(D/'theme_accessory_library.json');geom=read(D/'geometric_fit_checks.json')
    original=read(D/'baseline/Collection/Data/Themes/variant_catalog.json')['entries'];assert {e['collectibleId'] for e in entries}=={e['collectibleId'] for e in original}
    assert len(assign)==150 and len(lib)==20 and len(profiles)==13 and not any(g['flags'] for g in geom)
    controls=[]
    for e in entries:
        ident=e['collectibleId'];base=np.array(Image.open(D/'before_after/before'/(ident+'.png')).convert('RGB')).astype(float);hidden=np.array(Image.open(D/'views/HiddenAccessories'/(ident+'.png')).convert('RGB')).astype(float)
        delta=np.abs(base-hidden);controls.append({'variantId':ident,'meanAbsoluteChannelError':float(delta.mean()),'maxChannelError':float(delta.max()),'exactPixelMatch':bool(np.all(delta==0))})
    write(D/'hidden_accessory_controls.json',controls)
    assert max(x['meanAbsoluteChannelError'] for x in controls)<.1, 'Baseline render drift needs review'
    briefs=read(D/'design_briefs.json');bytheme={x['themeId']:x for x in briefs}
    for r in lib:
        t=r['themeId'];r.update({'kitPrefabPath':f'Assets/TinyRiders/Art/ThemeAccessories/Prefabs/{t}/KIT_{t}.prefab','materialPaths':[f'Assets/TinyRiders/Art/ThemeAccessories/Materials/{t}/Accessory.mat'],'compatibleMountProfiles':sorted({a['profileId'] for a in assign if a['themeId']==t}),'validationStatus':'Blender_mesh_and_Unity_import_fit_review_passed','unityValidation':'unity_validation_second.json','modelReviewImage':f'ArtSource/ThemeAccessories/Renders/{t}.png','designName':bytheme[t]['name'],'geometryBudgetNote':'Deliberately simple shape below the starting budget; no filler geometry.' if r['triangles']<400 else 'Within 400–1500 working target.'})
        bytheme[t]['status']='modeled_exported_imported_fitted_reviewed'
    write(D/'theme_accessory_library.json',lib);write(D/'design_briefs.json',briefs)
    for r in assign:
        ident=r['variantId'];r.update({'fitStatus':'passed_measured_surface_and_three_view_review','unityValidation':'unity_validation_second.json','evidence':{'front':r['afterPreviewPath'],'side':f'{D.relative_to(P).as_posix()}/views/Side/{ident}.png','elevated':f'{D.relative_to(P).as_posix()}/views/Elevated/{ident}.png','clay':f'{D.relative_to(P).as_posix()}/views/Clay/{ident}.png','thumbnail64':f'{D.relative_to(P).as_posix()}/thumbnails/64/{ident}.png'},'shelfCompatibility':'unsupported_raw_scale_preexisting; separate shelf collection uses smaller cars','visualReviewLimitation':'Review is visual/geometry evidence, not user recognition playtesting.'})
    write(D/'variant_assignments.json',assign)
    for p in profiles:
        p.update({'validationStatus':'used_and_reviewed_on_all_assigned_existing_variants','verifiedVariantIds':[a['variantId'] for a in assign if a['baseCarId']==p['baseCarId']],'evidence':'variant_assignments.json; geometric_fit_checks.json; contact_sheets/Side_*.png and Elevated_*.png'})
    write(D/'fitting_profiles.json',profiles)
    # Mixed sheets use ten existing IDs per image, preserving the existing image paths.
    queues=[list(g) for t in dict.fromkeys(e['themeId'] for e in entries) for g in [[e for e in entries if e['themeId']==t]]]
    mixed=[]
    while any(queues):
        for q in queues:
            if q:mixed.append(q.pop(0))
    membership=[]
    for start in range(0,150,10):
        group=mixed[start:start+10];name='Mixed_%02d.png'%(start//10+1);outpath=C/'Preview/MixedGroups'/name
        for rev in ['before','after']:
            dest=D/'before_after/mixed'/rev/name;sheet([(D/'before_after'/rev/(e['collectibleId']+'.png'),'') for e in group],dest,cols=5,cell=256,labels=False)
            if rev=='after':shutil.copy2(dest,outpath)
        for e in group:e['mixedPreviewPath']=outpath.relative_to(P).as_posix()
        membership.append({'path':outpath.relative_to(P).as_posix(),'variantIds':[e['collectibleId'] for e in group],'method':'Composite of actual Unity renders at identical before/after settings; names hidden'})
    write(D/'mixed_membership.json',membership)
    for e in entries:
        e['contentRevision']=3;e['accessoryKitId']='KIT_'+e['themeId'];e['accessoryFitRecord']='TinyRidersProduction/ThemeAccessoryPass/variant_assignments.json'
        e['validationNotes']='R03: shared '+bytheme[e['themeId']]['name']+' kit; original R02 body artwork preserved. Surface fitting, front/side/elevated/clay and 128/64 px car-width review passed. Shelf-scale integration is separate; recognition still requires playtesting.'
    write(C/'Data/Themes/variant_catalog.json',cat)
    oldfields=list(next(csv.DictReader((C/'Data/Themes/variant_catalog.csv').open(encoding='utf-8-sig'))).keys());fields=list(dict.fromkeys(oldfields+['accessoryKitId','accessoryFitRecord']))
    with (C/'Data/Themes/variant_catalog.csv').open('w',newline='',encoding='utf-8') as f:
        w=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');w.writeheader();w.writerows(entries)
    for t in bytheme:
        shutil.copy2(D/'contact_sheets'/(t+'_Front.png'),C/'Preview/ContactSheets'/(t+'.png'))
        rows=[e for e in entries if e['themeId']==t]
        sheet([(D/'thumbnails/128'/(e['collectibleId']+'.png'),e['baseCarId']+' 128px') for e in rows]+[(D/'thumbnails/64'/(e['collectibleId']+'.png'),e['baseCarId']+' 64px') for e in rows],C/'Preview/Multiscale'/(t+'.png'),cols=4,cell=256)
    shutil.copy2(D/'contact_sheets/AllThemes_Front.png',C/'Preview/CollectionOverview.png')
    reps=[next(e for e in entries if e['themeId']==t) for t in bytheme]
    sheet([(D/'before_after'/rev/(e['collectibleId']+'.png'),e['themeId']+' '+rev) for e in reps for rev in ['before','after']],D/'before_after/AllThemes_BeforeAfter.png',cols=4,cell=256)
    # A new R03 comparison supplements the historical R01/R02 comparison.
    shutil.copy2(D/'before_after/AllThemes_BeforeAfter.png',C/'Preview/BeforeAfter_R02_R03.png')
    counts=collections.Counter(e['themeId'] for e in entries);tri=[r['triangles'] for r in lib];table='\n'.join('| '+t+' | '+bytheme[t]['name']+' | '+str(next(r['triangles'] for r in lib if r['themeId']==t))+' | '+str(counts[t])+' |' for t in bytheme)
    bodymin=min(g['maxVerticalBodyPenetration'] for g in geom);bodymax=max(g['maxVerticalBodyPenetration'] for g in geom)
    report=f'''# Theme accessory refinement R03 — validation report

Completed: 20 discovered themes, 20 usable shared kits, 20 unique primary meshes, 0 secondary models, 150 existing variants updated from revision 2 to 3 across 13 base builds. No new collectible IDs; 0 skipped variant fits. C006 and C013 remain outside the existing catalog, as before this pass.

| Existing theme | Primary model | Added triangles | Updated variants |
|---|---|---:|---:|
{table}

## Actual checks

- Blender 5.1 sources and FBX exports: one UV layer and material slot per kit, zero boundary/non-manifold edges, no rigs or negative scale. Per-theme scene-based .blend files include packed original 1024 atlases. Triangle range {min(tri)}–{max(tri)}. Ocean and Ice intentionally use fewer than 400 triangles; their simple silhouette/facets do not need filler geometry.
- Unity 6000.3.22f1 / existing URP 17.3.0: imported mesh scale/axes, UV coverage, shaders, shared prefab instances and geometry budgets checked for all 150. No new runtime behavior, colliders, rigidbodies or animation components.
- All original nodes retain meshes, material assignments, active state, transforms and component types. All 150 prefab GUIDs and catalog IDs retained. Supplier protection: 1,292 files unchanged; existing body material/texture protection: 1,384 files unchanged; 2,338 existing .meta files unchanged; 10 runtime files unchanged.
- Unity's material validation had added _EMISSION/EmissiveIsBlack to 109 derived R02 materials. Restored only these flags through the Material API; final file hashes match the baseline exactly.
- Regeneration completed twice. Both audits report 150 owned roots, 150 shared-kit instances, 20 kit assets, 20 accessory materials, unchanged prefab/mesh/kit GUIDs, no duplicate IDs or roots. See regeneration_validation.json.
- Measured actual body triangles selected 13 fitting profiles. Open-cockpit C010 uses the solid rear deck ahead of the fixed wing. Uniform scales and small concealed mounting embed only. Maximum vertical vertex/body penetration {bodymax:.4f} source units; no glass-penetration or missing-contact flags. This numerical screen complements visual review; it is not a physics collision proof.
- Reviewed actual Unity front, side and elevated images for all 150, all 20 gray-clay representatives, native 64/128 px car-width sheets, and two fixed static pile arrangements. All 150 also have individual clay and perspective renders. Large body art, wheels and windshields remain visible; no optional vendor parts were disabled.
- Controlled before/after uses the same baseline bounds, camera, light, background, scale and exposure. Hidden-accessory controls: {sum(x['exactPixelMatch'] for x in controls)}/150 exact pixel matches; maximum mean absolute 8-bit channel error {max(x['meanAbsoluteChannelError'] for x in controls):.6f}. See hidden_accessory_controls.json.

## Camera and shelf limits

The existing Level Main Camera is at (500, 2.9, -20.857), rotation (0,0,0), FOV 60, aspect 16:9. Captured its existing view read-only and rendered 20 before/after representatives at an observed displayed product pose, with identical camera projection/transform. Tests use an isolated preview and studio lighting; this is not completed scene integration. The chosen existing product is rotated in the current pile, so these views also expose side/underside occlusion. See actual_camera_test.json.

The existing shelf-ready collection is separate and substantially smaller. Its cell is approximately 0.464 × 0.54 × 0.648 units. All 150 unscaled themed prefabs already exceeded those limits before R03 and remain unsupported at that shelf's raw scale. Exact IDs and expanded bounds are in geometric_fit_checks.json and variant_assignments.json. No car/shelf scale, scene, shelf, or gameplay changes were made to force compatibility.

## Visual findings and limits

Pirate's broad three-corner folded hat differs from Wizard's tall bent cone. Space's physical ring, Alien's lens/dome and Robot's angular visor remain different. Ocean's single swept foil differs from Ice's three facets. Fire's lobes differ from Spooky's ribbed pumpkin; Candy's wrappers differ from Food's layered bun. Gray-clay comparisons retain these differences without artwork.

At 64 px, small printed marks are naturally lost. Beach's low board is weaker from a low side view; Music's arch is strongest from front/elevated and narrow edge-on; Rescue's heart is less clear edge-on. Farm and Construction both use hat language, distinguished by brim/crown/ridge, palette and body art. In the piled samples some accessories are occluded by other cars; no claim is made that every theme is visible from every pose. These are visual review results, not evidence of one-second player recognition. Recognition playtesting remains a later integration task.

## Evidence

Start with contact_sheets/AllThemes_Front.png, AllThemes_Clay.png, Native128.png, Native64.png, MixedNamesHidden.png and before_after/AllThemes_BeforeAfter.png. Per-theme sheets and all 150 multi-view renders are beside them. Two pile pairs and 15 mixed pairs retain matching before/after layouts. Baseline recovery is under baseline/Collection with original .meta files and source hashes.
'''
    (D/'validation_report.md').write_text(report,encoding='utf-8')
    (D/'integration_notes.md').write_text('''# Integrating R03 accessories

Use the existing Assets/TinyRiders/ThemedCollection/Prefabs/Themed prefabs and ThemedCarCatalog.asset. Existing product IDs, prefab references, body materials and textures are retained; contentRevision is 3. No runtime attachment script is needed. Each prefab contains ThemeAccessories/KIT_TH_* with a nested shared kit. The neutral kit root preserves Blender's FBX import transform on AccessoryGeometry; do not overwrite that child transform.

Reusable kits, meshes, 1024 atlases and URP materials are under Assets/TinyRiders/Art/ThemeAccessories. Editable scene-based Blender files with packed textures are under ArtSource/ThemeAccessories/Blender. ThemeAccessories_Studio.blend presents the library in a grid; exported meshes and individual source files use a local bottom-center origin.

The authoritative fit records are fitting_profiles.json and variant_assignments.json. They record measured surface, local position/rotation/uniform scale and expanded bounds. The existing shelf system uses a separate smaller collection: do not substitute these raw full-size prefabs without a later explicit integration/clearance pass. No physics or storage rules were changed here.

## Regeneration

Use Unity CLI with the connected project open. Every `unity command` invocation needs `--caller plugin --skill unity-cli`.

1. To reapply existing kits: `unity command run_script --caller plugin --skill unity-cli --file ArtSource/ThemeAccessories/Scripts/RunAccessory.cs --entry RunAccessory.Run --args '["all"]' --timeout_ms 600000 --detach --format json`. Poll the returned job ID to terminal success.
2. For one kit/base: use args `["apply","TH_SPACE","C001,C007,C010"]`. This changes the existing prefab assets in place.
3. Validate with ValidateAccessories.cs / ValidateAccessories.Run and a distinct pass name. Rebuild sheets using `python ArtSource/ThemeAccessories/Scripts/review_sheets.py`. Publish previews with PublishAccessoryReview.cs / PublishAccessoryReview.Run.
4. Run check_protection.py after Unity saves. If Unity's material validator only reintroduces the documented emission/lightmap flags, RestoreFoundationFlags.cs restores them from the baseline through the Material API; inspect any other diff before changing it. Never overwrite manual edits blindly.
5. For geometry edits, use live Blender MCP and model_accessories.py (execute its definitions then `build("TH_SPACE")`, for example), or edit the corresponding source .blend. Export only that accessory with -Z forward/Y up. Do not export the studio's presentation offsets. Existing .meta files must stay in place. Regeneration scripts are deterministic art recipes, not permission to overwrite future manual revisions.

The old R02 ThemedCarBuilder now refuses to recreate a collection containing R03 entries, because doing so would discard fitted overrides. R01/R02 artwork scripts and historical comparisons remain archived.

## Recovery

baseline/Collection is the complete R02 collection snapshot, including original .meta files; baseline/assignments.json and baseline/source_hashes.json record original references and hashes. For an explicit rollback, restore only the affected project-owned files from that snapshot while preserving GUIDs; use Unity asset import/save operations, then validate. Do not restore supplier files or unrelated workspace changes. No automatic rollback was performed.
''',encoding='utf-8')
    state={'stage':'complete','contentRevision':3,'discoveredThemes':list(bytheme),'completedKits':['KIT_'+t for t in bytheme],'primaryModelCount':20,'secondaryModelCount':0,'updatedVariantIds':[e['collectibleId'] for e in entries],'updatedVariantCount':150,'pendingThemes':[],'skippedVariants':[],'blockedJobs':[],'pilot':'pilot_validation.json','sourceProtection':'source_protection.json','regeneration':'regeneration_validation.json','limitations':['Existing separate shelf raw-scale compatibility is unsupported for all 150; no shelf/scene scaling was changed.','Human recognition playtesting remains outside this art production pass.']}
    write(D/'production_state.json',state)
    (D/'RESUME_HERE.md').write_text('''# R03 handoff

Production is complete: 20 shared kits / 20 primary models / 0 secondary models / all 150 existing variants at revision 3. No pending or blocked production jobs. See validation_report.md for actual checks and remaining display/playtesting limits; integration_notes.md for regeneration and recovery.

Open contact_sheets/AllThemes_Front.png and before_after/AllThemes_BeforeAfter.png first. Existing Unity catalog preview paths now show R03. Keep baseline/Collection as the recoverable R02 snapshot.

If work resumes, start by reading production_state.json and checking current hashes against output_hashes.json to detect intervening manual edits. Do not rerun the old R02 collection builder. Do not replace the separate shelf cars or resize them without a new integration request.
''',encoding='utf-8')
    (C/'READ_ME.txt').write_text('''THEMED COLLECTIBLE CARS — CONTENT REVISION 3

150 existing IDs, 20 themes, 13 purchased base cars; original R02 body artwork preserved.
20 original shared physical accessory kits have been fitted in place. No new products.

OPEN FIRST
Preview/CollectionOverview.png — all 20 R03 themes.
Preview/BeforeAfter_R02_R03.png — controlled comparisons.
Preview/ContactSheets — every updated variant.
Preview/Multiscale — native 128/64 px car-width reviews.
Preview/MixedGroups — all 150 variants, ten per names-hidden composite.
Data/Themes/ThemedCarCatalog.asset — same Unity catalog, revision 3.

Full evidence, measured fitting records, baseline recovery, regeneration checks and notes:
TinyRidersProduction/ThemeAccessoryPass/validation_report.md
TinyRidersProduction/ThemeAccessoryPass/integration_notes.md

The separate existing shelf collection uses smaller cars; these raw prefabs have not been
resized or integrated into that shelf system. Recognition still needs player testing.
''',encoding='utf-8')
    paths=list(A.rglob('*'))+list(C.rglob('*'))+list((P/'ArtSource/ThemeAccessories/Blender').glob('*.blend'))+list((P/'ArtSource/ThemeAccessories/Artwork').rglob('*.png'))+list((P/'ArtSource/ThemeAccessories/Scripts').glob('*.py'))+list((P/'ArtSource/ThemeAccessories/Scripts').glob('*.cs'))
    write(D/'output_hashes.json',{p.relative_to(P).as_posix():sha(p) for p in paths if p.is_file()})
    print('Finalized 20 kits, 150 variants; second-run invariants and baseline controls passed.')
if __name__=='__main__':main()
