using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Thẻ giao diện đại diện cho từng slot máy trong menu Quản Lý Nhà Máy (MachineCardView).
/// Hiển thị thông số cấp độ và gắn các nút bấm nâng cấp, mở khóa, thuê công nhân.
/// </summary>
public class MachineCardView : MonoBehaviour
{
    [Header("--- THÔNG TIN HIỂN THỊ ---")]
    [SerializeField] private TextMeshProUGUI machineNameText;
    [SerializeField] private TextMeshProUGUI tierLevelText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private TextMeshProUGUI storageLevelText;

    [Header("--- HÌNH ẢNH UI (DỄ DÀNG KÉO THẢ THAY THẾ) ---")]
    [Tooltip("Ảnh nền của toàn bộ thẻ máy (Để trống sẽ dùng màu mặc định)")]
    [SerializeField] private Image cardBackgroundImage;

    [Tooltip("Icon đại diện cỗ máy hoặc tài nguyên (Để trống sẽ tự động dùng Emoji)")]
    [SerializeField] private Image machineIconImage;

    [Tooltip("Text Emoji hiển thị tạm thời khi chưa gắn hình ảnh 2D")]
    [SerializeField] private TextMeshProUGUI fallbackEmojiText;

    [Tooltip("Lớp phủ mờ khi máy chưa mở khóa")]
    [SerializeField] private Image lockOverlayImage;

    [Tooltip("Icon ổ khóa khi máy chưa mở khóa")]
    [SerializeField] private Image lockIconImage;

    [Tooltip("Ảnh nền nút Lên Tier")]
    [SerializeField] private Image tierButtonImage;

    [Tooltip("Ảnh nền nút Tăng Tốc Độ")]
    [SerializeField] private Image speedButtonImage;

    [Tooltip("Ảnh nền nút Mở Rộng Kho")]
    [SerializeField] private Image storageButtonImage;

    [Tooltip("Ảnh nền nút Thuê Công Nhân")]
    [SerializeField] private Image workerButtonImage;

    [Tooltip("Ảnh nền nút Mở Khóa Máy")]
    [SerializeField] private Image unlockButtonImage;

    [Header("--- NÚT BẤM CHỨC NĂNG ---")]
    [SerializeField] private Button upgradeTierBtn;
    [SerializeField] private TextMeshProUGUI upgradeTierCostText;

    [SerializeField] private Button upgradeSpeedBtn;
    [SerializeField] private TextMeshProUGUI upgradeSpeedCostText;

    [SerializeField] private Button upgradeStorageBtn;
    [SerializeField] private TextMeshProUGUI upgradeStorageCostText;

    [SerializeField] private Button hireWorkerBtn;
    [SerializeField] private TextMeshProUGUI hireWorkerCostText;

    [SerializeField] private Button unlockMachineBtn;
    [SerializeField] private TextMeshProUGUI unlockCostText;

    [Header("--- LỚP PHỦ KHI CHƯA MỞ KHÓA ---")]
    [SerializeField] private GameObject lockOverlay;

    private MachineSlot currentSlot;
    private ProductionLine currentLine;

    private void OnEnable()
    {
        GameEvents.OnWalletChanged += HandleWalletChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnWalletChanged -= HandleWalletChanged;
    }

    private void HandleWalletChanged(int newBalance)
    {
        UpdateDisplay();
    }

    public void Setup(MachineSlot slot, ProductionLine line)
    {
        currentSlot = slot;
        currentLine = line;

        if (machineNameText != null)
        {
            machineNameText.text = slot.machineConfig != null ? slot.machineConfig.machineName : slot.slotName;
        }

        UpdateDisplay();

        if (upgradeTierBtn != null)
        {
            upgradeTierBtn.onClick.RemoveAllListeners();
            upgradeTierBtn.onClick.AddListener(OnUpgradeTierClicked);
        }

        if (upgradeSpeedBtn != null)
        {
            upgradeSpeedBtn.onClick.RemoveAllListeners();
            upgradeSpeedBtn.onClick.AddListener(OnUpgradeSpeedClicked);
        }

        if (upgradeStorageBtn != null)
        {
            upgradeStorageBtn.onClick.RemoveAllListeners();
            upgradeStorageBtn.onClick.AddListener(OnUpgradeStorageClicked);
        }

        if (hireWorkerBtn != null)
        {
            hireWorkerBtn.onClick.RemoveAllListeners();
            hireWorkerBtn.onClick.AddListener(OnHireWorkerClicked);
        }

        if (unlockMachineBtn != null)
        {
            unlockMachineBtn.onClick.RemoveAllListeners();
            unlockMachineBtn.onClick.AddListener(OnUnlockClicked);
        }
    }

    private static string GetResourceEmoji(ResourceType type) => type switch
    {
        ResourceType.Wood => "🌲",
        ResourceType.Stone => "⛏️",
        ResourceType.Copper => "🥉",
        ResourceType.Iron => "🔩",
        ResourceType.Silver => "🥈",
        ResourceType.Gold => "🥇",
        _ => "⚙️"
    };

