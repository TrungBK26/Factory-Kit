using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Quản Lý & Nâng Cấp Nhà Máy (Factory Management UI / Tablet).
/// Hỗ trợ cả 2 chế độ:
/// 1. Chế độ uGUI Canvas: Khi đã gán PanelRoot và Card Prefab, hệ thống sẽ sinh thẻ uGUI động.
/// 2. Chế độ OnGUI Tablet Fallback: Giao diện Tablet gỗ Retro cao cấp, bố cục thẻ máy chi tiết,
///    hỗ trợ lọc tab (Tất cả, Đang hoạt động, Chưa mở khóa), màu sắc trực quan theo từng loại nâng cấp.
/// </summary>
public class FactoryManagementUI : MonoBehaviour
{
    private static FactoryManagementUI _instance;
    public static FactoryManagementUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FactoryManagementUI>();
                if (_instance == null)
                {
                    var go = new GameObject("FactoryManagementUI_Singleton");
                    _instance = go.AddComponent<FactoryManagementUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- THÀNH PHẦN CANVAS UGUI (TÙY CHỌN GẮN PREFAB) ---")]
    [Tooltip("Panel gốc chứa toàn bộ menu quản lý")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Khu vực ScrollView chứa danh sách các thẻ máy")]
    [SerializeField] private Transform cardContainer;

    [Tooltip("Prefab của thẻ máy (MachineCardView)")]
    [SerializeField] private GameObject machineCardPrefab;

    [Tooltip("Nút X đóng menu trên Canvas")]
    [SerializeField] private Button closeButton;

    private bool isOpen = false;
    private Vector2 scrollPos = Vector2.zero;
    private int selectedTab = 0; // 0: Tất cả, 1: Đang hoạt động, 2: Chưa mở khóa

    public bool IsOpen => isOpen;

    private float lastToggleTime = -1f;

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
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnMenuTogglePressed += ToggleMenu;
        GameEvents.OnSettingsTogglePressed += HandleEscape;
    }

    private void OnDisable()
    {
        GameEvents.OnMenuTogglePressed -= ToggleMenu;
        GameEvents.OnSettingsTogglePressed -= HandleEscape;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }
            else if (kb.escapeKey.wasPressedThisFrame && isOpen)
            {
                CloseMenu();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M))
        {
            ToggleMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseMenu();
        }
#endif
    }

    public void ToggleMenu()
    {
        if (Time.unscaledTime - lastToggleTime < 0.25f) return;
        lastToggleTime = Time.unscaledTime;

        if (isOpen) CloseMenu();
        else OpenMenu();
    }

    public void OpenMenu()
    {
        lastToggleTime = Time.unscaledTime;
        isOpen = true;
        if (panelRoot != null) panelRoot.SetActive(true);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void CloseMenu()
    {
        lastToggleTime = Time.unscaledTime;
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
    }

    private void HandleEscape()
    {
        if (isOpen)
        {
            CloseMenu();
        }
    }

    public void RefreshUI()
    {
        if (cardContainer == null || machineCardPrefab == null) return;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        if (FactoryManager.Instance == null) return;

        foreach (var line in FactoryManager.Instance.ProductionLines)
        {
            if (line == null) continue;

            foreach (var slot in line.MachineSlots)
            {
                if (slot == null) continue;

                var cardObj = Instantiate(machineCardPrefab, cardContainer);
                if (cardObj.TryGetComponent<MachineCardView>(out var cardView))
                {
                    cardView.Setup(slot, line);
                }
            }
        }
    }

    // ==================== ONGUI TABLET CAO CẤP ====================
    private void OnGUI()
    {
        if (!isOpen || panelRoot != null) return;
        GUI.depth = -100;

        try
        {
            DrawTabletGUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FactoryManagementUI] Lỗi khi vẽ Tablet: {ex.Message}");
        }
    }

    private void DrawTabletGUI()
    {
        UIThemeManager.Initialize();

        // 1. Phủ mờ không gian phía sau
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float tabletWidth = Mathf.Min(980f, Screen.width - 40f);
        float tabletHeight = Mathf.Min(680f, Screen.height - 40f);
        float tabletX = (Screen.width - tabletWidth) / 2f;
        float tabletY = (Screen.height - tabletHeight) / 2f;

        // 2. Khung viền Tablet gỗ Retro
        GUI.Box(new Rect(tabletX, tabletY, tabletWidth, tabletHeight), GUIContent.none, UIThemeManager.PanelStyle);

        // Khung nội dung có đệm lề an toàn chống tràn mép
        GUILayout.BeginArea(new Rect(tabletX + 18, tabletY + 16, tabletWidth - 36, tabletHeight - 32));

        // 3. Header: Tiêu đề, Ví tiền và Nút đóng
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("<size=19><color=#FFEAA7><b>TRUNG TÂM QUẢN LÝ & NÂNG CẤP NHÀ MÁY</b></color></size>", UIThemeManager.HeaderStyle);
        GUILayout.Label("<size=12><color=#DDD0C0><i>• Nâng cấp cỗ máy • Tăng tốc độ • Mở rộng kho • Thuê NPC vận chuyển tự động</i></color></size>", UIThemeManager.LabelStyle);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Huy hiệu số dư ví tiền
        int wallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Box($"<size=15><color=#FFD700><b>$ {wallet:N0}</b></color></size>", UIThemeManager.MoneyBadgeStyle, GUILayout.Height(34));
        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.95f, 0.40f, 0.35f);
        if (GUILayout.Button("✕ Đóng (Esc)", UIThemeManager.ButtonRoundedStyle, GUILayout.Width(125), GUILayout.Height(34)))
        {
            CloseMenu();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 4. Thanh lọc danh mục máy (Tabs)
        int totalCount = 0;
        int activeCount = 0;
        int lockedCount = 0;

        if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines != null)
        {
            foreach (var l in FactoryManager.Instance.ProductionLines)
            {
                if (l == null || l.MachineSlots == null) continue;
                foreach (var s in l.MachineSlots)
                {
                    if (s == null) continue;
                    totalCount++;
                    if (s.isUnlocked) activeCount++;
                    else lockedCount++;
                }
            }
        }

        GUILayout.BeginHorizontal();
        if (DrawFilterTab($"Tất Cả ({totalCount})", selectedTab == 0, 150)) selectedTab = 0;
        GUILayout.Space(6);
        if (DrawFilterTab($"Đang Hoạt Động ({activeCount})", selectedTab == 1, 190)) selectedTab = 1;
        GUILayout.Space(6);
        if (DrawFilterTab($"Chưa Mở Khóa ({lockedCount})", selectedTab == 2, 180)) selectedTab = 2;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // 5. Thân Tablet: Danh sách cuộn thẻ máy
        // Tính toán chiều cao ScrollView vừa vặn chính xác, loại bỏ 100% lỗi thanh cam lòi ở đáy!
        float scrollHeight = tabletHeight - 128f;
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Width(tabletWidth - 36), GUILayout.Height(scrollHeight));

        bool renderedAny = false;
        if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines != null)
        {
            foreach (var line in FactoryManager.Instance.ProductionLines)
            {
                if (line == null || line.MachineSlots == null) continue;

                foreach (var slot in line.MachineSlots)
                {
                    if (slot == null || slot.machineConfig == null) continue;

                    // Lọc theo Tab
                    if (selectedTab == 1 && !slot.isUnlocked) continue;
                    if (selectedTab == 2 && slot.isUnlocked) continue;

                    DrawMachineCardGUI(slot, line, wallet, tabletWidth - 76);
                    GUILayout.Space(10);
                    renderedAny = true;
                }
            }
        }

        if (!renderedAny)
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle);
            GUILayout.Label("<color=#CCCCCC>Không có cỗ máy nào phù hợp với bộ lọc hiện tại.</color>", UIThemeManager.LabelStyle);
            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private bool DrawFilterTab(string text, bool isActive, float width)
    {
        bool clicked = GUILayout.Button(text, isActive ? UIThemeManager.TabActiveStyle : UIThemeManager.TabInactiveStyle, GUILayout.Width(width), GUILayout.Height(32));
        return clicked;
    }

    private void DrawMachineCardGUI(MachineSlot slot, ProductionLine line, int currentWallet, float cardWidth)
    {
        bool unlocked = slot.isUnlocked;
        string resTag = GetResourceTag(slot.machineConfig.resourceType);
        string mName = slot.machineConfig != null ? slot.machineConfig.machineName : slot.slotName;

        GUILayout.BeginVertical(UIThemeManager.MachineCardStyle, GUILayout.Width(cardWidth));

        // 1. HÀNG TIÊU ĐỀ THẺ MÁY
        GUILayout.BeginHorizontal();
        GUILayout.Label($"<size=16><b>{resTag} {mName.ToUpper()}</b></size>", UIThemeManager.SubHeaderStyle, GUILayout.Width(280));

        if (unlocked)
        {
            GUILayout.Box("<size=12><color=#52C41A><b>● ĐANG HOẠT ĐỘNG</b></color></size>", UIThemeManager.MiniBadgeStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            GUILayout.Box($"<size=12><color=#FFD700><b>CẤP TIER: {slot.currentTier}/3</b></color></size>", UIThemeManager.MiniBadgeStyle, GUILayout.Height(24));
        }
        else
        {
            GUILayout.Box("<size=12><color=#FFA940><b>CHƯA MỞ KHÓA</b></color></size>", UIThemeManager.MiniBadgeStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 2. NỘI DUNG THẺ: MÁY CHƯA MỞ KHÓA
        if (!unlocked)
        {
            int unlockCost = slot.machineConfig.unlockCost;
            bool canAfford = currentWallet >= unlockCost;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"<color=#BBAA99>Dây chuyền sản xuất tự động tài nguyên {slot.machineConfig.resourceType}. Mở khóa để bắt đầu vận hành và xuất bán hàng hóa!</color>", UIThemeManager.LabelStyle);
            GUILayout.Label($"Chi phí xây dựng: <size=14><color=#FFD700><b>${unlockCost:N0}</b></color></size>", UIThemeManager.LabelStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = canAfford ? new Color(0.35f, 0.88f, 0.45f) : new Color(0.55f, 0.50f, 0.45f);
            GUI.enabled = canAfford;

            string btnText = canAfford 
                ? (unlockCost > 0 ? $"MỞ KHÓA (${unlockCost:N0})" : "MỞ KHÓA MIỄN PHÍ")
                : $"Thiếu ${(unlockCost - currentWallet):N0}";

            if (GUILayout.Button(btnText, UIThemeManager.ButtonRoundedStyle, GUILayout.Width(220), GUILayout.Height(40)))
            {
                if (unlockCost <= 0 || (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(unlockCost)))
                {
                    line.UnlockSlot(slot);
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            return;
        }

        // 3. NỘI DUNG THẺ: MÁY ĐÃ HOẠT ĐỘNG (THÔNG SỐ VẬN HÀNH)
        int tier = slot.currentTier;
        float curInterval = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.GetCurrentSpawnInterval() : 2.5f;
        int curCap = (line.TargetStoragePad != null) ? line.TargetStoragePad.MaxCapacity : 10;
        int workerCount = line.WorkerCount;
        int maxWorkers = line.MaxWorkers;

        // Băng thông số vận hành cao cấp, tương phản cao, bo góc hoàn hảo
        GUILayout.BeginHorizontal(UIThemeManager.StatsBarStyle, GUILayout.Height(34));
        GUILayout.Space(8);
        GUILayout.Label($"<color=#DDD0C0>Tốc độ ra hàng:</color> <color=#FFD166><b>{curInterval:F1}s / sản phẩm</b></color>", UIThemeManager.LabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("<color=#5A5042><b>│</b></color>", UIThemeManager.LabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"<color=#DDD0C0>Sức chứa kho:</color> <color=#06D6A0><b>{curCap} kiện</b></color>", UIThemeManager.LabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label("<color=#5A5042><b>│</b></color>", UIThemeManager.LabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"<color=#DDD0C0>Công nhân:</color> <color=#48CAE4><b>{workerCount}/{maxWorkers} NPC</b></color>", UIThemeManager.LabelStyle);
        GUILayout.Space(8);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // 4. HÀNG 4 NÚT HÀNH ĐỘNG NÂNG CẤP CHUYÊN BIỆT MÀU SẮC
        float btnWidth = (cardWidth - 48f) / 4f;
        GUILayout.BeginHorizontal();

        // NÚT 1: NÂNG CẤP TIER (Màu Vàng Đồng)
        int tierCost = slot.machineConfig.GetTierCost(tier + 1);
        bool canTier = tier < 3 && tierCost != -1;
        bool affTier = canTier && currentWallet >= tierCost;
        GUI.backgroundColor = canTier ? (affTier ? new Color(0.98f, 0.65f, 0.15f) : new Color(0.58f, 0.50f, 0.40f)) : new Color(0.40f, 0.38f, 0.36f);
        GUI.enabled = affTier;
        string tierTxt = canTier ? $"Lên Tier {tier + 1}\n(${tierCost:N0})" : "Tối Đa\n(Tier 3)";
        if (GUILayout.Button(tierTxt, UIThemeManager.ButtonRoundedStyle, GUILayout.Width(btnWidth), GUILayout.Height(46)))
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(tierCost))
            {
                slot.currentTier++;
                if (slot.spawnedMachineInstance != null) slot.spawnedMachineInstance.SetTier(slot.currentTier);
                GameEvents.OnMachineTierUpgraded?.Invoke(slot.machineConfig.resourceType, slot.currentTier);
            }
        }

        GUILayout.Space(4);

        // NÚT 2: TĂNG TỐC ĐỘ (Màu Xanh Biển)
        int speedCost = slot.machineConfig.GetSpeedCost(slot.currentSpeedLevel);
        bool canSpeed = speedCost != -1 && (slot.machineConfig.spawnIntervalLevels == null || slot.currentSpeedLevel < slot.machineConfig.spawnIntervalLevels.Length - 1);
        bool affSpeed = canSpeed && currentWallet >= speedCost;
        GUI.backgroundColor = canSpeed ? (affSpeed ? new Color(0.20f, 0.65f, 0.95f) : new Color(0.42f, 0.52f, 0.58f)) : new Color(0.40f, 0.38f, 0.36f);
        GUI.enabled = affSpeed;
        string spdTxt = canSpeed ? $"Tăng Tốc Cấp {slot.currentSpeedLevel + 2}\n(${speedCost:N0})" : "Tối Đa\n(Tốc Độ)";
        if (GUILayout.Button(spdTxt, UIThemeManager.ButtonRoundedStyle, GUILayout.Width(btnWidth), GUILayout.Height(46)))
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(speedCost))
            {
                slot.currentSpeedLevel++;
                if (slot.spawnedMachineInstance != null) slot.spawnedMachineInstance.SetSpeedLevel(slot.currentSpeedLevel);
            }
        }

        GUILayout.Space(4);

        // NÚT 3: MỞ RỘNG KHO (Màu Tím Mộng Mơ)
        int storageCost = slot.machineConfig.GetStorageCost(slot.currentStorageLevel);
        bool canStorage = storageCost != -1 && (slot.machineConfig.storageCapacityLevels == null || slot.currentStorageLevel < slot.machineConfig.storageCapacityLevels.Length - 1);
        bool affStorage = canStorage && currentWallet >= storageCost;
        GUI.backgroundColor = canStorage ? (affStorage ? new Color(0.78f, 0.40f, 0.88f) : new Color(0.52f, 0.42f, 0.55f)) : new Color(0.40f, 0.38f, 0.36f);
        GUI.enabled = affStorage;
        string storTxt = canStorage ? $"Kho Cấp {slot.currentStorageLevel + 2}\n(${storageCost:N0})" : "Tối Đa\n(Sức Chứa)";
        if (GUILayout.Button(storTxt, UIThemeManager.ButtonRoundedStyle, GUILayout.Width(btnWidth), GUILayout.Height(46)))
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(storageCost))
            {
                slot.currentStorageLevel++;
                if (line.TargetStoragePad != null && slot.machineConfig.storageCapacityLevels != null &&
                    slot.currentStorageLevel < slot.machineConfig.storageCapacityLevels.Length)
                {
                    line.TargetStoragePad.SetCapacity(slot.machineConfig.storageCapacityLevels[slot.currentStorageLevel]);
                }
            }
        }

        GUILayout.Space(4);

        // NÚT 4: THUÊ CÔNG NHÂN NPC (Màu Xanh Lá Mạ)
        int workerCost = slot.machineConfig.GetWorkerCost(workerCount);
        bool canWorker = workerCount < maxWorkers && workerCost != -1;
        bool affWorker = canWorker && currentWallet >= workerCost;
        GUI.backgroundColor = canWorker ? (affWorker ? new Color(0.28f, 0.78f, 0.35f) : new Color(0.42f, 0.55f, 0.45f)) : new Color(0.40f, 0.38f, 0.36f);
        GUI.enabled = affWorker;
        string wrkTxt = canWorker ? $"Thuê NPC ({workerCount + 1})\n(${workerCost:N0})" : $"Đủ NPC\n({maxWorkers}/{maxWorkers})";
        if (GUILayout.Button(wrkTxt, UIThemeManager.ButtonRoundedStyle, GUILayout.Width(btnWidth), GUILayout.Height(46)))
        {
            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(workerCost))
            {
                line.HireWorker();
            }
        }

        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private static string GetResourceTag(ResourceType type) => type switch
    {
        ResourceType.Wood => "[GỖ]",
        ResourceType.Stone => "[ĐÁ]",
        ResourceType.Copper => "[ĐỒNG]",
        ResourceType.Iron => "[SẮT]",
        ResourceType.Silver => "[BẠC]",
        ResourceType.Gold => "[VÀNG]",
        _ => "[MÁY]"
    };
}
