# Quy Tắc Xuất File 3D (Blender -> Unity) & Import Settings

Khi chuyển đổi model 3D và textures từ Blender sang Unity cho dự án này, luôn tuân thủ các quy tắc sau:

## 1. Cấu hình Texture Import (Rất quan trọng)
- **Normal Map (`*_Normal.png`):**
  - Luôn phải được cấu hình `Texture Type` = **`Normal map`** (`TextureImporterType.NormalMap`, giá trị `textureType: 1` trong `.meta`).
  - Không được để ở `Default`, vì Unity sẽ hiểu sai thành ảnh màu RGB phẳng, khiến model bị biến dạng màu sắc và mất khối 3D.
  - Trong Material (URP Lit / Standard), luôn kích hoạt từ khóa `_NORMALMAP` (`mat.EnableKeyword("_NORMALMAP")`) và gán vào property `_BumpMap`.
- **Base Color (`*_BaseColor.png`):**
  - Để `Texture Type` = `Default`, `sRGB (Color Texture)` = `true`.
- **Metallic / Roughness (`*_MetallicRoughness.png`):**
  - `sRGB (Color Texture)` = `false` (Linear).

## 2. Kích thước & Trục tọa độ Model Chibi
- **Tọa độ chân:** Chân nhân vật luôn căn chỉnh chính xác tại $Y = 0.00\text{m}$ (mặt đất).
- **Chiều cao tiêu chuẩn:** Tầm $1.85\text{m} - 1.90\text{m}$.
- **HeadSocket / CarrySocket:** Điểm neo vác đồ trên đỉnh đầu nhân vật luôn đặt tại $Y = 1.85\text{m}$ (trên đỉnh đầu chibi) để thùng/ván gỗ không bị thụt vào trong đầu hoặc lơ lửng quá cao.

## 3. Hoạt Ảnh (Animation)
- Các clip hoạt ảnh chu kỳ (`Idle`, `Walk`, `WalkCarry`) khi export FBX vào Unity phải luôn bật `loopTime = true` trong `ModelImporter`.