    public void UpdateDisplay()
    {
        if (currentSlot == null || currentSlot.machineConfig == null) return;

        bool unlocked = currentSlot.isUnlocked;
        int currentWallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;

        // 1. Cập nhật Icon máy & Background an toàn tuyệt đối (Không lỗi khi chưa có ảnh hoặc khi thêm ảnh sau này)
        Sprite icon = currentSlot.machineConfig.machineIcon;
        if (icon == null && currentSlot.machineConfig.tier1Resource != null)
        {
            icon = currentSlot.machineConfig.tier1Resource.resourceIcon;
        }

        if (machineIconImage != null)
        {
            Sprite effectiveSprite = icon != null ? icon : machineIconImage.sprite;
            if (effectiveSprite != null)
            {
                machineIconImage.sprite = effectiveSprite;
                machineIconImage.preserveAspect = true;
                machineIconImage.color = Color.white;
                machineIconImage.gameObject.SetActive(true);
                if (fallbackEmojiText != null) fallbackEmojiText.gameObject.SetActive(false);
            }
            else
            {
                // Chưa gán Sprite: ẩn Image để tránh ô vuông trắng
                machineIconImage.gameObject.SetActive(false);
                if (fallbackEmojiText != null)
                {
                    fallbackEmojiText.text = GetResourceEmoji(currentSlot.machineConfig.resourceType);
                    fallbackEmojiText.gameObject.SetActive(true);
                }
            }
        }
        else if (fallbackEmojiText != null)
        {
            fallbackEmojiText.text = GetResourceEmoji(currentSlot.machineConfig.resourceType);
            fallbackEmojiText.gameObject.SetActive(true);
        }

        // Ảnh nền thẻ
        if (cardBackgroundImage != null)
        {
            if (currentSlot.machineConfig.cardBackgroundOverride != null)
            {
                cardBackgroundImage.sprite = currentSlot.machineConfig.cardBackgroundOverride;
                cardBackgroundImage.color = Color.white;
            }
        }

        // Lớp phủ và icon khóa
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
        if (lockOverlayImage != null) lockOverlayImage.gameObject.SetActive(!unlocked);

        if (lockIconImage != null)
        {
            lockIconImage.preserveAspect = true;
            // Chỉ hiển thị icon ổ khóa nếu đã được gán sprite và slot đang khóa
            lockIconImage.gameObject.SetActive(!unlocked && lockIconImage.sprite != null);
        }

        // Giữ tỉ lệ cho các ảnh nền nút bấm nếu được gán sprite tùy biến
        if (tierButtonImage != null) tierButtonImage.preserveAspect = true;
        if (speedButtonImage != null) speedButtonImage.preserveAspect = true;
        if (storageButtonImage != null) storageButtonImage.preserveAspect = true;
        if (workerButtonImage != null) workerButtonImage.preserveAspect = true;
        if (unlockButtonImage != null) unlockButtonImage.preserveAspect = true;

        // 2. Trạng thái máy chưa mở khóa
        if (!unlocked)
        {
            if (unlockMachineBtn != null)
            {
                unlockMachineBtn.gameObject.SetActive(true);
                int unlockCost = currentSlot.machineConfig.unlockCost;
                unlockMachineBtn.interactable = currentWallet >= unlockCost;
                if (unlockCostText != null)
                {
                    unlockCostText.text = unlockCost > 0 ? $"Mở khóa: ${unlockCost:N0}" : "Mở khóa Miễn phí";
                }
            }
            return;
        }

        if (unlockMachineBtn != null) unlockMachineBtn.gameObject.SetActive(false);

        // 3. Hiển thị thông số cấp độ
        if (tierLevelText != null) tierLevelText.text = $"Tier: {currentSlot.currentTier}/3";
        if (speedLevelText != null) speedLevelText.text = $"Tốc độ: Cấp {currentSlot.currentSpeedLevel + 1}/4";
        if (storageLevelText != null) storageLevelText.text = $"Kho: Cấp {currentSlot.currentStorageLevel + 1}/4";

        // 4. Nút lên Tier
        int tierCost = currentSlot.machineConfig.GetTierCost(currentSlot.currentTier + 1);
        bool canUpgradeTier = currentSlot.currentTier < 3 && tierCost != -1;
        if (upgradeTierBtn != null)
        {
            upgradeTierBtn.interactable = canUpgradeTier && currentWallet >= tierCost;
            if (upgradeTierCostText != null)
            {
                upgradeTierCostText.text = canUpgradeTier ? $"Tier +1 (${tierCost:N0})" : "Tối đa (MAX)";
            }
        }

        // 5. Nút tăng tốc độ
        int speedCost = currentSlot.machineConfig.GetSpeedCost(currentSlot.currentSpeedLevel);
        bool canUpgradeSpeed = speedCost != -1 && (currentSlot.machineConfig.spawnIntervalLevels == null || currentSlot.currentSpeedLevel < currentSlot.machineConfig.spawnIntervalLevels.Length - 1);
        if (upgradeSpeedBtn != null)
        {
            upgradeSpeedBtn.interactable = canUpgradeSpeed && currentWallet >= speedCost;
            if (upgradeSpeedCostText != null)
            {
                upgradeSpeedCostText.text = canUpgradeSpeed ? $"Tốc độ (${speedCost:N0})" : "Tối đa (MAX)";
            }
        }

        // 6. Nút tăng sức chứa kho
        int storageCost = currentSlot.machineConfig.GetStorageCost(currentSlot.currentStorageLevel);
        bool canUpgradeStorage = storageCost != -1 && (currentSlot.machineConfig.storageCapacityLevels == null || currentSlot.currentStorageLevel < currentSlot.machineConfig.storageCapacityLevels.Length - 1);
        if (upgradeStorageBtn != null)
        {
            upgradeStorageBtn.interactable = canUpgradeStorage && currentWallet >= storageCost;
            if (upgradeStorageCostText != null)
            {
                upgradeStorageCostText.text = canUpgradeStorage ? $"Kho (${storageCost:N0})" : "Tối đa (MAX)";
            }
        }

        // 7. Nút thuê công nhân
        int workerCount = currentLine != null ? currentLine.WorkerCount : 0;
        int maxWorkers = currentLine != null ? currentLine.MaxWorkers : 2;
        int workerCost = currentSlot.machineConfig.GetWorkerCost(workerCount);
        bool canHireWorker = workerCount < maxWorkers && workerCost != -1;
        if (hireWorkerBtn != null)
        {
            hireWorkerBtn.interactable = canHireWorker && currentWallet >= workerCost;
            if (hireWorkerCostText != null)
            {
                hireWorkerCostText.text = canHireWorker
                    ? $"Thuê NPC (Hiện có: {workerCount}/{maxWorkers}) - ${workerCost:N0}"
                    : $"Đủ công nhân ({maxWorkers}/{maxWorkers})";
            }
        }
    }

