"""Append new permanent IDs to history; reject changed old bindings."""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
source = json.loads((ROOT / "source_catalog.json").read_text(encoding="utf-8"))
path = ROOT / "registry_history.json"
history = json.loads(path.read_text(encoding="utf-8"))
known = {item["variantId"]: item for item in history.get("variantBindings", [])}
retired = set(history.get("retiredVariantIds", []))
for product in source["products"]:
    identifier = product["variantId"]
    if identifier in retired:
        raise ValueError(f"Retired ID cannot be reused: {identifier}")
    binding = {key: product[key] for key in ("variantId", "baseCarId", "buildCode", "liveryCode")}
    binding["firstRelease"] = product["releaseId"]
    binding["prefabPath"] = (f"Assets/TinyRiders/Prefabs/Variants/{product['baseCarId']}/"
                             f"{product['buildCode']}/{identifier}.prefab")
    if identifier in known and known[identifier] != binding:
        raise ValueError(f"Historical ID binding changed: {identifier}")
    known[identifier] = binding
history["variantBindings"] = [known[key] for key in sorted(known)]
path.write_text(json.dumps(history, indent=2) + "\n", encoding="utf-8")
print(f"Sealed {len(known)} permanent variant bindings")
