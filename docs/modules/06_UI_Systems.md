# 🖥️ Module 06: Giao Diện Người Dùng & Quản Lý (UI Systems)

Tài liệu này đặc tả kiến trúc hiển thị giao diện người dùng, menu nâng cấp máy móc dạng thẻ (Card View), bảng cài đặt và màn hình chiến thắng.

---

## 1. MÀN HÌNH CHÍNH (`HUDView.cs`)

### 1.1. Mục tiêu & Cơ chế Hybrid
* Hiển thị các thông số thời gian thực quan trọng nhất:
  * Số dư ví người chơi ($).
  * Lời nhắc tương tác khi đứng gần trạm (`[F] Rút tiền`, `[F] Mở khóa...`).
  * Phím tắt mở menu nâng cấp (`[Tab]`).
* **Hỗ trợ Cơ chế Hybrid (uGUI + OnGUI Fallback):**
  * Ưu tiên sử dụng hệ thống Canvas uGUI hiện đại (`TextMeshProUGUI`, `Image`, `Button`).
  * Nếu chạy trong Scene Prototype chưa dựng Canvas, script tự động chuyển sang chế độ vẽ `OnGUI` dự phòng để lập trình viên vẫn test được game bình thường mà không phát sinh lỗi.

---

## 2. BẢNG QUẢN LÝ NHÀ MÁY & NÂNG CẤP (`FactoryManagementUI.cs` & `MachineCardView.cs`)

### 2.1. Kích hoạt Menu
Menu quản lý có thể được mở/đóng bằng 2 cách:
1. Nhấn phím `Tab` trên bàn phím (thông qua `InputManager.OnMenuTogglePressed`).
2. Đi lại gần bệ điều khiển `UpgradeStation` in-world và nhấn phím `F`.

### 2.2. Thẻ Điều Khiển Từng Slot Máy (`MachineCardView.cs`)
Mỗi slot máy trong nhà máy được đại diện bằng một Thẻ thông tin trực quan:

| Mục nâng cấp | Cấp độ tối đa | Tác dụng | Công thức giá nâng cấp |
|---|---|---|---|
| **Tier Level** | Tier 3 | Mở khóa công nghệ mới, đổi model máy, nhả nguyên liệu giá trị cao gấp nhiều lần | $\text{Cost} = \text{BaseTierCost} \times \text{CurrentTier}$ |
| **Speed Level** | Level 3 | Tăng tốc độ chu kỳ nhả hàng của máy (+25% mỗi cấp) | $\text{Cost} = 50 \times (\text{SpeedLevel} + 1)$ |
| **Storage Level**| Level 3 | Mở rộng diện tích bệ chứa pallet (+5 món mỗi cấp) | $\text{Cost} = 40 \times (\text{StorageLevel} + 1)$ |
| **Tuyển Dụng NPC** | 2 NPC / Slot | Tự động hóa bốc hàng từ bệ sang trạm bán SellZone | $\text{Cost} = 100 \times (\text{WorkerCount} + 1)$ |

### 2.3. Tự Động Khóa Nút Khi Thiếu Tiền (Interactive State Gating)
* Khi số dư ví của người chơi thay đổi, `MachineCardView` tự động kiểm tra `EconomyManager.Instance.HasEnoughMoney(cost)`.
* Nếu không đủ tiền: Nút bấm chuyển sang màu xám (Disabled) để tránh lỗi thao tác thừa.

---

## 3. BẢNG CÀI ĐẶT ÂM THANH (`SettingsPopup.cs`)
* Cung cấp 2 thanh trượt (Sliders):
  * **SFX Slider:** Điều chỉnh âm lượng hiệu ứng tiếng động.
  * **BGM Slider:** Điều chỉnh âm lượng nhạc nền.
* Giá trị được tự động ghi nhớ vào `PlayerPrefs` để giữ nguyên cho các phiên chơi sau.

---

## 4. MÀN HÌNH CHIẾN THẮNG (`VictoryPopup.cs`)

### 4.1. Kích hoạt
* Lắng nghe sự kiện `GameEvents.OnGameVictory`.
* Tự động bật Modal chúc mừng, tạm dừng di chuyển của nhân vật (`InputManager.IsInputBlocked = true`).

### 4.2. Các Lựa Chọn Sau Chiến Thắng
1. **Tiếp Tục Chơi (Free Play):**
   * Đóng bảng chiến thắng, mở khóa lại di chuyển.
   * Người chơi tiếp tục tận hưởng nhà máy tự động hóa hoàn toàn.
2. **Khởi Động Lại Nhà Máy (Reset Game):**
   * Gọi `SaveManager.Instance.ResetSaveData()`.
   * Xóa toàn bộ tiến trình, đưa người chơi về vạch xuất phát ban đầu để thử thách lại.
