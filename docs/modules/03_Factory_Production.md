# ⚙️ Module 03: Dây Chuyền Sản Xuất & Tự Động Hóa (Factory Production)

Tài liệu này đặc tả cơ chế vận hành của máy móc, băng tải vận chuyển, bệ chứa thành phẩm, cấu trúc cụm dây chuyền và quy trình nâng cấp.

---

## 1. MÔ HÌNH DỮ LIỆU SCRIPTABLEOBJECT (DATA ASSETS)

### 1.1. `ResourceDataSO.cs`
Định nghĩa thông số cho từng loại tài nguyên:
* `resourceName`: Tên hiển thị ("Gỗ", "Đá", "Đồng", "Sắt", "Bạc", "Vàng").
* `resourceType`: Enum định danh loại tài nguyên (`ResourceType`).
* `tier`: Cấp độ nguyên liệu (1, 2, 3).
* `baseValue`: Giá trị bán lẻ mặc định ($).
* `itemPrefab`: Prefab hiển thị (Khối hộp Prototype hoặc Model KayKit).

### 1.2. `MachineConfigSO.cs`
Cấu hình thông số kỹ thuật và ngoại hình 3 Tier cho từng loại máy:
* `machineName`: Tên máy.
* `unlockCost`: Chi phí mở khóa máy ban đầu ($).
* `tier1VisualPrefab`, `tier2VisualPrefab`, `tier3VisualPrefab`: 3 Prefab đại diện cho 3 cấp độ ngoại hình.
* `tier1Resource`, `tier2Resource`, `tier3Resource`: Loại tài nguyên nhả ra tương ứng với từng cấp độ.

---

## 2. CẤU TRÚC CỤM DÂY CHUYỀN (`ProductionLine.cs` & `MachineSlot.cs`)

### 2.1. Cụm Dây Chuyền (`ProductionLine`)
* Một cụm dây chuyền đại diện cho 1 loại tài nguyên khép kín, bao gồm:
  1. `MachineSlot`: Vị trí đặt máy sản xuất.
  2. `ConveyorBelt`: Băng chuyền đưa hàng ra bệ chứa.
  3. `StoragePad`: Bệ chứa hàng pallet cuối băng chuyền.
  4. `WorkerNPC[]`: Danh sách công nhân AI phụ trách cụm này (Tối đa 2 công nhân / cụm).
  5. `MachineUnlockPad`: Bệ mở khóa máy bằng phím F trong không gian 3D.

### 2.2. Vị Trí Máy Đa Hình (`MachineSlot`)
* Lưu trữ trạng thái cấp độ độc lập của từng máy:
  * `isUnlocked`: Đã được mở khóa hay chưa.
  * `currentTier`: Cấp độ máy (Tier 1 -> Tier 3).
  * `currentSpeedLevel`: Cấp độ tốc độ sản xuất (Level 0 -> 3).
  * `currentStorageLevel`: Cấp độ sức chứa bệ hàng (Level 0 -> 3).
* **Cơ chế Hoán đổi Ngoại hình (Visual Swap):**
  * Khi nâng cấp Tier, máy cũ bị hủy (hoặc ẩn đi), Prefab ngoại hình của Tier mới được khởi tạo ngay tại `spawnPoint` với góc xoay chuẩn xác.

---

## 3. CƠ CHẾ SẢN XUẤT VÀ VẬN CHUYỂN

### 3.1. Bộ Điều Khiển Máy (`MachineController.cs`)
* Hoạt động theo chu kỳ đếm nhịp (`productionTimer += Time.deltaTime`).
* **Thời gian chu kỳ sản xuất:**
$$\text{ProductionInterval} = \frac{\text{BaseInterval}}{1.0 + (\text{SpeedLevel} \times 0.25)}$$
* **Kiểm tra chống tràn hàng:**
  * Trước khi nhả vật phẩm, máy kiểm tra `StoragePad.IsFull`.
  * Nếu bệ chứa đã đầy $\rightarrow$ Máy tạm ngưng nhả hàng và chuyển trạng thái chờ (Idle) để tránh nghẽn băng chuyền.

### 3.2. Băng Chuyền Tịnh Tiến (`ConveyorBelt.cs`)
* Tự động phát hiện các vật phẩm rơi vào vùng kích hoạt (`OnTriggerStay`).
* Tịnh tiến tọa độ của vật phẩm theo vector hướng dọc băng chuyền:
```csharp
item.transform.position += beltDirection * (beltSpeed * Time.deltaTime);
```
* Đẩy vật phẩm nhẹ nhàng trượt vào vị trí trống trên `StoragePad`.

### 3.3. Bệ Chứa Hàng Pallet (`StoragePad.cs`)
* **Sắp xếp theo lưới không gian (Grid Layout):**
  * Vật phẩm được xếp thành các hàng và cột trên mặt phẳng pallet (ví dụ: lưới $2 \times 2$ hoặc $3 \times 3$), sau đó xếp chồng lên nhau thành các tầng.
* **Sức chứa mở rộng:**
$$\text{MaxCapacity} = \text{BaseCapacity} + (\text{StorageLevel} \times \text{ExtraCapacityPerLevel})$$
* **Giao diện lấy hàng:**
  * Hỗ trợ hàm `PopItem()` trả về vật phẩm ở tầng trên cùng cho Player hoặc NPC.

---

## 4. BỆ MỞ KHÓA MÁY TRONG THẾ GIỚI 3D (`MachineUnlockPad.cs`)

### 4.1. Mục tiêu
* Cho phép người chơi đứng vào bệ tròn trước slot máy và nhấn `F` để trả tiền mở khóa máy trực tiếp thay vì chỉ dùng UI.
* Tự động ẩn bệ đi sau khi máy đã được mở khóa, và hiển thị lại khi người chơi Reset Game.

### 4.2. Luồng Tương Tác
1. Người chơi lại gần bệ $\rightarrow$ `GetInteractionPrompt()` hiển thị: `[F] Mở khóa Máy Gỗ - $150`.
2. Người chơi nhấn `F`:
   * Kiểm tra `EconomyManager.Instance.CurrentWallet >= unlockCost`.
   * Trừ tiền ví: `EconomyManager.Instance.SpendMoney(unlockCost)`.
   * Kích hoạt dây chuyền: `productionLine.UnlockSlot(slot)`.
   * Bệ tự tắt `gameObject.SetActive(false)`.
