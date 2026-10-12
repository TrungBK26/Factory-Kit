# 📋 GAME FEATURE DOCUMENT (GFD)
# Đặc Tả Tính Năng Chi Tiết: Factory Reborn

Tài liệu này phân rã và quy định chi tiết hoạt động của từng tính năng trong game để phục vụ quá trình lập trình và kiểm thử.

---

## 1. TÍNH NĂNG 1: HỆ THỐNG ĐIỀU KHIỂN & INPUT MANAGER (INPUT & PLAYER CONTROLLER)

### 1.1. Bộ Quản Lý Tín Hiệu Đầu Vào (`InputManager`)
* **Mô tả:** Đóng vai trò là tầng trung gian thu nhận, chuẩn hóa toàn bộ tín hiệu điều khiển từ phần cứng (Bàn phím, Chuột, Gamepad hoặc Virtual Joystick trên Mobile sau này).
* **Quy tắc Kiến trúc:**
  * Các component logic (như `PlayerMovement`, `PlayerInteraction`, `UIManager`) **không được gọi trực tiếp `Input.GetKey()` hay `Input.GetAxis()`**; thay vào đó chỉ đọc dữ liệu hoặc đăng ký lắng nghe sự kiện từ `InputManager`.
  * **Các kênh tín hiệu quản lý:**
    * `MovementInput`: Trả về `Vector2(X, Y)` chuẩn hóa (chuỗi phím `WASD` hoặc cụm phím mũi tên).
    * `OnInteractPressed`: C# `Action` kích hoạt khi nhấn phím `F` (hoặc nút bấm Action trên màn hình cảm ứng). Hỗ trợ cả ấn từng lần và giữ phím với nhịp giãn cách `0.5s/lần`.
    * `OnMenuTogglePressed`: C# `Action` khi bấm `Tab` / `M` (Mở bảng Quản lý Nhà máy).
    * `OnSettingsTogglePressed`: C# `Action` khi bấm `Esc` (Bật/tắt menu Cài đặt). Lưu ý: Nếu đang mở Bảng Quản Lý hoặc Popup khác, phím `Esc` sẽ ưu tiên đóng cửa sổ đó trước, khi không còn cửa sổ nào mới mở Bảng Cài Đặt.
  * **Lợi ích mở rộng:** Giúp dự án dễ dàng bổ sung các phím chức năng mới trong tương lai (Lướt nhanh Dash, Bật bản đồ, Phím tắt nâng cấp...) và sẵn sàng hỗ trợ đổi phím (Key Rebinding) hoặc cắm Gamepad/Mobile Touch mà không làm ảnh hưởng đến bất kỳ code nhân vật nào.

### 1.2. Di chuyển Nhân vật (`PlayerMovement`)
* **Mô tả:** Nhân vật di chuyển trên mặt phẳng 3D theo tín hiệu từ `InputManager`.
* **Quy tắc:**
  * Đọc `Vector2` từ `InputManager.MovementInput`, tính toán hướng di chuyển theo góc nhìn của Camera Top-down nghiêng (Isometric).
  * Nhân vật tự động xoay mặt mượt mà (`Smooth Slerp Rotation`) theo hướng đang di chuyển.
  * Tốc độ di chuyển cố định: `5.0 units/giây` (có thể tinh chỉnh trong Inspector).
  * Không di chuyển xuyên qua các vật cản (Máy móc, Băng chuyền, Tường, Bờ rào) nhờ `CharacterController` hoặc `Rigidbody`.

### 1.3. Hệ thống Tương tác phím `F` (`PlayerInteraction`)
* **Mô tả:** Phát hiện và tương tác với các trạm/vật thể chức năng xung quanh nhân vật khi nhận tín hiệu từ `InputManager`.
* **Quy tắc:**
  * Sử dụng một vùng Trigger hoặc SphereCast bán kính `2.6m` quanh nhân vật để tìm kiếm các GameObject có gắn component kế thừa interface `IInteractable` (dò qua `col.GetComponentInParent<IInteractable>()` để bắt trúng cả khi collider nằm ở object con của Model 3D).
  * **Hiển thị gợi ý tương tác (Interaction Prompt):**
    * Khi bước vào bán kính `2.6m` của trạm, hiển thị bảng chữ nổi 3D (Floating UI / Canvas Overlay): VD: `[F] Nhặt Gỗ từ Bệ chứa (5/10)`, `[F] Bảng Nâng Cấp Máy Đốn Gỗ (Tier 1/3)`, `[F] Mở Bảng Quản Lý Trung Tâm`, `[F] Rút tiền (+$150)`.
    * Trạm tương tác có hiệu ứng viền sáng nhẹ (Highlight Outline) giúp người chơi nhận biết đối tượng mục tiêu.
    * Khi người chơi bước ra xa (`> 2.6m`), bảng gợi ý tự động ẩn đi.
  * **Kích hoạt tương tác:** Khi nhận sự kiện `OnInteractPressed` từ `InputManager`, gọi trực tiếp hàm `OnInteract(playerCarry)` trên trạm mục tiêu đó. Hỗ trợ giữ phím `F` nhặt liên tục mỗi `0.5s`.

---

## 2. TÍNH NĂNG 2: HỆ THỐNG VÁC ĐỒ TRÊN ĐẦU & HOẠT ẢNH (HEAD CARRY & ANIMATION)

