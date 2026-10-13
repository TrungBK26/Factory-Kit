using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng Cài Đặt (Settings) & Hướng Dẫn Trò Chơi (Tutorial / How to Play).
/// - Hỗ trợ đầy đủ điều chỉnh âm lượng BGM/SFX, Lưu/Reset/Thoát game.
/// - Tích hợp cẩm nang hướng dẫn điều khiển và vòng lặp gameplay 5 bước kiếm tiền & chiến thắng.
/// - Hỗ trợ mở bằng phím ESC (Cài đặt), phím H (Hướng dẫn), hoặc nút bấm trên góc màn hình.
/// - Tự động tạm dừng trò chơi (Pause - Time.timeScale = 0) khi đang mở menu để trải nghiệm tối ưu.
/// </summary>
public class SettingsPopup : MonoBehaviour
{
    public enum MenuTab
    {
        Settings = 0,
        Tutorial = 1
    }

    [Header("--- THÀNH PHẦN CANVAS (NẾU CÓ) ---")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button resumeButton;

    private bool isOpen = false;
    private MenuTab currentTab = MenuTab.Settings;
    public MenuTab CurrentTab => currentTab;
    private string saveFeedbackText = string.Empty;
    private float saveFeedbackTimer = 0f;
    private float lastToggleTime = -10f;
    private Vector2 tutorialScrollPos = Vector2.zero;

    private static SettingsPopup _instance;
    public static SettingsPopup Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SettingsPopup>();
                if (_instance == null)
                {
                    var go = new GameObject("SettingsPopup_Singleton");
                    _instance = go.AddComponent<SettingsPopup>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public bool IsOpen => isOpen;

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

        if (panelRoot != null) panelRoot.SetActive(false);

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(CloseMenu);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnSettingsTogglePressed += ToggleSettings;
    }

    private void OnDisable()
    {
        GameEvents.OnSettingsTogglePressed -= ToggleSettings;
    }

    private void Update()
    {
        // Nếu InputManager vắng mặt (fallback), xử lý an toàn có cooldown chống toggle 2 lần
        if (InputManager.Instance == null)
        {
            bool hPressed = false;
            bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.hKey.wasPressedThisFrame) hPressed = true;
                if (kb.escapeKey.wasPressedThisFrame) escPressed = true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.H)) hPressed = true;
                if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
            }
            catch { }

            if (escPressed && Time.unscaledTime - lastToggleTime > 0.25f)
            {
                lastToggleTime = Time.unscaledTime;
                if (isOpen) CloseMenu();
                else OpenSettings();
            }
            else if (hPressed && Time.unscaledTime - lastToggleTime > 0.25f)
            {
                lastToggleTime = Time.unscaledTime;
                if (isOpen && currentTab == MenuTab.Tutorial) CloseMenu();
                else OpenTutorial();
            }
        }

        // Đếm ngược thông báo lưu game
        if (saveFeedbackTimer > 0f)
        {
            saveFeedbackTimer -= Time.unscaledDeltaTime;
            if (saveFeedbackTimer <= 0f) saveFeedbackText = string.Empty;
        }
    }

    public void ToggleSettings()
    {
        if (Time.unscaledTime - lastToggleTime < 0.2f) return;
        lastToggleTime = Time.unscaledTime;

        if (isOpen) CloseMenu();
        else OpenSettings();
    }

    public void OpenSettings()
    {
        currentTab = MenuTab.Settings;
        OpenMenuInternal();
    }

    public void OpenTutorial()
    {
        currentTab = MenuTab.Tutorial;
        OpenMenuInternal();
    }

    private void OpenMenuInternal()
    {
        isOpen = true;
        Time.timeScale = 0f; // Tạm dừng game để người chơi đọc hướng dẫn thoải mái

        if (panelRoot != null) panelRoot.SetActive(true);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isOpen = false;
        Time.timeScale = 1f; // Tiếp tục chạy game bình thường

        if (panelRoot != null) panelRoot.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
    }

    private void OnBGMChanged(float val)
    {
        PlayerPrefs.SetFloat("BGM_Volume", val);
        if (SoundManager.Instance != null) SoundManager.Instance.ApplyVolumes();
    }

    private void OnSFXChanged(float val)
    {
        PlayerPrefs.SetFloat("SFX_Volume", val);
        if (SoundManager.Instance != null) SoundManager.Instance.ApplyVolumes();
    }

    // ==================== ONGUI RENDERER ====================
    private void OnGUI()
    {
        // Không vẽ khi MainMenu hoặc Thư mở đầu đang hoạt động
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) return;
        if (StoryIntroLetter.Instance != null && StoryIntroLetter.Instance.IsOpen) return;

        UIThemeManager.Initialize();

        // 1. Luôn vẽ 2 nút tiện ích nhỏ ở góc trên phải màn hình khi chưa mở menu
        if (!isOpen)
        {
            float btnW = 125f;
            float btnH = 32f;
            float margin = 15f;

            // Nút Cài đặt
            if (GUI.Button(new Rect(Screen.width - (btnW * 2) - margin - 8, margin, btnW, btnH), "Cài Đặt (Esc)", UIThemeManager.ButtonSmallStyle))
            {
                OpenSettings();
            }

            // Nút Hướng dẫn
            if (GUI.Button(new Rect(Screen.width - btnW - margin, margin, btnW, btnH), "Hướng Dẫn (H)", UIThemeManager.ButtonSmallStyle))
            {
                OpenTutorial();
            }
            return;
        }

        // 2. KHI MỞ MENU: Vẽ lớp phủ tối toàn màn hình (Modal Overlay)
        GUI.depth = -200;
        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Kích thước hộp thoại chính giữa màn hình
        float popW = 560f;
        float popH = 540f;
        float x = (Screen.width - popW) / 2f;
        float y = (Screen.height - popH) / 2f;

        // Khung gỗ 9-slice nguyên bản Theme Wood
        GUI.Box(new Rect(x, y, popW, popH), GUIContent.none, UIThemeManager.PanelStyle);
        GUILayout.BeginArea(new Rect(x + 18, y + 16, popW - 36, popH - 32));

        // --- THANH TAB TRÊN CÙNG (DÙNG NÚT GỖ CAM CHUẨN ĐẸP GIỐNG ẢNH 5) ---
        GUILayout.BeginHorizontal();

        bool isSettings = currentTab == MenuTab.Settings;
        GUI.backgroundColor = isSettings ? Color.white : new Color(0.72f, 0.66f, 0.60f);
        string settingsTitle = isSettings ? "<b>Cài Đặt (Settings)</b>" : "Cài Đặt (Settings)";
        if (GUILayout.Button(settingsTitle, UIThemeManager.ButtonStyle, GUILayout.Height(36)))
        {
            currentTab = MenuTab.Settings;
        }

        bool isTuto = currentTab == MenuTab.Tutorial;
        GUI.backgroundColor = isTuto ? Color.white : new Color(0.72f, 0.66f, 0.60f);
        string tutoTitle = isTuto ? "<b>Hướng Dẫn (Tutorial)</b>" : "Hướng Dẫn (Tutorial)";
        if (GUILayout.Button(tutoTitle, UIThemeManager.ButtonStyle, GUILayout.Height(36)))
        {
            currentTab = MenuTab.Tutorial;
        }

        GUI.backgroundColor = new Color(0.95f, 0.40f, 0.40f);
        if (GUILayout.Button("✕", UIThemeManager.ButtonStyle, GUILayout.Width(44), GUILayout.Height(36)))
        {
            CloseMenu();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // --- NỘI DUNG TỪNG TAB ---
        if (currentTab == MenuTab.Settings)
        {
            DrawSettingsTab();
        }
        else
        {
            DrawTutorialTab();
        }

        GUILayout.EndArea();
    }

    private void DrawSettingsTab()
    {
        // Gói TOÀN BỘ nội dung cài đặt vào MỘT khung nền gỗ tối (InteriorBoxStyle) đồng nhất như phần Hướng dẫn
        GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle, GUILayout.MinHeight(350));

        // 1. Nhóm Điều chỉnh Âm thanh
        GUILayout.Label("<size=15><b>ĐIỀU CHỈNH ÂM THANH TRÒ CHƠI</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Space(6);

        // BGM Slider
        float bgm = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Nhạc nền (BGM): <b>{Mathf.RoundToInt(bgm * 100)}%</b>", UIThemeManager.LabelStyle, GUILayout.Width(170));
        float newBgm = GUILayout.HorizontalSlider(bgm, 0f, 1f, GUILayout.Height(20));
        if (GUILayout.Button(bgm > 0.05f ? "Tắt" : "Bật", UIThemeManager.ButtonSmallStyle, GUILayout.Width(65), GUILayout.Height(22)))
        {
            newBgm = bgm > 0.05f ? 0f : 0.8f;
        }
        GUILayout.EndHorizontal();
        if (Mathf.Abs(newBgm - bgm) > 0.01f) OnBGMChanged(newBgm);

        GUILayout.Space(6);

        // SFX Slider
        float sfx = PlayerPrefs.GetFloat("SFX_Volume", 1.0f);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Hiệu ứng (SFX): <b>{Mathf.RoundToInt(sfx * 100)}%</b>", UIThemeManager.LabelStyle, GUILayout.Width(170));
        float newSfx = GUILayout.HorizontalSlider(sfx, 0f, 1f, GUILayout.Height(20));
        if (GUILayout.Button(sfx > 0.05f ? "Tắt" : "Bật", UIThemeManager.ButtonSmallStyle, GUILayout.Width(65), GUILayout.Height(22)))
        {
            newSfx = sfx > 0.05f ? 0f : 1.0f;
        }
        GUILayout.EndHorizontal();
        if (Mathf.Abs(newSfx - sfx) > 0.01f) OnSFXChanged(newSfx);

        GUILayout.Space(12);

        // 2. Nhóm Lưu & Nạp Tiến Trình
        GUILayout.Label("<size=15><b>QUẢN LÝ TIẾN TRÌNH & DỮ LIỆU</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Lưu Tiến Trình Ngay", UIThemeManager.ButtonStyle, GUILayout.Height(32)))
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                saveFeedbackText = $"✓ Đã lưu thành công lúc {DateTime.Now:HH:mm:ss}!";
                saveFeedbackTimer = 3.0f;
            }
        }

        if (GUILayout.Button("Nạp Lại Bản Lưu", UIThemeManager.ButtonStyle, GUILayout.Height(32)))
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
                saveFeedbackText = $"✓ Đã nạp lại bản lưu gần nhất!";
                saveFeedbackTimer = 3.0f;
            }
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(saveFeedbackText))
        {
            GUILayout.Space(4);
            GUI.color = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"<b>{saveFeedbackText}</b>", UIThemeManager.LabelStyle);
            GUI.color = Color.white;
        }

        GUILayout.Space(12);

        // 3. Nhóm Điều Hướng & Hệ Thống
        GUILayout.Label("<size=15><b>MENU & HỆ THỐNG</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Space(6);

        if (GUILayout.Button("Về Menu Chính (Main Menu)", UIThemeManager.ButtonStyle, GUILayout.Height(34)))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            CloseMenu();
            if (MainMenuUI.Instance != null) MainMenuUI.Instance.OpenMenu();
        }

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
        if (GUILayout.Button("Xóa Save & Chơi Lại", UIThemeManager.ButtonStyle, GUILayout.Height(30)))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.ResetSaveData();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        GUI.backgroundColor = new Color(0.95f, 0.45f, 0.45f);
        if (GUILayout.Button("Thoát Game (Quit)", UIThemeManager.ButtonStyle, GUILayout.Height(30)))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Nút Tiếp tục chơi (To rõ ở góc dưới)
        GUI.backgroundColor = new Color(0.35f, 0.95f, 0.40f);
        if (GUILayout.Button("<size=15><b>TIẾP TỤC CHƠI (ESC)</b></size>", UIThemeManager.ButtonStyle, GUILayout.Height(40)))
        {
            CloseMenu();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawTutorialTab()
    {
        // Khung nội dung hướng dẫn
        GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle);
        tutorialScrollPos = GUILayout.BeginScrollView(tutorialScrollPos, GUILayout.Height(330));

        GUILayout.Label("<size=15><b>1. PHÍM ĐIỀU KHIỂN (CONTROLS)</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Label("• <b>[W, A, S, D]</b> hoặc <b>[Phím Mũi Tên]</b>: Di chuyển nhân vật.", UIThemeManager.LabelStyle);
        GUILayout.Label("• <b>[Phím F]</b> (hoặc giữ F): Tương tác nhặt hàng / Rút tiền từ Két / Bán hàng.", UIThemeManager.LabelStyle);
        GUILayout.Label("• <b>[Phím Tab]</b>: Mở Tablet Quản Lý Nhà Máy (nâng cấp máy & thuê NPC từ xa).", UIThemeManager.LabelStyle);
        GUILayout.Label("• <b>[Phím Esc]</b>: Mở / Đóng Menu Cài Đặt.", UIThemeManager.LabelStyle);
        GUILayout.Label("• <b>[Phím H]</b>: Mở nhanh trang Hướng Dẫn Chơi này.", UIThemeManager.LabelStyle);

        GUILayout.Space(12);
        GUILayout.Label("<size=15><b>2. VÒNG LẶP KIẾM TIỀN & PHÁT TRIỂN (5 BƯỚC)</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Label("<b>Bước 1 - Thu Hoạch:</b> Lại gần bệ chứa pallet của máy sản xuất (Gỗ, Đá...) bấm <b>[F]</b> để bốc hàng lên đỉnh đầu (tối đa 5 món/lượt).", UIThemeManager.LabelStyle);
        GUILayout.Label("<b>Bước 2 - Bán Hàng:</b> Chạy ra vùng <b>Khu Bán Hàng (SellZone)</b> màu xanh lá phía trước xưởng gỗ. Hàng sẽ tự động được dỡ xuống và bán ra tiền.", UIThemeManager.LabelStyle);
        GUILayout.Label("<b>Bước 3 - Rút Tiền Két:</b> Tiền thu được sẽ cất an toàn vào <b>Két Sắt (Vault)</b> bên hông xưởng. Hãy lại gần Két sắt bấm <b>[F]</b> để rút tiền về ví cá nhân!", UIThemeManager.LabelStyle);
        GUILayout.Label("<b>Bước 4 - Nâng Cấp & Mở Rộng:</b> Dùng tiền trong ví để mở thêm các mỏ mới (Đồng, Sắt, Bạc, Vàng) và nâng cấp máy lên <b>Tier 2, Tier 3</b> để tăng tốc độ sản xuất gấp nhiều lần.", UIThemeManager.LabelStyle);
        GUILayout.Label("<b>Bước 5 - Tự Động Hóa (Thuê NPC):</b> Thuê <b>Công Nhân NPC</b> tại từng dây chuyền để họ tự động khuân vác hàng ra bãi bán thay cho bạn!", UIThemeManager.LabelStyle);

        GUILayout.Space(12);
        GUILayout.Label("<size=15><b>3. MỤC TIÊU CHIẾN THẮNG TRÒ CHƠI</b></size>", UIThemeManager.SubHeaderStyle);
        GUILayout.Label("Để phá đảo trò chơi, bạn cần hoàn thành 2 điều kiện:", UIThemeManager.LabelStyle);
        GUILayout.Label("• Tích lũy đủ <b>$200,000</b> trong ví cá nhân.", UIThemeManager.LabelStyle);
        GUILayout.Label("• Nâng cấp toàn bộ <b>6 Dây Chuyền Sản Xuất lên Tier 3</b>!", UIThemeManager.LabelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Nút Đã hiểu (Hiển thị đầy đủ, thoải mái không bị cắt xén)
        GUI.backgroundColor = new Color(0.35f, 0.90f, 0.40f);
        if (GUILayout.Button("<size=14><b>✓ ĐÃ HIỂU, BẮT ĐẦU CHƠI NGAY!</b></size>", UIThemeManager.ButtonStyle, GUILayout.Height(38)))
        {
            CloseMenu();
        }
        GUI.backgroundColor = Color.white;
    }
}
