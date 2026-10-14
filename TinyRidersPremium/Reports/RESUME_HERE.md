# Resume Tiny Riders 3.1

The goal is incomplete. Do not reuse prior prototype counts.

Approved masters are foundations only, saved under `Blender/Masters` and `Exports/Masters`. Review each `Previews/<master>/review.json` and the ledger for actual counts. The shared packaging template has been visually validated; final SKU back artwork remains pending. Continue original masters and the shared component library, then alternate identities/builds. Do not regenerate approved geometry unnecessarily. To rebuild the latest Parcel candidate after an intentional source change:

```powershell
python TinyRidersPremium/scripts/make_m02_textures.py
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --python TinyRidersPremium/scripts/build_m02.py
& 'C:\Program Files\Blender Foundation\Blender 5.1\blender.exe' --background --python TinyRidersPremium/scripts/render_wire_review.py -- M02
unity command run_script --file TinyRidersPremium/scripts/UnityCandidateImport.cs --entry UnityCandidateImport.Main --args '["M02"]' --caller plugin --skill unity-cli --format json
python TinyRidersPremium/scripts/checkpoint.py
```

Packaging source commands, only when a dependency changes: Blender `render_package_hero.py`, local Python `compose_package_master.py`, Blender `build_package_master.py`, then Unity `UnityPackageImport.cs` entry `UnityPackageImport.Main`. Preserve the canonical logo/layout. Review `Previews/Packaging/review.json` for scope and limitations. Continue remaining masters, 12 wheel designs, named identities, physical builds and the full 25/50/100/500 objective. `PRODUCTION_STATE.json` lists every unfinished SKU. No generation process is implied to remain running by this file; inspect the current process or session before resuming.