* **Mô tả:** Nhân vật vác vật phẩm xếp chồng trên đỉnh đầu và chuyển sang tư thế giơ 2 tay lên đỡ.
* **Quy tắc:**
  * **Sức chứa tối đa (Max Capacity):** Mặc định ban đầu là **5 món**.
  * **Điểm gắn đồ (Carry Socket):** Một Empty GameObject đặt chính xác tại **đỉnh đầu** (`Y = 1.2m` trên model Capsule hoặc gắn vào Xương Đầu `Head Bone` của KayKit).
  * **Hiệu ứng xếp chồng (Visual Head-Stacking):**
    * Món đồ đầu tiên gắn vào vị trí Carry Socket trên đầu.
    * Các món đồ tiếp theo xếp chồng thẳng đứng lên trên món trước theo trục Y (offset `+0.4m` mỗi tầng).
  * **Cơ chế Vác đồ hỗn hợp (Mixed Stacking):** Người chơi có thể bốc nhiều loại tài nguyên khác nhau lên đầu cùng lúc (VD: 2 Gỗ + 3 Đồng). Mỗi vật phẩm trên đầu lưu trữ tham chiếu đến `ResourceDataSO` riêng biệt để phục vụ tính tiền chính xác từng món khi bán.
  * **Hoạt ảnh Animator:**
    * Khi số lượng vật phẩm `> 0`: Kích hoạt cờ `isCarrying = true` trong Animator.
    * Nhân vật kích hoạt Layer hoạt ảnh phần thân trên: **Giơ 2 tay lên cao đỡ kiện hàng trên đầu** (`TwoHand_Overhead_Hold`).
    * Khi di chuyển hoặc đứng yên, 2 tay vẫn giơ lên đỡ đồ.
    * Khi trả hết hàng (`count == 0`): `isCarrying = false`, 2 tay hạ xuống bình thường.
  * **Chống nhặt quá tải:** Nếu đã đầy tải (5/5), bấm `F` tại bệ nhả đồ sẽ không nhặt thêm và hiển thị thông báo nổi: *"Đã đầy sức chứa (5/5)!"*.

---

## 3. TÍNH NĂNG 3: MÁY SẢN XUẤT, BĂNG CHUYỀN & BỆ CHỨA (SPAWNER, CONVEYOR BELT & STORAGE PAD)

* **Mô tả:** Chuỗi sản xuất khép kín bao gồm: Cỗ máy nhả nguyên liệu $\rightarrow$ Rơi lên Băng chuyền vận chuyển $\rightarrow$ Đổ vào Bệ chứa thành phẩm để người chơi/NPC đến bốc vác đi bán.

### 3.1. Máy Sản Xuất & Bậc Ngoại Hình (Resource Spawner & 3 Tiers)
* **Quy tắc Khởi đầu (Anti-Softlock):**
  * **Máy Gỗ số 1 (Tier 1) được MỞ KHÓA SẴN MIỄN PHÍ** ngay khi bắt đầu game, tự động nhả mẩu gỗ đầu tiên xuống băng chuyền.
* **3 Cấp độ ngoại hình phân biệt rõ ràng (Visual Distinction):**
  * **Tier 1 (Máy rỉ sét/khung gỗ):** Kích thước nhỏ, màu gỗ/nâu xỉn, phễu xả đơn giản $\rightarrow$ Nhả ra từng mảnh vụn.
  * **Tier 2 (Máy thép hiện đại):** Kích thước trung bình, màu kim loại xám/xanh, có ống dẫn và đèn tín hiệu $\rightarrow$ Nhả ra bó buộc dây / đống quặng.
  * **Tier 3 (Máy công nghiệp tối tân):** Kích thước bề thế, màu vàng/đồng bóng bẩy, ống khói nhả hơi nước $\rightarrow$ Nhả ra kiện hàng lớn đóng pallet.
* **Chu kỳ nhả (Spawn Interval - 4 cấp):**
  * Cấp 1 (Gốc): `3.0s/lần` | Cấp 2: `2.2s/lần` | Cấp 3: `1.5s/lần` | Cấp 4 (Tối đa): `1.0s/lần`.
* **Cơ chế nhả hàng:** Khi đồng hồ chu kỳ hoàn tất, vật phẩm được sinh ra tại họng xả và rơi chính xác xuống đầu đón của Băng chuyền.

### 3.2. Hệ Thống Băng Chuyền Vận Chuyển (Conveyor Belt System)
* **Mô tả:** Băng tải chuyển động liên tục kết nối từ họng xả của máy dẫn thẳng tới Bệ chứa thành phẩm.
* **Quy tắc Vận hành:**
  * **Tốc độ truyền tải:** Mặc định `2.0 units/giây` (đưa vật phẩm trượt thẳng tắp theo trục định hướng của băng chuyền).
  * **Hiệu ứng trực quan:** Bề mặt băng tải cuộn chuyển động liên tục (`Material Texture Offset Animation`) tạo cảm giác cơ khí sống động.
  * **Kiểm soát bốc dỡ:** Người chơi và NPC **không được phép nhặt hàng khi vật phẩm đang trôi trên băng chuyền**, đảm bảo luồng chuyển giao nguyên liệu có quy củ.

### 3.3. Bệ Chứa Thành Phẩm (Storage Pad) & Cơ Chế Ứ Đọng (Anti-Jam)
* **Mô tả:** Tấm bệ pallet đặt tại điểm kết thúc của băng chuyền, là nơi duy nhất người chơi và NPC có thể bốc vác hàng hóa.
* **Quy tắc Tiếp nhận:**
  * Vật phẩm trôi đến cuối băng chuyền sẽ tự động chuyển vào Bệ chứa và xếp ngay ngắn thành từng lớp trên pallet.
  * **Sức chứa bệ chứa (Storage Capacity - 4 cấp):**
    * Cấp 1 (Gốc): `10 món` | Cấp 2: `15 món` | Cấp 3: `20 món` | Cấp 4 (Tối đa): `30 món`.
