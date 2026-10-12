# 🤖 Module 04: Trí Tuệ Nhân Tạo Công Nhân (Worker NPC AI)

Tài liệu này đặc tả máy trạng thái hữu hạn (FSM), cơ chế tìm đường, bốc dỡ hàng hóa và đồng bộ hoạt ảnh của công nhân AI.

---

## 1. MÁY TRẠNG THÁI HỮU HẠN (FINITE STATE MACHINE - FSM)

Công nhân AI hoạt động dựa trên chu trình 5 trạng thái liên tục:

```text
[Idle (Chờ hàng)] 
       │ (Khi StoragePad có hàng)
       ▼
[MoveToStorage (Đi đến bệ chứa)]
       │ (Khi tới bệ chứa)
       ▼
[PickUpItem (Bốc hàng lên đầu)]
       │ (Khi đã đạt sức chứa tối đa hoặc hết hàng)
       ▼
[MoveToSellZone (Đi đến trạm bán)]
       │ (Khi tới trạm bán)
       ▼
[DepositItem (Dỡ hàng vào trạm bán)]
       │ (Sau khi dỡ hết hàng)
       └──────────────────────────► Quay lại [Idle / MoveToStorage]
```

---

## 2. ĐẶC TẢ TỪNG TRẠNG THÁI

### 2.1. Trạng Thái Chờ (`Idle`)
* Công nhân đứng tại vị trí chờ gần bệ máy được phân công.
* Liên tục kiểm tra `assignedStoragePad.CurrentCount > 0`.
* Ngay khi bệ máy có ít nhất 1 sản phẩm $\rightarrow$ Chuyển sang `MoveToStorage`.

### 2.2. Đi Đến Bệ Chứa (`MoveToStorage`)
* Sử dụng `NavMeshAgent.SetDestination(assignedStoragePad.PickupPoint.position)`.
* *Fallback an toàn:* Nếu Scene chưa bake NavMesh, script tự động chuyển sang di chuyển tịnh tiến bằng `Vector3.MoveTowards`.
* Khi khoảng cách đến đích $< 0.8\text{m}$ $\rightarrow$ Chuyển sang `PickUpItem`.

### 2.3. Bốc Hàng Lên Đầu (`PickUpItem`)
* Thực hiện bốc hàng theo nhịp thời gian (`pickupInterval = 0.25f` giây/món).
* Lấy vật phẩm từ `StoragePad.PopItem()`.
* Đưa vật phẩm vào `carrySocket` trên đầu (tương tự như Player).
* Điều kiện dừng:
  * Đã đầy tải trọng (`CurrentCount >= maxCarryCapacity`).
  * Hoặc bệ chứa đã hết hàng (`StoragePad.CurrentCount == 0`).
* Chuyển sang `MoveToSellZone`.

### 2.4. Đi Đến Trạm Bán (`MoveToSellZone`)
* Đặt đích đến là `SellZone.DepositPoint.position`.
* Khi khoảng cách đến đích $< 1.2\text{m}$ $\rightarrow$ Chuyển sang `DepositItem`.

### 2.5. Dỡ Hàng Xuống Trạm Bán (`DepositItem`)
* Bốc dỡ từng món hàng trên đầu xuống trạm theo nhịp (`depositInterval = 0.2f` giây/món).
* Chuyển vật phẩm vào `SellZone.AcceptItem(item)`.
* Khi trên đầu đã hết sạch hàng $\rightarrow$ Kiểm tra lại `StoragePad`:
  * Nếu bệ có hàng $\rightarrow$ Tiếp tục vòng lặp `MoveToStorage`.
  * Nếu bệ trống $\rightarrow$ Trở về `Idle`.

---

## 3. TÍCH HỢP HOẠT ẢNH (KAYKIT ANIMATOR)

Worker NPC hỗ trợ kết nối trực tiếp với Animator Controller của KayKit Character:

| Tên tham số (Parameter) | Kiểu dữ liệu | Ý nghĩa |
|---|---|---|
| `Speed` | `Float` | Vận tốc di chuyển thực tế của NPC (điều khiển Blend Tree Idle $\rightarrow$ Walk/Run) |
| `IsCarrying` | `Bool` | `true` khi đang vác ít nhất 1 vật phẩm trên đầu (kích hoạt Layer vác đồ giơ tay) |

* Tự động kiểm tra `GetComponentInChildren<Animator>()` trong `Awake()` để an toàn nếu prefab chưa được gán sẵn.

---

## 4. QUY TẮC PHÂN BỔ & GIẢI TÁN CÔNG NHÂN

### 4.1. Giới hạn Tuyển Dụng
* Mỗi cụm dây chuyền (`ProductionLine`) chỉ cho phép tối đa **2 công nhân**.
* Toàn bộ 6 dây chuyền có tối đa **12 công nhân**.
* Công nhân chỉ phục vụ riêng cho bệ máy được gán, không đi tranh hàng của dây chuyền khác, triệt tiêu xung đột va chạm.

### 4.2. Giải Tán Khi Reset Game (`DismissAllWorkers`)
* Khi người chơi chọn Reset Game, `ProductionLine` gọi hàm `DismissAllWorkers()`.
* Bất kỳ vật phẩm nào NPC đang vác dở trên đầu sẽ được trả về Object Pool hoặc tiêu hủy an toàn trước khi `Destroy(worker.gameObject)` để tránh rò rỉ rác trong Scene.
