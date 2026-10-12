# 🏭 GAME DESIGN DOCUMENT (GDD)
# Dự án: Factory Reborn (Di Sản Nhà Máy)

---

## 1. TỔNG QUAN DỰ ÁN (EXECUTIVE SUMMARY)
* **Tên dự án (Dự kiến):** Factory Reborn (hoặc Tycoon Factory)
* **Thể loại:** 3D Top-Down / Arcade Tycoon / Quản lý sản xuất
* **Nền tảng mục tiêu:** PC & WebGL (Thiết kế kiến trúc sẵn sàng mở rộng sang Mobile sau này)
* **Tiêu chuẩn hiệu năng:** Cam kết duy trì ổn định **60 FPS trên cả máy cấu hình yếu và WebGL** nhờ hệ thống Object Pooling khử rác RAM (0 Bytes GC Alloc), GPU Instancing (gộp Draw Call) và triệt tiêu tải trọng vật lý CPU.
* **Phong cách hình ảnh:** 3D Low-Poly hoạt hình (Stylized Cartoon, phong cách KayKit)
* **Thời gian thực hiện:** 1 tháng (Dự án bài tập trên lớp - Ưu tiên cốt lõi vững chắc, hoàn thiện chức năng trước, trang trí sau)

---

## 2. CỐT TRUYỆN & BỐI CẢNH (STORY & THEME)
* **Cốt truyện:** Nhân vật chính được thừa kế một nhà máy cũ nát, hoang phế từ người cha để lại cùng một khoản nợ nhỏ. Để khôi phục lại danh tiếng của gia đình, người con bắt đầu từ những nguyên liệu thô sơ nhất, tự tay vận hành và nâng cấp nhà máy trở thành một đế chế công nghiệp hiện đại.
* **Bối cảnh:** Một khu đất thoáng đãng. Nhà máy được xây dựng theo kiểu **Lộ thiên (Open-roof)** trên nền gạch móng vững chắc với tường lửng bao quanh, giúp góc nhìn từ trên cao xuống luôn thoáng đãng, không bị mái nhà che khuất tầm nhìn.

---

## 3. VÒNG LẶP CỐT LÕI (CORE GAMEPLAY LOOP)

```
        ┌────────────────────────────────────────────────────────┐
        │  1. SẢN XUẤT & BĂNG CHUYỀN (PRODUCE & CONVEY)           │
        │  Máy nhả vật phẩm lên Băng chuyền đưa về Bệ chứa       │
        └──────────────────────────┬─────────────────────────────┘
                               │
                               ▼
        ┌────────────────────────────────────────────────────────┐
        │  2. VẬN CHUYỂN & BÁN HÀNG (DELIVER & SELL)             │
        │  Nhân vật/Công nhân bốc từ Bệ chứa đem đến Sell Zone   │
        └──────────────────────────┬─────────────────────────────┘
                               │
                               ▼
        ┌────────────────────────────────────────────────────────┐
        │  3. THU GOM TIỀN (COLLECT MONEY)                       │
        │  Rút tiền từ Két tiền / Bàn thanh toán vào ví cá nhân  │
        └──────────────────────────┬─────────────────────────────┘
                               │
                               ▼
        ┌────────────────────────────────────────────────────────┐
        │  4. NÂNG CẤP & MỞ KHÓA (UPGRADE & EXPAND)              │
        │  - Nâng cấp máy (Tier, tốc độ nhả, tốc độ băng chuyền) │
        │  - Nâng cấp sức chứa Bệ chứa (Storage Capacity)        │
        │  - Thuê NPC công nhân hỗ trợ bốc vác từ bệ chứa        │
        │  - Mở khóa máy tài nguyên mới giá trị hơn              │
        └──────────────────────────┬─────────────────────────────┘
                               │
                               └──────── (Lặp lại vòng lặp)
```

---

## 4. HỆ THỐNG TÀI NGUYÊN & TIẾN TRÌNH (RESOURCE PROGRESSION)