* **Cơ chế Chống tràn & Tạm dừng sản xuất (Jamming & Overflow Prevention):**
  * Khi Bệ chứa đạt tối đa sức chứa (`currentItems == maxCapacity`): Bệ chứa từ chối tiếp nhận thêm.
  * Các kiện hàng tiếp theo sẽ **dừng lại xếp hàng trên băng chuyền**.
  * Nếu hàng trên băng chuyền dồn ứ đến tận họng xả của máy: Máy sản xuất sẽ **tạm ngưng chu kỳ nhả hàng mới** (hiển thị đèn đỏ cảnh báo bệ đầy).
  * Khi người chơi hoặc công nhân NPC bốc bớt hàng khỏi bệ chứa: Băng chuyền lập tức đẩy các kiện hàng chờ vào bệ, máy sản xuất tự động tiếp tục chu kỳ làm việc bình thường.
* **Quy tắc Bốc vác (Interaction):**
  * Khi người chơi bước vào bán kính tương tác của Bệ chứa và bấm phím `F` (hoặc giữ phím): Lần lượt bốc từng món từ bệ chứa đưa lên đầu với nhịp giãn cách `0.5s/món`, hiển thị từng món đồ nảy lên đầu lần lượt rõ ràng cho đến khi bệ hết hàng hoặc người chơi đạt tối đa tải trọng (5 món). NPC khi đến bệ cũng bốc từng món theo nhịp giãn cách này.

---

## 4. TÍNH NĂNG 4: KHU VỰC BÁN HÀNG & KÉT TIỀN (SELL ZONE & MONEY VAULT)

### 4.1. Khu vực Bán hàng (Sell Zone) - TỰ ĐỘNG
* **Mô tả:** Nơi tiếp nhận các thành phẩm do người chơi hoặc NPC mang tới.
* **Quy tắc Tự động (Auto-Trigger):**
  * Hỗ trợ đa đối tượng thông qua interface chung `ICarryable`: Người chơi hoặc NPC chỉ cần **bước chân vào vòng tròn Sell Zone** (sử dụng `OnTriggerEnter` / `OnTriggerStay`), hệ thống sẽ **tự động hút và dỡ hàng lần lượt từng món** trên đầu xuống bãi bán mà **không cần bấm phím `F`**.
  * Hỗ trợ dỡ hàng đồng thời (Multi-carrier): Nếu cả người chơi và NPC cùng bước vào SellZone, hệ thống xử lý độc lập cho từng đối tượng mà không bị nghẽn (No blocking).
  * Nhịp dỡ hàng: `0.5s/món` kèm hiệu ứng âm thanh *"ting ting"* và hiệu ứng hạt sáng (particle), tạo cảm giác nhả từng món đồ trên đầu rơi xuống bãi bán nhịp nhàng thay vì biến mất quá chớp nhoáng.
  * Duyệt qua từng món đồ (tính giá trị theo `ResourceDataSO` của món đó) và **chuyển thẳng tổng số tiền vào Két tiền (Money Vault)**.
  * Khi dỡ hết hàng, hoạt ảnh của người chơi/NPC tự động hạ tay xuống bình thường (`isCarrying = false`).

### 4.2. Két tiền (Money Vault / Collector) - BẤM `F`
* **Mô tả:** Nơi lưu giữ doanh thu bán hàng trước khi người chơi rút vào ví cá nhân.
* **Quy tắc:**
  * Hiển thị số tiền đang tích lũy dạng 3D Text hoặc Floating UI phía trên két: `💰 +$X`.
  * Có hiệu ứng nhấp nháy phát sáng nhẹ khi két có tiền (`money > 0`) để hướng dẫn trực quan người chơi đến rút.
  * Khi người chơi đến gần bấm `F`:
    * Toàn bộ số tiền tích lũy trong két được cộng vào số dư cá nhân của người chơi (`Player Wallet`).
    * Số tiền trong két reset về `$0`.
    * Kích hoạt âm thanh mở két tiền và cập nhật ngay lên thanh HUD.

---

## 5. TÍNH NĂNG 5: HỆ THỐNG QUẢN LÝ NHÀ MÁY & NÂNG CẤP (FACTORY MANAGER & UPGRADES)

Một Manager trung tâm (`FactoryManager`) kiểm soát toàn bộ chuỗi sản xuất thông qua các Cụm Dây Chuyền Modular (`ProductionLine`):

### 5.1. Cấu trúc Cụm Dây Chuyền Kéo Thả (Modular Production Line & Slots)
* Mỗi khu vực sản xuất được gắn một component `ProductionLine.cs` cho phép Level Designer cấu hình hoàn toàn qua thao tác kéo thả trong Inspector:
  * Danh sách vị trí máy (`MachineSlot[]`): Mỗi slot chứa file cấu hình riêng (`MachineConfigSO`), điểm xuất hiện Transform, và băng chuyền đón hàng.
  * Bệ chứa thành phẩm (`StoragePad`).
  * Danh sách điểm sinh của công nhân NPC (`Transform[] workerSpawnPoints`).
* **Hỗ trợ cụm đa máy linh hoạt (Heterogeneous Machine Slots):** Trong cùng một cụm, các slot có thể chứa máy cùng loại hoặc máy thuộc loại khác nhau mà code hoàn toàn không bị lỗi.

