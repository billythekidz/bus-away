import bpy
import os

# --- Settings ---
ASPHALT_W = 1.6
TILE_S = 2.0
H_BORDER = 0.02
H_ASPHALT = 0.04

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()

def new_mat(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = color
    return m

def make_box(name, scale, loc):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.scale = scale
    obj.name = name
    return obj

def combine(parent_name, border_objs, asphalt_objs, mat_b, mat_a):
    bpy.ops.object.select_all(action='DESELECT')
    
    # Merge borders
    if len(border_objs) > 1:
        for o in border_objs: o.select_set(True)
        bpy.context.view_layer.objects.active = border_objs[0]
        bpy.ops.object.join()
    border = border_objs[0]
    border.data.materials.append(mat_b)
    
    # Merge asphalts
    bpy.ops.object.select_all(action='DESELECT')
    if len(asphalt_objs) > 1:
        for o in asphalt_objs: o.select_set(True)
        bpy.context.view_layer.objects.active = asphalt_objs[0]
        bpy.ops.object.join()
    asphalt = asphalt_objs[0]
    asphalt.data.materials.append(mat_a)
    
    # Final join
    bpy.ops.object.select_all(action='DESELECT')
    border.select_set(True)
    asphalt.select_set(True)
    bpy.context.view_layer.objects.active = border
    bpy.ops.object.join()
    border.name = parent_name
    
    # Xuất file FBX
    bpy.ops.object.select_all(action='DESELECT')
    border.select_set(True)
    bpy.context.view_layer.objects.active = border
    
    # Save directly to the Art folder where the python script resides
    script_dir = os.path.dirname(os.path.abspath(__file__))
    outpath = os.path.join(script_dir, f"{parent_name}.fbx")
    bpy.ops.export_scene.fbx(filepath=outpath, use_selection=True)
    
    # Move sang một bên để nhìn cho rõ
    border.location = (len(bpy.data.objects)*3, 0, 0)

def build_straight(mb, ma):
    b = make_box("b", (TILE_S, TILE_S, H_BORDER), (0,0,H_BORDER/2))
    a = make_box("a", (ASPHALT_W, TILE_S, H_ASPHALT), (0,0,H_ASPHALT/2))
    combine("Tile_Straight", [b], [a], mb, ma)

def build_corner(mb, ma):
    # Dùng Boolean cắt Cylinder để được vòng cung cong hoàn hảo
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=2.0, depth=H_BORDER, location=(1, -1, H_BORDER/2))
    b = bpy.context.active_object
    
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=1.8, depth=H_ASPHALT, location=(1, -1, H_ASPHALT/2))
    a = bpy.context.active_object
    
    # Cắt rỗng phần lõi của Asphalt (Inner Corner)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.2, depth=1, location=(1, -1, 0))
    cut_cyl = bpy.context.active_object
    mod = a.modifiers.new('Cut', 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.object = cut_cyl
    bpy.context.view_layer.objects.active = a
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut_cyl)
    
    # Xóa các phần ngoài phạm vi Quadrant [-1, 1]
    for obj in [b, a]:
        box = make_box("cut", (2,2,2), (0,0,0))
        mod = obj.modifiers.new('Int', 'BOOLEAN')
        mod.operation = 'INTERSECT'
        mod.object = box
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(box)
        
    combine("Tile_Corner", [b], [a], mb, ma)

def build_t_junction(mb, ma):
    # T-Junction kết nối North, South, East
    b = make_box("b", (TILE_S, TILE_S, H_BORDER), (0,0,H_BORDER/2))
    
    a1 = make_box("a1", (ASPHALT_W, TILE_S, H_ASPHALT), (0,0,H_ASPHALT/2))
    a2 = make_box("a2", (TILE_S/2, ASPHALT_W, H_ASPHALT), (TILE_S/4, 0, H_ASPHALT/2))
    
    combine("Tile_TJunction", [b], [a1, a2], mb, ma)

def build_cross(mb, ma):
    # Ngã tư 
    b = make_box("b", (TILE_S, TILE_S, H_BORDER), (0,0,H_BORDER/2))
    a1 = make_box("a1", (ASPHALT_W, TILE_S, H_ASPHALT), (0,0,H_ASPHALT/2))
    a2 = make_box("a2", (TILE_S, ASPHALT_W, H_ASPHALT), (0,0,H_ASPHALT/2))
    combine("Tile_Cross", [b], [a1, a2], mb, ma)

reset()
mb = new_mat("Mat_Border", (1,1,1,1))
ma = new_mat("Mat_Asphalt", (0.1,0.1,0.1,1))

build_straight(mb, ma)
build_corner(mb, ma)
build_t_junction(mb, ma)
build_cross(mb, ma)

print("✅ Đã tạo và Export toàn bộ Tile ra FBX!")
