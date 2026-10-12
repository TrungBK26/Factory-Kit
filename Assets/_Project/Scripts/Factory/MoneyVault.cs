using UnityEngine;
using TMPro;

/// <summary>
/// Két tiền tích lũy doanh thu (Money Vault / Collector).
/// Lắng nghe sự kiện OnItemsSold từ SellZone để cộng dồn tiền trong két.
/// Người chơi tiến lại gần bấm phím F để rút toàn bộ tiền tích lũy vào ví cá nhân.
/// </summary>
public class MoneyVault : MonoBehaviour, IInteractable
{
    [Header("--- HIỂN THỊ TRỰC QUAN ---")]
    [Tooltip("Text 3D hiển thị số tiền đang có trong két")]
    [SerializeField] private TextMeshPro moneyDisplayText;

    [Tooltip("Hiệu ứng phát sáng nhẹ khi két có tiền")]
    [SerializeField] private GameObject pulseGlowEffect;

    private int accumulatedMoney = 0;

    public int AccumulatedMoney => accumulatedMoney;

    private void OnEnable()
    {
        GameEvents.OnItemsSold += HandleItemSold;
    }

    private void OnDisable()
    {
        GameEvents.OnItemsSold -= HandleItemSold;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void HandleItemSold(int amount)
    {
        accumulatedMoney += amount;
        GameEvents.OnMoneyAccumulated?.Invoke(accumulatedMoney);
        UpdateUI();
    }

    /// <summary>
    /// Cho phép SaveManager thanh lý hàng tồn kho nạp trực tiếp vào két khi lưu game.
    /// </summary>
    public void AddAccumulatedMoney(int amount)
    {
        if (amount > 0)
        {
            HandleItemSold(amount);
        }
    }

    /// <summary>
    /// Thiết lập chính xác số tiền két khi nạp file Save (tránh bị nhân đôi/cộng dồn).
    /// </summary>
    public void SetAccumulatedMoney(int amount)
    {
        accumulatedMoney = Mathf.Max(0, amount);
        GameEvents.OnMoneyAccumulated?.Invoke(accumulatedMoney);
        UpdateUI();
    }

    // --- TRIỂN KHAI INTERFACE IINTERACTABLE ---

    public string GetInteractionPrompt()
    {
        return accumulatedMoney > 0
            ? $"[F] Rút tiền két (+$ {accumulatedMoney})"
            : "Két tiền rỗng ($0)";
    }

    public void OnInteract(PlayerCarry playerCarry)
    {
        if (accumulatedMoney <= 0)
        {
            Debug.Log("[MoneyVault] Két tiền hiện đang rỗng!");
            return;
        }

        int withdrawn = accumulatedMoney;
        accumulatedMoney = 0;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(withdrawn);
        }

        GameEvents.OnMoneyVaultWithdrawn?.Invoke(withdrawn);
        Debug.Log($"[Economy] Đã rút ${withdrawn} từ két vào ví cá nhân.");
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (moneyDisplayText != null)
        {
            moneyDisplayText.text = accumulatedMoney > 0 ? $"+${accumulatedMoney}" : "$0";
        }

        if (pulseGlowEffect != null)
        {
            pulseGlowEffect.SetActive(accumulatedMoney > 0);
        }
    }
}