### 5.2. Nâng cấp Bậc máy (Tier Upgrade - Cấp 1 $\rightarrow$ 2 $\rightarrow$ 3)
* Đổi model ngoại hình máy (từ rỉ sét sang thép gia cố $\rightarrow$ bọc vàng).
* Đổi loại vật phẩm nhả ra (to hơn, giá trị gấp 3 lần, 10 lần theo bảng kinh tế ở GDD). Tự động fallback xuống cấp thấp hơn nếu chưa gán tài nguyên cấp cao.
* Chi phí lên Tier được quy định riêng cho từng loại tài nguyên trong `MachineConfigSO`.
* **Giới hạn an toàn (Max Clamp):** Cấp Tier bị chặn cứng trong khoảng `[1, 3]`. Khi đạt Tier 3 hoặc `tierCost == -1`, nút bấm chuyển sang trạng thái `(MAX)` và vô hiệu hóa, tuyệt đối không bị quay vòng về Tier 1.

### 5.3. Nâng cấp Tốc độ sản xuất (Spawn Rate)
* Giảm thời gian chờ giữa các lần nhả theo 4 cấp: `3.0s` $\rightarrow$ `2.2s` $\rightarrow$ `1.5s` $\rightarrow$ `1.0s`.
* Chi phí cơ bản tự động nhân theo hệ số kinh tế của máy tương ứng.
* **Giới hạn an toàn (Max Clamp):** Cấp độ tốc độ bị chặn cứng trong khoảng `[0, spawnIntervalLevels.Length - 1]`. Khi đạt cấp tối đa, nút bấm chuyển sang trạng thái `(MAX)` và vô hiệu hóa, tuyệt đối không bị quay vòng về cấp 1 (2.5s/3.0s).

### 5.4. Nâng cấp Sức chứa bệ nhả (Storage Capacity)
* Tăng sức chứa bệ chứa theo 4 cấp: `10 món` $\rightarrow$ `15 món` $\rightarrow$ `20 món` $\rightarrow$ `30 món`.
* Chi phí cơ bản tự động nhân theo hệ số kinh tế của máy tương ứng.

### 5.5. Xây thêm số lượng máy trong cụm (Multiple Machine Slots)
* Kích hoạt các slot máy tiếp theo trong cụm (Tối đa số slot được Designer thiết lập sẵn trong Inspector):
  * Kích hoạt máy tại đúng vị trí Transform đã định sẵn, không lo lỗi vị trí hay đè vật thể.
  * Cấp độ và chỉ số của từng máy được quản lý độc lập.

### 5.6. Tuyển dụng Công nhân NPC (Hire Worker NPCs)
* Mỗi bệ máy/bệ chứa có thể thuê tối đa **2 Công nhân NPC** phụ việc để tự động hóa toàn diện chuỗi vận chuyển.
* **Hiển thị chuẩn xác:** Nút thuê hiển thị đúng số lượng công nhân hiện có: `👷 Thuê NPC (Hiện có: 0/2) - $200` (không bị cộng dư `+ 1` khi chưa thuê ai).
* Khi đã đủ công nhân, nút chuyển sang trạng thái `👷 Đã Đủ NPC (2/2)` và vô hiệu hóa.

### 5.7. Bệ Mở Khóa Máy Trong Không Gian 3D (`MachineUnlockPad`)
* Bên cạnh việc mở khóa qua menu UI, mỗi cụm máy (ngoại trừ Máy Gỗ được mở sẵn) sở hữu một bệ tương tác 3D nổi bật đặt ngay trước vị trí dây chuyền.
* Bệ hiển thị bảng giá 3D nổi (Floating Text / Canvas Overlay): VD: `[F] Mở khóa Máy Đá: $100`.
* Khi người chơi tích lũy đủ tiền và bấm phím `F`:
  * Trừ số tiền tương ứng trong ví người chơi.
  * Kích hoạt máy và dây chuyền bắt đầu vận hành sản xuất.
  * Bệ `MachineUnlockPad` tự động ẩn đi (`SetActive(false)`).
* Khi tiến hành Reset toàn bộ nhà máy hoặc Chơi lại từ đầu: Các bệ `MachineUnlockPad` tự động được tái kích hoạt trở lại và máy quay về trạng thái khóa ban đầu.

### 5.8. Kiến Trúc Giao Diện Tablet Kép (`FactoryManagementUI`)
* **Tự Phục Hồi (Self-Healing Singleton):** Tự động khởi tạo ngay khi nạp Scene (`AfterSceneLoad`) và bảo vệ chống mất dữ liệu khi Domain Reload tắt (`SubsystemRegistration`).
* **Chế độ Kép Độc Lập Tài Nguyên (Dual-Mode Architecture):**
  * *Chế độ IMGUI Tablet Fallback (`OnGUI`):* Khi chưa có Canvas Prefab (`panelRoot == null`), hệ thống tự động vẽ bảng Tablet hoàn chỉnh với thanh cuộn, hiển thị ví tiền, thẻ nâng cấp 6 dòng máy, các nút Tier, Tốc độ, Kho, Thuê NPC. Đặt `GUI.depth = -100` để vẽ đè lên mọi HUD khác.
  * *Chế độ uGUI Canvas:* Khi Designer gán Prefab và Panel Canvas vào `panelRoot` và `machineCardPrefab`, code tự động chuyển sang sinh thẻ uGUI động mượt mà mà không cần sửa 1 dòng code nào.
* **Chống Nhấp Nháy (Input Debounce 0.25s):** Ngăn chặn hoàn toàn hiện tượng menu mở rồi tự đóng do sự kiện phím bấm kép.
* **Đa Cổng Truy Cập (Dual Access Points):** Mở linh hoạt bằng phím tắt `Tab` / `M` ở bất kỳ đâu, hoặc đi lại gần trạm `UpgradeStation` / cỗ máy `MachineController` bất kỳ rồi bấm phím `F`.

