using System;

/// <summary>
/// Kênh sự kiện tĩnh toàn cục (Event-Driven Architecture / Observer Pattern).
/// Cho phép các hệ thống giao tiếp mà không cần phụ thuộc trực tiếp vào nhau (Loose Coupling).
/// </summary>
public static class GameEvents
{
    // --- TIỀN TỆ & GIAO DỊCH ---
    public static Action<int> OnWalletChanged;           // Số dư ví cá nhân thay đổi (số dư mới)
    public static Action<int> OnItemsSold;               // Có món hàng được xả vào SellZone (giá trị món hàng)
    public static Action<int> OnMoneyAccumulated;        // Tiền tích lũy trong két tăng
    public static Action<int> OnMoneyVaultWithdrawn;     // Người chơi rút tiền két vào ví (số tiền vừa rút)

    // --- SẢN XUẤT & VẬN CHUYỂN ---
    public static Action OnItemProduced;                 // Máy vừa nhả 1 món xuống băng tải
    public static Action<int> OnItemPickedUp;            // Player/NPC bốc 1 món lên đầu (tải trọng hiện tại)
    public static Action<ResourceType, int> OnMachineTierUpgraded; // (Loại tài nguyên, Tier mới)
    public static Action<ResourceType, int> OnMachineCountChanged;  // (Loại tài nguyên, số lượng máy đang chạy)
    public static Action<ResourceType, int> OnWorkerHired;          // (Loại tài nguyên, số lượng công nhân hiện tại)

    // --- LƯU TRỮ & TIẾN TRÌNH ---
    public static Action OnSaveDataRequested;           // Yêu cầu lưu tiến trình game
    public static Action OnDataLoaded;                  // Đã tải xong dữ liệu từ file Save

    // --- GIAO DIỆN & MENU ---
    public static Action OnMenuTogglePressed;           // Mở/Đóng Bảng Quản Lý Nhà Máy
    public static Action OnSettingsTogglePressed;       // Mở/Đóng Bảng Cài Đặt

    // --- ĐIỀU KIỆN CHIẾN THẮNG ---
    public static Action OnGameVictory;                 // Đạt đủ 3 điều kiện thắng game

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        OnWalletChanged = null;
        OnItemsSold = null;
        OnMoneyAccumulated = null;
        OnMoneyVaultWithdrawn = null;
        OnItemProduced = null;
        OnItemPickedUp = null;
        OnMachineTierUpgraded = null;
        OnMachineCountChanged = null;
        OnWorkerHired = null;
        OnSaveDataRequested = null;
        OnDataLoaded = null;
        OnMenuTogglePressed = null;
        OnSettingsTogglePressed = null;
        OnGameVictory = null;
    }
}
