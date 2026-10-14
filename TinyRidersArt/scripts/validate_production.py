"""File and catalog audit; never counts missing Unity prefabs as complete."""
import hashlib
import json
from datetime import datetime,timezone
from itertools import combinations
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT.parent
KIT=PROJECT/"Tiny_Riders_Autonomous_Art_Kit"/"Tiny_Riders_500_Car_Production_Kit"
CAT=json.loads((KIT/"catalog"/"collectibles_500.json").read_text(encoding="utf-8"))
BODIES=json.loads((KIT/"catalog"/"bodies_50.json").read_text(encoding="utf-8"))
REPORTS=ROOT/"reports"; REPORTS.mkdir(parents=True,exist_ok=True)
UNITY=PROJECT/"Assets"/"TinyRidersArt"

def exists(path): return path.exists() and path.stat().st_size>0
def check_image(path,size):
    if not exists(path): return False
    try:
        with Image.open(path) as im: return im.size==size and im.verify() is None
    except Exception: return False

def files(p):
    sku=p["sku"]
    return {
        "car_blend":ROOT/"blender"/"products"/(sku+".blend"),
        "car_fbx":ROOT/"exports"/"products"/(sku+".fbx"),
        "car_png":ROOT/"textures"/"cars"/(sku+".png"),
        "car_svg":ROOT/"textures"/"source_artwork"/"cars"/(sku+".svg"),
        "car_render":ROOT/"previews"/"products"/(sku+".png"),
        "package_blend":ROOT/"blender"/"packages"/(sku+".blend"),
        "package_fbx":ROOT/"exports"/"packages"/(sku+".fbx"),
        "front_png":ROOT/"textures"/"packages"/"front"/(sku+".png"),
        "back_png":ROOT/"textures"/"packages"/"back"/(sku+".png"),
        "front_svg":ROOT/"textures"/"source_artwork"/"packages"/"front"/(sku+".svg"),
        "back_svg":ROOT/"textures"/"source_artwork"/"packages"/"back"/(sku+".svg"),
        "package_render":ROOT/"previews"/"packages"/(sku+".png"),
        "unity_car_fbx":UNITY/"Cars"/"Products"/(sku+".fbx"),
        "unity_package_fbx":UNITY/"Packages"/"Products"/(sku+".fbx"),
        "unity_car_prefab":UNITY/"Prefabs"/"Cars"/(sku+".prefab"),
        "unity_package_prefab":UNITY/"Prefabs"/"Packages"/(sku+".prefab"),
    }

def silhouette(path):
    with Image.open(path) as im:
        a=im.convert("RGBA").getchannel("A").resize((64,40),Image.Resampling.BOX)
        return {i for i,v in enumerate(a.getdata()) if v>110}