---

## 6. TÍNH NĂNG 6: HỆ THỐNG CÔNG NHÂN TỰ ĐỘNG (DEDICATED NPC WORKER SYSTEM)

Nhằm tối ưu hóa năng suất và loại bỏ hoàn toàn lỗi tranh chấp tài nguyên:
* **Nguyên tắc Phân quyền Tuyệt đối (Dedicated Worker):**
  * Mỗi NPC được **khởi tạo và gán cứng với đúng 1 bệ chứa duy nhất** (`assignedStoragePad`).
  * NPC của bệ nào chỉ chăm sóc đúng bệ đó: Không bao giờ chạy sang bệ khác để lấy trộm hàng.
* **Cơ chế Điều hướng NavMesh & Chống Xuyên Tường:**
  * Sàn nhà máy được nướng bản đồ đi bộ (`NavMesh Walkable`).
  * Tường bao, rào chắn được đánh dấu tĩnh (`Navigation Static`).
  * Các máy móc xuất hiện trong game được gắn component **`NavMeshObstacle` (Carve = true)** $\rightarrow$ NPC tự động nhận diện và đi vòng quanh máy móc, **tuyệt đối không bao giờ đi xuyên tường hay máy móc**.
* **Chống Kẹt Xô Đẩy (Collision Matrix):**
  * Tắt va chạm vật lý giữa Layer `Player` và Layer `WorkerNPC` để Người chơi và NPC lướt qua nhau mượt mà khi đi chung lối đi hẹp.
* **Vòng lặp hành vi State Machine chuẩn xác (NPC State Machine):**
  1. `IDLE`: Đứng chờ ở Bệ chứa thành phẩm của cụm máy được phân công nếu bệ chưa có hàng.
  2. `MOVING_TO_STORAGE`: Tìm đường (NavMeshAgent) di chuyển về phía bệ chứa được chỉ định. Nhận diện đã đến bệ khi khoảng cách `distToPad <= 2.2m` (bù trừ kích thước bệ 2.2m x 2.2m) hoặc `remainingDistance <= 1.5f` (tránh bị kẹt vì stoppingDistance quá nhỏ).
  3. `LOADING`: Đứng tại bệ chứa bốc hàng lần lượt với nhịp `0.5s/món`. Nếu bốc đủ tải (`capacity = 3 món`) hoặc bệ hết đồ sau khi đã vác $\ge 1$ món kèm thời gian kiên nhẫn `patienceTimeout = 1.5s` $\rightarrow$ Chuyển sang `MOVING_TO_SELL`. Nếu bệ rỗng không có món nào $\rightarrow$ Chuyển về `IDLE` chờ.
  4. `MOVING_TO_SELL`: Tự động tìm đường di chuyển đến Sell Zone. Nhận diện đến nơi khi `distToSell <= 2.5m` hoặc `remainingDistance <= 1.5f`.
  5. `UNLOADING`: Đứng tại Sell Zone, dỡ hàng tự động lần lượt `0.5s/món`. Khi tay vác trống (`CurrentCount == 0`), lập tức chuyển hướng `NavMeshAgent` quay về bệ chứa (`assignedStoragePad`) và chuyển sang trạng thái `MOVING_TO_STORAGE` (RETURN loop), liên tục tuần hoàn mà không bao giờ bị đứng kẹt tại bãi bán.
* **Bảo toàn Số lượng Công nhân Khi Save/Load:** Số lượng công nhân được lưu trữ theo từng cụm dây chuyền và tự động phục hồi chính xác sau khi nạp lại game hoặc giải tán toàn bộ khi Reset game.

---

## 7. TÍNH NĂNG 7: GIAO DIỆN NGƯỜI DÙNG & CÀI ĐẶT (UI & SETTINGS)

### 7.1. Màn hình Chơi chính (HUD)
* **Thanh tiền tệ (Money Display):** Hiển thị số tiền trong ví góc trên màn hình: `💰 $1,250`.
* **Thanh tải trọng (Capacity Display):** Hiển thị số lượng hàng đang vác: `📦 3/5`.
* **Nút Cài đặt (Settings Button):** Nằm ở góc màn hình để mở bảng thiết lập âm thanh.
* **Nút Mở Bảng Quản Lý (Tablet Button):** Hỗ trợ click chuột trực tiếp để mở Tablet mà không cần bấm phím.

### 7.2. Bảng Quản lý & Nâng cấp Nhà máy (Factory Management UI)
* Mở ra khi bấm `F` tại Bảng Quản Lý trung tâm, bấm `F` tại máy, hoặc ấn phím tắt `Tab` / `M`.
* **Cơ chế Khóa di chuyển (Input Lock):** Khi Menu mở, tự động vô hiệu hóa lệnh di chuyển của Player và giải phóng con trỏ chuột (`CursorLockMode.None`). Đóng Menu sẽ kích hoạt lại di chuyển.
* **Dynamic Slot Cards (Sinh thẻ tự động):**
  * Duyệt danh sách các Cụm Dây Chuyền (`ProductionLine`) và các Slot máy (`MachineSlot`) để sinh động các thẻ giao diện.
  * Hiển thị tên máy, icon, cấp độ hiện tại, trạng thái Mua/Mở khóa.
  * Cung cấp các nút: Lên Tier, Tăng tốc độ nhả, Mở rộng kho, Mở khóa máy mới, Thuê công nhân NPC (kèm chi phí tương ứng và trạng thái Max Level).
* **Nút đóng (Close Button):** Nút `X` góc trên hoặc bấm phím `Esc` / `Tab`.