Dựa trên bộ asset **KayKit Resource Bits**, chuỗi mở khóa tài nguyên sẽ đi từ giá trị thấp đến cao.

### 4.1. Chuỗi bậc tài nguyên (6 Loại máy sản xuất)
1. **Gỗ (Wood):** Nguyên liệu khởi đầu, giá rẻ, tốc độ nhả hàng nhanh. **ĐẶC BIỆT: Máy Gỗ số 1 (Tier 1) được mở khóa sẵn MIỄN PHÍ ngay khi vào game** để người chơi có vốn khởi nghiệp, tránh tình trạng kẹt game ban đầu (Anti-Softlock).
2. **Đá (Stone):** Khai thác khoáng sản cơ bản.
3. **Đồng (Copper):** Kim loại sơ cấp.
4. **Sắt (Iron):** Kim loại công nghiệp phổ thông.
5. **Bạc (Silver):** Kim loại quý, giá trị cao.
6. **Vàng (Gold):** Tài nguyên tối thượng của nhà máy, giá trị đắt đỏ nhất.

### 4.2. Cấp bậc & Thay đổi ngoại hình máy (3 Tiers Distinct Visuals)
Mỗi loại máy có thể nâng cấp qua 3 cấp độ với **ngoại hình thay đổi rõ rệt để dễ phân biệt bằng mắt**:
* **Tier 1 (Máy rỉ sét / Khung gỗ):** Khung máy thô sơ, màu xỉn, phễu nhỏ $\rightarrow$ Nhả ra từng mảnh nhỏ (mẩu gỗ, quặng vụn). Giá trị: $X.
* **Tier 2 (Máy thép gia cố):** Khung thép vững chắc, có đèn báo hiệu nhấp nháy $\rightarrow$ Nhả ra bó buộc dây / đống nhỏ. Giá trị: $3X.
* **Tier 3 (Máy mạ đồng/vàng hiện đại):** Máy móc bọc giáp bóng bẩy, ống xả khói động lực, hiệu ứng hạt vận hành $\rightarrow$ Nhả ra cả kiện lớn đóng pallet gỗ. Giá trị: $10X.

### 4.3. Bảng Cân bằng Kinh tế & Giá trị Tài nguyên (Economy Balancing Matrix)

| Loại tài nguyên | Giá mở khóa máy mới | Giá bán Tier 1 ($X) | Giá bán Tier 2 ($3X) | Giá bán Tier 3 ($10X) | Chi phí lên Tier 2 | Chi phí lên Tier 3 |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| **1. Gỗ (Wood)** | **$0 (Miễn phí)** | $2 | $6 | $20 | $150 | $400 |
| **2. Đá (Stone)** | $200 | $5 | $15 | $50 | $350 | $900 |
| **3. Đồng (Copper)** | $800 | $15 | $45 | $150 | $1,200 | $3,000 |
| **4. Sắt (Iron)** | $3,000 | $40 | $120 | $400 | $4,500 | $10,000 |
| **5. Bạc (Silver)** | $10,000 | $100 | $300 | $1,000 | $12,000 | $25,000 |
| **6. Vàng (Gold)** | $25,000 | $250 | $750 | $2,500 | $30,000 | $60,000 |

* **Nâng cấp Tốc độ nhả hàng (Spawn Interval - 4 cấp):**
  * Tốc độ: Cấp 1 (Gốc): `3.0s` | Cấp 2: `2.2s` | Cấp 3: `1.5s` | Cấp 4 (Max): `1.0s`
  * Chi phí nâng cấp cơ bản (Gỗ): Cấp 2 = $150 | Cấp 3 = $400 | Cấp 4 = $1,000 (Tự động nhân theo hệ số kinh tế của từng dòng tài nguyên trong `MachineConfigSO` để đảm bảo cân bằng).
