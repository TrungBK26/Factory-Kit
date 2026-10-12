using UnityEngine;

/// <summary>
/// Giao diện HUD OnGUI phục vụ kiểm tra trong quá trình phát triển (Dev Test HUD).
/// QUAN TRỌNG: Mặc định bảng công cụ Test Cheat BÊN PHẢI BỊ ẨN HOÀN TOÀN để sẵn sàng xuất Game (Build).
/// Chỉ người phát triển mới có thể mở bằng phím tắt bí mật: F12, ~, hoặc Ctrl + Shift + D.
/// Bảng thông tin tiến trình bên trái có thể bật/tắt tự do bằng phím F1.
/// </summary>
public class PrototypeTestHUD : MonoBehaviour
{
    [Header("--- CẤU HÌNH HIỂN THỊ (MẶC ĐỊNH ẨN KHI XUẤT GAME) ---")]
    [Tooltip("Bảng công cụ Test Cheat (MẶC ĐỊNH: FALSE / ẨN). Bật/tắt bằng F12, ~, hoặc Ctrl+Shift+D")]
    [SerializeField] private bool showCheatPanel = false;

    [Tooltip("Bảng thông tin tiến trình bên trái. Bật/tắt bằng phím F1")]
    [SerializeField] private bool showStatusPanel = true;

    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private PlayerInteraction playerInteraction;

    private float lastCheatToggleTime = -1f;
    private float lastStatusToggleTime = -1f;
    private const float TOGGLE_COOLDOWN = 0.25f;

    public void ToggleCheatPanel()
    {
        if (Time.unscaledTime - lastCheatToggleTime < TOGGLE_COOLDOWN) return;
        lastCheatToggleTime = Time.unscaledTime;
        showCheatPanel = !showCheatPanel;
        Debug.Log($"[DevHUD] Bảng Test Chức Năng (Secret Cheat Menu): {(showCheatPanel ? "HIỆN" : "ẨN")}");
    }

    public void ToggleStatusPanel()
    {
        if (Time.unscaledTime - lastStatusToggleTime < TOGGLE_COOLDOWN) return;
        lastStatusToggleTime = Time.unscaledTime;
        showStatusPanel = !showStatusPanel;
        Debug.Log($"[DevHUD] Bảng Trạng Thái Dây Chuyền (F1 Menu): {(showStatusPanel ? "HIỆN" : "ẨN")}");
    }

    private void Awake()
    {
        if (playerCarry == null) playerCarry = FindAnyObjectByType<PlayerCarry>();
        if (playerInteraction == null) playerInteraction = FindAnyObjectByType<PlayerInteraction>();
    }

    private void Update()
    {
        CheckInputs();
    }

    private void CheckInputs()
    {
        bool f12Pressed = false;
        bool f1Pressed = false;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            // Cheat panel: F12, Backquote (~), Ctrl+Shift+D, hoặc Ctrl+D
            if (kb.f12Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame || 
                (ctrl && shift && kb.dKey.wasPressedThisFrame) || (ctrl && kb.dKey.wasPressedThisFrame))
            {
                f12Pressed = true;
            }

            // Status panel: F1, hoặc Ctrl+Shift+H, hoặc Ctrl+H
            if (kb.f1Key.wasPressedThisFrame || 
                (ctrl && shift && kb.hKey.wasPressedThisFrame) || (ctrl && kb.hKey.wasPressedThisFrame))
            {
                f1Pressed = true;
            }
        }
#endif

