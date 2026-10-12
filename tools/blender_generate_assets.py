import bpy
import math
import os

# ==============================================================================
# FACTORY REBORN - PROCEDURAL LOW-POLY ASSET GENERATOR FOR BLENDER
# ==============================================================================
# Script này tự động tạo các model 3D chuẩn phong cách Low-Poly / Stylized
# và xuất thẳng file .fbx vào thư mục Assets của Unity project.
# ==============================================================================

# Đường dẫn tự động nhận diện thư mục dự án Unity
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__)) if "__file__" in locals() else "D:/lamviec/Unity project/Dự án chính/Factory Kit/tools"
PROJECT_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, ".."))
OUTPUT_RESOURCES_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Resources")
OUTPUT_ENV_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Models", "Environment")

os.makedirs(OUTPUT_RESOURCES_DIR, exist_ok=True)
os.makedirs(OUTPUT_ENV_DIR, exist_ok=True)

def clear_scene():
    """Dọn sạch scene hiện tại trong Blender."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)

def create_stylized_material(name, color, roughness=0.6, metallic=0.0):
    """Tạo Material màu phẳng hoạt hình (Flat Stylized Material)."""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        # Base Color (RGBA)
        bsdf.inputs['Base Color'].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs['Roughness'].default_value = roughness
        if 'Metallic' in bsdf.inputs:
            bsdf.inputs['Metallic'].default_value = metallic
    return mat

def set_pivot_to_bottom(obj):
    """Đặt điểm neo (Pivot Point / Origin) về chính giữa đáy vật thể để dễ đặt trong Unity."""
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    # Hạ điểm Z về đáy
    bbox = [obj.matrix_world @ mathutils.Vector(corner) for corner in obj.bound_box]
    min_z = min(v.z for v in bbox)
    cursor = bpy.context.scene.cursor
    cursor.location = (obj.location.x, obj.location.y, min_z)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    cursor.location = (0, 0, 0)
    obj.location = (0, 0, 0)

import mathutils

def export_fbx(obj, filepath):
    """Xuất đối tượng đang chọn ra file FBX chuẩn trục Unity (Y-up, scale 1.0)."""
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE'
    )
    print(f"[Exported] -> {filepath}")

# ------------------------------------------------------------------------------
# 1. TẠO KHÚC GỖ (WOOD LOG)
# ------------------------------------------------------------------------------
def generate_wood_log():
    clear_scene()
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8,
        radius=0.22,
        depth=0.7,
        end_fill_type='NGON',
        location=(0, 0, 0)
    )
    obj = bpy.context.active_object
    obj.name = "SM_Item_Wood"
    
    # Xoay nằm ngang
    obj.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(rotation=True)

    # Bevel nhẹ các cạnh cho mềm mại
    mod = obj.modifiers.new(name="Bevel", type='BEVEL')
    mod.width = 0.03
    mod.segments = 1
    bpy.ops.object.modifier_apply(modifier="Bevel")

    mat_wood = create_stylized_material("M_Wood_Bark", (0.45, 0.28, 0.15), roughness=0.8)
    obj.data.materials.append(mat_wood)

    set_pivot_to_bottom(obj)
    export_fbx(obj, os.path.join(OUTPUT_RESOURCES_DIR, "SM_Item_Wood.fbx"))

# ------------------------------------------------------------------------------
# 2. TẠO TẢNG ĐÁ QUẶNG (STONE / ROCK)
# ------------------------------------------------------------------------------
def generate_stone_ore():
    clear_scene()
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.3, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = "SM_Item_Stone"

    # Làm méo ngẫu nhiên để tạo dáng đá tự nhiên
    for v in obj.data.vertices:
        scale_mod = 1.0 + (math.sin(v.co.x * 12.0) * 0.18) + (math.cos(v.co.z * 10.0) * 0.15)
        v.co.x *= scale_mod
        v.co.y *= (scale_mod * 0.9)
        v.co.z *= 0.85

    # Flat shading để có hiệu ứng low-poly đặc trưng
    bpy.ops.object.shade_flat()

    mat_stone = create_stylized_material("M_Stone_Ore", (0.52, 0.55, 0.58), roughness=0.9)
    obj.data.materials.append(mat_stone)

    set_pivot_to_bottom(obj)
    export_fbx(obj, os.path.join(OUTPUT_RESOURCES_DIR, "SM_Item_Stone.fbx"))

# ------------------------------------------------------------------------------
# 3. TẠO THỎI KIM LOẠI (INGOT: COPPER, IRON, SILVER, GOLD)
# ------------------------------------------------------------------------------
def generate_metal_ingot(name, filename, color, metallic=0.85, roughness=0.3):
    clear_scene()
    # Tạo khối hình hộp
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name

    # Co giãn thành tỷ lệ thỏi vàng: Dài 0.55, Rộng 0.28, Cao 0.16
    obj.scale = (0.55, 0.28, 0.16)
    bpy.ops.object.transform_apply(scale=True)

    # Làm nhỏ mặt trên để tạo dạng hình thang (Trapezoid Ingot)
    for v in obj.data.vertices:
        if v.co.z > 0:
            v.co.x *= 0.82
            v.co.y *= 0.82

    # Vát mép các cạnh (Bevel)
    mod = obj.modifiers.new(name="Bevel", type='BEVEL')
    mod.width = 0.025
    mod.segments = 2
    bpy.ops.object.modifier_apply(modifier="Bevel")

    mat = create_stylized_material(f"M_{name}", color, roughness=roughness, metallic=metallic)
    obj.data.materials.append(mat)

    set_pivot_to_bottom(obj)
    export_fbx(obj, os.path.join(OUTPUT_RESOURCES_DIR, f"{filename}.fbx"))

# ------------------------------------------------------------------------------
# 4. TẠO BỆ PALLET GỖ KÊ HÀNG (STORAGE PALLET)
# ------------------------------------------------------------------------------
def generate_pallet():
    clear_scene()
    
    # 3 thanh đà dưới
    beams = []
    for i, x in enumerate([-0.4, 0.0, 0.4]):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x, 0, 0.05))
        b = bpy.context.active_object
        b.scale = (0.08, 0.95, 0.1)
        bpy.ops.object.transform_apply(scale=True)
        beams.append(b)

    # 5 thanh ván mặt trên
    planks = []
    for y in [-0.4, -0.2, 0.0, 0.2, 0.4]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, y, 0.12))
        p = bpy.context.active_object
        p.scale = (0.95, 0.15, 0.04)
        bpy.ops.object.transform_apply(scale=True)
        planks.append(p)

    # Gộp tất cả thành 1 đối tượng
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = beams[0]
    bpy.ops.object.join()
    pallet = bpy.context.active_object
    pallet.name = "SM_Storage_Pallet"

    mat_pallet = create_stylized_material("M_Pallet_Wood", (0.68, 0.52, 0.35), roughness=0.85)
    pallet.data.materials.append(mat_pallet)

    set_pivot_to_bottom(pallet)
    export_fbx(pallet, os.path.join(OUTPUT_ENV_DIR, "SM_Storage_Pallet.fbx"))

# ------------------------------------------------------------------------------
# 5. TẠO ĐOẠN BĂNG CHUYỀN (CONVEYOR SEGMENT)
# ------------------------------------------------------------------------------
def generate_conveyor():
    clear_scene()
    
    # Khung đế
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.3))
    frame = bpy.context.active_object
    frame.scale = (0.7, 1.2, 0.3)
    bpy.ops.object.transform_apply(scale=True)

    # Mặt dây đai đen phía trên
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.46))
    belt = bpy.context.active_object
    belt.scale = (0.58, 1.2, 0.04)
    bpy.ops.object.transform_apply(scale=True)

    # 4 chân đế
    for x in [-0.3, 0.3]:
        for y in [-0.5, 0.5]:
            bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.04, depth=0.3, location=(x, y, 0.15))
            leg = bpy.context.active_object
            leg.select_set(True)

    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = frame
    bpy.ops.object.join()
    conveyor = bpy.context.active_object
    conveyor.name = "SM_Conveyor_Segment"

    mat_metal = create_stylized_material("M_Conveyor_Frame", (0.3, 0.35, 0.4), roughness=0.5, metallic=0.6)
    conveyor.data.materials.append(mat_metal)

    set_pivot_to_bottom(conveyor)
    export_fbx(conveyor, os.path.join(OUTPUT_ENV_DIR, "SM_Conveyor_Segment.fbx"))

# ==============================================================================
# THỰC THI TOÀN BỘ QUY TRÌNH
# ==============================================================================
def main():
    print(">>> [Factory Reborn] Bắt đầu sinh tài nguyên 3D tự động...")
    
    # 1. Gỗ & Đá
    generate_wood_log()
    generate_stone_ore()

    # 2. Bốn loại thỏi kim loại
    generate_metal_ingot("SM_Item_Copper", "SM_Item_Copper", (0.85, 0.45, 0.25), metallic=0.8, roughness=0.35)
    generate_metal_ingot("SM_Item_Iron",   "SM_Item_Iron",   (0.55, 0.58, 0.62), metallic=0.7, roughness=0.4)
    generate_metal_ingot("SM_Item_Silver", "SM_Item_Silver", (0.88, 0.90, 0.92), metallic=0.9, roughness=0.2)
    generate_metal_ingot("SM_Item_Gold",   "SM_Item_Gold",   (1.00, 0.78, 0.15), metallic=0.95, roughness=0.25)

    # 3. Pallet & Băng chuyền
    generate_pallet()
    generate_conveyor()

    print(">>> [HOÀN TẤT 100%] Toàn bộ 8 model đã được xuất thành công vào Assets/_Project/Models/!")

if __name__ == "__main__":
    main()
