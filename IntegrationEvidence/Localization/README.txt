Complete game localization

The CSV master contains 923 keys across en, es, pt-BR, id, tr, pl, vi, and ar.
Stable theme IDs and collectible IDs identify translations; catalog IDs and saves are unchanged.

Tools > Localization > Build Complete Localization imports the CSV into Unity's GameText
StringTable collection, refreshes runtime references/fonts, and wires labels in build scenes
and referenced prefabs. Save open scene edits before running it.
Tools > Localization > Import CSV also synchronizes the Unity StringTables.

Unity LocalizationSettings.SelectedLocale is the source of the current language. The existing
Localizer API remains available. Direct locale/table providers avoid synchronous Addressables
loads on WebGL while allowing Unity LocalizedString consumers to use the same StringTables.
The legacy database retains picker metadata and compatibility with authoring tools.

Runtime messages retain keys and deferred name arguments. Static labels refresh when enabled;
live HUDs, theme headers, review navigation, carried names, and floating feedback refresh on
language changes. Arabic uses the existing shaping implementation with the Arabic font.

Authored-text coverage: authored-text-audit.txt. No UNMATCHED or MISSING KEY entries remain.
Translation/placeholder coverage: translation-coverage.json.
Runtime switching: runtime-switching.txt.
Visual layout: visual-layout.txt and menu/loading PNGs.
Addressables content build: addressables-build.txt.

CompleteLocalizationTests covers all translation entries, format placeholders, stable catalog
IDs, fonts including Arabic presentation glyphs, official Unity string lookups, immediate and
rapid language changes, reopening inactive labels, preferences, unchanged session data, and
menu/loading layout in all eight languages at 1280x800.

Artwork audit: current shop mural, skyline, ceiling, and theme pictograms do not contain
readable words. The older ceiling SVG still includes an unused English title; the current
runtime PNG has no lettering. Small English shelf labels in the loading illustration were
replaced with pictograms in CreativeLoadingBackgroundUniversal.png. The shared loading prefab
and its builder now use this universal artwork; the original image remains available.

Translations are project assets and do not call an online translation service at runtime.
