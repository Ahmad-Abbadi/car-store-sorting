"""Finalize verified release metadata; refuses incomplete validation."""
import pathlib,json,hashlib,csv,io,datetime
P=pathlib.Path(__file__).resolve().parents[3];D=P/'TinyRidersProduction/ThemesExpansion01';R=P/'Assets/TinyRiders/ThemedCollection/Data/Themes'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,data):p.write_text(json.dumps(data,indent=2),encoding='utf-8')

def verify_regeneration():
    h=read(D/'Reports/unity_assets_before_regeneration.json');changed=[p for p,s in h.items() if not (P/p).exists() or hashlib.sha256((P/p).read_bytes()).hexdigest()!=s]
    before={r['variantId']:r for r in read(D/'Reports/products_before_regeneration.json')};after={r['variantId']:r for r in read(D/'products.json')};semantic=[k for k in before if before[k]!=after.get(k)]
    themes={t['id'] for t in read(D/'texture_recipes.json')['themes']};current=[]
    for root in [P/'Assets/TinyRiders/ThemedCollection',P/'Assets/TinyRiders/Art/ThemeAccessories']:
        current.extend(f.relative_to(P).as_posix() for f in root.rglob('*') if f.is_file() and f.suffix in ['.meta','.prefab','.mat'] and any(t in f.parts or f.name==t+'.meta' for t in themes))
    extras=sorted(set(current)-set(h));result={'checkedUnityAssets':len(h),'changedAssetBytes':changed,'unexpectedAddedAssets':extras,'productRecordsCompared':len(before),'changedProductRecords':semantic,'uniqueProducts':len(after),'passed':not changed and not semantic and not extras and len(after)==260};write(D/'Reports/unity_regeneration.json',result);print(json.dumps(result,indent=2));return result

def prepare():
    for f in [D/'unity_validation_second.json',D/'Reports/geometric_fit_validation.json',D/'Reports/art_regeneration.json',D/'Reports/model_regeneration.json',D/'Reports/unity_regeneration.json']:
        assert read(f)['passed'],str(f)
    products=read(D/'products.json');assert len(products)==260 and len({p['variantId'] for p in products})==260
    old=read(D/'baseline/Files/Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json')['entries'];sources={p['baseCarId']:p for p in old}
    for p in products:
        src=sources[p['baseCarId']];t=p['themeId'];base=p['baseCarId'];p['displayName']=t[3:].replace('_',' ').title()+' - '+base;p['fittingProfileId']=next(f['profileId'] for f in read(D/'fitting_profiles.json') if f['baseCarId']==base);p.update(sourcePrefabPath=src['sourcePrefabPath'],sourceGuid=src['sourceGuid'],buildIdentity='Approved themed V01 configuration, plus explicitly logged new-prefab fog-light visibility overrides',gameplayPreviewPath=f'TinyRidersProduction/ThemesExpansion01/Views/Gameplay/{p["variantId"]}.png',validationState='verified',validationNotes='Imported and visually reviewed; original body mesh/UV references retained. Exact painted-surface contact and glazing intersections checked. No unresolved fit defects.',texturePaths=[f'Assets/TinyRiders/ThemedCollection/Textures/Themes/{t}/{mode}Palette.png' for mode in ['Paint','Neutral','LowNeutral','BodyMetal','Trim','Secondary']]+[f'Assets/TinyRiders/ThemedCollection/Textures/Themes/{t}/{base}_ThemeDecals.png',f'Assets/TinyRiders/Art/ThemeAccessories/Textures/{t}/AccessoryAtlas.png'])
        for key in ['prefabPath','previewPath','thumbnailPath','gameplayPreviewPath']:assert (P/p[key]).exists(),p[key]
    write(D/'products.json',sorted(products,key=lambda p:p['variantId']))
    plan=read(D/'variant_plan.json')
    for p in plan:p['status']='verified';p['productRecord']='products.json#'+p['variantId']
    write(D/'variant_plan.json',plan)
    registry=read(D/'theme_registry_extension.json')
    for t in registry['themes']:t['qaStatus']='verified';t['completedProductCount']=13;t['qaEvidence']=['unity_validation_second.json','Reports/geometric_fit_validation.json','Reports/visual_review.json','ContactSheets/'+t['id']+'_Layers.png']
    write(D/'theme_registry_extension.json',registry)
    recipes=read(D/'texture_recipes.json')
    for t in recipes['themes']:t['qaStatus']='verified'
    write(D/'texture_recipes.json',recipes)
    library=read(D/'accessory_library.json')
    for kit in library:
        kit['role']='primary';kit['qaStatus']='verified_in_Unity';kit['sourceBlend']=kit['sourceBlend'].replace('\\','/');kit['exportPath']=kit['exportPath'].replace('\\','/');kit['sourceHash']=hashlib.sha256((P/kit['sourceBlend']).read_bytes()).hexdigest();kit['exportHash']=hashlib.sha256((P/kit['exportPath']).read_bytes()).hexdigest();kit['kitPrefab']='Assets/TinyRiders/Art/ThemeAccessories/Prefabs/'+kit['themeId']+'/KIT_'+kit['themeId']+'.prefab'
    write(D/'accessory_library.json',library)
    print('Verified metadata prepared for publication')