* **Nâng cấp Sức chứa bệ nhả (Storage Capacity - 4 cấp):**
  * Dung lượng: Cấp 1 (Gốc): `10 món` | Cấp 2: `15 món` | Cấp 3: `20 món` | Cấp 4 (Max): `30 món`
  * Chi phí nâng cấp cơ bản (Gỗ): Cấp 2 = $100 | Cấp 3 = $250 | Cấp 4 = $600 (Nhân theo hệ số kinh tế từng dòng).
* **Nhân bản máy cùng loại:** Cho phép mua tối đa **3 máy** mỗi cụm tài nguyên (Máy 2: bằng 150% giá mở khóa gốc, Máy 3: bằng 250% giá mở khóa gốc).

### 4.4. Quản lý Mở rộng & Công nhân NPC (Factory Expansion & NPC Workers)
* **Cụm Dây Chuyền Modular & Vị Trí Máy (Machine Slots):**
  * Mỗi khu vực sản xuất được tổ chức thành một Cụm Dây Chuyền (`ProductionLine`), cho phép Designer kéo thả định vị trước các vị trí đặt máy (`MachineSlot`) trực quan trong Inspector.
  * Hỗ trợ cụm đa máy linh hoạt: Các slot trong cùng một cụm có thể chứa cùng loại máy (nhân bản máy) hoặc chứa **các loại máy bổ trợ khác nhau** mà không làm thay đổi hay xung đột code.
* **Đội ngũ Công nhân NPC (Worker NPCs):**
  * Người chơi có thể thuê NPC hỗ trợ việc bốc vác hàng hóa tự động. Mỗi máy cho phép thuê tối đa **2 Công nhân NPC** (Giá thuê cơ bản: NPC 1 = $300, NPC 2 = $800, nhân theo hệ số cấp máy).
  * **Quy tắc phân quyền rõ ràng (Dedicated Worker):** Mỗi NPC được **gán trực tiếp vào một bệ máy/bệ chứa cụ thể**. NPC chỉ đến đúng bệ của mình để lấy hàng và mang ra Sell Zone, loại bỏ hoàn toàn tình trạng tranh chấp vị trí.
  * **Cơ chế chống kẹt chờ (Patience Timeout):** NPC có sức chứa tối đa 3 món. Khi bốc đồ từ Bệ chứa:
    * Nếu bệ có $\ge 3$ món $\rightarrow$ Bốc đủ 3 món và đi bán ngay.
    * Nếu bệ chỉ có $1 - 2$ món $\rightarrow$ Bốc số món hiện có và **chờ tối đa 1.5 giây**. Nếu sau 1.5s không có thêm món mới rơi xuống $\rightarrow$ Lập tức di chuyển ra Sell Zone đi bán, không đứng kẹt vô thời hạn.
  * **Chống xuyên tường & Công trình:** NPC điều hướng bằng hệ thống **NavMesh**. Tường bao được nướng (bake) tĩnh, máy móc mới gắn `NavMeshObstacle` (Carve = true) $\rightarrow$ NPC tuyệt đối không đi xuyên tường hay máy móc.
  * **Chống kẹt xô đẩy:** Tắt va chạm vật lý giữa Người chơi và NPC (`Physics.IgnoreLayerCollision(Player, WorkerNPC)`) để hai bên lướt qua nhau mượt mà khi đi chung đường.

---

