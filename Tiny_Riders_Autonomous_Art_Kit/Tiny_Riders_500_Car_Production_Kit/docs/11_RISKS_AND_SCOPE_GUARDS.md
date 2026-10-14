# 11 — Production Risks and Practical Mitigations

| Risk | Symptom | Mandatory mitigation |
|---|---|---|
| AI mesh quality too low | Awkward smooth wedge and tiny wheels | Deliberate chunky low-poly style; approved physical base body and silhouette QA before variants |
| Modeled words/logos | Heavy mesh letters floating on cards | Flat SVG/PNG typesetting and actual UVs; clay/wireframe rejection test |
| Package drift | Family logo moves, different bubble and product name location | One master package layout; fixed measured coordinates; no per-car reinvention |
| Mismatched card art | Printed car has different spoiler/colors | Render exact 3D car and composite, never prompt AI for separate hero car |
| Disappointing variety | 500 hues of same little car | 50 different silhouettes plus 2 physical builds; contact sheet shape tests |
| Game takes too long to load | 500 detailed textures/models initial scene | Shared meshes, package master, reduced atlases, deferred content, actual WebGL profiling |
| AI invents logos/copyrighted vehicles | Famous toy logos, real automaker trademarks | Fictional original models/labels; original brand name checked before launch |
| Machine generates entire set without reviews | Hundreds of mediocre products | Phase gating and batch sign-off; first one car + one master package |
| Source image is ambiguous | Different angle/number details conflict | One physical body authority; Track Club master package structure; human review |
| Huge asset production cost | 500 unrelated models and cards | Deterministic generator/metadata; modular parts; family-based background skins |

## Honest limits
This kit does not guarantee AI can generate 50 beautiful, coherent original car bodies without artistic fixes. The main advantage is limiting and making repeatable the work, so the first failure is caught at 1 item rather than 500. If the first few procedural bodies still look weak, replace the generator’s body meshes with properly authored or rights-cleared stylized masters; keep the same catalog and packaging pipeline.
