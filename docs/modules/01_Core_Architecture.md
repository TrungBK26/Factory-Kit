# 🏛️ Module 01: Kiến Trúc Cốt Lõi & Hạ Tầng (Core Architecture)

Tài liệu này quy chuẩn hạ tầng kỹ thuật trung tâm của dự án **Factory Reborn**, đảm bảo tính ổn định, khử rác bộ nhớ (Zero Allocation) và tương thích hoàn toàn với cơ chế Domain Reload của Unity 6.

---

## 1. HỆ THỐNG SỰ KIỆN TOÀN CỤC (`GameEvents.cs`)

### 1.1. Mục tiêu & Nguyên tắc
* Đóng vai trò là **Bus truyền thông điệp (Event Bus)** trung tâm.
* Loại bỏ hoàn toàn sự phụ thuộc trực tiếp (Coupling) giữa các module: UI không gọi trực tiếp Machine, Machine không gọi trực tiếp Economy, NPC không gọi trực tiếp Player.
* Khử rác bộ nhớ: Dùng C# `System.Action` tĩnh, không tạo đối tượng rác (Garbage Collection) khi phát tín hiệu.

### 1.2. Danh mục Sự kiện (Event Catalog)

```csharp
public static class GameEvents
{
    // --- TIỀN TỆ & GIAO DỊCH ---
    public static Action<int> OnWalletChanged;                    // Số dư ví cá nhân thay đổi (số dư mới)
    public static Action<int> OnItemsSold;                        // Có món hàng được xả vào SellZone (giá trị món hàng)
    public static Action<int> OnMoneyAccumulated;                 // Tiền tích lũy trong két tăng
    public static Action<int> OnMoneyVaultWithdrawn;              // Người chơi rút tiền két vào ví (số tiền vừa rút)

    // --- SẢN XUẤT & VẬN CHUYỂN ---
    public static Action OnItemProduced;                          // Máy vừa nhả 1 món xuống băng tải
    public static Action<int> OnItemPickedUp;                     // Player/NPC bốc 1 món lên đầu (tải trọng hiện tại)
    public static Action<ResourceType, int> OnMachineTierUpgraded;// (Loại tài nguyên, Tier mới)
    public static Action<ResourceType, int> OnMachineCountChanged;// (Loại tài nguyên, số lượng máy đang chạy)
    public static Action<ResourceType, int> OnWorkerHired;        // (Loại tài nguyên, số lượng công nhân hiện tại)

    // --- LƯU TRỮ & TIẾN TRÌNH ---
    public static Action OnSaveDataRequested;                    // Yêu cầu lưu tiến trình game
    public static Action OnDataLoaded;                           // Đã tải xong dữ liệu từ file Save

    // --- GIAO DIỆN & MENU ---
    public static Action OnMenuTogglePressed;                    // Mở/Đóng Bảng Quản Lý Nhà Máy (Tab)
    public static Action OnSettingsTogglePressed;                // Mở/Đóng Bảng Cài Đặt

    // --- ĐIỀU KIỆN CHIẾN THẮNG ---
    public static Action OnGameVictory;                          // Đạt đủ 3 điều kiện thắng game
}
```

### 1.3. Quy tắc An toàn khi Đăng ký Sự kiện (Event Subscription Lifecycle)
* Mọi MonoBehaviour lắng nghe sự kiện **bắt buộc** phải đăng ký trong `OnEnable` và hủy đăng ký trong `OnDisable`:
```csharp
private void OnEnable() => GameEvents.OnWalletChanged += UpdateWalletUI;
private void OnDisable() => GameEvents.OnWalletChanged -= UpdateWalletUI;
```
* **Nghiêm cấm** đăng ký trong `Start()` hoặc `Awake()` mà quên hủy trong `OnDisable()`, vì sẽ gây lỗi rò rỉ bộ nhớ hoặc gọi vào đối tượng đã bị Destroy (`MissingReferenceException`).

---

## 2. QUY CHUẨN SINGLETON & AN TOÀN TRÊN UNITY 6 (DOMAIN RELOAD SAFETY)

### 2.1. Vấn đề trong Unity 6
Khi tắt "Domain Reload" trong `Project Settings -> Editor -> Enter Play Mode Settings` để tăng tốc độ nạp game, các biến tĩnh (`static Instance`) sẽ **không tự reset về null** giữa các lần ấn Play/Stop. Hậu quả là `Instance` trỏ vào GameObject của lần Play trước (đã bị hủy), gây lỗi `MissingReferenceException`.

### 2.2. Mẫu triển khai Self-Healing Singleton chuẩn
Tất cả các Manager (`InputManager`, `EconomyManager`, `FactoryManager`, `ResourcePoolManager`, `SoundManager`, `VFXManager`, `WinConditionChecker`) đều áp dụng mẫu sau:

```csharp
public class CoreManagerExample : MonoBehaviour
{
    public static CoreManagerExample Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null; // Đảm bảo reset biến static khi nạp game
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
```

---

## 3. TRÌNH QUẢN LÝ VÒNG ĐỜI & CHẠY NỀN (`GameManager.cs`)

### 3.1. Mục tiêu & Cơ chế Idle
* Phụ trách cấu hình vòng đời ứng dụng tổng thể.
* Kích hoạt chế độ **Chạy Ngầm (Run In Background)**: Game tiếp tục tính tiền, máy móc tiếp tục nhả hàng, NPC tiếp tục vận chuyển ngay cả khi người chơi Alt+Tab hoặc chuyển sang ứng dụng khác (trọng tâm của game Idle).
* Khóa tốc độ khung hình (`targetFrameRate = 60`) để tối ưu hóa hiệu năng, chống nóng máy và giảm tải CPU/GPU khi chạy ẩn.