### 7.3. Bảng Cài đặt (Settings Panel)
* Hỗ trợ cơ chế Kép (IMGUI Fallback `OnGUI` + uGUI Canvas): Cho phép điều chỉnh âm lượng mượt mà ngay cả khi chưa có Asset UI.
* **Thanh trượt Âm nhạc (BGM Slider):** Điều chỉnh âm lượng nhạc nền (0% - 100%), lưu vào `PlayerPrefs` và đồng bộ ngay với `SoundManager`.
* **Thanh trượt Hiệu ứng (SFX Slider):** Điều chỉnh âm lượng tiếng nhặt đồ, tiếng bán hàng, tiếng bấm nút.
* **Nút Lưu Game Nhanh:** Hỗ trợ lưu tiến trình ngay từ bảng Cài đặt.
* **Nút Đóng / Tiếp tục (Resume/Close):** Ẩn bảng cài đặt, tiếp tục chơi.

### 7.4. Màn hình Chiến thắng (Victory Popup)
* Hỗ trợ cơ chế Kép (IMGUI Fallback `OnGUI` + uGUI Canvas), đặt `GUI.depth = -120` để luôn hiển thị trên cùng.
* Tự động hiển thị khi hoàn thành đủ 3 điều kiện thắng cuộc:
  1. Mở khóa đủ 6 dòng tài nguyên (Gỗ $\rightarrow$ Vàng).
  2. Nâng cấp ít nhất 1 máy của mỗi dòng lên Tier 3.
  3. Đạt mốc tài chính tối thiểu $50,000 trong ví.
* Cung cấp 2 lựa chọn: 
  * **"Chơi tiếp" (Free Play):** Đóng popup, lưu cờ `hasClaimedVictory = true` để không hiện lại popup, cho phép tiếp tục chơi tự do.
  * **"Chơi lại từ đầu" (Restart):** Xóa toàn bộ dữ liệu Save và khởi động lại game về trạng thái sạch $0.

---

## 8. TÍNH NĂNG 8: HỆ THỐNG TỰ ĐỘNG LƯU TRỮ TIẾN TRÌNH (AUTO-SAVE & PERSISTENCE)

* **Quy tắc Lưu trữ:**
  * **Lưu thủ công (Manual Save):** Người chơi có thể chủ động bấm "Lưu Game" để tạo một điểm mốc checkpoint.
  * **Tự động lưu định kỳ (Auto-Save):** Chạy định kỳ một bộ đếm thời gian tự động lưu mỗi **30 giây** trong nền (có thể bật/tắt trong HUD phục vụ mục đích kiểm thử), và tự động lưu khi tắt game (`OnApplicationQuit`).
  * **Cơ Chế Bảo Toàn Tài Sản (Anti-Loss Item Liquidation):** Khi tắt game (`OnApplicationQuit`), toàn bộ hàng hóa đang nằm trên đầu Người chơi, Công nhân NPC và trong các Bệ chứa được tự động quy đổi thành tiền mặt nạp thẳng vào Két tiền, triệt tiêu 100% nguy cơ kẹt hoặc mất vật phẩm khi nạp lại game.
  * **Quy tắc Không Lưu Tràn Lan (Anti-Thrashing):** Các thao tác nhỏ như mua máy, nâng cấp máy hay thuê công nhân không tự động kích hoạt lưu đè ngay lập tức, đảm bảo người chơi có thể nạp lại điểm mốc checkpoint khi muốn thử nghiệm.
* **Cơ chế Đồng Bộ 2 Chiều Khi Tải Lại Game (Two-Way State Reconciliation):**
  * Khi nạp lại bản lưu (`LoadGame`):
    * Các máy có trong file save là "Mở khóa": Kích hoạt máy, khôi phục đúng cấp độ Tier, tốc độ nhả, sức chứa kho và ẩn bệ mở khóa `MachineUnlockPad`.
    * Các máy có trong file save là "Chưa mở" (hoặc máy đã lỡ mở trong phiên chơi hiện tại nhưng file save cũ chưa mở): Hệ thống tự động **khóa lại (`LockSlot`)**, ẩn GameObject của máy đi, dọn sạch hàng tồn trên bệ chứa tương ứng, và **kích hoạt hiển thị lại bệ `MachineUnlockPad`** trong không gian 3D.
    * Giải tán và tái đồng bộ đúng số lượng công nhân NPC đã lưu cho từng cụm máy.
    * Dọn sạch đồ trên tay người chơi để tránh mang nhầm tài nguyên của máy vừa bị khóa lại.
* **Định danh theo Slot ID độc lập:**
  * Dữ liệu nâng cấp từng máy được lưu theo chuỗi `slotID` (VD: `Cluster_Wood_Slot01`), hỗ trợ thêm/bớt máy linh hoạt mà không làm sai lệch bản lưu cũ.
* **Cơ chế Chống Thất Thoát Tài Sản (Asset Liquidation):**
  * **Quy tắc Chỉ Quy Đổi Khi Thoát Game (Quit-Only Liquidation):** Khi lưu tự động định kỳ hoặc khi người chơi chủ động bấm Lưu trong lúc chơi, tài nguyên trên đầu người chơi/NPC và trên bệ chứa được giữ nguyên vẹn để người chơi và NPC tiếp tục vác đồ bình thường. Cơ chế thanh lý quy đổi đồ thành tiền mặt chỉ kích hoạt khi tắt game hoàn toàn (`OnApplicationQuit` / `OnDisable`), thu hồi sạch sẽ vật thể về Pool để tránh rác RAM.
