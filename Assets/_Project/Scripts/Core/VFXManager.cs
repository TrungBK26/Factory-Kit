using UnityEngine;

/// <summary>
/// Trình Quản Lý Hiệu Ứng Hình Ảnh (VFXManager).
/// Phụ trách hiệu ứng hạt (Particles), chữ tiền bay (Floating Text), sóng nổ nâng cấp (Radial Burst).
/// Lắng nghe gián tiếp qua GameEvents, kiểm tra an toàn 100% không văng lỗi kể cả khi chưa gán Prefab.
/// Tích hợp Self-Healing Singleton và an toàn tuyệt đối với Domain Reload trong Unity 6.
/// </summary>
public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<VFXManager>();
                if (_instance == null)
                {
                    var go = new GameObject("VFXManager_Singleton");
                    _instance = go.AddComponent<VFXManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- PREFABS HIỆU ỨNG (PARTICLES & TEXT) ---")]
    [Tooltip("Hiệu ứng hạt lấp lánh nổ nhẹ khi dỡ bán hàng tại SellZone")]
    [SerializeField] private GameObject sellSparklePrefab;

    [Tooltip("Hiệu ứng sóng xung kích tròn bung tỏa khi nâng cấp máy")]
    [SerializeField] private GameObject upgradeBurstPrefab;

    [Tooltip("Prefab chữ tiền nổi (Floating Text) +$X")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("--- THIẾT LẬP TỰ ĐỘNG DỌN RÁC HIỆU ỨNG ---")]
    [SerializeField] private float vfxLifetime = 2.0f;

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
        }
    }

    private void OnEnable()
    {
        GameEvents.OnItemsSold += HandleItemsSold;
        GameEvents.OnMoneyVaultWithdrawn += HandleVaultWithdrawn;
        GameEvents.OnMachineTierUpgraded += HandleMachineTierUpgraded;
        GameEvents.OnWorkerHired += HandleWorkerHired;
    }

    private void OnDisable()
    {
        GameEvents.OnItemsSold -= HandleItemsSold;
        GameEvents.OnMoneyVaultWithdrawn -= HandleVaultWithdrawn;
        GameEvents.OnMachineTierUpgraded -= HandleMachineTierUpgraded;
        GameEvents.OnWorkerHired -= HandleWorkerHired;
    }

    public void PlaySellVFX(Vector3 position, int earnedMoney)
    {
        // 1. Nổ hạt lấp lánh tại SellZone
        if (sellSparklePrefab != null)
        {
            GameObject fx = Instantiate(sellSparklePrefab, position + Vector3.up * 0.2f, Quaternion.identity);
            Destroy(fx, vfxLifetime);
        }

        // 2. Chữ tiền nổi +$X bay lên
        if (floatingTextPrefab != null)
        {
            GameObject txtObj = Instantiate(floatingTextPrefab, position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(txtObj, vfxLifetime);
        }
    }

    public void PlayUpgradeVFX(Vector3 position)
    {
        if (upgradeBurstPrefab != null)
        {
            GameObject fx = Instantiate(upgradeBurstPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(fx, vfxLifetime);
        }
    }

    private void HandleItemsSold(int value)
    {
        var sellZone = FindAnyObjectByType<SellZone>();
        Vector3 pos = sellZone != null ? sellZone.transform.position : Vector3.zero;
        PlaySellVFX(pos, value);
    }

    private void HandleVaultWithdrawn(int amount)
    {
        var vault = FindAnyObjectByType<MoneyVault>();
        Vector3 pos = vault != null ? vault.transform.position : Vector3.zero;
        if (floatingTextPrefab != null)
        {
            GameObject txtObj = Instantiate(floatingTextPrefab, pos + Vector3.up * 1.8f, Quaternion.identity);
            Destroy(txtObj, vfxLifetime);
        }
    }

    private void HandleMachineTierUpgraded(ResourceType type, int tier)
    {
        if (FactoryManager.Instance != null)
        {
            foreach (var line in FactoryManager.Instance.ProductionLines)
            {
                if (line == null) continue;
                foreach (var slot in line.MachineSlots)
                {
                    if (slot != null && slot.machineConfig != null && slot.machineConfig.resourceType == type)
                    {
                        Vector3 pos = slot.spawnPoint != null ? slot.spawnPoint.position : line.transform.position;
                        PlayUpgradeVFX(pos);
                        return;
                    }
                }
            }
        }
    }

    private void HandleWorkerHired(ResourceType type, int count)
    {
        if (FactoryManager.Instance != null)
        {
            foreach (var line in FactoryManager.Instance.ProductionLines)
            {
                if (line == null) continue;
                if (line.MachineSlots.Count > 0 && line.MachineSlots[0].machineConfig != null && line.MachineSlots[0].machineConfig.resourceType == type)
                {
                    PlayUpgradeVFX(line.transform.position);
                    return;
                }
            }
        }
    }
}
