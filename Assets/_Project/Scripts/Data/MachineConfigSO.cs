using UnityEngine;

[CreateAssetMenu(fileName = "NewMachineConfig", menuName = "Factory/Machine Config")]
public class MachineConfigSO : ScriptableObject
{
    [Header("--- THÔNG TIN MÁY ---")]
    public string machineName = "Máy Sản Xuất";
    public ResourceType resourceType = ResourceType.Wood;
    [Tooltip("Chi phí mở khóa cỗ máy in-world (0 = Mở khóa sẵn mặc định)")]
    public int unlockCost = 0;
    [Header("--- HÌNH ẢNH / UI ICONS (Dễ dàng thay thế sau này) ---")]
    [Tooltip("Icon đại diện cỗ máy trên Menu Nâng Cấp (Để trống sẽ tự động dùng Icon tài nguyên hoặc Emoji)")]
    public Sprite machineIcon;

    [Tooltip("Khung viền / Card Background đặc trưng (nếu muốn mỗi loại máy có màu thẻ khác nhau)")]
    public Sprite cardBackgroundOverride;

    [Header("--- 3 TIERS VISUALS (Ngoại hình phân biệt) ---")]
    public GameObject tier1VisualPrefab; // Máy rỉ sét / khung gỗ
    public GameObject tier2VisualPrefab; // Máy thép hiện đại, đèn báo
    public GameObject tier3VisualPrefab; // Máy công nghiệp bọc giáp hoàng gia

    [Header("--- TÀI NGUYÊN NHẢ THEO TIER ---")]
    public ResourceDataSO tier1Resource; // Mẩu vụn ($X)
    public ResourceDataSO tier2Resource; // Bó dây/quặng đống ($3X)
    public ResourceDataSO tier3Resource; // Pallet kiện hàng lớn ($10X)

    [Header("--- GIỚI HẠN MỞ RỘNG ---")]
    public int maxMachineCount = 3;      // Tối đa 3 máy cùng loại trong cụm (VD: 3 máy Đồng)
    public int maxWorkersPerMachine = 2; // Tối đa 2 NPC phụ trách máy này

    [Header("--- BẢNG CHỈ SỐ NÂNG CẤP (4 Cấp độ chuẩn) ---")]
    public float[] spawnIntervalLevels = { 3.0f, 2.2f, 1.5f, 1.0f }; // Tốc độ nhả hàng (giây)
    public int[] storageCapacityLevels = { 10, 15, 20, 30 };          // Dung lượng bệ chứa (món)

    [Header("--- CHI PHÍ NÂNG CẤP CƠ BẢN ---")]
    public int[] tierUpgradeCosts = { 0, 150, 400 };                  // Lên Tier 1 ($0), Tier 2 ($150), Tier 3 ($400)
    public int[] baseSpeedUpgradeCosts = { 150, 400, 1000 };          // Chi phí gốc Cấp 2, 3, 4
    public int[] baseStorageUpgradeCosts = { 100, 250, 600 };         // Chi phí gốc Cấp 2, 3, 4
    public int[] baseWorkerHireCosts = { 300, 800 };                  // Thuê Worker 1, 2

    [Header("--- HỆ SỐ KINH TẾ CẤP CAO ---")]
    [Tooltip("Hệ số nhân chi phí nâng cấp (Gỗ = 1x, Đá = 2x, Đồng = 5x, Vàng = 50x...)")]
    public float economyMultiplier = 1.0f;

    public int GetTierCost(int targetTier)
    {
        if (targetTier <= 1 || targetTier > 3) return -1;
        if (tierUpgradeCosts == null || targetTier - 1 >= tierUpgradeCosts.Length) return -1;
        return tierUpgradeCosts[targetTier - 1];
    }

    public int GetSpeedCost(int currentLevel)
    {
        if (currentLevel >= baseSpeedUpgradeCosts.Length) return -1; // Max
        return Mathf.RoundToInt(baseSpeedUpgradeCosts[currentLevel] * economyMultiplier);
    }

    public int GetStorageCost(int currentLevel)
    {
        if (currentLevel >= baseStorageUpgradeCosts.Length) return -1; // Max
        return Mathf.RoundToInt(baseStorageUpgradeCosts[currentLevel] * economyMultiplier);
    }

    public int GetWorkerCost(int currentWorkerCount)
    {
        if (currentWorkerCount >= baseWorkerHireCosts.Length) return -1; // Max
        return Mathf.RoundToInt(baseWorkerHireCosts[currentWorkerCount] * economyMultiplier);
    }
}
