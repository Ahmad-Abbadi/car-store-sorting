# Resume Tiny Riders production

Last updated: 2026-10-13T20:14:05.894091+00:00

All required production and validation items passed. Revalidate with:

```powershell
python TinyRidersArt/scripts/validate_production.py
```

To repeat the live Unity audit with this project open in the Editor:

```powershell
unity command eval --caller plugin --skill unity-cli --project-path 'C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting' --timeout 300 --format json 'return TinyRidersArt.UnityArtAudit.Run();'
```

If source art changes, rerun the corresponding deterministic factory scripts before staging assets and rebuilding prefabs.
