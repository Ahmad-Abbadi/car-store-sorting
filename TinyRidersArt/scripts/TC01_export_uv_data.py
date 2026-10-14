"""Export exact body/cabin UV edge coordinates for A1 artwork QA."""
from pathlib import Path
import json
import bpy

ROOT = Path(r"C:\Users\User\Documents\Games_Projects\My_Games_Projects\Car Store Sorting\TinyRidersArt")
OUT = ROOT / "qa" / "phase_A1" / "TC-01_uv_edges.json"
size = 2048
edges = []
limits = [1, 1, 0, 0]
for name in ("TC01_Body_ConnectedShell", "TC01_Cabin_PrintOnFacets"):
    mesh = bpy.data.objects[name].data
    uv = mesh.uv_layers.active.data
    for poly in mesh.polygons:
        loops = list(poly.loop_indices)
        for loop in loops:
            u, v = uv[loop].uv
            limits[0] = min(limits[0], u)
            limits[1] = min(limits[1], v)
            limits[2] = max(limits[2], u)
            limits[3] = max(limits[3], v)
        for i, loop in enumerate(loops):
            nxt = loops[(i + 1) % len(loops)]
            p, q = uv[loop].uv, uv[nxt].uv
            edges.append([[round(p.x * size, 2), round((1 - p.y) * size, 2)],
                          [round(q.x * size, 2), round((1 - q.y) * size, 2)]])
OUT.write_text(json.dumps({"canvas": size, "mesh_objects": 2,
                           "uv_range": limits, "edges": edges}, separators=(",", ":")), encoding="utf-8")
print("UV_DATA_OK", len(edges), "uv_range", limits, OUT)