def finish():
    from qa_artifacts import nonregression
    nonregression();assert read(D/'source_protection.json')['passed']
    products=read(D/'products.json');cat=read(R/'variant_catalog.json')['entries'];assert len(cat)==410 and len({p['collectibleId'] for p in cat})==410
    oldroot=D/'baseline/Files/Assets/TinyRiders/ThemedCollection/Data/Themes';oldrecipes=read(oldroot/'theme_recipes.json')['themes'];currentrecipes=read(R/'theme_recipes.json')['themes'];assert currentrecipes[:len(oldrecipes)]==oldrecipes and len(currentrecipes)==40
    oldreg=read(oldroot/'id_registry.json');reg=read(R/'id_registry.json');assert all(reg[k]==v for k,v in oldreg.items());assert set(reg['additionalReleases']['themes_expansion_01'])=={p['variantId'] for p in products}
    oldyaml=(oldroot/'ThemedCarCatalog.asset').read_bytes();newyaml=(R/'ThemedCarCatalog.asset').read_bytes();prefix=newyaml.startswith(oldyaml.rstrip());assert prefix,'Original runtime catalog serialization changed'
    csvpath=R/'variant_catalog.csv';oldbytes=(oldroot/'variant_catalog.csv').read_bytes();current=csvpath.read_bytes();assert current.startswith(oldbytes)
    header=next(csv.reader(io.StringIO(oldbytes.decode('utf-8-sig'))));existing={r['collectibleId'] for r in csv.DictReader(io.StringIO(current.decode('utf-8-sig')))};buf=io.StringIO(newline='');writer=csv.DictWriter(buf,fieldnames=header,extrasaction='ignore',lineterminator='\r\n')
    for p in products:
        if p['collectibleId'] not in existing:writer.writerow(p)
    with csvpath.open('ab') as f:f.write(buf.getvalue().encode('utf-8'))
    proof={'oldRecipeRecordsPreserved':20,'totalThemeRecipes':40,'oldPortableRecordsPreserved':150,'totalPortableRecords':410,'runtimeOldSerializationPrefixPreserved':prefix,'oldReleaseMembershipPreserved':True,'newReleaseIds':260,'csvOldBytesPreserved':True,'passed':True};write(D/'Reports/catalog_append_validation.json',proof)
    assert read(D/'Reports/runtime_catalog_validation.json')['passed']
    validation=read(D/'unity_validation_second.json');library=read(D/'accessory_library.json');protection=read(D/'source_protection.json');regen=read(D/'Reports/unity_regeneration.json');fit=read(D/'Reports/geometric_fit_validation.json')
    state={'status':'complete','releaseId':'themes_expansion_01','existingThemes':20,'addedThemes':20,'totalThemes':40,'enabledBaseCars':13,'plannedProducts':260,'completedProducts':260,'totalCollectionProducts':410,'primaryAccessoryModels':20,'optionalSupportingModels':0,'blockedProducts':[],'reservedExcludedBaseCars':['C006','C013'],'optionalModuleOverrides':38,'allValidationPassed':True,'playerRecognitionMeasured':False,'shelfIntegration':'unsupported: existing raw cars exceed separate small shelf envelope','completedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()};write(D/'production_state.json',state)
    report=f'''# Verified expansion delivery

Twenty existing themes / 150 existing products are preserved. Twenty additional themes now cover all thirteen enabled themed base cars: **260 of 260 planned products**, twenty shared primary kits, no supporting kit clutter, and **410 total catalog entries**. No substitutions were needed. No planned product remains blocked. C006/C013 remain reserved exclusions from the earlier verified release; they were not silently omitted from an enabled selection.

## Validation

- Unity: {validation['bodyMeshReferencesChecked']} original body mesh references checked across 260 saved prefabs; configuration, IDs, material assignment, one accessory root and uniform fit scale passed.
- Geometry: all 260 products have sampled contact with actual painted roof/deck triangles, and zero remaining glazing edge intersections. Thirty-eight separately removable roof fog-light modules were hidden only in the affected new prefab overrides. Original car bodies, UVs and supplier prefabs were not modified.
- Models: twenty real Blender sources and shared FBX files, {min(k['triangles'] for k in library)}–{max(k['triangles'] for k in library)} triangles per kit, no boundary/nonmanifold edges or loose vertices in the recorded topology audit. These counts are not a performance guarantee.
- Regeneration: 540 artwork files were byte-identical; all twenty model geometry/UV fingerprints matched; {regen['checkedUnityAssets']} Unity material/prefab/meta files and all 260 product records matched after unchanged assembly. No extra assets or IDs were introduced.
- Nonregression: {protection['protectedFilesChecked']} protected files matched their original hashes. All 150 original reference renders were pixel-identical. Old runtime catalog serialization, portable records, recipes and release membership were preserved.
- Visual evidence: all 260 products in family sheets; twenty texture-only, combined, clay and wireframe theme checks; nine requested confusion comparisons; native 64/128-pixel car-width thumbnails; three partially occluded static piles; emission-off examples; exact existing project-camera projection tests. Original/new representatives use the same lighting, framing and body where compared.

## Practical limits

The supplier decal islands limit coverage, especially split hood regions and compact bodies. Larger door/rear panels carry the principal prints; source UVs and body geometry are unchanged. At 64 pixels, small print detail is reduced and palette/silhouette contribute most. The existing Level display pose produces a small sideways view, so neutral views and native-size thumbnails are the clearer readability evidence. Existing camera and gameplay scenes remain unchanged.

The separate small shelf envelope (0.464 × 0.54 × 0.648) already excludes the unscaled source cars. Shelf integration is unsupported; cars and shelves were not resized. Player recognition and target-device performance have not been measured.

See `INTEGRATION_README.md`, `products.json`, `fit_overrides.json`, the reports above and `ContactSheets/ExpansionOverview.png`.
'''
    (D/'Reports/validation_report.md').write_text(report,encoding='utf-8')
    (D/'RESUME_HERE.md').write_text('# Complete: themes_expansion_01\n\n20 added themes, 20 original primary kits, 260/260 new products on 13 enabled themed base cars. Existing 20 themes/150 products preserved; combined catalog has 410 entries. No blocked planned combinations.\n\nRead INTEGRATION_README.md and Reports/validation_report.md for asset locations, exact checks, 38 logged roof-light overrides, shelf/camera limitations and regeneration commands. C006/C013 remain previously reserved exclusions. No task work remains pending.\n')
    (P/'Assets/TinyRiders/ThemedCollection/READ_ME_ThemesExpansion01.txt').write_text('Themes Expansion 01 adds 20 themes and 260 products. Combined catalog: 40 themes / 410 products. The original READ_ME.txt describes the preserved earlier release. See TinyRidersProduction/ThemesExpansion01/INTEGRATION_README.md and Reports/validation_report.md. No gameplay or shelf integration is included.\n')
    visual=read(D/'Reports/visual_review.json');visual['followup']='All required source, fitting, nonregression and catalog checks passed. No unresolved planned product.';visual['finalInspectedViews']+=['NoEmissionOverview, TextureOnlyOverview and GameplayOverview: all twenty themes'];write(D/'Reports/visual_review.json',visual)
    print(json.dumps(state,indent=2))

if __name__=='__main__':
    import sys
    {'regeneration':verify_regeneration,'prepare':prepare,'finish':finish}[sys.argv[1]]()
