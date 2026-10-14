"""Read-only integrity checks for the saved initial release.

Unity's Validate Initial Release command performs the live object checks. This
script checks the saved files, source snapshot, identity bindings, and previews.
"""

import csv
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
PRODUCTION = ROOT / "TinyRidersProduction"
REPORT = PRODUCTION / "Reports" / "release_integrity.json"


def read_json(name):
    return json.loads((PRODUCTION / name).read_text(encoding="utf-8"))


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path):
    match = re.search(r"^guid: ([0-9a-f]{32})$", Path(str(path) + ".meta").read_text(encoding="utf-8"), re.M)
    assert match, f"Missing Unity GUID: {path}"
    return match.group(1)


def main():
    catalog = read_json("source_catalog.json")
    state = read_json("production_state.json")
    history = read_json("registry_history.json")
    snapshot = read_json("source_file_hashes.json")
    products = catalog["products"]
    records = {r["variantId"]: r for r in state["products"]}
    expected_ids = {p["variantId"] for p in products}
    errors = []
    checks = {}

    def check(condition, message):
        if not condition:
            errors.append(message)

    check(len(products) == len(expected_ids) == 150, "Expected 150 unique authoring IDs")
    check(set(records) == expected_ids, "Production-state IDs differ from authoring IDs")
    base = {b["id"]: b for b in catalog["baseCars"]}
    builds = {(b["baseCarId"], b["code"]): b for b in catalog["builds"]}
    check(len(base) == 15 and len(builds) == 30, "Expected 15 bases and 30 builds")
    check(all(b["supported"] for b in builds.values()), "Unsupported build in initial release")
    for binding in history["baseCars"]:
        car = base.get(binding["baseCarId"])
        check(car is not None and (car["sourceGuid"], car["sourcePrefabPath"]) ==
              (binding["sourceGuid"], binding["sourcePrefabPath"]),
              f"History binding changed: {binding['baseCarId']}")
    historical_products = {item["variantId"]: item for item in history["variantBindings"]}
    check(set(historical_products) == expected_ids, "Historical variant IDs differ from release")
    for product in products:
        old = historical_products.get(product["variantId"])
        if old:
            check(all(old[key] == product[key] for key in ("baseCarId", "buildCode", "liveryCode")) and
                  old["firstRelease"] == product["releaseId"],
                  f"Historical product binding changed: {product['variantId']}")
    checks["baseCars"] = len(base)
    checks["builds"] = len(builds)
    checks["products"] = len(products)

    source_changes = []
    for item in snapshot["files"]:
        path = ROOT / item["path"]
        if not path.is_file() or sha256(path) != item["sha256"]:
            source_changes.append(item["path"])
    current_source = {p.relative_to(ROOT).as_posix() for p in (ROOT / "Assets/ithappy/Modular_Racing_Cars").rglob("*") if p.is_file()}
    original_source = {item["path"] for item in snapshot["files"]}
    source_changes.extend(sorted(current_source - original_source))
    check(not source_changes, f"Supplier source changed: {source_changes[:5]}")
    checks["sourceFilesUnchanged"] = len(snapshot["files"]) - len(source_changes)

    with (PRODUCTION / "variant_catalog.csv").open(newline="", encoding="utf-8-sig") as stream:
        rows = list(csv.DictReader(stream))
    csv_by_id = {row["variantId"]: row for row in rows}
    check(len(rows) == len(csv_by_id) == len(products), "CSV IDs differ or duplicate")
    check(set(csv_by_id) == expected_ids, "CSV membership differs")

    release = (ROOT / "Assets/TinyRiders/Data/Releases/initial_v1.asset").read_text(encoding="utf-8")
    manifest_guids = re.findall(r"^  - \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$", release, re.M)
    scene = (ROOT / "Assets/TinyRiders/Scenes/ArtPreview/initial_v1_lineup.unity").read_text(encoding="utf-8")
    scene_guids = re.findall(r"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32}), type: 3\}", scene)
    definition_guids = set()
    prefab_guids = set()
    icon_hashes = {}
    icon_count = 0
    for product in products:
        identifier = product["variantId"]
        record = records.get(identifier)
        if record is None:
            continue
        check(record["status"] == "technical_validated" and record["visualReviewed"] and not record["error"],
              f"Not ready: {identifier}")
        check(product["baseCarId"] in base and (product["baseCarId"], product["buildCode"]) in builds,
              f"Bad base/build mapping: {identifier}")
        check(product["releaseId"] == "initial_v1", f"Wrong release: {identifier}")
        row = csv_by_id.get(identifier, {})
        check(historical_products.get(identifier, {}).get("prefabPath") == record["prefabPath"],
              f"Historical prefab path changed: {identifier}")
        check(row.get("status") == "ready" and row.get("prefabPath") == record["prefabPath"] and
              row.get("previewPath") == record["thumbnailPath"] and row.get("releaseId") == product["releaseId"],
              f"CSV mismatch: {identifier}")
        paths = [ROOT / record[key] for key in ("prefabPath", "definitionPath", "thumbnailPath", "materialPath")]
        if not all(path.is_file() and Path(str(path) + ".meta").is_file() for path in paths):
            errors.append(f"Missing asset or meta: {identifier}")
            continue
        prefab, definition, icon, _ = paths
        prefab_guid = guid(prefab)
        icon_guid = guid(icon)
        prefab_guids.add(prefab_guid)
        definition_guids.add(guid(definition))
        definition_text = definition.read_text(encoding="utf-8")
        check(f"variantId: {identifier}\n" in definition_text and
              f"baseCarId: {product['baseCarId']}\n" in definition_text and
              f"buildCode: {product['buildCode']}\n" in definition_text and
              f"liveryCode: {product['liveryCode']}\n" in definition_text and
              f"guid: {prefab_guid}" in definition_text and f"guid: {icon_guid}" in definition_text,
              f"Definition binding mismatch: {identifier}")
        check(f"variantId: {identifier}\n" in prefab.read_text(encoding="utf-8"),
              f"Prefab identity mismatch: {identifier}")
        with Image.open(icon) as image:
            check(image.mode == "RGBA" and image.size == (128, 128) and
                  image.getchannel("A").getextrema() == (0, 255),
                  f"Thumbnail alpha/size problem: {identifier}")
        icon_count += 1
        icon_hashes[identifier] = sha256(icon)
    checks["savedPrefabs"] = len(prefab_guids)
    checks["savedDefinitions"] = len(definition_guids)
    checks["savedThumbnails"] = icon_count
    check(len(prefab_guids) == len(definition_guids) == icon_count == 150, "Output count mismatch")
    check(len(manifest_guids) == 150 and set(manifest_guids) == definition_guids, "Release manifest mismatch")
    scene_variant_guids = [item for item in scene_guids if item in prefab_guids]
    check(len(scene_variant_guids) == 150 and set(scene_variant_guids) == prefab_guids,
          "Lineup scene membership mismatch")
    checks["releaseCatalogEntries"] = len(manifest_guids)
    checks["sceneVariantInstances"] = len(scene_variant_guids)
    for car in base:
        ten_hashes = [icon_hashes.get(f"TR-{car}-{build}-L{i:02d}")
                      for build in ("STD", "CUS") for i in range(1, 6)]
        check(len(set(ten_hashes)) == 10, f"Duplicate ten-look preview: {car}")
        for build in ("STD", "CUS"):
            hashes = [icon_hashes.get(f"TR-{car}-{build}-L{i:02d}") for i in range(1, 6)]
            check(len(set(hashes)) == 5, f"Duplicate livery preview: {car}-{build}")
        for i in range(1, 6):
            check(icon_hashes.get(f"TR-{car}-STD-L{i:02d}") != icon_hashes.get(f"TR-{car}-CUS-L{i:02d}"),
                  f"Duplicate build preview: {car}-L{i:02d}")
    checks["contactSheets"] = sum((PRODUCTION / "ContactSheets" / f"{car}.png").is_file() for car in base)
    check(checks["contactSheets"] == 15 and (PRODUCTION / "ContactSheets/initial_v1_overview.png").is_file(),
          "Contact sheets missing")

    # Adding a future product to the front must not change existing bindings.
    bindings_before = {p["variantId"]: (p["displayName"], historical_products[p["variantId"]]["prefabPath"],
                                         records[p["variantId"]]["thumbnailPath"])
                       for p in products}
    future = {"variantId": "TR-C016-STD-L01", "baseCarId": "C016", "displayName": "Future fixture"}
    fixture = [future, *products]
    bindings_after = {p["variantId"]: bindings_before[p["variantId"]]
                      for p in fixture if p["variantId"] in bindings_before}
    check(bindings_before == bindings_after and future["variantId"] not in expected_ids,
          "Future-extension fixture altered current bindings")
    checks["extensionFixturePreservedBindings"] = len(bindings_after)
    idempotency = json.loads((PRODUCTION / "Reports/idempotency_result.json").read_text(encoding="utf-8"))
    check(idempotency["passed"] and not idempotency["changed"] and not idempotency["missing"]
          and not idempotency.get("added") and idempotency.get("productIdsUnchanged", True),
          "Idempotency check failed")
    checks["idempotencyFiles"] = idempotency["afterCount"]

    result = {"passed": not errors, "checks": checks, "errors": errors,
              "sourceChanges": source_changes}
    REPORT.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    if errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
