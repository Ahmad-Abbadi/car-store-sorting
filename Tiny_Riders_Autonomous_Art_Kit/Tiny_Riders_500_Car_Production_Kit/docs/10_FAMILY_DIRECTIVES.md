# 10 — Family Design Directives (visual variety rules)

Every family has 5 intentionally different base bodies. Every base has a physical standard/custom build pair and 5 printed liveries on each build. The references are **illustrative multi-panel mood boards**; modeling must convert them into coherent 3D silhouettes and usable repeatable geometry.

Each family has a dedicated detailed prompt in `prompts/families/` linked to its original image. Copy family mood, palette, visible vehicle TYPES and accessory vocabulary; do not let independent brand/package layout change. `catalog/bodies_50.json` defines ALL named body silhouettes.

## How to avoid 500 lookalikes
- At least one of body profile, roofline, wheel configuration, wheelbase, overall height, nose treatment, boxy load, or large unique accessory visibly differs between family bodies.
- For custom build, add one **meaningful geometry** change supported by plausible attachment mounts, not just change material.
- For five liveries, vary large-color blocks AND graphic motif/print, not merely adjust the base hue. Keep family palette recognizable.
- Each family needs one visually distinctive card motif: skyline, track checkers, diner, mountains, crown/lightning, rescue symbols, industrial shapes, snack patterns, futuristic city, friendly character world. **Flat 2D card art only.**
- All five named models get their own 3/4, top, side, rear and black silhouette approved views. Do not use a 2D image of model A while printing model B's name.

## Anti-drift proof
Generate a `comparison_grid_{family}.png` containing 5 base silhouettes + custom comparisons + sample liveries at exactly same camera size and white/gray lighting. Owner rejects shapes that are too similar. Generate a `packaging_grid_{family}.png` of 5 product packages on identical geometry/layout. If logos/footer shift, fix generator/master package, never hand-adjust individual exports.
