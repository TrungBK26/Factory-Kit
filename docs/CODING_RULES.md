# 📜 SỔ TAY QUY TẮC & TRIẾT LÝ LẬP TRÌNH (CODING BEST PRACTICES)
# Dự án: Factory Reborn | Kim chỉ nam phát triển Unity bền vững

> *"Viết code không chỉ để máy hiểu và chạy được ở hiện tại, mà là để khi thêm tính năng mới trong tương lai, code cũ tuyệt đối không bao giờ bị hỏng."*

---

## 1. TRIẾT LÝ CỐT LÕI: "GHÉP NỐI LỎNG" (LOOSE COUPLING)

### 📌 Vấn đề thường gặp
Các script gọi trực tiếp đến nhau (ví dụ: `SellZone` gọi thẳng `MoneyVault.AddMoney()`, gọi tiếp `SoundManager.PlaySound()`, gọi tiếp `QuestManager.UpdateQuest()`). 
$\rightarrow$ **Hậu quả:** Khi xóa hoặc sửa bất kỳ script nào, cả dự án sẽ báo lỗi đỏ rực (Compilation Errors), việc nâng cấp trở thành "cơn ác mộng".

### 💡 Quy tắc vàng
* **Không để các hệ thống phụ thuộc cứng vào nhau.**
* Sử dụng **Event-Driven Architecture (Observer Pattern)** thông qua lớp trung gian `GameEvents.cs`.
* Một class chỉ phát đi tín hiệu: *"Tôi vừa làm xong việc X!"* (`Invoke`). Ai quan tâm đến việc đó thì tự đăng ký (`Subscribe`) lắng nghe.

```csharp
// ❌ CÁCH LÀM XẤU (Ghép nối cứng - Sửa 1 nơi hỏng cả game):
public class SellZone : MonoBehaviour 
{
    public MoneyVault moneyVault;
    public SoundManager soundManager;
    public AchievementManager achievementManager;

    void Sell() {
        moneyVault.AddMoney(100);
        soundManager.PlayCashSound();
        achievementManager.AddProgress("SellFirstItem");
    }
}

// ✅ CÁCH LÀM CHUẨN (Ghép nối lỏng - Thêm/xóa tính năng không cần sửa SellZone):
public class SellZone : MonoBehaviour 
{
    void Sell() {
        // Chỉ việc phát loa thông báo, không cần biết ai đang nghe
        GameEvents.OnItemsSold?.Invoke(100);
    }
}
```

---

## 2. NGUYÊN TẮC: "TỰ TÌM ĐẾN NHAU & TỰ BẢO VỆ" (SELF-DISCOVERY & DEFENSIVE CODE)

### 📌 Vấn đề thường gặp
Lập trình viên hoặc Designer quên kéo thả component vào Inspector, dẫn đến lỗi `NullReferenceException` khiến game dừng hoạt động đột ngột giữa chừng.

### 💡 Quy tắc vàng
1. **Dùng `[RequireComponent]`:** Bắt buộc Unity tự động đính kèm component cần thiết nếu người tạo Prefab quên.
2. **Tự tìm kiếm dự phòng trong `Awake()` hoặc `Reset()`:** Nếu trên Inspector chưa gán, code sẽ tự tìm trong nội bộ GameObject hoặc con của nó.
3. **Luôn dùng `TryGetComponent`:** Tuyệt đối không dùng `GetComponent<T>()` trần trụi mà không kiểm tra null.

```csharp
// ❌ CÁCH LÀM DỄ LỖI:
public class PlayerMovement : MonoBehaviour 
{
    public CharacterController controller; // Nếu quên kéo trong Inspector -> CRASH GAME!

    void Update() {
        controller.Move(Vector3.forward);
    }
}

// ✅ CÁCH LÀM AN TOÀN (Tự tìm & Tự bảo vệ):
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour 
{
    [SerializeField] private CharacterController controller;

    private void Reset() {
        // Tự động gán ngay khi vừa kéo script vào GameObject trong Editor
        controller = GetComponent<CharacterController>();
    }

    private void Awake() {
        // Dự phòng an toàn tuyệt đối khi Runtime chạy
        if (controller == null) controller = GetComponent<CharacterController>();
    }
}
```