* **Cơ chế Khởi Động Lại Toàn Diện (Full Factory Reset):**
  * Khi người chơi bấm "Chơi lại từ đầu" (Restart trên bảng Victory) hoặc "Xóa File Save & Reset":
    * Số dư ví (`Player Wallet`) và két tiền (`MoneyVault`) về `$0`.
    * Cấp độ Tier của toàn bộ máy về Tier 1, Tốc độ nhả về cấp 1 (`3.0s`), Bệ chứa về cấp 1 (`10 món`).
    * Toàn bộ máy mua thêm (Đá, Đồng, Sắt, Bạc, Vàng) bị khóa và ẩn đi (`SetActive(false)`), chỉ giữ lại Máy Gỗ mặc định ban đầu (`isUnlockedByDefault = true`).
    * Toàn bộ công nhân NPC đã thuê được giải tán và xóa khỏi Scene.
    * Tất cả các bệ mở khóa `MachineUnlockPad` trong không gian 3D được kích hoạt hiển thị trở lại.
    * Tải lại Scene (`SceneManager.LoadScene`) để đảm bảo dọn sạch toàn bộ trạng thái bộ nhớ và vật thể 3D.
* **Tải lại game (Auto-load):** Khi mở lại game hoặc refresh trang WebGL, game tự động đọc dữ liệu đã lưu và tái dựng nguyên vẹn toàn bộ máy móc, công nhân và số dư tài khoản của người chơi.

---

## 9. TÍNH NĂNG 9: HỆ THỐNG ÂM THANH & HIỆU ỨNG HÌNH ẢNH (AUDIO & VFX SPECIFICATION)

Nhằm mang lại trải nghiệm đã tay, đã mắt (Game Juice) theo phong cách Arcade Tycoon sống động:

### 9.1. Hệ Thống Âm Thanh (Audio Architecture & Sound Matrix)
* **Quy tắc Kiến trúc:**
  * Toàn bộ âm thanh được quản lý qua `SoundManager.cs`, chia làm 2 kênh độc lập: **BGM (Nhạc nền)** và **SFX (Hiệu ứng âm thanh)**.
  * Âm thanh được kích hoạt gián tiếp qua các sự kiện tĩnh `GameEvents` để đảm bảo code logic không bị phụ thuộc vào audio clip.
  * Hỗ trợ điều chỉnh âm lượng mượt mà thông qua thanh trượt trong Bảng Cài đặt.

* **Bảng Danh Mục Âm Thanh Chi Tiết (Sound Cue Matrix):**

| Mã Sound Cue | Loại | Nguồn kích hoạt | Mô tả âm thanh & Cảm xúc |
|:---|:---:|:---|:---|
| `BGM_Factory_Groove` | BGM | Khởi động game (Lặp vô tận) | Giai điệu Lofi/Arcade vui nhộn, thư thái, nhịp trống nhẹ nhàng thúc đẩy năng suất sản xuất |
| `SFX_Machine_Drop` | SFX | Máy hoàn thành chu kỳ sản xuất | Tiếng *"bụp / clank"* cơ khí nhẹ khi nguyên liệu rơi xuống đầu băng chuyền |
| `SFX_Conveyor_Hum` | SFX | Băng chuyền hoạt động | Tiếng rù rù êm ái của con lăn băng tải (âm lượng nhỏ, đặt ở vị trí 3D gần máy) |
| `SFX_Item_Arrival` | SFX | Vật phẩm chạm vào Bệ chứa | Tiếng va chạm nhẹ của khối vật liệu rơi lên tấm pallet gỗ |
| `SFX_Item_Pickup` | SFX | Bấm `F` nhặt hàng từ Bệ chứa | Tiếng *"pop / whoosh"* nảy số vui tai; âm vực (Pitch) tăng dần theo từng món đồ được xếp lên đầu (`1/5 -> 5/5`) |
| `SFX_Item_Sell` | SFX | Xả hàng vào Sell Zone (`0.1s/lần`) | Tiếng *"ting ting / keng keng"* leng keng của đồng xu rơi dồn dập |
| `SFX_Vault_Collect` | SFX | Bấm `F` rút tiền từ Két vào ví | Tiếng mở két sắt máy tính tiền *"Ka-ching! 💰"* giòn giã và thỏa mãn |
| `SFX_Upgrade_Success` | SFX | Mua máy mới / Lên Tier / Mở rộng kho | Tiếng chuông hòa âm Fanfare ngắn báo hiệu sự phát triển của nhà máy |
| `SFX_UI_Click` | SFX | Nhấn các nút trên Canvas UI | Tiếng click thanh mảnh, phản hồi xúc giác tốt |
| `SFX_Warning_Full` | SFX | Bệ chứa và băng tải bị dồn ứ đầy | Tiếng còi cảnh báo ngắn nhẹ nhàng nhắc người chơi đến bốc hàng |
| `SFX_Victory_Fanfare` | SFX | Đạt mốc điều kiện thắng cuộc | Đoạn nhạc kèn chiến thắng hoành tráng kèm tiếng vỗ tay hoan hô |

---

### 9.2. Hệ Thống Hiệu Ứng Hình Ảnh (VFX & Juice Polish)

* **Bảng Ma Trận Hiệu Ứng (VFX Matrix):**