### 3.2. Cấu hình Tự Động
* Sử dụng `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` để bật `Application.runInBackground = true` và thiết lập `Application.targetFrameRate = 60` ngay trước khi nạp Scene đầu tiên.
* Lắng nghe `OnApplicationFocus(bool hasFocus)` để đảm bảo game không bao giờ bị đóng băng khi mất tiêu điểm cửa sổ.

---

## 4. HỆ THỐNG OBJECT POOLING (`ResourcePoolManager.cs`)

### 4.1. Mục tiêu
* Các khối tài nguyên (Gỗ, Đá, Đồng, Sắt, Vàng...) được sản xuất liên tục từ băng chuyền và bán vào SellZone với số lượng hàng trăm viên mỗi phút.
* Nếu dùng `Instantiate` và `Destroy` liên tục, Unity Garbage Collector (GC) sẽ gây khựng/lag hình (Frame Drop).
* `ResourcePoolManager` cung cấp cơ chế tái sử dụng 100% đối tượng khối tài nguyên.

### 3.2. API & Cách sử dụng
* **Spawn đối tượng:**
```csharp
GameObject item = ResourcePoolManager.Instance.Spawn(prefab, position, rotation);
```
* **Thu hồi (Despawn) đối tượng:**
```csharp
ResourcePoolManager.Instance.Despawn(item);
```
* Khi Despawn: Đối tượng được trả về Pool cha, đưa về tọa độ Vector3.zero, tắt Active, reset vận tốc Rigidbody (nếu có) và sẵn sàng cho chu kỳ nhả hàng tiếp theo.

---

## 5. QUẢN LÝ ÂM THANH (`SoundManager.cs`)

### 5.1. Cấu trúc Kênh Âm Thanh
* Quản lý 2 `AudioSource` độc lập:
  * **BGM Source:** Phát nhạc nền lặp vô tận (Loop), âm lượng êm dịu.
  * **SFX Source:** Phát âm thanh tương tác tức thì qua `PlayOneShot()`.
* Tách biệt điều chỉnh âm lượng thông qua PlayerPrefs:
  * `SFX_Volume` (0.0f - 1.0f)
  * `BGM_Volume` (0.0f - 1.0f)

### 5.2. Tích hợp Sự kiện Tự Động
`SoundManager` tự động lắng nghe `GameEvents` mà không cần các script khác phải gọi trực tiếp:
* Nghe `GameEvents.OnItemsSold` -> Phát SFX bán hàng leng keng (`sfxItemSell`).
* Nghe `GameEvents.OnMoneyVaultWithdrawn` -> Phát SFX rút tiền két (`sfxVaultCollect`).
* Nghe `GameEvents.OnItemPickedUp` -> Phát SFX bốc đồ (`sfxItemPickup`).
* Nghe `GameEvents.OnItemProduced` -> Phát SFX máy nhả hàng (`sfxMachineDrop`).
* Nghe `GameEvents.OnMachineTierUpgraded` -> Phát SFX nâng cấp búa gõ (`sfxUpgradeSuccess`).
* Nghe `GameEvents.OnGameVictory` -> Phát SFX chuông chiến thắng (`sfxVictoryFanfare`).

---

## 6. HIỆU ỨNG HÌNH ẢNH (`VFXManager.cs`)

### 6.1. Mục tiêu
* Tách rời toàn bộ logic Particle System, Floating Text (chữ số tiền nổi) ra khỏi gameplay logic.
* Lắng nghe `GameEvents.OnItemsSold` và `GameEvents.OnMachineUpgraded` để kích hoạt hiệu ứng tại vị trí tương ứng.

### 6.2. Chức năng
* **Particle Pool:** Tạo sẵn các cụm Particle (Sparkles, Dust, Money Burst) khi khởi động scene.
* **Floating Text:** Hiển thị số tiền cộng thêm bay lên và mờ dần (Fade Out) tại vị trí SellZone hoặc MoneyVault.

---

## 7. KIỂM TRA ĐIỀU KIỆN CHIẾN THẮNG (`WinConditionChecker.cs`)

### 7.1. Điều kiện Thắng (Win Conditions)
Người chơi chiến thắng khi thỏa mãn **toàn bộ** các tiêu chí sau:
1. Mở khóa đủ toàn bộ 6 loại máy (Gỗ, Đá, Đồng, Sắt, Bạc, Vàng).
2. Toàn bộ 6 máy đều đạt cấp tối đa:
   * **Tier Level = 3** (Cấp công nghệ cao nhất).
   * **Speed Level = 3** (Tốc độ tối đa).
   * **Storage Level = 3** (Sức chứa bệ tối đa).
3. Tuyển dụng tối đa công nhân cho toàn bộ các bệ (2 công nhân / dây chuyền = 12 công nhân).

### 6.2. Cơ chế Free Play
* Khi đạt chiến thắng, Game phát sự kiện `GameEvents.OnGameVictory`.
* `VictoryPopup` hiển thị vinh danh.
* Người chơi có thể bấm **"Tiếp Tục Chơi (Free Play)"**: Cờ `HasClaimedVictory = true` được lưu vào SaveData để không hiện lại Popup lần nữa trong cùng phiên chơi.
