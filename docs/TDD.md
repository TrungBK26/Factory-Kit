# 🛠️ TECHNICAL DESIGN DOCUMENT (TDD)
# Thiết Kế Kỹ Thuật Tổng Thể: Factory Reborn

Tài liệu này đóng vai trò là **Bản đồ Thiết kế Kỹ thuật Trung tâm (Master Architecture Document)** của dự án **Factory Reborn**. 

Hệ thống tài liệu được tổ chức theo mô hình **Module Hóa (Modular Documentation)** kết hợp với **"Code là Tài liệu Sống" (Living Documentation)**:
* **TDD & Module Docs:** Đặc tả kiến trúc, luồng sự kiện, giao diện công khai (Public APIs) và máy trạng thái.
* **C# Source Code (`Assets/_Project/Scripts/`):** Là nguồn chân lý duy nhất (Single Source of Truth) cho chi tiết cài đặt, được chú thích bằng XML Comments (`/// <summary>`).

---

## 1. NGUYÊN TẮC THIẾT KẾ CỐT LÕI (CORE ARCHITECTURAL PRINCIPLES)

Nhằm đảm bảo quy tắc vàng: *"Khi thêm một tính năng mới, tuyệt đối không bao giờ làm hỏng tính năng cũ"*, dự án tuân thủ nghiêm ngặt 4 nguyên tắc:

### 1.1. Kiến Trúc Hướng Sự Kiện (Event-Driven Architecture / Observer Pattern)
* Các hệ thống không được gọi trực tiếp lẫn nhau (No tight coupling).
* Sử dụng lớp trung gian [GameEvents](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Core/GameEvents.cs) chứa các C# `Action` tĩnh không sinh rác bộ nhớ (Zero Allocation).
* **Ví dụ:** Khi bán hàng, [SellZone](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Factory/SellZone.cs) chỉ phát `GameEvents.OnItemsSold?.Invoke(value)`. [MoneyVault](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Factory/MoneyVault.cs), [SoundManager](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Core/SoundManager.cs), và [VFXManager](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Core/VFXManager.cs) tự đăng ký lắng nghe để thực thi nhiệm vụ tương ứng mà không cần `SellZone` phải biết đến sự tồn tại của chúng.

### 1.2. Nguyên Tắc Trách Nhiệm Đơn Lẻ (Single Responsibility Principle - SRP)
* Mỗi script chỉ phụ trách đúng 1 chức năng duy nhất:
  * [InputManager](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Core/InputManager.cs): Chỉ thu nhận và chuẩn hóa tín hiệu bàn phím/chuột.
  * [PlayerMovement](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Player/PlayerMovement.cs): Chỉ di chuyển nhân vật theo góc nhìn camera.
  * [PlayerCarry](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Player/PlayerCarry.cs): Chỉ quản lý ngăn xếp hàng hóa trên đầu.
  * [ConveyorBelt](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Factory/ConveyorBelt.cs): Chỉ tịnh tiến vật phẩm dọc băng tải.
  * [StoragePad](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/Assets/_Project/Scripts/Factory/StoragePad.cs): Chỉ quản lý sức chứa và bốc dỡ hàng tại bệ cuối băng tải.

### 1.3. Lập Trình Phòng Vệ & Tự Tìm Kiếm (Defensive Coding)
* Sử dụng `[RequireComponent(typeof(T))]` trên đầu class để Unity tự động đính kèm component bắt buộc.
* Luôn sử dụng `TryGetComponent<T>(out var component)` thay vì `GetComponent<T>()` trực tiếp để triệt tiêu hoàn toàn lỗi `NullReferenceException`.

