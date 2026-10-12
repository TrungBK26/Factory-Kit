using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tổng thể toàn bộ các cụm dây chuyền sản xuất trong nhà máy (Factory Manager).
/// Theo dõi danh sách ProductionLine, cung cấp dữ liệu cho FactoryManagementUI
/// và kiểm tra điều kiện hoàn thành 6 dòng tài nguyên đạt Tier 3 (Win Condition).
/// </summary>
public class FactoryManager : MonoBehaviour
{
    private static FactoryManager _instance;
    public static FactoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FactoryManager>();
                if (_instance == null)
                {
                    var go = new GameObject("FactoryManager_Singleton");
                    _instance = go.AddComponent<FactoryManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- DANH SÁCH CỤM DÂY CHUYỀN ---")]
    [SerializeField] private List<ProductionLine> productionLines = new List<ProductionLine>();

    public IReadOnlyList<ProductionLine> ProductionLines => productionLines;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        var _ = Instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        RefreshProductionLines();
    }

    /// <summary>
    /// Làm mới danh sách dây chuyền, loại bỏ các đối tượng đã bị hủy khi nạp lại Scene.
    /// </summary>
    public void RefreshProductionLines()
    {
        productionLines.RemoveAll(p => p == null);
        var foundLines = FindObjectsByType<ProductionLine>();
        foreach (var pl in foundLines)
        {
            if (pl != null && !productionLines.Contains(pl))
            {
                productionLines.Add(pl);
            }
        }
    }

    public void RegisterProductionLine(ProductionLine line)
    {
        productionLines.RemoveAll(p => p == null);
        if (line != null && !productionLines.Contains(line))
        {
            productionLines.Add(line);
        }
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ 6 dòng tài nguyên (Gỗ, Đá, Đồng, Sắt, Bạc, Vàng)
    /// đã được mở khóa và mỗi dòng có ít nhất 1 cỗ máy đạt Tier 3 tối đa chưa.
    /// </summary>
    public bool IsAllSixResourcesMaxTier()
    {
        HashSet<ResourceType> maxTierResources = new HashSet<ResourceType>();

        foreach (var line in productionLines)
        {
            if (line == null) continue;

            foreach (var slot in line.MachineSlots)
            {
                if (slot != null && slot.isUnlocked && slot.machineConfig != null)
                {
                    int tier = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.CurrentTier : slot.currentTier;
                    if (tier >= 3)
                    {
                        maxTierResources.Add(slot.machineConfig.resourceType);
                    }
                }
            }
        }

        return maxTierResources.Count >= 6;
    }
}
