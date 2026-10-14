"""Capture and compare the Unity-generated collection across a no-change pass."""

import hashlib
import json
import sys
from pathlib import Path


root = Path(__file__).resolve().parent.parent
reports = root / "TinyRidersProduction" / "Reports"
state_path = root / "TinyRidersProduction" / "production_state.json"


def snapshot():
    state = json.loads(state_path.read_text(encoding="utf-8"))
    paths = {entry["path"] for entry in state["assets"]}
    paths |= {path + ".meta" for path in paths if (root / (path + ".meta")).is_file()}
    return {
        "files": [
            {"path": path, "sha256": hashlib.sha256((root / path).read_bytes()).hexdigest()}
            for path in sorted(paths)
        ],
        "productIds": sorted(item["variantId"] for item in state["products"]),
    }


current = snapshot()
if sys.argv[1] == "before":
    (reports / "idempotency_before.json").write_text(json.dumps(current, indent=2) + "\n", encoding="utf-8")
    print(f"Captured {len(current['files'])} files and {len(current['productIds'])} IDs")
elif sys.argv[1] == "after":
    before = json.loads((reports / "idempotency_before.json").read_text(encoding="utf-8"))
    previous = {entry["path"]: entry["sha256"] for entry in before["files"]}
    latest = {entry["path"]: entry["sha256"] for entry in current["files"]}
    result = {
        "beforeCount": len(previous),
        "afterCount": len(latest),
        "changed": sorted(path for path in previous.keys() & latest.keys() if previous[path] != latest[path]),
        "missing": sorted(previous.keys() - latest.keys()),
        "added": sorted(latest.keys() - previous.keys()),
        "productIdsUnchanged": before["productIds"] == current["productIds"],
    }
    result["passed"] = not any((result["changed"], result["missing"], result["added"])) and result["productIdsUnchanged"]
    (reports / "idempotency_result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    if not result["passed"]:
        sys.exit(1)
else:
    raise SystemExit("Usage: check_idempotency.py before|after")