    private void OnUpgradeTierClicked()
    {
        if (currentSlot == null || currentSlot.machineConfig == null) return;
        if (currentSlot.currentTier >= 3) return;

        int cost = currentSlot.machineConfig.GetTierCost(currentSlot.currentTier + 1);
        if (cost > 0 && EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost))
        {
            currentSlot.currentTier++;
            if (currentSlot.spawnedMachineInstance != null)
            {
                currentSlot.spawnedMachineInstance.SetTier(currentSlot.currentTier);
            }
            UpdateDisplay();
            GameEvents.OnMachineTierUpgraded?.Invoke(currentSlot.machineConfig.resourceType, currentSlot.currentTier);
        }
    }

    private void OnUpgradeSpeedClicked()
    {
        if (currentSlot == null || currentSlot.machineConfig == null) return;

        int cost = currentSlot.machineConfig.GetSpeedCost(currentSlot.currentSpeedLevel);
        if (cost > 0 && EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost))
        {
            currentSlot.currentSpeedLevel++;
            if (currentSlot.spawnedMachineInstance != null)
            {
                currentSlot.spawnedMachineInstance.SetSpeedLevel(currentSlot.currentSpeedLevel);
            }
            UpdateDisplay();
        }
    }

    private void OnUpgradeStorageClicked()
    {
        if (currentSlot == null || currentSlot.machineConfig == null) return;

        int cost = currentSlot.machineConfig.GetStorageCost(currentSlot.currentStorageLevel);
        if (cost > 0 && EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost))
        {
            currentSlot.currentStorageLevel++;
            if (currentLine != null && currentLine.TargetStoragePad != null && currentSlot.machineConfig.storageCapacityLevels != null)
            {
                if (currentSlot.currentStorageLevel >= 0 && currentSlot.currentStorageLevel < currentSlot.machineConfig.storageCapacityLevels.Length)
                {
                    int newCap = currentSlot.machineConfig.storageCapacityLevels[currentSlot.currentStorageLevel];
                    currentLine.TargetStoragePad.SetCapacity(newCap);
                }
            }
            UpdateDisplay();
        }
    }

    private void OnHireWorkerClicked()
    {
        if (currentLine == null || currentSlot == null || currentSlot.machineConfig == null) return;

        int cost = currentSlot.machineConfig.GetWorkerCost(currentLine.WorkerCount);
        if (cost > 0 && EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost))
        {
            currentLine.HireWorker();
            UpdateDisplay();
        }
    }

    private void OnUnlockClicked()
    {
        if (currentLine == null || currentSlot == null || currentSlot.machineConfig == null) return;

        int cost = currentSlot.machineConfig.unlockCost;
        if (cost <= 0 || (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost)))
        {
            currentLine.UnlockSlot(currentSlot);
            UpdateDisplay();
        }
    }
}
