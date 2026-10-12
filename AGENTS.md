# Hướng Dẫn & Quy Tắc Dự Án Factory Kit

## 1. Không Đặt Lại Scene / Không Reset Vị Trí Thủ Công Của User
- Tuyệt đối KHÔNG tự ý di chuyển hoặc đặt lại tọa độ của các GameObject do User đã tự tay căn chỉnh (Xưởng gỗ, mỏ đá, các máy sản xuất, terrain, etc.).
- Tất cả các script Editor tự động chạy bằng `[InitializeOnLoadMethod]` đã được gỡ bỏ hoặc kiểm soát qua flag để bảo toàn vị trí scene.

## 2. Quy Tắc 3D Model & Texture (Blender -> Unity)
- **Normal Map (`*_Normal.png`):** Luôn chuyển `Texture Type` sang **`Normal map`** (`TextureImporterType.NormalMap`). Đồng thời bật keyword `_NORMALMAP` trên Material (`_BumpMap`).
- **Pivot:** Luôn đảm bảo đáy model/chân nhân vật ở $Y = 0.0$.
- **CarrySocket / HeadSocket:** Đối với nhân vật chibi, đỉnh đầu nằm ở $Y = 1.85\text{m}$.
- **Animation Loop:** Luôn thiết lập `loopTime = true` cho các clip `Idle`, `Walk`, `WalkCarry`.
