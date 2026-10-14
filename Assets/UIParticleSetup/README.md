# UI Particle Setup

Converts a normal `ParticleSystem` hierarchy into one that renders **inside a Canvas**. For every
ParticleSystem under the root it adds a `UIParticleSystem` renderer, builds a UI material from the
texture that system's renderer already uses, and (optionally) ports the sub-emitters, forces local
simulation space, disables raycast targets, and rescales for canvas pixels. Fully reversible
(**Remove UI Setup**).

Open via **IrahimzTools > UI Particle Setup**.

## ✅ Self-contained — no external asset needed

This package ships everything it needs, all under the `Irahimz.UIParticles` namespace:

- `UIParticleSystem.cs` — the runtime renderer that draws particles as Canvas UI quads. It's a
  re-namespaced copy of the free UI Extensions community script (credit: glennpow, Zarlang), so
  the package no longer depends on Layer Lab's UIParticle extension being installed.
- `UIParticleSubEmitter.cs` — a runtime shim that reproduces Birth/Death sub-emitters on the
  per-system `Simulate()` path the renderer uses (native sub-emitters can't survive it). The tool
  adds and configures it automatically.
- `Editor/UIParticleSetup.cs` — the editor window (the tool itself).

Just copy the whole `UIParticleSetup` folder into any project's `Assets/` and it works.

## Install

Copy the folder into `Assets/`. There is intentionally **no assembly definition** — the two
runtime scripts land in the predefined `Assembly-CSharp` (so scenes/prefabs reference them by GUID
as usual) and the tool, being under an `Editor/` folder, lands in `Assembly-CSharp-Editor`. That
keeps it a zero-config drop-in. (You *can* add asmdefs now that it's dependency-free, but it isn't
required.)

## Use

1. Put the particle-effect root **under a Canvas**.
2. Open the window, drop the root into **Root**, pick a **Material Folder** and a UI **Shader**
   (`UI/Additive` for glows/sparks, `UI/Default` for solid art).
3. **Set Up For UI**. Read the report for untextured systems, converted/dropped sub-emitters, and
   Mesh-render-mode warnings.
4. **Remove UI Setup** reverts everything (generated materials are left on disk).

Draw order becomes hierarchy sibling order, not the particle render queue.

## Note when both this and Layer Lab's UIParticle exist in the same project

They coexist safely (different namespaces / GUIDs). But this tool now adds the **bundled**
`Irahimz.UIParticles.UIParticleSystem`, not Layer Lab's. Effects previously set up with Layer Lab's
component keep working on Layer Lab's runtime; just don't re-run this tool on them, or you'll get
both renderers on the same object. New effects should be set up entirely with this package.

## Requirements

Unity 2019.3+ (uses `ParticleSystem`/`MaskableGraphic` APIs and the `Undo`/`PrefabUtility` editor APIs).
