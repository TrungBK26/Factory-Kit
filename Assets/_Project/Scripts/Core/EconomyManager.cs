using UnityEngine;

/// <summary>
/// Quản lý ví tiền của người chơi và các giao dịch kinh tế trong nhà máy.
/// Phát sự kiện OnWalletChanged qua GameEvents để HUD cập nhật tự động.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    private static EconomyManager _instance;
    public static EconomyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<EconomyManager>();
                if (_instance == null)
                {
                    var go = new GameObject("EconomyManager_Singleton");
                    _instance = go.AddComponent<EconomyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- SỐ DƯ TÀI KHOẢN ---")]
    [Tooltip("Số tiền khởi đầu ban đầu của người chơi ($)")]
    [SerializeField] private int initialWallet = 0;

    public int CurrentWallet { get; private set; } = 0;

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
            CurrentWallet = initialWallet;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Nạp thêm tiền vào ví (từ két tiền hoặc bán hàng).
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        CurrentWallet += amount;
        Debug.Log($"[Economy] Đã cộng ${amount} vào ví. Số dư hiện tại: ${CurrentWallet}");
        GameEvents.OnWalletChanged?.Invoke(CurrentWallet);
    }

    /// <summary>
    /// Thử trừ tiền khi mua máy mới, lên Tier, tăng tốc độ, hoặc thuê công nhân.
    /// </summary>
    /// <returns>True nếu đủ tiền và giao dịch thành công, False nếu không đủ tiền.</returns>
    public bool TrySpendMoney(int amount)
    {
        if (amount < 0) return false;

        if (CurrentWallet < amount)
        {
            Debug.Log($"[Economy] Không đủ tiền! Cần: ${amount}, Hiện có: ${CurrentWallet}");
            return false;
        }

        CurrentWallet -= amount;
        Debug.Log($"[Economy] Đã trừ ${amount}. Số dư còn lại: ${CurrentWallet}");
        GameEvents.OnWalletChanged?.Invoke(CurrentWallet);
        return true;
    }

    /// <summary>
    /// Dành riêng cho SaveManager khôi phục dữ liệu ví khi load game.
    /// </summary>
    public void SetWalletBalance(int amount)
    {
        CurrentWallet = Mathf.Max(0, amount);
        GameEvents.OnWalletChanged?.Invoke(CurrentWallet);
    }
}
