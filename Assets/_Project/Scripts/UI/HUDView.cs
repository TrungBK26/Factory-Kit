using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Màn hình HUD chính trong lúc chơi (HUDView).
/// Hiển thị số dư ví cá nhân, số lượng hàng vác trên đầu, và nút tắt mở nhanh Cài đặt/Quản lý.
/// </summary>
public class HUDView : MonoBehaviour
{
    [Header("--- THÔNG TIN TIỀN TỆ & TẢI TRỌNG ---")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI capacityText;

    [Header("--- NÚT BẤM TIỆN ÍCH ---")]
    [SerializeField] private Button factoryMenuButton;
    [SerializeField] private Button settingsButton;

    private PlayerCarry playerCarry;

    private void Awake()
    {
        if (factoryMenuButton != null)
        {
            factoryMenuButton.onClick.AddListener(() => FactoryManagementUI.Instance.ToggleMenu());
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => GameEvents.OnSettingsTogglePressed?.Invoke());
        }
    }

    private void Start()
    {
        playerCarry = FindAnyObjectByType<PlayerCarry>();

        int wallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;
        UpdateWallet(wallet);

        int count = playerCarry != null ? playerCarry.CurrentCount : 0;
        UpdateCapacity(count);
    }

    private void OnEnable()
    {
        GameEvents.OnWalletChanged += UpdateWallet;
        GameEvents.OnItemPickedUp += UpdateCapacity;
        GameEvents.OnItemsSold += HandleItemSold;
    }

    private void OnDisable()
    {
        GameEvents.OnWalletChanged -= UpdateWallet;
        GameEvents.OnItemPickedUp -= UpdateCapacity;
        GameEvents.OnItemsSold -= HandleItemSold;
    }

    private void UpdateWallet(int newWallet)
    {
        if (moneyText != null)
        {
            moneyText.text = $"$ {newWallet:N0}";
        }
    }

    private void UpdateCapacity(int currentCount)
    {
        if (playerCarry == null) playerCarry = FindAnyObjectByType<PlayerCarry>();

        int max = playerCarry != null ? playerCarry.MaxCapacity : 5;
        if (capacityText != null)
        {
            capacityText.text = $"{currentCount}/{max}";
        }
    }

    private void HandleItemSold(int value)
    {
        if (playerCarry != null)
        {
            UpdateCapacity(playerCarry.CurrentCount);
        }
    }
}
