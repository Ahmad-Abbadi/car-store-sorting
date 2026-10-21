"""Stage small, verified StreamingAssets batches for the browser directory picker."""
import hashlib
import json
from pathlib import Path
import shutil

root = Path(__file__).resolve().parents[2]
evidence = Path(__file__).parent
package = json.loads((evidence / "package-validation.json").read_text(encoding="utf-8"))
source = Path(package["portalFolder"]) / "StreamingAssets"
files = sorted(p for p in source.rglob("*") if p.is_file())
destination = root / "Builds" / ("CrazyGamesStreamUpload-" + package["sha256"][:8])
batches = {}
for index, path in enumerate(files):
    batch = destination / ("batch" + str(index // 3 + 1).zfill(2)) / "StreamingAssets"
    target = batch / path.relative_to(source)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, target)
    assert hashlib.sha256(path.read_bytes()).digest() == hashlib.sha256(target.read_bytes()).digest()
    batches.setdefault(str(batch), []).append(path.relative_to(source).as_posix())
result = {"packageSha256": package["sha256"], "files": len(files), "batches": batches}
(evidence / "upload-batches.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result, indent=2))
