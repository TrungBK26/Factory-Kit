using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trình quản lý tự động lưu và khôi phục tiến trình (Save / Load System).
/// Tích hợp cơ chế bảo toàn tài sản (Anti-Loss Asset Liquidation):
/// tự động quy đổi toàn bộ hàng tồn kho thành tiền mặt nạp vào két khi thoát/lưu game,
/// lưu trữ độc lập theo slotID chống lỗi lệch vị trí khi thêm máy mới.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "FactoryReborn_SaveData";
    private const float AUTO_SAVE_INTERVAL = 30f; // Tự động lưu mỗi 30 giây

    [Header("--- THIẾT LẬP AUTO-SAVE ---")]
    [SerializeField] private bool autoSaveEnabled = true;

    private float autoSaveTimer = 0f;
    private bool isLoading = false;

    public bool AutoSaveEnabled
    {
        get => autoSaveEnabled;
        set => autoSaveEnabled = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnSaveDataRequested += SaveGame;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnSaveDataRequested -= SaveGame;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.RefreshProductionLines();
        }
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FixNegativeBoxColliders();

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            LoadGame();
        }
    }

    private void Start()
    {
        FixNegativeBoxColliders();
        LoadGame();
    }

    private void FixNegativeBoxColliders()
    {
        foreach (var col in FindObjectsByType<BoxCollider>())
        {
            if (col != null)
            {
                var s = col.size;
                if (s.x < 0 || s.y < 0 || s.z < 0)
                {
                    col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
            }
        }
    }

    private void Update()
    {
        if (!autoSaveEnabled) return;

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            autoSaveTimer = 0f;
            SaveGame();
        }
    }

    /// <summary>
    /// Cơ chế chống thất thoát tài sản (Anti-Loss Asset Liquidation):
    /// Quét sạch mọi vật phẩm đang nằm trên đầu Player và trong các Bệ chứa,
    /// quy đổi 100% thành tiền mặt nạp thẳng vào két để tránh lỗi kẹt vật thể 3D khi nạp lại Scene.
    /// </summary>
    public void LiquidateAllPendingItemsToVault()
    {
        int totalValue = 0;

        // 1. Quét đồ còn trên đầu Player
        var player = FindAnyObjectByType<PlayerCarry>();
        if (player != null)
        {
            while (player.CurrentCount > 0)
            {
                var item = player.PopTopItem();
                if (item != null)
                {
                    int val = 2;
                    if (item.TryGetComponent<ResourceItemHolder>(out var holder) && holder.Data != null)
                    {
                        val = holder.Data.baseValue;
                    }
                    totalValue += val;

                    if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(item);
                    else Destroy(item);
                }
            }
        }

        // 2. Quét đồ còn trên các Bệ chứa (StoragePad)
        var pads = FindObjectsByType<StoragePad>();
        foreach (var pad in pads)
        {
            if (pad == null) continue;

            while (pad.CurrentCount > 0)
            {
                var item = pad.PopItem();
                if (item != null)
                {
                    int val = 2;
                    if (item.TryGetComponent<ResourceItemHolder>(out var holder) && holder.Data != null)
                    {
                        val = holder.Data.baseValue;
                    }
                    totalValue += val;

                    if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(item);
                    else Destroy(item);
                }
            }
        }

        // 3. Quét đồ còn trên đầu các WorkerNPC
        var workers = FindObjectsByType<WorkerNPC>();
        foreach (var worker in workers)
        {
            if (worker == null) continue;

            while (worker.CurrentCount > 0)
            {
                var item = worker.PopTopItem();
                if (item != null)
                {
                    int val = 2;
                    if (item.TryGetComponent<ResourceItemHolder>(out var holder) && holder.Data != null)
                    {
                        val = holder.Data.baseValue;
                    }
                    totalValue += val;

                    if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(item);
                    else Destroy(item);
                }
            }
        }

        // 4. Quy đổi trực tiếp nạp vào két tiền
        if (totalValue > 0)
        {
            var vault = FindAnyObjectByType<MoneyVault>();
            if (vault != null)
            {
                vault.AddAccumulatedMoney(totalValue);
            }
            Debug.Log($"[SaveManager] Đã bảo toàn và quy đổi ${totalValue} hàng tồn kho vào két tiền an toàn.");
        }
    }

    private void OnApplicationQuit()
    {
        // Chỉ thanh lý hàng tồn thành tiền két khi THỰC SỰ THOÁT GAME để chống kẹt vật thể 3D khi nạp lại
        LiquidateAllPendingItemsToVault();
        SaveGame();
    }

    public void SaveGame()
    {
        if (isLoading) return; // Không lưu đè khi đang trong tiến trình LoadGame

        GameSaveData data = new GameSaveData();

        if (EconomyManager.Instance != null)
        {
            data.playerWallet = EconomyManager.Instance.CurrentWallet;
        }

        var vault = FindAnyObjectByType<MoneyVault>();
        if (vault != null)
        {
            data.vaultMoney = vault.AccumulatedMoney;
        }

        var checker = FindAnyObjectByType<WinConditionChecker>();
        if (checker != null)
        {
            data.hasClaimedVictory = checker.HasClaimedVictory;
        }

        // Thu thập tất cả dây chuyền
        List<ProductionLine> lines = new List<ProductionLine>();
        if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines != null)
        {
            lines.AddRange(FactoryManager.Instance.ProductionLines);
        }
        foreach (var pl in FindObjectsByType<ProductionLine>())
        {
            if (!lines.Contains(pl)) lines.Add(pl);
        }

        int activeMachines = 0;

        foreach (var line in lines)
        {
            if (line == null) continue;

            if (line.MachineSlots.Count > 0)
            {
                foreach (var slot in line.MachineSlots)
                {
                    if (slot == null) continue;

                    SlotSaveState state = new SlotSaveState
                    {
                        slotID = slot.slotID,
                        isUnlocked = slot.isUnlocked,
                        tierLevel = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.CurrentTier : slot.currentTier,
                        speedLevel = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.CurrentSpeedLevel : slot.currentSpeedLevel,
                        storageLevel = slot.currentStorageLevel,
                        hiredWorkers = line.WorkerCount
                    };
                    data.slots.Add(state);
                    if (slot.isUnlocked) activeMachines++;
                }
            }
            else
            {
                var childM = line.GetComponentInChildren<MachineController>();
                SlotSaveState state = new SlotSaveState
                {
                    slotID = $"{line.ClusterID}_Slot01",
                    isUnlocked = true,
                    tierLevel = childM != null ? childM.CurrentTier : 1,
                    speedLevel = childM != null ? childM.CurrentSpeedLevel : 0,
                    storageLevel = line.TargetStoragePad != null ? line.TargetStoragePad.MaxCapacity : 10,
                    hiredWorkers = line.WorkerCount
                };
                data.slots.Add(state);
                activeMachines++;
            }
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] Đã LƯU TIẾN TRÌNH THÀNH CÔNG: Ví ${data.playerWallet}, Két ${data.vaultMoney}, Máy hoạt động: {activeMachines}/6.");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("[SaveManager] Chưa có file Save trong bộ nhớ.");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) return;

        isLoading = true;
        try
        {
            // 1. Khôi phục ví tiền
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetWalletBalance(data.playerWallet);
            }

            // 2. Khôi phục tiền két (thiết lập chính xác thay vì cộng dồn)
            var vault = FindAnyObjectByType<MoneyVault>();
            if (vault != null)
            {
                vault.SetAccumulatedMoney(data.vaultMoney);
            }

            // 3. Khôi phục cờ chiến thắng
            var checker = FindAnyObjectByType<WinConditionChecker>();
            if (checker != null)
            {
                checker.HasClaimedVictory = data.hasClaimedVictory;
            }

            // 4. Thu thập tất cả dây chuyền
            List<ProductionLine> lines = new List<ProductionLine>();
            if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines != null)
            {
                lines.AddRange(FactoryManager.Instance.ProductionLines);
            }
            foreach (var pl in FindObjectsByType<ProductionLine>())
            {
                if (!lines.Contains(pl)) lines.Add(pl);
            }

            var slotMap = new Dictionary<string, SlotSaveState>();
            if (data.slots != null)
            {
                foreach (var s in data.slots)
                {
                    if (!string.IsNullOrEmpty(s.slotID)) slotMap[s.slotID] = s;
                }
            }

            int activeMachines = 0;

            // 5. Khôi phục từng ProductionLine và từng Slot
            foreach (var line in lines)
            {
                if (line == null) continue;

                int targetWorkers = 0;

                foreach (var slot in line.MachineSlots)
                {
                    if (slot == null) continue;

                    bool shouldBeUnlocked = false;
                    int tier = 1;
                    int spd = 0;
                    int stor = 0;

                    if (slotMap.TryGetValue(slot.slotID, out var state))
                    {
                        shouldBeUnlocked = state.isUnlocked;
                        tier = state.tierLevel;
                        spd = state.speedLevel;
                        stor = state.storageLevel;
                        targetWorkers = Mathf.Max(targetWorkers, state.hiredWorkers);
                    }
                    else
                    {
                        // Nếu slot không có trong file save -> dùng giá trị mặc định của slot
                        shouldBeUnlocked = slot.isUnlockedByDefault;
                        tier = 1;
                        spd = 0;
                        stor = 0;
                    }

                    if (shouldBeUnlocked)
                    {
                        if (!slot.isUnlocked)
                        {
                            line.UnlockSlot(slot);
                        }

                        slot.currentTier = tier;
                        slot.currentSpeedLevel = spd;
                        slot.currentStorageLevel = stor;

                        if (slot.spawnedMachineInstance != null)
                        {
                            slot.spawnedMachineInstance.gameObject.SetActive(true);
                            slot.spawnedMachineInstance.SetTier(tier);
                            slot.spawnedMachineInstance.SetSpeedLevel(spd);
                        }

                        if (line.TargetStoragePad != null && slot.machineConfig != null && slot.machineConfig.storageCapacityLevels != null)
                        {
                            int capIndex = Mathf.Clamp(slot.currentStorageLevel, 0, slot.machineConfig.storageCapacityLevels.Length - 1);
                            line.TargetStoragePad.SetCapacity(slot.machineConfig.storageCapacityLevels[capIndex]);
                        }

                        activeMachines++;
                    }
                    else
                    {
                        // Máy bị khóa theo file save -> Khóa lại và ẩn đi!
                        line.LockSlot(slot);

                        // Dọn sạch hàng trên bệ chứa của máy bị khóa nếu có
                        if (line.TargetStoragePad != null)
                        {
                            while (line.TargetStoragePad.CurrentCount > 0)
                            {
                                var item = line.TargetStoragePad.PopItem();
                                if (item != null)
                                {
                                    if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(item);
                                    else Destroy(item);
                                }
                            }
                        }
                    }
                }

                // Fallback nếu slotMap có dữ liệu theo ClusterID
                foreach (var kvp in slotMap)
                {
                    if (kvp.Key.StartsWith(line.ClusterID))
                    {
                        targetWorkers = Mathf.Max(targetWorkers, kvp.Value.hiredWorkers);
                    }
                }

                // Đồng bộ chính xác số công nhân
                line.DismissAllWorkers();
                while (line.WorkerCount < targetWorkers && line.WorkerCount < line.MaxWorkers)
                {
                    line.HireWorker();
                }
            }

            // 6. Cập nhật lại toàn bộ các Bệ Mở Khóa In-World (MachineUnlockPad)
            foreach (var pad in Resources.FindObjectsOfTypeAll<MachineUnlockPad>())
            {
                if (pad != null && pad.gameObject.scene.isLoaded)
                {
                    pad.gameObject.SetActive(true);
                    pad.RefreshState();
                }
            }

            // 7. Dọn sạch đồ trên đầu người chơi để tránh đồ thừa
            var playerCarry = FindAnyObjectByType<PlayerCarry>();
            if (playerCarry != null)
            {
                while (playerCarry.CurrentCount > 0)
                {
                    var item = playerCarry.PopTopItem();
                    if (item != null)
                    {
                        if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(item);
                        else Destroy(item);
                    }
                }
            }

            Debug.Log($"[SaveManager] Đã NẠP LẠI TIẾN TRÌNH THÀNH CÔNG: Ví ${data.playerWallet}, Két ${data.vaultMoney}, Máy hoạt động: {activeMachines}/6.");
        }
        finally
        {
            isLoading = false;
        }

        GameEvents.OnDataLoaded?.Invoke();
    }

    public void ResetSaveData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();

        // 1. Đặt lại ví cá nhân và két về 0
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetWalletBalance(0);
        }

        var vault = FindAnyObjectByType<MoneyVault>();
        if (vault != null)
        {
            vault.SetAccumulatedMoney(0);
        }

        var checker = FindAnyObjectByType<WinConditionChecker>();
        if (checker != null)
        {
            checker.HasClaimedVictory = false;
        }

        // 2. Dọn sạch toàn bộ vật phẩm trên tất cả các Băng Chuyền (ConveyorBelt)
        foreach (var cb in FindObjectsByType<ConveyorBelt>())
        {
            if (cb != null) cb.ClearBelt();
        }

        // 3. Dọn sạch vật phẩm trên đầu người chơi
        var playerCarry = FindAnyObjectByType<PlayerCarry>();
        if (playerCarry != null)
        {
            while (playerCarry.CurrentCount > 0)
            {
                var it = playerCarry.PopTopItem();
                if (it != null)
                {
                    if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(it);
                    else Destroy(it);
                }
            }
        }

        // 4. Dọn sạch các bệ chứa
        foreach (var pad in FindObjectsByType<StoragePad>())
        {
            if (pad != null)
            {
                while (pad.CurrentCount > 0)
                {
                    var it = pad.PopItem();
                    if (it != null)
                    {
                        if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(it);
                        else Destroy(it);
                    }
                }
            }
        }

        // 5. Quét và dọn sạch TOÀN BỘ vật phẩm vương vãi trên sàn (ResourceItemHolder & PooledResourceItem)
        foreach (var holder in FindObjectsByType<ResourceItemHolder>())
        {
            if (holder != null && holder.gameObject != null)
            {
                if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(holder.gameObject);
                else Destroy(holder.gameObject);
            }
        }
        foreach (var pooled in FindObjectsByType<PooledResourceItem>())
        {
            if (pooled != null && pooled.gameObject != null && pooled.gameObject.activeInHierarchy && pooled.transform.parent != ResourcePoolManager.Instance?.transform)
            {
                if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(pooled.gameObject);
                else Destroy(pooled.gameObject);
            }
        }

        // 6. Giải tán và tiêu hủy toàn bộ WorkerNPC
        foreach (var line in FindObjectsByType<ProductionLine>())
        {
            if (line != null)
            {
                line.DismissAllWorkers();
            }
        }
        foreach (var w in FindObjectsByType<WorkerNPC>())
        {
            if (w != null) Destroy(w.gameObject);
        }

        // 7. Khóa lại toàn bộ các máy cấp cao, chỉ giữ lại máy mặc định (Gỗ)
        foreach (var line in FindObjectsByType<ProductionLine>())
        {
            if (line != null)
            {
                foreach (var slot in line.MachineSlots)
                {
                    if (slot != null)
                    {
                        slot.isUnlocked = slot.isUnlockedByDefault;
                        slot.currentTier = 1;
                        slot.currentSpeedLevel = 0;
                        slot.currentStorageLevel = 0;

                        if (slot.spawnedMachineInstance != null)
                        {
                            slot.spawnedMachineInstance.SetTier(1);
                            slot.spawnedMachineInstance.SetSpeedLevel(0);
                            slot.spawnedMachineInstance.gameObject.SetActive(slot.isUnlockedByDefault);
                        }
                        else if (slot.spawnPoint != null)
                        {
                            slot.spawnPoint.gameObject.SetActive(slot.isUnlockedByDefault);
                        }
                    }
                }
            }
        }

        // 8. Kích hoạt lại toàn bộ các Bệ Mở Khóa In-World (MachineUnlockPad)
        foreach (var pad in Resources.FindObjectsOfTypeAll<MachineUnlockPad>())
        {
            if (pad != null && pad.gameObject.scene.isLoaded)
            {
                pad.gameObject.SetActive(true);
            }
        }

        // 9. Đưa nhân vật chính về vị trí xuất phát ban đầu (0, 0.8, -2)
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            if (playerObj.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;
            playerObj.transform.position = new Vector3(0f, 0.8f, -2f);
            playerObj.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = true;
        }

        // 10. Khôi phục quyền điều khiển và thời gian chơi
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.RefreshProductionLines();
        }

        Debug.Log("[SaveManager] Đã reset toàn bộ: Tiền về $0, băng chuyền & bệ chứa sạch bóng, máy cấp cao bị khóa lại, bệ mở khóa tái hiện, Player đã được hồi phục quyền di chuyển.");
    }
}