```csharp
// ❌ Tương tác dễ lỗi Null:
void OnTriggerEnter(Collider other) {
    other.GetComponent<PlayerCarry>().AddItem(item); // Nếu va chạm với Tường/Quái vật -> CRASH!
}

// ✅ Tương tác an toàn (TryGetComponent):
void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent<PlayerCarry>(out var playerCarry)) {
        playerCarry.AddItem(item);
    }
}
```

---

## 3. NGUYÊN TẮC THIẾT KẾ KÉO THẢ THÂN THIỆN (INSPECTOR-FRIENDLY DESIGN)

### 📌 Vấn đề thường gặp
Biến số đặt bừa bãi `public`, Inspector rối rắm, nhập nhầm số âm hoặc chia cho 0 làm sai lệch toàn bộ logic game.

### 💡 Quy tắc vàng
* Không dùng `public` cho biến nội bộ. Thay vào đó dùng `[SerializeField] private`.
* Dùng `[Header]`, `[Tooltip]`, `[Range]` để giao diện trực quan, có giải thích rõ ràng khi rê chuột vào.
* Luôn đặt giá trị mặc định hợp lý để khi tạo mới Prefab là có thể chạy được ngay.

```csharp
public class MachineController : MonoBehaviour 
{
    [Header("--- CẤU HÌNH NHẢ VẬT PHẨM ---")]
    [Tooltip("Thời gian chờ giữa 2 lần nhả hàng (giây)")]
    [Range(0.5f, 5.0f)]
    [SerializeField] private float spawnInterval = 3.0f;

    [Header("--- BỆ CHỨA TẠI CHỖ ---")]
    [Tooltip("Sức chứa tối đa của tấm bệ pallet")]
    [SerializeField] private int maxStorage = 10;
}
```

---

## 4. TÁCH RỜI LOGIC VÀ DỮ LIỆU (DATA-DRIVEN VỚI SCRIPTABLEOBJECT)

### 📌 Vấn đề thường gặp
Hardcode trực tiếp giá tiền, chỉ số máy móc trong mã nguồn C# (ví dụ: `if (type == "Wood") price = 2; else if (type == "Gold") price = 250;`).
$\rightarrow$ **Hậu quả:** Mỗi lần thầy cô hoặc Designer muốn đổi giá bán, phải mở code ra sửa, rất dễ sinh bug logic và mất thời gian compile lại.

### 💡 Quy tắc vàng
* Code chỉ xử lý **Luật chơi (Logic)**.
* **Số liệu (Data)** đưa hết vào file tài nguyên `ScriptableObject`.
* Muốn thêm loại quặng mới hay cân bằng kinh tế: Chỉ việc click chuột phải trong Unity chọn `Create -> Factory -> Resource Data` mà **không cần đụng tới 1 dòng code nào**.

```
Assets/ScriptableObjects/Resources/
├── Resource_Wood.asset     (Giá: $2, Tier: 1, Prefab: Gỗ)
├── Resource_Copper.asset   (Giá: $15, Tier: 1, Prefab: Quặng đồng)
└── Resource_Gold.asset     (Giá: $250, Tier: 1, Prefab: Thỏi vàng)
```

---

## 5. NGUYÊN TẮC TRÁCH NHIỆM ĐƠN LẺ (SINGLE RESPONSIBILITY PRINCIPLE - SRP)

### 📌 Vấn đề thường gặp
Viết 1 file `Player.cs` dài hơn 1500 dòng chứa tất cả: di chuyển, xoay người, hoạt ảnh, nhặt đồ, trừ tiền, âm thanh, UI...
$\rightarrow$ **Hậu quả:** Khi sửa di chuyển lại làm hỏng nhặt đồ, rất khó đọc và không thể tái sử dụng.

### 💡 Quy tắc chia nhỏ:
* `InputManager.cs`: Chỉ đọc tín hiệu phần cứng (bàn phím, gamepad, touch) và cung cấp vector di chuyển / sự kiện phím bấm chuẩn hóa.
* `PlayerMovement.cs`: Chỉ nhận vector từ `InputManager` và di chuyển nhân vật.
* `PlayerCarry.cs`: Chỉ quản lý danh sách vật phẩm vác trên đầu.
* `PlayerInteraction.cs`: Chỉ lắng nghe sự kiện phím từ `InputManager` và tương tác với trạm gần nhất.

---

