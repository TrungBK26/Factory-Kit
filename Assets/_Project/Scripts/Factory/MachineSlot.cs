using System;
using UnityEngine;

/// <summary>
/// Cấu hình vị trí máy đa hình (Modular Machine Slot).
/// Cho phép Level Designer kéo thả định vị các vị trí đặt máy trực quan trên Scene
/// mà không bao giờ lo lỗi vị trí hay đè vật thể.
/// </summary>
[Serializable]
public class MachineSlot
{
    [Tooltip("Định danh duy nhất không trùng lặp dùng để lưu file Save (VD: Cluster_Wood_Slot01)")]
    public string slotID;

    [Tooltip("Tên gợi nhớ trên Scene (VD: Máy Cưa #1, Máy Ép #2)")]
    public string slotName = "Slot 1";

    [Tooltip("Cấu hình SO riêng của máy này (Hỗ trợ máy cùng loại hoặc máy khác loại trong cùng cụm)")]
    public MachineConfigSO machineConfig;

    [Tooltip("Transform vị trí máy sẽ xuất hiện trên sàn xưởng")]
    public Transform spawnPoint;

    [Tooltip("Băng chuyền mà máy này sẽ nhả hàng vào")]
    public ConveyorBelt targetConveyor;

    [Tooltip("Máy có sẵn từ đầu game hay phải mua mở khóa")]
    public bool isUnlockedByDefault = false;

    // --- TRẠNG THÁI RUNTIME ---
    [HideInInspector] public bool isUnlocked;
    [HideInInspector] public int currentTier = 1;
    [HideInInspector] public int currentSpeedLevel = 0;
    [HideInInspector] public int currentStorageLevel = 0;
    [HideInInspector] public int hiredWorkerCount = 0;
    [HideInInspector] public MachineController spawnedMachineInstance;
}