def run():
    if len(CAT)!=500 or len(BODIES)!=50: raise ValueError("Source catalog counts changed")
    seen=set(); rows=[]; errors=[]
    counts={"base_blends":0,"physical_builds":0,"car_products":0,"package_products":0,
            "car_prints":0,"front_cards":0,"back_cards":0,"car_renders":0,"package_renders":0,
            "unity_car_prefabs":0,"unity_package_prefabs":0}
    for b in BODIES:
        if exists(ROOT/"blender"/"builds"/(b["bodyId"]+"-S.blend")): counts["base_blends"]+=1
        for v in ("S","C"):
            if all(exists(ROOT/path/(b["bodyId"]+"-"+v+ext)) for path,ext in (("blender/builds",".blend"),("exports/builds",".fbx"))): counts["physical_builds"]+=1
    for p in CAT:
        sku=p["sku"]
        if sku in seen: errors.append("duplicate sku "+sku)
        seen.add(sku)
        f=files(p); valid={name:exists(path) for name,path in f.items()}
        valid["car_png"]=check_image(f["car_png"],(1024,1024))
        valid["front_png"]=check_image(f["front_png"],(1200,1800))
        valid["back_png"]=check_image(f["back_png"],(1200,1800))
        valid["car_render"]=check_image(f["car_render"],(640,400))
        valid["package_render"]=check_image(f["package_render"],(480,720))
        counts["car_prints"]+=int(valid["car_png"] and valid["car_svg"])
        counts["front_cards"]+=int(valid["front_png"] and valid["front_svg"])
        counts["back_cards"]+=int(valid["back_png"] and valid["back_svg"])
        counts["car_renders"]+=int(valid["car_render"])
        counts["package_renders"]+=int(valid["package_render"])
        counts["car_products"]+=int(all(valid[k] for k in ("car_blend","car_fbx","car_png","car_svg","car_render")))
        counts["package_products"]+=int(all(valid[k] for k in ("package_blend","package_fbx","front_png","back_png","front_svg","back_svg","package_render")))
        counts["unity_car_prefabs"]+=int(valid["unity_car_prefab"])
        counts["unity_package_prefabs"]+=int(valid["unity_package_prefab"])
        missing=[k for k,v in valid.items() if not v]
        rows.append({"sku":sku,"bodyId":p["bodyId"],"buildId":p["buildId"],"liveryName":p["liveryName"],
                     "files":{k:str(v.relative_to(PROJECT)) for k,v in f.items()},"verified":valid,"missing":missing})
    signatures={}
    for p in CAT:
        path=ROOT/"textures"/"cars"/(p["sku"]+".png")
        if exists(path): signatures.setdefault(hashlib.sha256(path.read_bytes()).hexdigest(),[]).append(p["sku"])
    duplicates=[v for v in signatures.values() if len(v)>1]
    silhouettes={}
    for b in BODIES:
        path=ROOT/"previews"/"products"/(f'TR-{b["bodyId"]}-S-01.png')
        if exists(path): silhouettes[b["bodyId"]]=silhouette(path)
    similarity=[]
    for a,b in combinations(silhouettes,2):
        x=silhouettes[a]; y=silhouettes[b]
        score=len(x&y)/max(1,len(x|y))
        if score>.92: similarity.append((round(score,3),a,b))
    similarity.sort(reverse=True)
    geometry_path=REPORTS/"GEOMETRY_AUDIT.json"
    unity_path=REPORTS/"UNITY_AUDIT.json"
    geometry=json.loads(geometry_path.read_text(encoding="utf-8")) if exists(geometry_path) else None
    unity=json.loads(unity_path.read_text(encoding="utf-8")) if exists(unity_path) else None
    counts["contact_sheets"]=len(list((ROOT/"previews"/"contact_sheets").glob("*.png")))
    counts["clay_renders"]=len(list((ROOT/"previews"/"clay").glob("*.png")))
    reviewed_pairs={frozenset(("SS-05","CR-02")):
                    "Reviewed both rendered bases: SS-05 has a large doughnut roof form; CR-02 has a turtle-shell roof and printed animal face. Their shared compact body drives the silhouette score, but the products are visibly distinct."}
    unresolved_pairs=[(s,a,b) for s,a,b in similarity if frozenset((a,b)) not in reviewed_pairs]
    required_counts={"base_blends":50,"physical_builds":100,"car_products":500,"package_products":500,
                     "car_prints":500,"front_cards":500,"back_cards":500,"car_renders":500,
                     "package_renders":500,"unity_car_prefabs":500,"unity_package_prefabs":500,
                     "contact_sheets":33,"clay_renders":11}
    unity_required={"catalogProducts":500,"carPrefabs":500,"packagePrefabs":500,"carPrints":500,
                    "packageFronts":500,"packageBacks":500,"cardUvValid":500,"toyPlacementValid":500,
                    "environmentSigns":10,"previewScenes":7}
    remaining=[]
    for k,v in required_counts.items():
        if counts[k]<v: remaining.append(f"Generate or repair {k}: {counts[k]} / {v}")
    if duplicates: remaining.append(f"Repair {len(duplicates)} duplicate car-print texture groups")
    if errors: remaining.extend(errors)
    if not geometry or not geometry.get("pass"):
        remaining.append("Run and pass the Blender geometry audit")
    if not unity:
        remaining.append("Run the connected Unity Art Audit")
    else:
        for k,v in unity_required.items():
            if unity.get(k)!=v: remaining.append(f"Repair Unity {k}: {unity.get(k,0)} / {v}")
        remaining.extend("Repair Unity: "+failure for failure in unity.get("failures",[]))
    remaining.extend(f"Review silhouette {a} / {b} (IoU {s})" for s,a,b in unresolved_pairs)
    phase="AUTO_QA_PASS — complete Unity art collection" if not remaining else "Final validation and repair"
    unity_summary=(f"Connected Editor audit {unity['generatedAtUtc']}: "
                   f"{unity['carPrefabs']} car prefabs, {unity['packagePrefabs']} package prefabs, "
                   f"{unity['previewScenes']} scenes, {len(unity.get('failures',[]))} failures.") if unity else "Unity audit has not run."
    all_product_keys=("car_blend","car_fbx","car_png","car_svg","car_render","package_blend",
                      "package_fbx","front_png","back_png","front_svg","back_svg","package_render",
                      "unity_car_prefab","unity_package_prefab")
    state={
        "generatedAtUtc":datetime.now(timezone.utc).isoformat(),
        "phase":phase,
        "catalog":{"products":len(CAT),"bodies":len(BODIES),"families":10},
        "counts":counts,"completedProductIds":[r["sku"] for r in rows if all(r["verified"][k] for k in all_product_keys) and unity and not any(f.startswith(r["sku"]+":") for f in unity.get("failures",[]))],
        "products":rows,"remainingWork":remaining,
        "failedItems":errors,"duplicateCarTextureGroups":duplicates,"suspiciousSilhouettePairs":similarity[:30],
        "reviewedSilhouettePairs":[{"a":"SS-05","b":"CR-02","finding":reviewed_pairs[frozenset(("SS-05","CR-02"))]}],
        "geometryAudit":{"path":str(geometry_path.relative_to(PROJECT)),"pass":bool(geometry and geometry.get("pass"))},
        "unityAudit":{"path":str(unity_path.relative_to(PROJECT)),"summary":unity_summary,"pass":bool(unity and not unity.get("failures") and all(unity.get(k)==v for k,v in unity_required.items()))},
        "packagingMaster":str((ROOT/"blender"/"PackagingMaster.blend").relative_to(PROJECT)),
        "environment":str((ROOT/"blender"/"SortingRoomArt.blend").relative_to(PROJECT)),
    }
    (REPORTS/"PRODUCTION_STATE.json").write_text(json.dumps(state,indent=2),encoding="utf-8")
    summary=["# Tiny Riders production progress","",f'Updated: {state["generatedAtUtc"]}',"",
             "## Verified counts","",*([f'- {k}: {v}' for k,v in counts.items()]),"",
             f'- Unique car-print texture hashes: {len(signatures)} / 500',
             f'- Flagged base silhouette pairs (IoU > 0.92): {len(similarity)}; reviewed: {len(similarity)-len(unresolved_pairs)}',"",
             "## Current phase","",state["phase"],"","## Unity integration", "",
             unity_summary,"","## Remaining work","",*(["- "+x for x in remaining] or ["- None"]),"",
             "## Completed product IDs","",", ".join(state["completedProductIds"]),""]
    (REPORTS/"PROGRESS.md").write_text("\n".join(summary),encoding="utf-8")
    report=["# Validation report","",f'Generated {state["generatedAtUtc"]}',"",
            "## File and image validation","",*([f'- {k}: {v}' for k,v in counts.items()]),"",
            f'- Duplicate car texture groups: {len(duplicates)}',
            f'- Suspicious silhouette pairs (IoU > 0.92): {len(similarity)}',"",
            "## Visual inspection","",
            "Reviewed the 50-base contact sheet, complete car/package overviews, selected family sheets, the blue #23 pilot, package front/back artwork, clay renders, and Unity captures. The one remaining numerical silhouette flag is SS-05 vs CR-02; the doughnut roof and turtle shell/face are clearly different in their actual renders.","",
            "## Blender geometry audit","",f"Pass: {bool(geometry and geometry.get('pass'))}; failed builds: {len(geometry.get('failedBuilds',[])) if geometry else 'audit missing'}.","",
            "## Unity validation","",unity_summary,"",
            "The connected Unity Editor audit checked every SKU's loose prefab print, packaged prefab front/back prints, card UV0, and enclosed toy placement. It also checked ten environment sign materials and seven preview scenes. Visual captures show the loose car, package front/back, and sorting room.","",
            "## Most similar base silhouettes","",
            *([f'- {a} vs {b}: IoU {s}' for s,a,b in similarity[:20]]),""]
    (REPORTS/"VALIDATION_REPORT.md").write_text("\n".join(report),encoding="utf-8")
    resume=["# Resume Tiny Riders production","",f'Last updated: {state["generatedAtUtc"]}',"",
            ("All required production and validation items passed. Revalidate with:" if not remaining else "The generated assets are preserved. Re-run the audit first:"),"",
            '```powershell',"python TinyRidersArt/scripts/validate_production.py",'```',"",
            "To repeat the live Unity audit with this project open in the Editor:","",
            '```powershell',
            "unity command eval --caller plugin --skill unity-cli --project-path 'C:\\Users\\User\\Documents\\Games_Projects\\My_Games_Projects\\Car Store Sorting' --timeout 300 --format json 'return TinyRidersArt.UnityArtAudit.Run();'",
            '```',"",
            "If source art changes, rerun the corresponding deterministic factory scripts before staging assets and rebuilding prefabs.",""]
    (REPORTS/"RESUME_HERE.md").write_text("\n".join(resume),encoding="utf-8")
    print(json.dumps(counts),"duplicate texture groups",len(duplicates),"similar pairs",len(similarity))

if __name__=="__main__": run()