### 1.4. Tương Thích Tuyệt Đối Unity 6 (Domain Reload & Subsystem Safety)
* Mọi Manager sử dụng mẫu Singleton đều bắt buộc có hàm:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics() { Instance = null; }
```
* Ngăn chặn hoàn toàn lỗi `MissingReferenceException` khi chạy trong môi trường tắt Domain Reload của Unity 6.

---

## 2. BẢN ĐỒ CẤU TRÚC THƯ MỤC DỰ ÁN

```text
Assets/
├── _Project/                           <-- Toàn bộ tài nguyên tự phát triển
│   ├── Scripts/
│   │   ├── Core/                       <-- Quản lý vòng đời game, âm thanh, VFX, kinh tế
│   │   │   ├── GameManager.cs
│   │   │   ├── InputManager.cs         <-- Chuẩn hóa tín hiệu đầu vào (WASD, F, Tab, Esc)
│   │   │   ├── EconomyManager.cs       <-- Quản lý số dư ví người chơi
│   │   │   ├── GameEvents.cs           <-- C# Action tĩnh trung tâm
│   │   │   ├── SoundManager.cs         <-- Quản lý BGM & SFX
│   │   │   ├── VFXManager.cs           <-- Quản lý hiệu ứng hạt và chữ số tiền nổi
│   │   │   ├── SaveManager.cs          <-- Tự động lưu/đọc dữ liệu 2 chiều an toàn
│   │   │   ├── ResourcePoolManager.cs  <-- Object Pooling tái chế vật phẩm (Zero GC)
│   │   │   └── WinConditionChecker.cs  <-- Đánh giá điều kiện chiến thắng & Free Play
│   │   ├── Player/                     <-- Điều khiển & vác đồ nhân vật
│   │   │   ├── PlayerMovement.cs
│   │   │   ├── PlayerInteraction.cs
│   │   │   ├── PlayerCarry.cs
│   │   │   └── PrototypeTestHUD.cs
│   │   ├── Factory/                    <-- Cơ chế máy móc & trạm sản xuất
│   │   │   ├── IInteractable.cs        <-- Interface tương tác chung (Phím F)
│   │   │   ├── ICarryable.cs           <-- Interface vác đồ chung cho Player & NPC
│   │   │   ├── MachineSlot.cs          <-- Dữ liệu và trạng thái slot máy đa hình
│   │   │   ├── ProductionLine.cs       <-- Cụm dây chuyền sản xuất khép kín
│   │   │   ├── FactoryManager.cs       <-- Điều phối các cụm máy & danh sách slot
│   │   │   ├── MachineController.cs    <-- Điều khiển nhả hàng theo nhịp
│   │   │   ├── ConveyorBelt.cs         <-- Băng chuyền vận chuyển hàng
│   │   │   ├── StoragePad.cs           <-- Bệ chứa hàng pallet cuối chuyền
│   │   │   ├── WorkerNPC.cs            <-- AI công nhân tự động bốc hàng
│   │   │   ├── SellZone.cs             <-- Trạm bán hàng tự động
│   │   │   ├── MoneyVault.cs           <-- Két tiền tích lũy doanh thu
│   │   │   ├── UpgradeStation.cs       <-- Bệ điều khiển mở menu quản lý
│   │   │   └── MachineUnlockPad.cs     <-- Bệ mở khóa máy in-world (Phím F)
│   │   ├── Data/                       <-- Dữ liệu ScriptableObject & SaveData
│   │   │   ├── ResourceDataSO.cs
│   │   │   ├── MachineConfigSO.cs
│   │   │   ├── ResourceItemHolder.cs
│   │   │   └── GameSaveData.cs
│   │   ├── UI/                         <-- Giao diện người dùng
│   │   │   ├── UIManager.cs
│   │   │   ├── HUDView.cs
│   │   │   ├── FactoryManagementUI.cs  <-- Menu quản lý nâng cấp máy & tuyển dụng
│   │   │   ├── MachineCardView.cs      <-- Thẻ hiển thị từng slot máy trên UI
│   │   │   ├── SettingsPopup.cs        <-- Bảng cài đặt âm lượng
│   │   │   └── VictoryPopup.cs         <-- Màn hình chiến thắng
│   │   └── Editor/                     <-- Công cụ Editor tự động
│   ├── Models/                         <-- Chứa 3D Mesh (.fbx / .obj) phân loại theo nhóm
│   │   ├── Characters/                 <-- Model Player, Worker NPC
│   │   ├── Machines/                   <-- Model máy móc các Tier
│   │   ├── Resources/                  <-- Model khối hàng hóa (Gỗ, Đá, Vàng...)
│   │   └── Environment/                <-- Model bệ pallet, băng chuyền, két tiền, trạm bán
│   ├── Animations/                     <-- Hoạt ảnh nhân vật
│   │   ├── Controllers/                <-- Animator Controller (.controller)
│   │   └── Clips/                      <-- Animation clips (.anim)
│   ├── Textures/                       <-- Ảnh màu vẽ tay / Color Palette map
│   ├── Prefabs/                        <-- Khối Prefab hoàn chỉnh (Model + Collider + Scripts)
│   ├── Materials/                      <-- Vật liệu màu sắc hoạt hình
│   ├── ScriptableObjects/              <-- Cấu hình dữ liệu tài nguyên và máy móc
│   └── Scenes/
│       ├── MainScene.unity
│       └── PrototypeScene.unity
└── ThirdParty/                         <-- Tài nguyên bên ngoài (KayKit...)
```

---

## 3. DANH MỤC CÁC MODULE CHI TIẾT (MODULAR DOCUMENTATION INDEX)

Tài liệu chi tiết của từng hệ thống được phân tách thành các module chuyên biệt dưới thư mục `docs/modules/`:

| Module | Tên Tài Liệu | Nội Dung Chính | Liên Kết |
|---|---|---|---|
| **01** | **Core Architecture** | Event Bus, Singleton Unity 6, Object Pooling, Âm thanh, VFX, Điều kiện thắng | [01_Core_Architecture.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/01_Core_Architecture.md) |
| **02** | **Player Systems** | Input tập trung, Di chuyển camera, Tương tác F (`IInteractable`), Vác đồ (`ICarryable`) | [02_Player_Systems.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/02_Player_Systems.md) |
| **03** | **Factory Production** | Máy 3 Tier, Băng chuyền, Bệ chứa pallet, Bệ mở khóa máy 3D, Setup 6 dây chuyền | [03_Factory_Production.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/03_Factory_Production.md) |
| **04** | **Worker NPC AI** | FSM 5 trạng thái, AI tìm đường, Tích hợp KayKit Animator, Tuyển dụng & Giải tán | [04_Worker_NPC.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/04_Worker_NPC.md) |
| **05** | **Economy & Save** | Dòng tiền ví/két, Trạm bán SellZone, Thanh lý bảo toàn hàng tồn, Khôi phục 2 chiều | [05_Economy_Save.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/05_Economy_Save.md) |
| **06** | **UI Systems** | HUD thời gian thực, Menu nâng cấp thẻ máy (Card View), Cài đặt âm lượng, Màn hình thắng | [06_UI_Systems.md](file:///d:/lamviec/Unity%20project/D%E1%BB%B1%20%C3%A1n%20ch%C3%ADnh/Factory%20Kit/docs/modules/06_UI_Systems.md) |

---

## 4. QUY CHUẨN GHI CHÚ DEBUG & THEO DÕI LỖI (DEBUGGING STANDARD)

Mọi thông báo `Debug.Log` trong toàn bộ mã nguồn bắt buộc tuân theo tiền tố định danh:

| Tiền Tố (Tag) | Khu Vực Hoạt Động | Ví Dụ Minh Họa |
|---|---|---|
| `[Player]` | Di chuyển, Vác đồ, Bấm phím tương tác | `Debug.Log("[Player] Đã vác 1 vật phẩm. Tải trọng: 3/5");` |
| `[Machine]` | Nhả hàng, Trạng thái đầy bệ chứa | `Debug.Log("[Machine] Bệ chứa đã đầy (10/10), tạm ngưng nhả hàng.");` |
| `[Economy]` | Giao dịch, Bán hàng, Rút tiền két, Nâng cấp | `Debug.Log("[Economy] Đã rút vào ví: $150. Số dư hiện tại: $1250");` |
| `[SaveManager]` | Lưu hoặc Nạp tiến trình game | `Debug.Log("[SaveManager] Đã tự động lưu dữ liệu.");` |
| `[Audio]` | Phát SFX hoặc BGM | `Debug.Log("[Audio] Đang phát SFX: Cash_Register");` |

---

## 5. LỘ TRÌNH CHUYỂN ĐỔI MODEL: TỪ PROTOTYPE SANG MODEL KAYKIT

Dự án được thiết kế độc lập hoàn toàn giữa logic và ngoại hình hình học:

1. **Giai đoạn Khối Hộp (Prototype):**
   * Sử dụng Capsule cho Player/NPC và Cube cho máy móc, bệ chứa, khối hàng.
   * Hoàn thiện 100% logic: Di chuyển, va chạm trigger, vác đồ, nâng cấp, lưu game.
2. **Giai đoạn Thay Thế Model KayKit:**
   * Kéo Model KayKit vào làm con của đối tượng (ẩn khối hình học cũ).
   * Cấu hình Rig thành **Humanoid**.
   * Di chuyển điểm `carrySocket` vào làm con của xương đầu (`Head Bone`) hoặc xương tay (`Hand Bone`). Khi nhân vật bước đi và đầu lắc lư theo animation, các vật phẩm vác trên đầu sẽ **tự động lắc lư theo nhịp chân** mà không cần sửa bất kỳ dòng code nào.
