using UnityEngine;

/// <summary>
/// Kiểm tra điều kiện thắng cuộc của game (Win Condition Checker).
/// Đáp ứng đủ 3 tiêu chuẩn:
/// 1. Mở khóa đủ 6 dòng tài nguyên (Gỗ -> Vàng).
/// 2. Nâng cấp ít nhất 1 máy của mỗi dòng lên Tier 3 tối đa.
/// 3. Số dư ví cá nhân đạt mốc tối thiểu $50,000.
/// </summary>
public class WinConditionChecker : MonoBehaviour
{
    private static WinConditionChecker _instance;
    public static WinConditionChecker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<WinConditionChecker>();
                if (_instance == null)
                {
                    var go = new GameObject("WinConditionChecker_Singleton");
                    _instance = go.AddComponent<WinConditionChecker>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- MỤC TIÊU TÀI CHÍNH CHIẾN THẮNG ---")]
    [Tooltip("Số tiền mục tiêu trong ví để đạt chiến thắng ($)")]
    [SerializeField] private int winMoneyTarget = 200000;

    public int WinMoneyTarget => winMoneyTarget;
    public bool HasClaimedVictory { get; set; } = false;

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
        GameEvents.OnWalletChanged += CheckVictoryCondition;
        GameEvents.OnMachineTierUpgraded += HandleTierUpgraded;
    }

    private void OnDisable()
    {
        GameEvents.OnWalletChanged -= CheckVictoryCondition;
        GameEvents.OnMachineTierUpgraded -= HandleTierUpgraded;
    }

    private void HandleTierUpgraded(ResourceType type, int tier)
    {
        int wallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;
        CheckVictoryCondition(wallet);
    }

    public void CheckVictoryCondition(int currentMoney)
    {
        if (HasClaimedVictory) return;

        // 1. Kiểm tra số dư ví đạt $50,000
        if (currentMoney < winMoneyTarget) return;

        // 2 & 3. Kiểm tra máy móc
        bool isMachinesReady = false;

        if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines.Count >= 6)
        {
            isMachinesReady = FactoryManager.Instance.IsAllSixResourcesMaxTier();
        }
        else
        {
            // Trong chế độ Prototype hoặc khi đang mở rộng các cụm:
            // Kiểm tra tất cả các cụm máy hiện có trên Scene đạt Tier 3
            var machines = FindObjectsByType<MachineController>();
            if (machines.Length > 0)
            {
                bool allTier3 = true;
                foreach (var m in machines)
                {
                    if (m != null && m.CurrentTier < 3)
                    {
                        allTier3 = false;
                        break;
                    }
                }
                isMachinesReady = allTier3;
            }
            else
            {
                isMachinesReady = true;
            }
        }

        if (isMachinesReady)
        {
            HasClaimedVictory = true;
            Debug.Log("[GameManager] CHIẾN THẮNG! Máy đạt Tier 3 và ví cá nhân đạt $50,000.");
            GameEvents.OnGameVictory?.Invoke();
            GameEvents.OnSaveDataRequested?.Invoke();
        }
    }
}