## 6. ĐỘC LẬP HÓA PREFAB (PREFAB ISOLATION)

### 💡 Quy tắc vàng:
* **Không bao giờ kéo thả một GameObject trong Scene vào biến của Prefab.**
  *(Vì khi đổi Scene hoặc sinh Prefab mới bằng code `Instantiate`, liên kết đó sẽ lập tức biến thành `Missing / None`)*.
* Mọi Prefab phải hoạt động độc lập (Self-contained):
  * Cần tìm người chơi? $\rightarrow$ Tìm qua Tag `Player` hoặc đăng ký qua `PlayerManager`.
  * Cần báo sự kiện? $\rightarrow$ Gọi qua `GameEvents`.
  * Cần dữ liệu? $\rightarrow$ Kéo file `ScriptableObject`.

---

## 7. QUY CHUẨN DEBUG LOG (TAGGED LOGGING STANDARD)

### 💡 Quy tắc vàng:
Mọi dòng lệnh in thông tin kiểm tra đều phải có tiền tố `[Tag]` để dễ dàng lọc tìm kiếm trên cửa sổ Unity Console:

| Tiền tố (Tag) | Khu vực sử dụng | Ví dụ lệnh |
|---|---|---|
| `[Player]` | Di chuyển, Vác đồ, Bấm F | `Debug.Log("[Player] Đã bốc 1 món lên đầu. Tải: 3/5");` |
| `[Machine]` | Nhả hàng, Đầy bệ chứa, Lên cấp | `Debug.Log("[Machine] Bệ chứa đã đầy (10/10), tạm dừng.");` |
| `[Economy]` | Bán hàng, Rút tiền két, Trừ tiền | `Debug.Log("[Economy] Đã rút vào ví: $150. Số dư: $1,250");` |
| `[Save]` | Tự động lưu / Đọc tiến trình | `Debug.Log("[Save] Đã lưu dữ liệu vào PlayerPrefs.");` |
| `[AI]` | Trạng thái công nhân NPC | `Debug.Log("[AI] Công nhân #1 đang di chuyển ra SellZone.");` |

---

---

## 8. QUY CHUẨN TỐI ƯU HÓA HIỆU NĂNG CHO MÁY YẾU (PERFORMANCE & ZERO GC ALLOC)

### 📌 Vấn đề thường gặp
Liên tục gọi `Instantiate()` khi nhả vật phẩm và `Destroy()` khi bán hàng khiến bộ nhớ RAM bị phân mảnh, kích hoạt trình dọn rác Unity Garbage Collection (GC) gây khựng đứng hình định kỳ. Hàng trăm khối đồ chạy physics va quệt nhau gây sụt giảm FPS nghiêm trọng trên máy yếu/WebGL.

### 💡 4 Quy tắc vàng tối ưu hóa:
1. **Tuyệt đối không dùng `Destroy()` trong Gameplay Loop:**
   * Sử dụng **Object Pooling** (`ResourcePoolManager.cs`). Khi sinh đồ dùng `pool.Get()`, khi bán/rút đồ dùng `pool.Release()`.
   * Bộ nhớ GC phân bổ hàng frame phải đạt **0 Bytes**.
2. **Triệt tiêu CPU Physics cho Hàng Hóa:**
   * Tất cả vật phẩm để `Rigidbody.isKinematic = true` (không tính toán trọng lực/ma sát).
   * Khi hàng đã chuyển vào Bệ chứa (`StoragePad`) hoặc vác lên đầu (`PlayerCarry`): **Lập tức tắt Collider** (`col.enabled = false`). Khối đồ lúc này chỉ là một hình ảnh thuần túy, chiếm 0% CPU physics.
3. **Bắt buộc bật GPU Instancing:**
   * Tất cả Material của các khối tài nguyên phải tick chọn **"Enable GPU Instancing"** để gộp hàng trăm khối đồ vào đúng 1 Draw Call duy nhất.
4. **Kiểm tra cha (Parent Check) trên Băng Chuyền:**
   * Trên `ConveyorBelt.FixedUpdate`, luôn kiểm tra `if (itemRb.transform.parent != null) itemsOnBelt.RemoveAt(i);` để không bao giờ kéo nhầm các vật phẩm đã được Bệ chứa hoặc Người chơi nhặt.

---