## 5. CƠ CHẾ ĐIỀU KHIỂN & HOẠT ẢNH (CONTROLS & ANIMATIONS)
* **Camera:** Góc nhìn Top-down nghiêng chéo (Isometric Perspective), gắn cố định góc quay và mượt mà bám theo vị trí nhân vật (Camera Follow).
* **Quản lý Điều khiển Tập trung (`InputManager`):**
  * Tất cả tín hiệu đầu vào từ bàn phím/chuột/gamepad đều được chuẩn hóa và quản lý tập trung thông qua `InputManager`.
  * `PlayerMovement` chỉ nhận vector di chuyển `(X, Y)` và các component chức năng chỉ lắng nghe sự kiện nhấn phím từ `InputManager`.
  * **Cơ chế Khóa Điều Khiển Khi Mở UI (Menu Input Freeze):**
    * Khi mở Bảng Quản Lý Nhà Máy (`FactoryManagementUI`) hoặc Bảng Cài đặt, `InputManager` tạm ngắt tín hiệu di chuyển của nhân vật và giải phóng con trỏ chuột (`CursorLockMode.None`), ngăn ngừa tình trạng nhân vật vô tình chạy lung tung hoặc rơi khỏi sàn trong lúc thao tác bấm nút.
    * Đóng bảng Menu bằng phím `Tab`, `Esc` hoặc nút `X` trên Canvas $\rightarrow$ Tự động mở lại quyền điều khiển di chuyển.
* **Các phím điều khiển mặc định:**
  * **Di chuyển:** `W, A, S, D` - Nhân vật tự xoay mặt theo hướng di chuyển.
  * **Tương tác chức năng:** Phím `F` (hỗ trợ ấn từng lần hoặc giữ phím để nhặt hàng đều đặn mỗi `0.5s`).
  * **Phím tắt mở nhanh Menu Quản Lý:** Phím `Tab` hoặc `M`.
  * **Phím tắt mở Cài Đặt:** Phím `Esc` (nếu đang mở menu/tablet thì đóng menu đó trước; chỉ mở Cài đặt khi không có menu nào đang bật).
* **Hoạt ảnh & Cơ chế vác đồ (Head-Stack Carry System):**
  * **Sức chứa vác đồ:** Mặc định ban đầu **5 món**.
  * **Vác đồ hỗn hợp (Mixed Stacking):** Cho phép người chơi vác lẫn lộn nhiều loại tài nguyên trên đầu (VD: 2 Gỗ + 2 Đồng). Từng món đồ mang thông tin giá trị riêng theo ScriptableObject.
  * Khi nhân vật nhặt vật phẩm, vật phẩm được **xếp chồng ngay trên đỉnh đầu** (theo trục Y).
  * **Hoạt ảnh:** Nhân vật chuyển sang trạng thái **giơ 2 tay lên cao đỡ vật phẩm trên đầu** (`Two-Hand Carry / Overhead Hold`). Khi chạy, hai tay vẫn giữ thế đỡ đồ, tạo nét hoạt hình vui nhộn và trực quan.
* **Quy tắc Tương tác & Nhịp độ (Pacing):**
  * **Khu vực Bán hàng (Sell Zone): TỰ ĐỘNG (Auto-Trigger)** - Hỗ trợ cả **Người chơi lẫn Công nhân NPC** thông qua giao diện chung `ICarryable`. Khi bất kỳ đối tượng vác hàng bước vào vòng tròn Sell Zone, hàng trên đầu tự động xả bán lần lượt (`0.5s/món`), tiền chuyển trực tiếp vào Két tiền tích lũy.
  * **Phím `F` dùng cho các trạm (Bán kính 2.6m):**
    * Bấm `F` tại **Bệ chứa hàng (Storage Pad)** ở cuối băng chuyền: Bốc vật phẩm lên đầu theo nhịp **0.5s/món** (ngăn chặn tình trạng hút sạch bệ tức thì trong 1 frame, tạo cảm nhận chân thực về sức nặng hàng hóa).
    * Bấm `F` tại **Két tiền (Money Vault)**: Rút toàn bộ số tiền tích lũy vào ví cá nhân.
    * Bấm `F` tại **Bảng Quản Lý Nhà Máy (Upgrade Station)** hoặc **Cỗ Máy (MachineController)**: Mở trực tiếp Menu Quản Lý & Nâng Cấp Tablet UI.
    * Bấm `F` tại **Bệ Mở Khóa In-World (Machine Unlock Pad)**: Đặt tại vị trí các cỗ máy chưa mở khóa (Đá, Đồng, Sắt, Bạc, Vàng). Người chơi có thể đến gần bấm `F` để mở khóa máy trực tiếp tại sàn xưởng mà không bắt buộc phải mở qua Menu UI.

