# 🏃 Module 02: Hệ Thống Người Chơi (Player Systems)

Tài liệu này đặc tả cơ chế điều khiển, di chuyển, tương tác vật lý và hệ thống vác vật phẩm xếp tầng của nhân vật người chơi.

---

## 1. BỘ THU NHẬN TÍN HIỆU TẬP TRUNG (`InputManager.cs`)

### 1.1. Mục tiêu
* Tập trung hóa 100% việc đọc phím bấm từ người chơi vào một nơi duy nhất.
* Các script khác (`PlayerMovement`, `PlayerInteraction`, UI) **tuyệt đối không gọi trực tiếp** `Input.GetKey`, `Input.GetAxis` hay `UnityEngine.InputSystem`.
* Dễ dàng mở rộng sang Mobile Joystick hoặc Gamepad mà không phải sửa lại code nhân vật.

### 1.2. API & Tín hiệu Đầu ra
* **Trục di chuyển:** `public Vector2 MovementInput { get; }` (Giá trị chuẩn hóa từ -1 đến 1).
* **Tín hiệu tương tác:** `public Action OnInteractPressed;` (Kích hoạt khi nhấn phím `F` hoặc nút Action ảo).
* **Bật/Tắt Menu nâng cấp:** `public Action OnMenuTogglePressed;` (Kích hoạt khi nhấn phím `Tab`).
* **Hủy / Đóng cửa sổ:** `public Action OnCancelPressed;` (Kích hoạt khi nhấn phím `Escape`).

### 1.3. Khóa Input khi mở UI (UI Input Gating)
* Khi bất kỳ cửa sổ giao diện nào mở ra (ví dụ: `FactoryManagementUI`, `VictoryPopup`, `SettingsPopup`), `InputManager` hỗ trợ cờ:
```csharp
public bool IsInputBlocked { get; set; }
```
* Khi `IsInputBlocked = true`, `MovementInput` tự động trả về `Vector2.zero` và vô hiệu hóa phím bấm để nhân vật không bị trôi khi người chơi thao tác trên UI.

---

## 2. DI CHUYỂN THEO GÓC NHÌN CAMERA (`PlayerMovement.cs`)

### 2.1. Cấu hình & Component bắt buộc
* `[RequireComponent(typeof(CharacterController))]`
* Tham số có thể tinh chỉnh:
  * `moveSpeed` (Mặc định: 6.0f)
  * `rotationSpeed` (Mặc định: 720.0f deg/s)
  * `gravity` (Mặc định: -9.81f m/s²)

### 2.2. Thuật toán Di chuyển
1. Nhận `MovementInput` từ `InputManager.Instance`.
2. Chiếu vector chuyển động theo góc xoay của `Camera.main` trên mặt phẳng nằm ngang ($XZ$ plane):
```csharp
Vector3 forward = cameraTransform.forward;
Vector3 right = cameraTransform.right;
forward.y = 0; right.y = 0;
Vector3 desiredMove = (forward.normalized * input.y + right.normalized * input.x).normalized;
```
3. Xoay thân nhân vật mượt mà về hướng di chuyển bằng `Quaternion.RotateTowards`.
4. Áp dụng trọng lực và gọi `CharacterController.Move(velocity * Time.deltaTime)`.

---

## 3. TƯƠNG TÁC THẾ GIỚI (`PlayerInteraction.cs` & `IInteractable.cs`)

### 3.1. Giao diện Tương tác Chung (`IInteractable.cs`)
Mọi vật thể trong thế giới mà người chơi có thể bấm `F` để tương tác (Két tiền `MoneyVault`, Bảng nâng cấp `UpgradeStation`, Bệ mở khóa máy `MachineUnlockPad`) đều cài đặt interface này:

```csharp
public interface IInteractable
{
    string GetInteractionPrompt();     // Chuỗi nhắc nhở hiển thị trên HUD (VD: "[F] Rút tiền", "[F] Mở khóa $100")
    void Interact(GameObject player);   // Logic thực thi khi nhấn phím F
}
```

### 3.2. Cơ chế Phát Hiện Mục Tiêu (Detection Trigger)
* Sử dụng `SphereCollider` gắn cờ `isTrigger` đặt tại chân nhân vật để quét các đối tượng có cài đặt `IInteractable`.
* Ưu tiên mục tiêu gần nhất (`Closest Interactable`).
* Khi mục tiêu thay đổi, thông báo lên HUD thông qua sự kiện hiển thị chuỗi prompt tương ứng.

---

## 4. HỆ THỐNG VÁC ĐỒ TRÊN ĐẦU (`PlayerCarry.cs` & `ICarryable.cs`)

### 4.1. Giao diện Vác Đồ (`ICarryable.cs`)
Dùng chung cho cả Người chơi (`PlayerCarry`) và Công nhân AI (`WorkerNPC`):

```csharp
public interface ICarryable
{
    int MaxCapacity { get; }
    int CurrentCount { get; }
    bool CanCarryMore();
    bool AddItem(GameObject item);
    GameObject PopTopItem();
}
```

### 4.2. Cơ chế Xếp Tầng Khối Hàng (Vertical Stacking)
* Khối hàng được đưa vào làm con của `carrySocket` (Transform đặt tại đỉnh đầu người chơi).
* Mỗi vật phẩm tiếp theo được đặt đè lên trên vật phẩm trước theo trục $Y$:
```csharp
float stackHeight = itemIndex * itemSpacingY; // itemSpacingY thường từ 0.35m - 0.5m
item.transform.localPosition = new Vector3(0, stackHeight, 0);
item.transform.localRotation = Quaternion.identity;
```
* Tắt Rigidbody và Collider của vật phẩm khi đang vác để triệt tiêu ma sát và xung đột vật lý với cơ thể người chơi.

### 4.3. Tích hợp Animation Layer
* Khi `CurrentCount > 0`, kích hoạt `Animator.SetLayerWeight(carryLayerIndex, 1f)` và thiết lập cờ `IsCarrying = true` để chuyển đổi dáng đi từ thả lỏng sang giơ hai tay giữ đồ trên đầu.
* Khi `CurrentCount == 0`, mờ dần trọng số layer về 0.
