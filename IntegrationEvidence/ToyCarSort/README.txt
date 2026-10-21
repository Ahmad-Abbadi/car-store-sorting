Toy Car Sort branding

Artwork: Assets/00_Project/04_ِArt/UI/Branding/ToyCarSortLogo.png
1536 x 1024 RGBA PNG generated with built-in image_gen; the exact prompt is in logo-prompt.txt.
Spelling, transparent edges, small-size readability and bright/dark contrast were inspected.
Unity imports it as a single Sprite, with alpha transparency, no mipmaps, clamp wrapping,
bilinear filtering, a 2048 size limit and uncompressed texture data.

Integration
- Unity product name and README title: Toy Car Sort.
- Main menu: separate non-interactive Image; original localized title retained inactive.
  The logo fits the space above the menu card at different screen proportions.
- Shared loading prefab: top-center Image clear of the loading panel; used by startup and Play.
- Upgrade box and both brand localization keys updated across eight languages.
- Menu, loading, tutorial-menu and shop-sign authoring code retains the new branding.
- Internal identifiers, application identifiers and save keys were preserved.
- Existing tutorial and other uncommitted work was preserved.

Validation
- Unity CLI 1.0.0-beta.13, Unity Editor 6000.3.22f1, active WebGL target.
- Runtime frame advancement confirmed by wait_for (frame count 6 to 10).
- Startup reached MainMenu Scene; menu logo and existing controls inspected in Play mode.
- Detail control, theme switching, language picker, reset opening/cancellation passed.
- Guarded Play opened Loading Scene with the logo, then reached Level with IsLoading=false.
  Physics simulation was disabled for this navigation check, then restored.
- A subsequent runtime text audit hit a null TMP text; this was a diagnostic expression error.
  The separate null-safe source scan found no old title in authored game scenes/prefabs.
- StoreLoadingScreenTests.AuthoredScenes_SharePresentationAndKeepLoadingPolicy assertions
  were invoked directly and passed, including the new logo reference and input-blocking checks.
- Menu and loading layouts inspected at 1280x720, 1280x800 and 1680x720.
  Loading screenshots use a temporary instance of the real shared prefab held on-screen.
  startup-1280x720.png shows the menu reached from startup, not the startup frame itself.
- Language/tutorial preferences, detail/theme settings, physics and Game view size restored.
- WebGL build succeeded with zero errors and two existing configuration warnings.
  See build-summary.json for the build size, duration, title checks and packed logo entries.
  Output: Builds/ToyCarSortSmoke. Browser page title and productName are Toy Car Sort.

No store listing, application icon, external publication or released-save migration was included.