---

## 6. HỆ THỐNG LƯU TRỮ TIẾN TRÌNH (PERSISTENCE & AUTO-SAVE)
* Game tự động lưu tiến trình (Auto-save) sau mỗi lần rút tiền két, mỗi lần mua/nâng cấp máy hoặc công nhân, và tự động lưu định kỳ mỗi **30 giây**.
* **Định danh theo Slot ID:** Dữ liệu máy móc được lưu theo chuỗi `slotID` duy nhất (VD: `Cluster_Wood_Slot01`), đảm bảo tuyệt đối không bị mất hoặc lệch dữ liệu khi bổ sung các loại máy mới vào cụm.
* **Cơ chế Chống Thất Thoát Tài Sản Khi Thoát Game (Anti-Loss Asset Liquidation):**
  * **Chỉ kích hoạt khi THOÁT GAME (`OnApplicationQuit`):** Hệ thống tự động tính tổng giá trị toàn bộ vật phẩm đang nằm trên đầu người chơi/NPC và trên các Bệ chứa hàng, sau đó chuyển đổi thành tiền mặt nạp thẳng vào Két tiền (`MoneyVault`).
  * **Khi lưu trong lúc chơi thông thường (Auto-Save):** Không thanh lý hàng để bảo toàn nguyên vẹn số hàng đang tích trữ trên bệ chứa và băng tải của người chơi.
  * Khi người chơi tải lại game, tài sản của họ được bảo toàn 100% bằng tiền, sàn xưởng được làm sạch gọn gàng, tránh triệt để lỗi kẹt vật thể 3D hay lệch vị trí băng chuyền khi tải lại Scene.
* **Cơ chế Reset Dữ Liệu Khi Chơi Lại Từ Đầu (Full Factory Reset):**
  * Xóa hoàn toàn file lưu `PlayerPrefs`.
  * Khóa lại toàn bộ các máy cấp cao (`isUnlocked = false`), ẩn model máy và tái hiện lại bệ mở khóa `MachineUnlockPad` in-world.
  * Chỉ duy nhất Cụm Gỗ (Tier 1, Tốc độ 0) được giữ mở khóa mặc định ($0).
  * Tiêu hủy toàn bộ công nhân đã thuê và dọn sạch hàng hóa tồn đọng.
  * Nạp lại Scene ban đầu để người chơi bắt đầu lại từ con số $0 một cách hoàn hảo.

---

## 7. ĐIỀU KIỆN THẮNG CUỘC (WIN CONDITION)
Để đảm bảo mục tiêu bài tập có điểm dừng rõ ràng và cân đối thời lượng trải nghiệm:
1. **Mở khóa thành công toàn bộ 6 dòng tài nguyên** (Gỗ $\rightarrow$ Đá $\rightarrow$ Đồng $\rightarrow$ Sắt $\rightarrow$ Bạc $\rightarrow$ Vàng).
2. **Nâng cấp ít nhất 1 máy của mỗi dòng tài nguyên lên cấp tối đa (Tier 3)**.
3. **Đạt mốc tích lũy tài chính:** Sở hữu tối thiểu **$50,000** trong ví cá nhân.
* **Xử lý sau khi Thắng:**
  * Màn hình Victory xuất hiện thông báo bạn đã khôi phục thành công danh tiếng của Nhà máy, cung cấp tùy chọn:
    * **"Tiếp tục chơi tự do" (Free Play):** Đánh dấu cờ `hasClaimedVictory = true` vào file Save để Popup Victory không lặp lại phiền toái, cho phép người chơi tiếp tục mở rộng quy mô nhà máy.
    * **"Chơi lại từ đầu" (Restart):** Kích hoạt cơ chế Full Factory Reset và nạp lại Scene sạch hoàn toàn.
