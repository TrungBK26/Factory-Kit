# 💰 Module 05: Kinh Tế, Bán Hàng & Lưu Trữ (Economy & Save)

Tài liệu này đặc tả dòng tiền trong game, cơ chế bán hàng tự động, két tiền doanh thu, và hệ thống lưu/nạp tiến trình 2 chiều an toàn.

---

## 1. DÒNG CHẢY TIỀN TỆ (MONEY FLOW ARCHITECTURE)

```text
[Hàng hóa trên Bệ] 
       │
       ▼ (Người chơi hoặc NPC bốc hàng)
[Trạm Bán (SellZone)]
       │
       ▼ (Quy đổi ra giá trị $)
[Két Tiền Doanh Thu (MoneyVault)] ───► Tích lũy tiền bán hàng
       │
       ▼ (Người chơi đến bấm [F] để rút)
[Ví Người Chơi (EconomyManager)] ─────► Dùng để Nâng cấp & Thuê công nhân
```

---

## 2. QUẢN LÝ TÀI CHÍNH (`EconomyManager.cs`)

### 2.1. Trách nhiệm
* Quản lý số dư ví người chơi (`CurrentWallet`).
* Kiểm tra khả năng chi trả trước khi thực hiện các giao dịch nâng cấp máy móc hoặc tuyển dụng công nhân.
* Phát tín hiệu `GameEvents.OnWalletChanged` mỗi khi số dư thay đổi để UI tự động cập nhật số tiền hiển thị.

### 2.2. API Công Khai
* `public bool HasEnoughMoney(int amount)`: Trả về `true` nếu `CurrentWallet >= amount`.
* `public void AddMoney(int amount)`: Cộng tiền vào ví và phát sự kiện.
* `public bool SpendMoney(int amount)`: Trừ tiền ví (nếu đủ số dư) và phát sự kiện.
* `public void SetWalletBalance(int amount)`: Dùng khi nạp game hoặc Reset dữ liệu.

---

## 3. TRẠM BÁN HÀNG TỰ ĐỘNG (`SellZone.cs`)

### 3.1. Tiếp nhận Hàng hóa Đa Đối Tượng
* **Tiếp nhận từ Người chơi:** Khi người chơi bước vào vùng `OnTriggerStay` của SellZone, script tự động dỡ từng món hàng trên đầu người chơi sau mỗi nhịp `unloadInterval = 0.15f` giây.
* **Tiếp nhận từ Công nhân NPC:** Hỗ trợ hàm `public void AcceptItem(GameObject item)` cho phép NPC chủ động nhét hàng vào trạm bán.

### 3.2. Tính toán Doanh thu & Tái chế Object
1. Trích xuất giá trị hàng hóa từ component `ResourceItemHolder.Data.baseValue`.
2. Trả vật phẩm về `ResourcePoolManager.Instance.Despawn(item)` để khử rác bộ nhớ.
3. Chuyển tiền vào `MoneyVault.Instance.AddAccumulatedMoney(value)`.
4. Phát tín hiệu `GameEvents.OnItemsSold?.Invoke(value)` để kích hoạt âm thanh và hiệu ứng VFX.

---

## 4. KÉT TIỀN TÍCH LŨY (`MoneyVault.cs`)

### 4.1. Cơ chế Tích lũy & Rút tiền
* Két tiền lưu giữ doanh thu tạm thời (`accumulatedMoney`).
* Cài đặt interface `IInteractable`:
  * Prompt: `"[F] Rút Tiền: $" + accumulatedMoney`
  * Khi người chơi nhấn `F`: Rút toàn bộ tiền trong két sang ví người chơi thông qua `EconomyManager.Instance.AddMoney(amount)`, đưa két về $0 và phát sự kiện `GameEvents.OnMoneyCollected`.

---

## 5. HỆ THỐNG LƯU TRỮ TIẾN TRÌNH (`SaveManager.cs`)

### 5.1. Cấu Trúc Dữ Liệu (`GameSaveData.cs`)
```csharp
[Serializable]
public class SlotSaveState
{
    public string slotID;         // Định danh duy nhất của slot (VD: "Cluster_Wood_Slot00")
    public bool isUnlocked;
    public int tierLevel = 1;
    public int speedLevel = 0;
    public int storageLevel = 0;
    public int hiredWorkers = 0;
}

[Serializable]
public class GameSaveData
{
    public int playerWallet = 0;
    public int vaultMoney = 0;
    public bool hasClaimedVictory = false;
    public List<SlotSaveState> slots = new List<SlotSaveState>();
}
```

### 5.2. Cơ chế Thanh Lý Bảo Toàn Hàng Tồn (Quit-Only Liquidation)
* Khi người chơi tắt game (`OnApplicationQuit`), các vật phẩm đang nằm dở dang trên đầu người chơi hoặc trên các bệ chứa (`StoragePad`) sẽ được **tự động quy đổi thành tiền mặt** và cộng thẳng vào két tiền (`MoneyVault`).
* *Lợi ích:* Đảm bảo khi mở lại game, người chơi không bị mất hàng hóa đang sản xuất dở dang.

### 5.3. Khôi Phục Đồng Bộ 2 Chiều (Two-Way State Synchronization)
* Khi gọi `LoadGame()`:
  * Không chỉ mở khóa các slot có trong save, mà còn **chủ động khóa lại (`LockSlot`)** các slot chưa được mở khóa nếu trước đó đang ở trạng thái mở.
  * Tự động làm mới trạng thái của các bệ `MachineUnlockPad` in-world tương ứng.

### 5.4. Khởi Động Lại Toàn Diện (Full Factory Reset)
* Khi gọi `ResetSaveData()`:
  1. Xóa khóa `PlayerPrefs`.
  2. Reset ví tiền và két tiền về $0.
  3. Dọn sạch hàng tồn trên đầu người chơi và các bệ chứa.
  4. Giải tán và tiêu hủy toàn bộ công nhân AI (`DismissAllWorkers`).
  5. Đưa toàn bộ các máy về Tier 1, Level 0; khóa lại các máy cấp cao (chỉ giữ máy Gỗ mở sẵn).
  6. Kích hoạt lại toàn bộ các bệ mở khóa `MachineUnlockPad`.