| Tên Hiệu Ứng (VFX) | Loại kỹ thuật | Mô tả hiển thị |
|:---|:---|:---|
| `VFX_Item_Parabola` | Tweening (Parabolic Arc) | Khi vật phẩm bốc từ Bệ chứa lên đầu nhân vật hoặc từ đầu rơi vào Sell Zone, vật phẩm bay theo đường cong vòng cung Parabol kèm vệt mờ nhẹ (`TrailRenderer`) |
| `VFX_Sell_Sparkle` | Particle System | Chùm tia sáng vàng lấp lánh (Golden Sparkles) nổ nhẹ tại tâm Sell Zone mỗi nhịp dỡ hàng thành công |
| `VFX_Floating_Money` | 3D Text / Canvas World | Chữ số tiền màu vàng `+$X` bay từ từ lên cao và mờ dần (Alpha Fade out) khi bán hàng |
| `VFX_Outline_Highlight` | Shader Outline / Highlighting | Khi nhân vật bước vào bán kính `1.5m`, trạm tương tác phát ra viền sáng viền nhẹ màu vàng pastel để báo hiệu mục tiêu |
| `VFX_Tier3_Smoke` | Particle System (Loop) | Các máy đạt cấp Tier 3 liên tục nhả khói hơi nước trắng (`White Steam`) từ ống khói, tạo cảm giác nhà máy vận hành năng suất cao |
| `VFX_Upgrade_Burst` | Particle System (Burst) | Khi nâng cấp thành công: Sóng xung kích tròn bung tỏa (`Radial Shockwave`) kèm pháo giấy Confetti rơi xung quanh máy |
| `VFX_Warning_Light` | Emissive Material Blink | Đèn tín hiệu trên khung máy chuyển sang nhấp nháy đỏ khi băng chuyền bị nghẽn do bệ chứa quá tải |

---

### 9.3. Quy Chuẩn Hoạt Ảnh Chi Tiết (Animation Specification)

1. **Player & Worker NPC Animator Controller:**
   * **Base Layer:** Điều khiển bằng BlendTree 1D (`Speed` từ `0.0` đến `5.0`): Chuyển đổi mượt mà giữa `Idle` và `Walk/Run`.
   * **Upper Body Layer (AvatarMask):** Chỉ tác động lên các xương: Ngực, Vai, Cánh tay, Cẳng tay, Bàn tay:
     * Cờ kích hoạt: `isCarrying` (Bool).
     * Khi `isCarrying = true`: Hai tay giơ thẳng lên cao trong tư thế nâng đỡ (`TwoHand_Overhead_Hold`), không làm ảnh hưởng đến bước chân di chuyển của Base Layer.
   * **Head Socket Secondary Motion:** Socket mang hàng gắn làm con trực tiếp của xương đầu (`Head Bone`). Chuyển động lắc lư tự nhiên của đầu nhân vật theo bước chạy sẽ truyền thẳng lên cọc hàng trên đầu, tạo hiệu ứng chuyển động nảy sinh động.

2. **Chuyển Động Băng Chuyền (Conveyor Motion):**
   * Sử dụng kỹ thuật cuộn Texture Coordinate (`Material.mainTextureOffset`) theo thời gian thực trong Update, giúp tái hiện cảm giác băng tải cuộn vô tận với chi phí hiệu năng bằng không.

---

## 10. TÍNH NĂNG 10: HỆ THỐNG TỐI ƯU HÓA HIỆU NĂNG (PERFORMANCE OPTIMIZATION)

Đảm bảo trò chơi duy trì tốc độ khung hình ổn định **60 FPS ngay cả trên máy cấu hình yếu hoặc nền tảng WebGL**:

### 10.1. Tái Sử Dụng Đối Tượng Triệt Để (Object Pooling System)
* Quản lý tập trung qua `ResourcePoolManager.cs`.
* Khi máy sản xuất nhả hàng: Lấy vật phẩm tái sử dụng từ Pool (`pool.Spawn()`).
* Khi hàng được bán tại Sell Zone hoặc nạp két lúc lưu game: Thu hồi vật phẩm về Pool (`pool.Despawn()`), tuyệt đối không dùng lệnh `Destroy()` trong vòng lặp gameplay.
* **Lợi ích:** Đạt mốc **0 Bytes GC Alloc**, loại bỏ hoàn toàn hiện tượng khựng khung hình do bộ gom rác của Unity.

### 10.2. Triệt Tiêu Xung Đột Vật Lý Nặng (Kinematic & Collider Culling)
* Toàn bộ vật phẩm tài nguyên sử dụng `Rigidbody.isKinematic = true` (không tính toán trọng lực, ma sát vật lý rơi tự do).
* Khi vật phẩm trôi vào Bệ chứa (`StoragePad`) hoặc bốc lên đầu (`PlayerCarry` / `WorkerNPC`): Hệ thống tự động vô hiệu hóa Collider (`col.enabled = false`).
* Băng chuyền tịnh tiến hàng hóa theo công thức tọa độ mượt mà, đồng thời tự động loại bỏ vật phẩm khỏi danh sách vận chuyển ngay khi vật phẩm được gán làm con của Bệ chứa hoặc Người chơi.

### 10.3. Gộp Lệnh Vẽ Đồ Họa (GPU Instancing Batching)
* Tất cả Material của các khối tài nguyên và máy móc được kích hoạt tính năng **Enable GPU Instancing**.
* Card màn hình tự động gộp hàng trăm khối tài nguyên cùng loại để hiển thị chỉ trong **1 Draw Call duy nhất**, giữ tổng Draw Call toàn màn chơi luôn dưới mức 50.

### 10.4. Giới Hạn Tải Trọng Tự Nhiên (Backpressure Cap)
* Cơ chế tự ngắt nhả hàng khi Bệ chứa và Băng tải bị dồn ứ giúp khống chế tổng số lượng vật phẩm tồn tại cùng lúc trên toàn bộ map không bao giờ vượt quá ngưỡng an toàn (~600 vật phẩm), ngăn ngừa hoàn toàn tình trạng quá tải bộ nhớ.