"""Validate and ZIP the desktop WebGL release; never package project sources or credentials."""
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / "Builds" / "CrazyGamesRelease"
DEST = ROOT / "Builds" / "Toy-Car-Sort-CrazyGames-Desktop.zip"


def main():
    report_path = Path(__file__).parent / "build-report.json"
    report = json.loads(report_path.read_text(encoding="utf-8"))
    assert report["result"] == "Succeeded" and report["blockingErrors"] == 0, "Latest release build has blocking errors"
    assert not report["developmentBuild"], "Development builds cannot be submitted"
    restoration = json.loads((Path(__file__).parent / "mesh-restoration.json").read_text(encoding="utf-8"))
    assert restoration["restored"], "Temporary mesh sources have not been restored"
    files = sorted(p for p in BUILD.rglob("*") if p.is_file())
    assert (BUILD / "index.html").is_file(), "Missing root index.html"
    assert any((BUILD / "Build").glob("*.wasm.br")), "Missing Brotli WebAssembly"
    assert any((BUILD / "Build").glob("*.data.br")), "Missing Brotli game data"
    size = sum(p.stat().st_size for p in files)
    initial_size = sum(p.stat().st_size for p in files if p.relative_to(BUILD).parts[0] in ("Build", "index.html"))
    assert size == report["bytes"], "Build files changed after the release report was written"
    assert all(p.stat().st_mtime_ns <= report_path.stat().st_mtime_ns for p in files), "Build files were modified after reporting"
    assert initial_size <= 50_000_000, f"Initial-download gate exceeded: {initial_size:,} bytes"
    assert size <= 250_000_000, f"Total game-size gate exceeded: {size:,} bytes"
    assert len(files) < 1500, f"Too many files: {len(files)}"
    with zipfile.ZipFile(DEST, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in files:
            archive.write(path, path.relative_to(BUILD).as_posix())
    with zipfile.ZipFile(DEST) as archive:
        assert "index.html" in archive.namelist()
        assert archive.testzip() is None
    digest = hashlib.sha256(DEST.read_bytes()).hexdigest()
    # Unity 6's portal supplies its own HTML and asks for these folders only.
    portal = ROOT / "Builds" / ("CrazyGamesPortal-" + digest[:8])
    for folder in ("Build", "StreamingAssets"):
        source = BUILD / folder
        if source.is_dir():
            shutil.copytree(source, portal / folder, dirs_exist_ok=True)
    portal_files = sorted(p for p in portal.rglob("*") if p.is_file())
    expected = [p for p in files if p.relative_to(BUILD).parts[0] in ("Build", "StreamingAssets")]
    assert {p.relative_to(portal).as_posix() for p in portal_files} == {p.relative_to(BUILD).as_posix() for p in expected}
    for path in expected:
        staged = portal / path.relative_to(BUILD)
        assert hashlib.sha256(staged.read_bytes()).digest() == hashlib.sha256(path.read_bytes()).digest()
    manifest = {
        "zip": str(DEST), "zipBytes": DEST.stat().st_size,
        "totalBytes": size, "fileCount": len(files),
        "initialDownloadUpperBoundBytes": initial_size,
        "sha256": digest,
        "portalFolder": str(portal),
        "portalBytes": sum(p.stat().st_size for p in portal_files),
        "portalFileCount": len(portal_files),
        "files": [{"path": p.relative_to(BUILD).as_posix(), "bytes": p.stat().st_size} for p in files],
    }
    (Path(__file__).parent / "package-validation.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in manifest.items() if k != "files"}, indent=2))


if __name__ == "__main__":
    main()
