"""Build in a dedicated Blender background process; never clears a user's live scene.
Usage: blender --background --factory-startup --python this.py -- TH_PIRATE ...
No arguments builds all 40 themes. Geometry is authored in Blender; atlas paint is original.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'ArtSource/Shelves/ThemedShelves'
ASSET=ROOT/'Assets/TinyRiders/Art/Shelves/Themed'
RECIPES=json.loads((ROOT/'Assets/TinyRiders/ThemedCollection/Data/Themes/theme_recipes.json').read_text(encoding='utf-8-sig'))['themes']
BASE=ROOT/'ArtSource/Shelves/BaseShelf_01/TR_Shelf_Base_01.blend'
current_material=None
parts=[]
for folder in (SRC/'Blends', SRC/'Renders', ASSET/'Models', ASSET/'Materials', ASSET/'Prefabs'):
    folder.mkdir(parents=True,exist_ok=True)

def swatch(obj,color):
    obj.data.materials.clear();obj.data.materials.append(current_material)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='ShelfAtlas')
    for loop in uv.data:loop.uv=((color+.5)/8,.125)
    return obj

def poly(points,color=2,y=-.10,depth=.12,name='Relief'):
    n=len(points);verts=[(x,y,z) for x,z in points]+[(x,y+depth,z) for x,z in points]
    faces=[tuple(range(n)),tuple(range(n*2-1,n-1,-1))]+[(i,i+n,(i+1)%n+n,(i+1)%n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);swatch(obj,color);parts.append(obj)
    return obj
def ellipse(x,z,rx,rz,color=2,y=-.1,n=16):return poly([(x+math.cos(i*2*math.pi/n)*rx,z+math.sin(i*2*math.pi/n)*rz) for i in range(n)],color,y,name='Rounded relief')
def rect(x,z,w,h,color=2,y=-.1):return poly([(x-w/2,z-h/2),(x+w/2,z-h/2),(x+w/2,z+h/2),(x-w/2,z+h/2)],color,y)
def line(a,b,width=.1,color=2,y=-.12):
    dx,dz=b[0]-a[0],b[1]-a[1];l=math.hypot(dx,dz);nx,nz=-dz/l*width/2,dx/l*width/2
    return poly([(a[0]+nx,a[1]+nz),(b[0]+nx,b[1]+nz),(b[0]-nx,b[1]-nz),(a[0]-nx,a[1]-nz)],color,y)
def ring(x,z,r,thickness=.13,color=2,y=-.14,n=16):
    for i in range(n):
        a=i*math.tau/n;b=(i+1)*math.tau/n
        poly([(x+math.cos(a)*r,z+math.sin(a)*r),(x+math.cos(b)*r,z+math.sin(b)*r),(x+math.cos(b)*(r-thickness),z+math.sin(b)*(r-thickness)),(x+math.cos(a)*(r-thickness),z+math.sin(a)*(r-thickness))],color,y,depth=.08)
def star(x,z,r=.7,color=2,y=-.12,points=5):
    return poly([(x+math.cos(math.pi/2+i*math.pi/points)*r*(1 if i%2==0 else .45),z+math.sin(math.pi/2+i*math.pi/points)*r*(1 if i%2==0 else .45)) for i in range(points*2)],color,y)
def leaf(x,z,s=.7,color=2,y=-.15):return poly([(x,z-s),(x-s*.5,z-s*.3),(x-s*.55,z+s*.35),(x,z+s),(x+s*.55,z+s*.35),(x+s*.5,z-s*.3)],color,y)
def gear(x,z,r=.65,color=2,y=-.12):
    poly([(x+math.cos(i*math.tau/32)*r*(1 if i%4 in (1,2) else .79),z+math.sin(i*math.tau/32)*r*(1 if i%4 in (1,2) else .79)) for i in range(32)],color,y)
    ellipse(x,z,r*.29,r*.29,3,y-.03,n=12)

def badge(key):
    # Hand-authored low-poly relief silhouettes. The four repeated placements share mesh data.
    if key=='PIRATE':
        line((-.73,-.67),(.73,.29),.16,4);line((-.73,.29),(.73,-.67),.16,4)
        ellipse(0,.27,.61,.58,4,-.19);rect(0,-.22,.66,.38,4,-.19)
        for x in (-.25,.25):ellipse(x,.28,.16,.2,3,-.23,n=10)
        poly([(-.1,.04),(.1,.04),(0,.18)],3,-.24)
        for x in (-.2,0,.2):rect(x,-.33,.055,.2,3,-.23)
    elif key=='ROYAL':
        poly([(-.82,-.46),(-.91,.59),(-.44,.13),(0,.85),(.44,.13),(.91,.59),(.82,-.46)],2)
        rect(0,-.5,1.62,.22,1,-.15);star(0,0,.23,4,-.18)
    elif key=='SPACE':
        ellipse(0,0,.53,.53,1);line((-.95,-.35),(.95,.35),.19,2,-.19);ellipse(0,0,.4,.4,0,-.23);star(.73,.7,.2,4)
    elif key=='ALIEN':
        poly([(-.7,.6),(-.45,.93),(.45,.93),(.7,.6),(.57,-.27),(0,-.86),(-.57,-.27)],0)
        for s in (-1,1):poly([(s*.11,.24),(s*.55,.51),(s*.48,-.14),(s*.23,-.23)],3,-.15)
        line((-.15,-.49),(.15,-.49),.07,1,-.16)
    elif key=='BEACH':
        star(0,0,.95,2,points=10);ellipse(0,0,.58,.58,0,-.18);ellipse(-.17,.09,.05,.07,3,-.22,n=8);ellipse(.17,.09,.05,.07,3,-.22,n=8)
        line((-.21,-.2),(.21,-.2),.07,3,-.22)
    elif key=='JUNGLE':
        leaf(0,0,.94,2);line((0,-.8),(0,.65),.1,1,-.18)
        for z in (-.25,.12,.45):line((0,z-.15),(-.3,z),.07,1,-.18);line((0,z-.15),(.3,z),.07,1,-.18)
    elif key=='SPOOKY':
        poly([(-.72,-.7),(-.65,.39),(-.43,.82),(0,.95),(.43,.82),(.65,.39),(.72,-.7),(.35,-.5),(0,-.77),(-.35,-.5)],4)
        for x in (-.23,.23):ellipse(x,.23,.12,.2,3,-.17,n=10)
        ellipse(0,-.23,.12,.17,3,-.17,n=10)
    elif key=='CANDY':
        poly([(-.39,0),(-.95,.55),(-.95,-.55)],1);poly([(.39,0),(.95,.55),(.95,-.55)],1)
        ellipse(0,0,.61,.61,4,-.16);poly([(-.42,-.4),(-.11,-.57),(.48,.3),(.23,.53)],0,-.2);poly([(-.5,.2),(-.35,.44),(.18,-.53),(-.07,-.57)],2,-.21)
    elif key=='FOOD':
        poly([(-.86,.12),(-.74,.56),(-.39,.83),(.39,.83),(.74,.56),(.86,.12)],0)
        rect(0,-.03,1.7,.17,2,-.14);rect(0,-.24,1.57,.22,3,-.14);rect(0,-.46,1.65,.23,0,-.14)
        for x in (-.4,0,.4):line((x-.05,.51),(x+.04,.57),.06,4,-.17)
    elif key=='FIRE':
        poly([(-.8,-.4),(-.7,.4),(-.37,.08),(-.14,.96),(.3,.44),(.56,.7),(.8,-.35),(.5,-.81),(-.45,-.85)],0)
        poly([(-.4,-.38),(0,.48),(.4,-.38),(.2,-.75),(-.2,-.75)],2,-.17)
    elif key=='ICE':
        for a in (0,math.pi/3,2*math.pi/3):
            dx,dz=math.cos(a),math.sin(a);line((-dx*.9,-dz*.9),(dx*.9,dz*.9),.13,2)
            for s in (-1,1):
                for turn in (-.65,.65):line((s*dx*.54,s*dz*.54),(s*dx*.54+s*math.cos(a+turn)*.28,s*dz*.54+s*math.sin(a+turn)*.28),.09,4,-.15)
    elif key=='OCEAN':
        ellipse(-.1,0,.7,.48,2);poly([(.4,0),(.95,.58),(.95,-.58)],1)
        ellipse(-.42,.13,.095,.095,3,-.17,n=10);line((-.12,.3),(-.12,-.3),.08,4,-.17)
    elif key=='CONSTRUCTION':
        poly([(-.72,-.1),(-.65,.47),(-.37,.77),(.37,.77),(.65,.47),(.72,-.1)],0)
        rect(0,.36,.2,1.03,2,-.16);rect(0,-.23,1.8,.25,2,-.16);rect(0,-.5,1.3,.19,3)
    elif key=='RESCUE':
        poly([(0,-.88),(-.84,-.05),(-.82,.5),(-.42,.79),(0,.48),(.42,.79),(.82,.5),(.84,-.05)],1)
        rect(0,.08,.25,.78,4,-.17);rect(0,.08,.78,.25,4,-.17)
    elif key=='WIZARD':
        poly([(-.63,-.39),(-.17,.95),(.37,.61),(.25,.34),(.65,-.39)],1)
        ellipse(0,-.5,.94,.23,0,-.15);rect(0,-.26,1.15,.18,2,-.18);star(-.06,.27,.23,2,-.18)
    elif key=='ROBOT':
        rect(0,.05,1.35,1.12,0);rect(0,.05,1.08,.73,3,-.16)
        for x in (-.29,.29):rect(x,.2,.21,.21,2,-.2)
        rect(0,-.19,.68,.09,4,-.2);line((0,.6),(0,.84),.11,2);ellipse(0,.9,.12,.12,2,n=10)
        rect(-.81,.1,.19,.43,2);rect(.81,.1,.19,.43,2)
    elif key=='MUSIC':
        line((-.45,-.5),(-.45,.62),.16,2);line((.45,-.2),(.45,.85),.16,2);line((-.45,.62),(.45,.85),.21,2)
        ellipse(-.64,-.5,.3,.23,1,-.15);ellipse(.26,-.2,.3,.23,1,-.15)
    elif key=='SPORT':
        ellipse(0,0,.87,.87,4);poly([(-.35,.12),(-.2,.44),(.2,.44),(.35,.12),(0,-.13)],3,-.17)
        for a in range(5):
            th=math.pi/2+a*math.tau/5;line((math.cos(th)*.4,math.sin(th)*.4),(math.cos(th)*.83,math.sin(th)*.83),.09,3,-.17)
    elif key=='FARM':
        ellipse(-.27,-.1,.54,.65,0);ellipse(.27,-.1,.54,.65,0);line((0,.48),(.08,.83),.14,3,-.17)
        poly([(.04,.65),(.28,.99),(.71,.92),(.48,.64)],2,-.17)
    elif key=='PARTY':
        for x,z,c in [(-.5,.3,1),(.5,.3,2),(0,.54,0)]:
            line((x,z-.4),(0,-.94),.05,c);ellipse(x,z,.38,.48,c,-.16)
        star(0,.54,.19,4,-.2)
    elif key=='WILD_WEST':
        ellipse(0,-.42,.96,.25,1);poly([(-.61,-.3),(-.52,.6),(-.28,.8),(0,.55),(.28,.8),(.52,.6),(.61,-.3)],0,-.16)
        rect(0,-.14,1.15,.17,2,-.2);star(0,.12,.23,4,-.21)
    elif key=='DETECTIVE':
        poly([(-.66,-.1),(-.55,.5),(0,.8),(.55,.5),(.66,-.1)],0)
        ellipse(0,-.2,.96,.2,1,-.16);rect(0,.25,.13,.92,1,-.17)
        for x in (-.44,.44):rect(x,-.42,.25,.4,0,-.17)
        line((-.62,.3),(.62,.3),.09,2,-.19)
    elif key=='NINJA':
        ellipse(0,.15,.75,.71,0);rect(0,.23,1.55,.31,1,-.17)
        for x in (-.27,.27):poly([(x-.17,.26),(x+.17,.16),(x+.1,.06),(x-.1,.09)],4,-.2)
        poly([(.53,-.27),(.95,-.44),(.84,-.89),(.59,-.61)],1)
    elif key=='VIKING_LEGENDS':
        poly([(-.64,-.53),(-.64,.3),(-.35,.7),(.35,.7),(.64,.3),(.64,-.53)],0)
        for s in (-1,1):poly([(s*.51,.22),(s*.89,.4),(s*.91,.85),(s*.6,.61),(s*.46,.45)],1)
        rect(0,.1,.15,1.16,2,-.16);rect(0,-.43,1.34,.18,2,-.17)
    elif key=='ANCIENT_EGYPT':
        poly([(-.57,.58),(0,.87),(.57,.58),(.86,-.87),(.38,-.87),(.25,.26),(-.25,.26),(-.38,-.87),(-.86,-.87)],1)
        for z in (-.65,-.36,-.07,.22):
            for s in (-1,1):rect(s*(.56-z*.18),z,.32,.12,0,-.17)
        ellipse(0,.08,.3,.52,0,-.2);poly([(-.13,.69),(0,.94),(.13,.69),(0,.5)],2,-.23)
    elif key=='STEAMPUNK':gear(-.28,.12,.65,1);gear(.48,-.34,.45,2,-.18)
    elif key=='PREHISTORIC':
        rect(0,-.47,1.8,.26,1)
        for x,h in [(-.63,.48),(0,.89),(.63,.57)]:poly([(x-.29,-.35),(x-.22,h*.6),(x,h),(x+.22,h*.6),(x+.29,-.35)],2,-.16)
    elif key=='HONEY_BEE':
        ellipse(-.5,.43,.43,.4,4);ellipse(.5,.43,.43,.4,4);ellipse(0,-.1,.56,.75,0,-.16)
        for z in (-.48,-.1,.28):rect(0,z,1.02 if z==-.1 else .87,.15,2,-.2)
        ellipse(0,.66,.35,.29,2,-.19)
    elif key=='BUTTERFLY_GARDEN':
        for s in (-1,1):
            poly([(s*.06,.1),(s*.7,.94),(s*.97,.7),(s*.84,.16),(s*.55,-.12),(s*.86,-.56),(s*.61,-.84),(s*.1,-.37)],0)
            ellipse(s*.59,.45,.19,.26,2,-.17);ellipse(s*.48,-.42,.14,.16,1,-.17)
        rect(0,.03,.15,1.12,3,-.2)
    elif key=='MUSHROOM_GROVE':
        poly([(-.29,-.85),(-.19,.19),(.19,.19),(.29,-.85)],1)
        poly([(-.94,.06),(-.7,.6),(-.25,.89),(.25,.89),(.7,.6),(.94,.06)],0,-.17)
        for x,z,r in [(-.46,.29,.14),(0,.59,.17),(.49,.29,.14)]:ellipse(x,z,r,r,1,-.21,n=10)
    elif key=='THUNDERSTORM':
        for x,z,r in [(-.51,.25,.38),(0,.5,.5),(.52,.25,.36)]:ellipse(x,z,r,r,1)
        poly([(-.07,.22),(-.47,-.39),(-.09,-.39),(-.22,-.96),(.51,-.13),(.12,-.13),(.33,.22)],2,-.2)
    elif key=='WINTER_HOLIDAYS':
        rect(0,-.23,1.45,1.05,0);rect(0,-.23,.22,1.05,2,-.17);rect(0,-.22,1.45,.2,2,-.17)
        for s in (-1,1):poly([(0,.37),(s*.66,.85),(s*.83,.55),(s*.58,.3)],1,-.17)
        ellipse(0,.4,.2,.18,2,-.22,n=10)
    elif key=='RAILWAY':
        ellipse(0,-.12,.68,.67,0);ellipse(0,-.12,.48,.47,3,-.17);ellipse(0,-.12,.18,.18,2,-.21,n=12)
        rect(0,.7,.44,.59,1);rect(0,.92,.7,.18,2);rect(0,-.78,1.72,.2,2,-.18)
    elif key=='AVIATION':
        poly([(-.14,-.87),(-.18,.62),(0,.96),(.18,.62),(.14,-.87)],1,-.17)
        poly([(-.14,.27),(-.96,-.21),(-.96,-.43),(-.12,-.16),(.12,-.16),(.96,-.43),(.96,-.21),(.14,.27)],0)
        poly([(-.1,-.55),(-.5,-.8),(-.5,-.92),(0,-.79),(.5,-.92),(.5,-.8),(.1,-.55)],2,-.19)
    elif key=='CINEMA':
        rect(0,-.28,1.65,.91,0);poly([(-.83,.31),(-.91,.6),(.75,.97),(.83,.68)],4)
        for x in (-.6,-.1,.4):poly([(x,.38+x*.22),(x+.19,.42+x*.22),(x+.09,.72+x*.22),(x-.1,.68+x*.22)],3,-.17)
        rect(0,-.3,1.18,.1,4,-.17);rect(0,-.52,.83,.08,4,-.17)
    elif key=='PHOTOGRAPHY':
        rect(0,-.06,1.82,1.16,0);rect(-.27,.63,.72,.3,1);ellipse(.1,-.06,.53,.53,1,-.17);ellipse(.1,-.06,.38,.38,3,-.22)
        ellipse(.1,-.06,.21,.21,2,-.25);rect(-.62,.28,.25,.14,4,-.2)
    elif key=='ART_STUDIO':
        poly([(-.83,-.3),(-.78,.4),(-.27,.85),(.49,.78),(.89,.2),(.65,-.21),(.17,-.24),(-.04,-.75),(-.58,-.79)],0)
        for x,z,c in [(-.48,.26,1),(-.16,.58,2),(.35,.43,1),(-.44,-.24,2)]:ellipse(x,z,.17,.17,c,-.16,n=10)
        ellipse(.38,-.04,.18,.18,3,-.18,n=12);line((-.4,-.88),(.76,.74),.16,3,-.21);poly([(.6,.62),(.75,.98),(.91,.85),(.8,.48)],1,-.24)
    elif key=='POST_OFFICE':
        poly([(-.75,-.63),(-.75,.45),(-.48,.83),(.48,.83),(.75,.45),(.75,-.63)],0)
        rect(0,.05,1.17,.11,3,-.16);rect(0,-.42,.8,.25,1,-.16);line((.84,-.23),(.84,.7),.1,2,-.16);rect(.66,.68,.36,.3,2,-.17)
    elif key=='BARBERSHOP':
        rect(0,0,.85,1.48,4)
        for z in (-.6,-.13,.34):poly([(-.42,z),(.42,z+.33),(.42,z+.54),(-.42,z+.21)],1 if z!=-.13 else 2,-.17)
        ellipse(0,.78,.58,.15,2,-.19);ellipse(0,-.78,.58,.15,2,-.19)
    elif key=='SCIENCE_LAB':
        poly([(-.22,.84),(.22,.84),(.22,.24),(.79,-.7),(.63,-.87),(-.63,-.87),(-.79,-.7),(-.22,.24)],1)
        poly([(-.43,-.12),(.43,-.12),(.65,-.67),(-.65,-.67)],2,-.17);rect(0,.83,.65,.16,4,-.17)
        for x,z in [(-.21,-.41),(.21,-.32),(.06,.34)]:ellipse(x,z,.09,.09,4,-.2,n=10)
    else:raise ValueError(key)

def plain_material(name,color,rough=.4,metal=.1):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m

def remap_uv(obj,region):
    # Region uses image pixels with a top-left origin; Blender UVs have a bottom-left origin.
    x,y,w,h=region;uv=obj.data.uv_layers.active
    if uv is None:uv=obj.data.uv_layers.new(name='ShelfAtlas')
    for loop in uv.data:
        u,v=loop.uv;loop.uv=((x+8+u*(w-16))/1024,1-(y+8+(1-v)*(h-16))/1024)

def triangle_count(objects):
    deps=bpy.context.evaluated_depsgraph_get();count=0
    for o in objects:
        if o.type not in {'MESH','CURVE'}:continue
        ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();count+=len(me.loop_triangles);ev.to_mesh_clear()
    return count

def build(t):
    global current_material,parts
    bpy.ops.wm.open_mainfile(filepath=str(BASE));scene=bpy.context.scene
    root=bpy.data.objects['TR_Shelf_Base_01'];geo=bpy.data.objects['Geometry']
    for row in range(1,4):
        for col in range(1,6):
            o=bpy.data.objects[f'Divider_{row:02}_{col:02}']
            if col==5:bpy.data.objects.remove(o,do_unlink=True)
            else:o.location.x=-1.5+col*.6
        for col in range(1,7):
            o=bpy.data.objects[f'Slot_R{row:02}_C{col:02}']
            if col==6:bpy.data.objects.remove(o,do_unlink=True)
            else:o.location.x=-1.5+(col-.5)*.6
    current_material=plain_material('Shelf_'+t['id'],(.8,.8,.8),1-t.get('smoothness',.35),t.get('metallic',.1))
    image=bpy.data.images.load(str(SRC/'Textures'/(t['id']+'_BaseColor.png')));image.pack()
    surface=bpy.data.images.load(str(SRC/'Textures'/(t['id']+'_Surface.png')))
    surface.colorspace_settings.name='Non-Color';surface.use_fake_user=True;surface.pack()
    nodes=current_material.node_tree.nodes;p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    tex=nodes.new('ShaderNodeTexImage');tex.image=image;current_material.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
    frame=plain_material('Shelf_CommonFrame',(.025,.038,.055),.48,.1)
    tray=plain_material('Shelf_CommonTray',(.36,.43,.47),.4,.22)
    original_materials={o.name:[m.name for m in o.data.materials if m] for o in root.children_recursive if o.type in {'MESH','CURVE'}}
    for o in list(root.children_recursive):
        if o.type not in {'MESH','CURVE'}:continue
        names=original_materials[o.name]
        if o.name.endswith('LowerInset'):
            bpy.data.objects.remove(o,do_unlink=True);continue
        if o.type=='CURVE':
            bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
        # Linked row meshes must keep their sharing but colored UVs are per-object when needed.
        if any('Trays' in n for n in names):o.data.materials.clear();o.data.materials.append(tray)
        elif o.name=='BaseFrontInset':
            o.data=o.data.copy();swatch(o,0)
            uv=o.data.uv_layers.active
            for li in range(len(uv.data)):
                v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x/3+.5,v.z/.585+.5)
            remap_uv(o,(512,0,512,384))
        elif o.name.endswith('PaintedPanel') or o.name.endswith('LowerInset'):
            o.data=o.data.copy();o.data.materials.clear();o.data.materials.append(current_material);remap_uv(o,(0,0,512,768))
        elif o.name=='HeaderFace':
            o.data=o.data.copy();o.data.materials.clear();o.data.materials.append(current_material);remap_uv(o,(512,384,512,256))
        elif any('SidePanels' in n or 'Accent' in n for n in names):o.data=o.data.copy();swatch(o,2 if 'Diffuser' in o.name else 0)
        else:o.data.materials.clear();o.data.materials.append(frame)
    # Create one relief model and instance the meshes four times; no extra colliders or lights.
    parts=[];badge(t['id'][3:]);badge_tris=triangle_count(parts)
    if badge_tris*4>3000:raise RuntimeError(f'{t["id"]}: decoration budget exceeded: {badge_tris*4}')
    deco=bpy.data.objects.new('ThemeDecorations',None);scene.collection.objects.link(deco);deco.parent=geo
    placements=[((-1.18,-.31,3.32),(.22,.22,.22),0),((1.18,-.31,3.32),(.22,.22,.22),0),((-1.802,0,.54),(.21,.045,.21),-math.pi/2),((1.802,0,.54),(.21,.045,.21),math.pi/2)]
    for i,(pos,scale,angle) in enumerate(placements):
        holder=bpy.data.objects.new('ThemeBadge_'+str(i),None);scene.collection.objects.link(holder);holder.parent=deco;holder.location=pos;holder.scale=scale;holder.rotation_euler.z=angle
        for source in parts:
            obj=source if i==0 else source.copy()
            if i:obj.data=source.data.copy();scene.collection.objects.link(obj)
            obj.parent=holder
    # Baked previews get a name label; Unity keeps its existing localizable TMP label.
    font=bpy.data.curves.new('Preview label','FONT');font.body=t.get('name',t.get('displayName'));font.align_x='CENTER';font.align_y='CENTER';font.size=.2
    text=bpy.data.objects.new('PreviewThemeName',font);scene.collection.objects.link(text);text.location=(0,-.29,3.32);text.rotation_euler=(math.pi/2,0,0);font.materials.append(frame)
    bpy.context.view_layer.update()
    if text.dimensions.x>1.95:text.scale*=1.95/text.dimensions.x
    # Export only shelf geometry, excluding preview text, stage, lights and gameplay markers.
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root]+list(root.children_recursive):
        if o==root or o==geo or o in geo.children_recursive:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    path=ASSET/'Models'/(t['id']+'.fbx')
    # Blender 5.1 FBX requires consistent material slots on every exported mesh datablock.
    for obj in geo.children_recursive:
        if obj.type=='MESH':obj.data=obj.data.copy()
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'EMPTY','MESH'},use_mesh_modifiers=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=False)
    # Retain packed source images and editable per-part meshes in each source file.
    for img in bpy.data.images:
        if img.source=='FILE' and img.packed_file is None:
            try:img.pack()
            except RuntimeError:pass
    bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Blends'/(t['id']+'.blend')))
    try:scene.render.engine='CYCLES'
    except TypeError:pass
    scene.cycles.samples=12;scene.cycles.use_denoising=True
    scene.render.resolution_x=640;scene.render.resolution_y=760;scene.render.resolution_percentage=100
    scene.camera.location=(-5,-9,4.8);scene.camera.rotation_euler=(Vector((0,0,1.8))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=4.4
    scene.render.filepath=str(SRC/'Renders'/(t['id']+'.png'));bpy.ops.render.render(write_still=True)
    if t['id'] in {'TH_ANCIENT_EGYPT','TH_PIRATE','TH_CANDY','TH_SCIENCE_LAB'}:
        scene.render.resolution_x=1100;scene.render.resolution_y=700
        scene.camera.location=(0,-8,3.2);scene.camera.rotation_euler=(Vector((0,-.1,2.75))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=3.8
        scene.render.filepath=str(SRC/'Renders'/(t['id']+'_detail.png'));bpy.ops.render.render(write_still=True)
    report={'theme':t['id'],'slots':15,'decorationTriangles':badge_tris*4,'totalTriangles':triangle_count(geo.children_recursive),'materials':3,'atlasSize':1024,'source':str(SRC/'Blends'/(t['id']+'.blend'))}
    (SRC/'Renders'/(t['id']+'.json')).write_text(json.dumps(report,indent=2))
    print('THEME_SHELF_COMPLETE '+json.dumps(report),flush=True)

if __name__=='__main__':
    selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    for t in RECIPES:
        if not selected or t['id'] in selected:build(t)
