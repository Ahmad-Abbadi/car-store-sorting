"""Compare aisle-constrained routes for 320 identical theme/pickup-station pairs.

This is a layout metric, not a timed human playtest. Pickup stations represent
eight points along the starting stock's main-aisle edge; shelf endpoints stand
1.8 units in front of each rack. Both layouts use the nearer of the theme's racks.
"""
import json, math, re, statistics
from pathlib import Path

root = Path(__file__).resolve().parents[2]
layout = (root / 'Assets/TinyRiders/Gameplay/Compact/CollectorRoom/CollectorRoomLayout.asset').read_text()
poses = re.findall(r'- saveId: (\S+)\s+section: (\d+)\s+position: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}\s+yaw: (\S+)', layout)
new = {}
for sid, sec, x, y, z, yaw in poses:
    theme = sid.removeprefix('COMPACT-').rsplit('-S',1)[0]
    angle = math.radians(float(yaw))
    new.setdefault(theme, []).append((float(x)+1.8*math.sin(angle),float(z)+1.8*math.cos(angle)))
old = {}
for ti, theme in enumerate(sorted(new)):
    for unit in range(2):
        i = ti*2+unit
        if i < 24: x,z,yaw=-19,(i-11.5)*1.8,90
        elif i < 48: x,z,yaw=19,(i-35.5)*1.8,-90
        elif i < 68: x,z,yaw=(i-57.5)*1.8,24,180
        else:
            n=i-68
            x,z,yaw=(-12 if n<6 else 12)+(n%6-2.5)*1.8,-24,0
        a=math.radians(yaw)
        old.setdefault(theme,[]).append((x+1.8*math.sin(a),z+1.8*math.cos(a)))

def route(start, end, cross_aisles):
    x,z=start; tx,tz=end
    if abs(x-tx)<.01: return abs(z-tz)
    # Reach a cross aisle, traverse it, and approach the shelf along its aisle.
    return min(abs(z-c)+abs(x-tx)+abs(tz-c) for c in cross_aisles)

rows=[]
for station in range(8):
    old_start=(0,-13.5+station*27/7)
    new_start=((-2.6 if station%2==0 else 2.6),-15.75+(station//2)*10.5)
    for theme in sorted(new):
        before=min(route(old_start,p,[-22.2,-15.5,0,15.5,22.2]) for p in old[theme])
        after=min(route(new_start,p,[-21,-10.5,0,10.5,21]) for p in new[theme])
        rows.append(dict(station=station+1,theme=theme,before=before,after=after))
b=statistics.median(r['before'] for r in rows);a=statistics.median(r['after'] for r in rows)
report=dict(method=__doc__,samples=len(rows),beforeMedianUnits=b,afterMedianUnits=a,reductionPercent=100*(1-a/b),targetPercent=20,passed=a<=b*.8,routes=rows)
(Path(__file__).parent/'route_comparison.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='routes'},indent=2))
