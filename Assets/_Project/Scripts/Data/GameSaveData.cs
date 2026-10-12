using System;
using System.Collections.Generic;

/// <summary>
/// Trạng thái lưu trữ của từng Slot máy trong nhà máy.
/// Được nhận diện bằng slotID độc lập để không bao giờ bị lệch dữ liệu khi bổ sung máy mới.
/// </summary>
[Serializable]
public class SlotSaveState
{
    public string slotID;         // Định danh duy nhất: "Cluster_Wood_Slot00"
    public bool isUnlocked;       // Đã mở khóa hay chưa
    public int tierLevel = 1;     // Cấp bậc máy (Tier 1, 2, 3)
    public int speedLevel = 0;    // Cấp độ tốc độ nhả (0 -> 3)
    public int storageLevel = 0;  // Cấp độ sức chứa kho (0 -> 3)
    public int hiredWorkers = 0;  // Số lượng công nhân đang phụ trách
}

/// <summary>
/// Dữ liệu tổng thể lưu trữ tiến trình game (Game Save Data).
/// Tương thích serialization JsonUtility cho cả PC và WebGL (PlayerPrefs / LocalStorage).
/// </summary>
[Serializable]
public class GameSaveData
{
    public int playerWallet = 0;           // Số tiền trong ví cá nhân
    public int vaultMoney = 0;              // Tiền tích lũy trong két
    public bool hasClaimedVictory = false;  // Cờ đã đạt chiến thắng (phục vụ chế độ Free Play)
    public List<SlotSaveState> slots = new List<SlotSaveState>();
}