## 10. QUY CHUẨN AN TOÀN UNITY 6 & SINGLETON TỰ PHỤC HỒI (DOMAIN RELOAD & SELF-HEALING)

### 📌 Vấn đề thường gặp
Trong Unity 6 và các bản Unity bật *Enter Play Mode Options (Disable Domain Reload)* để tăng tốc độ phát triển:
* Biến tĩnh (`static Instance`) và `static event Action` trong `GameEvents` **không được xóa** giữa các lần ấn Play, giữ con trỏ rác đến GameObject đã bị Destroy từ phiên trước, gây lỗi `MissingReferenceException` hoặc làm Singleton tự hủy (`Destroy(gameObject)`).
* GameObject Manager bị thiếu trong Scene dẫn đến việc phát sự kiện vào khoảng không (`null delegate`), ấn phím không phản hồi.

### 💡 Quy tắc vàng xử lý:
1. **Reset biến tĩnh bằng `SubsystemRegistration`:**
   * Mọi class chứa biến `static` hoặc `static event` phải có hàm reset gắn `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
   ```csharp
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
   private static void ResetStatic() => _instance = null;
   ```
2. **Singleton Tự Phục Hồi (Self-Healing Lazy Singleton):**
   * Kết hợp thuộc tính `Instance` tự động tìm hoặc sinh GameObject mới (`go.AddComponent<T>()`) và gắn `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` để bảo đảm mọi Manager luôn tồn tại ngay khi nạp bất kỳ Scene nào.
3. **Thiết Kế UI Kép (Dual-Mode UI: IMGUI Fallback $\rightarrow$ uGUI Canvas):**
   * Các bảng UI (`FactoryManagementUI`, `SettingsPopup`, `VictoryPopup`) luôn có chế độ vẽ dự phòng bằng `OnGUI()` khi `panelRoot == null`.
   * Giúp kiểm tra 100% gameplay, nâng cấp, cài đặt âm lượng ngay lập tức mà không bị kẹt hay phụ thuộc vào việc Designer đã vẽ xong Canvas/Prefab hay chưa.
   * Thiết lập `GUI.depth` âm (`-100`, `-110`, `-120`) để đảm bảo thứ tự hiển thị đè đúng lớp.
4. **Chống Nhấp Nháy Sự Kiện (Input Debounce):**
   * Các hàm đóng/mở menu (`ToggleMenu`, `ToggleSettings`) bắt buộc có biến chặn `lastToggleTime` tối thiểu `0.2s - 0.25s` để tránh việc phím tắt và Event cùng kích hoạt trong 1 frame làm menu mở rồi tự tắt ngay lập tức.
5. **Dò Tìm Collider Phân Cấp (`GetComponentInParent`):**
   * Trong `PlayerInteraction`, luôn dùng `col.GetComponentInParent<IInteractable>()` thay vì `col.TryGetComponent` trần trụi. Khi sau này thay thế khối hộp bằng Model 3D có nhiều Collider con, nhân vật vẫn tương tác phím `F` chuẩn xác 100%.

---

## 11. BẢNG TỰ KIỂM TRA TRƯỚC KHI COMMIT CODE (CHECKLIST)

Trước khi coi một script là hoàn thành, hãy tự hỏi 8 câu sau:
- [ ] **1. Code này có gọi trực tiếp script khác không?** Nếu có, có thể thay bằng `GameEvents` hoặc `Interface` không?
- [ ] **2. Nếu quên kéo biến trong Inspector thì game có crash không?** Đã dùng `TryGetComponent` và `[RequireComponent]` chưa?
- [ ] **3. Thông số này có bị hardcode không?** Đã đưa ra biến `[SerializeField]` hoặc `ScriptableObject` chưa?
- [ ] **4. Prefab này đặt sang Scene trắng có chạy được không?** Có bị gãy tham chiếu ngoài không?
- [ ] **5. Đã gắn Tag phân loại vào `Debug.Log` chưa?**
- [ ] **6. Có gọi `Instantiate` / `Destroy` trong Update hoặc Trigger không?** Đã chuyển sang Object Pooling chưa?
- [ ] **7. Biến static và Event đã được reset an toàn cho Domain Reload chưa?**
- [ ] **8. Tương tác Collider có dùng `GetComponentInParent` để sẵn sàng cho Model 3D chưa?**