        try
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.BackQuote) || 
                (ctrl && shift && Input.GetKeyDown(KeyCode.D)) || (ctrl && Input.GetKeyDown(KeyCode.D)))
            {
                f12Pressed = true;
            }

            if (Input.GetKeyDown(KeyCode.F1) || 
                (ctrl && shift && Input.GetKeyDown(KeyCode.H)) || (ctrl && Input.GetKeyDown(KeyCode.H)))
            {
                f1Pressed = true;
            }
        }
        catch { }

        if (f12Pressed)
        {
            ToggleCheatPanel();
        }

        if (f1Pressed)
        {
            ToggleStatusPanel();
        }
    }

    private string GetResourceIcon(ResourceType type) => type switch
    {
        ResourceType.Wood => "🌲",
        ResourceType.Stone => "⛏️",
        ResourceType.Copper => "🥉",
        ResourceType.Iron => "🔩",
        ResourceType.Silver => "🥈",
        ResourceType.Gold => "🥇",
        _ => "⚙️"
    };

    private void OnGUI()
    {
        // Bắt sự kiện phím qua OnGUI nhưng sử dụng hàm Toggle có Cooldown để chống hoàn toàn hiện tượng Toggle 2 lần trong 1 frame
        if (Event.current != null && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.F12 || Event.current.keyCode == KeyCode.BackQuote)
            {
                ToggleCheatPanel();
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.F1)
            {
                ToggleStatusPanel();
                Event.current.Use();
            }
        }

        UIThemeManager.Initialize();

        // ==================== BẢNG BÊN TRÁI: THÔNG TIN TIẾN TRÌNH ====================
        if (!showStatusPanel)
        {
            // Nút nhỏ gọn để mở lại bảng trạng thái khi đang ẩn
            if (GUI.Button(new Rect(15, 15, 95, 26), "F1: Hiện", UIThemeManager.ButtonSmallStyle))
            {
                ToggleStatusPanel();
            }
        }
        else
        {
            float statusWidth = 330f;
            float statusHeight = 335f;
            GUI.Box(new Rect(15, 15, statusWidth, statusHeight), GUIContent.none, UIThemeManager.InteriorBoxStyle);
            GUILayout.BeginArea(new Rect(25, 20, statusWidth - 20, statusHeight - 15));

            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=14><b>🏭 FACTORY REBORN</b></size>", UIThemeManager.SubHeaderStyle);
            if (GUILayout.Button("F1: Ẩn", UIThemeManager.ButtonSmallStyle, GUILayout.Width(65), GUILayout.Height(22)))
            {
                ToggleStatusPanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            int wallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;
            GUILayout.Label($"💰 Số dư ví: <b>${wallet:N0}</b>", UIThemeManager.LabelStyle);

            var vault = FindAnyObjectByType<MoneyVault>();
            int vaultMoney = vault != null ? vault.AccumulatedMoney : 0;
            GUILayout.Label($"🏦 Tiền trong Két tạm: <b>${vaultMoney:N0}</b>", UIThemeManager.LabelStyle);

            int carryCount = playerCarry != null ? playerCarry.CurrentCount : 0;
            int maxCapacity = playerCarry != null ? playerCarry.MaxCapacity : 5;
            GUILayout.Label($"📦 Hàng trên đầu: <b>{carryCount} / {maxCapacity}</b>", UIThemeManager.LabelStyle);

            GUILayout.Space(4);
            GUILayout.Label("<b>--- 6 DÂY CHUYỀN SẢN XUẤT ---</b>", UIThemeManager.SubHeaderStyle);

            if (FactoryManager.Instance != null && FactoryManager.Instance.ProductionLines.Count > 0)
            {
                foreach (var line in FactoryManager.Instance.ProductionLines)
                {
                    if (line == null) continue;
                    foreach (var slot in line.MachineSlots)
                    {
                        if (slot == null) continue;
                        string resIcon = slot.machineConfig != null ? GetResourceIcon(slot.machineConfig.resourceType) : "⚙️";
                        string name = slot.machineConfig != null ? slot.machineConfig.machineName : slot.slotName;

                        if (slot.isUnlocked)
                        {
                            int t = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.CurrentTier : slot.currentTier;
                            float spd = slot.spawnedMachineInstance != null ? slot.spawnedMachineInstance.GetCurrentSpawnInterval() : 2.5f;
                            GUILayout.Label($"{resIcon} {name}: <b>Tier {t}/3</b> | {spd:F1}s | {line.WorkerCount} NPC", UIThemeManager.LabelStyle);
                        }
                        else
                        {
                            int cost = slot.machineConfig != null ? slot.machineConfig.unlockCost : 0;
                            GUILayout.Label($"<color=#aa9988>{resIcon} {name}: 🔒 Chưa mở (${cost:N0})</color>", UIThemeManager.LabelStyle);
                        }
                    }
                }
            }

            GUILayout.Space(3);
            if (playerInteraction != null && playerInteraction.HasInteractableTarget)
            {
                GUI.color = Color.green;
                GUILayout.Label($"👉 <b>{playerInteraction.CurrentPrompt}</b>", UIThemeManager.LabelStyle);
                GUI.color = Color.white;
            }

            GUILayout.EndArea();
        }

        // ==================== BẢNG BÊN PHẢI: CÔNG CỤ TEST NHANH (MẶC ĐỊNH ẨN 100%) ====================
        if (showCheatPanel)
        {
            float panelWidth = 290f;
            float panelHeight = 490f;
            GUI.Box(new Rect(Screen.width - panelWidth - 15, 15, panelWidth, panelHeight), GUIContent.none, UIThemeManager.InteriorBoxStyle);
            GUILayout.BeginArea(new Rect(Screen.width - panelWidth - 5, 22, panelWidth - 20, panelHeight - 15));

            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=13><b>🛠️ DEV CHEAT MENU</b></size>", UIThemeManager.SubHeaderStyle);
            if (GUILayout.Button("✕ Ẩn (F12)", UIThemeManager.ButtonSmallStyle, GUILayout.Width(75), GUILayout.Height(22)))
            {
                ToggleCheatPanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // 1. Thêm tiền
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("💵 +$1,000", GUILayout.Height(26)))
            {
                if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(1000);
            }
            if (GUILayout.Button("💰 +$10,000", GUILayout.Height(26)))
            {
                if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(10000);
            }
            GUILayout.EndHorizontal();

            // 2. Mở khóa tất cả máy
            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("🔓 Mở Khóa Tất Cả 6 Máy", GUILayout.Height(26)))
            {
                if (FactoryManager.Instance != null)
                {
                    foreach (var line in FactoryManager.Instance.ProductionLines)
                    {
                        if (line == null) continue;
                        foreach (var slot in line.MachineSlots)
                        {
                            if (slot != null && !slot.isUnlocked) line.UnlockSlot(slot);
                        }
                    }
                }
            }

            // 3. Nâng tất cả máy lên Tier 3
            GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
            if (GUILayout.Button("⬆️ Nâng Tất Cả Máy Lên Tier 3", GUILayout.Height(26)))
            {
                if (FactoryManager.Instance != null)
                {
                    foreach (var line in FactoryManager.Instance.ProductionLines)
                    {
                        if (line == null) continue;
                        foreach (var slot in line.MachineSlots)
                        {
                            if (slot != null && slot.isUnlocked)
                            {
                                slot.currentTier = 3;
                                if (slot.spawnedMachineInstance != null) slot.spawnedMachineInstance.SetTier(3);
                            }
                        }
                    }
                }
                var checker = FindAnyObjectByType<WinConditionChecker>();
                int curWallet = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWallet : 0;
                if (checker != null) checker.CheckVictoryCondition(curWallet);
            }

            // 4. Thuê công nhân
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("👷 Thuê 1 NPC", GUILayout.Height(26)))
            {
                var line = FindAnyObjectByType<ProductionLine>();
                if (line != null) line.HireWorker();
            }
            if (GUILayout.Button("👥 Full NPC (Tất Cả)", GUILayout.Height(26)))
            {
                if (FactoryManager.Instance != null)
                {
                    foreach (var line in FactoryManager.Instance.ProductionLines)
                    {
                        if (line == null) continue;
                        while (line.WorkerCount < line.MaxWorkers)
                        {
                            if (!line.HireWorker()) break;
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            // 5. Mở Menu Quản Lý Tablet UI
            GUI.backgroundColor = new Color(0.85f, 0.7f, 1f);
            if (GUILayout.Button("📱 Bật Menu Quản Lý (Phím Tab)", GUILayout.Height(26)))
            {
                FactoryManagementUI.Instance.ToggleMenu();
            }

            // 6. Lưu và Tải Game
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUILayout.Button("💾 Lưu Game", GUILayout.Height(26)))
            {
                if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            }
            if (GUILayout.Button("📂 Nạp Lại", GUILayout.Height(26)))
            {
                if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();
            }
            GUILayout.EndHorizontal();

            // Bật/tắt Auto-Save 30s
            if (SaveManager.Instance != null)
            {
                bool autoOn = SaveManager.Instance.AutoSaveEnabled;
                GUI.backgroundColor = autoOn ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button(autoOn ? "⏱️ Auto-Save (30s): BẬT" : "⏱️ Auto-Save: TẮT (Lưu Tay)", GUILayout.Height(22)))
                {
                    SaveManager.Instance.AutoSaveEnabled = !autoOn;
                }
            }

            // 7. Xóa dữ liệu lưu
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("🔄 Xóa Save & Reset về $0", GUILayout.Height(26)))
            {
                if (SaveManager.Instance != null) SaveManager.Instance.ResetSaveData();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }

            // 8. Kích hoạt Chiến Thắng Game ($200k + 6 Máy Tier 3)
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            if (GUILayout.Button("🏆 Thử Chiến Thắng ($200K + 6 Tier 3)", GUILayout.Height(30)))
            {
                if (FactoryManager.Instance != null)
                {
                    foreach (var line in FactoryManager.Instance.ProductionLines)
                    {
                        if (line == null) continue;
                        foreach (var slot in line.MachineSlots)
                        {
                            if (slot != null)
                            {
                                if (!slot.isUnlocked) line.UnlockSlot(slot);
                                slot.currentTier = 3;
                                if (slot.spawnedMachineInstance != null) slot.spawnedMachineInstance.SetTier(3);
                            }
                        }
                    }
                }

                int targetMoney = 200000;
                var checker = FindAnyObjectByType<WinConditionChecker>();
                if (checker != null) targetMoney = checker.WinMoneyTarget;

                if (EconomyManager.Instance != null) EconomyManager.Instance.SetWalletBalance(targetMoney);
                if (checker != null)
                {
                    checker.HasClaimedVictory = false;
                    checker.CheckVictoryCondition(targetMoney);
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndArea();
        }
    }
}
