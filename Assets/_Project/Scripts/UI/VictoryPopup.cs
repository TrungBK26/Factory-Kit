using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Màn hình thông báo Chiến Thắng (Victory Popup).
/// Hiển thị khi đạt đủ 3 điều kiện: 6 dòng tài nguyên, mỗi dòng đạt Tier 3, ví $50,000.
/// Cung cấp 2 lựa chọn: Free Play (Tiếp tục chơi tự do) hoặc Restart (Chơi lại từ đầu).
/// </summary>
public class VictoryPopup : MonoBehaviour
{
    [Header("--- THÀNH PHẦN CANVAS ---")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button freePlayButton;
    [SerializeField] private Button restartButton;

    private bool isOpen = false;

    private static VictoryPopup _instance;
    public static VictoryPopup Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<VictoryPopup>();
                if (_instance == null)
                {
                    var go = new GameObject("VictoryPopup_Singleton");
                    _instance = go.AddComponent<VictoryPopup>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

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

        if (freePlayButton != null)
        {
            freePlayButton.onClick.AddListener(OnFreePlayClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameVictory += ShowVictoryPopup;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnGameVictory -= ShowVictoryPopup;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void ShowVictoryPopup()
    {
        isOpen = true;
        if (panelRoot != null) panelRoot.SetActive(true);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[VictoryPopup] Đã mở màn hình chúc mừng chiến thắng!");
    }

    private void OnFreePlayClicked()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }

        // Đánh dấu đã nhận chiến thắng để popup không lặp lại phiền toái
        if (WinConditionChecker.Instance != null)
        {
            WinConditionChecker.Instance.HasClaimedVictory = true;
        }

        GameEvents.OnSaveDataRequested?.Invoke();
        Debug.Log("[VictoryPopup] Người chơi chọn chế độ Free Play để tiếp tục phát triển nhà máy.");
    }

    private void OnRestartClicked()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Xóa toàn bộ dữ liệu Save và reset trạng thái game
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetSaveData();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==================== ONGUI FALLBACK ====================
    private void OnGUI()
    {
        if (!isOpen || panelRoot != null) return;
        UIThemeManager.Initialize();
        GUI.depth = -120; // Luôn hiển thị trên cùng

        float popW = 600f;
        float popH = 460f;
        float x = (Screen.width - popW) / 2f;
        float y = (Screen.height - popH) / 2f;

        // Overlay mờ
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Khung gỗ 9-slice nguyên bản Theme Wood
        GUI.Box(new Rect(x, y, popW, popH), GUIContent.none, UIThemeManager.PanelStyle);

        GUILayout.BeginArea(new Rect(x + 22, y + 20, popW - 44, popH - 40));
        GUILayout.Label("<size=22><b>🏆 CHIẾN THẮNG TUYỆT ĐỐI!</b></size>", UIThemeManager.HeaderStyle);
        GUILayout.Space(8);

        int target = WinConditionChecker.Instance != null ? WinConditionChecker.Instance.WinMoneyTarget : 200000;
        string monologue =
            "<b>💬 Lời tự thoại của bạn:</b>\n" +
            $"<i>\"Cha ơi... con đã làm được rồi! Khu công xưởng hoang tàn năm xưa giờ đã rực sáng với 6 dây chuyền sản xuất hiện đại nhất, ngân khố đã vượt ${target:N0}! Danh tiếng của gia đình ta đã được khôi phục trọn vẹn!\"</i>\n\n" +
            "• Mở khóa hoàn tất 6/6 dòng tài nguyên (Gỗ -> Vàng)\n" +
            "• Nâng cấp toàn bộ cỗ máy lên Tier 3 tối đa\n" +
            $"• Tích lũy vượt mốc ngân khố ${target:N0}";

        GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle);
        GUILayout.Label($"<size=14>{monologue}</size>", UIThemeManager.LabelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(12);

        GUI.backgroundColor = new Color(0.35f, 0.95f, 0.40f);
        if (GUILayout.Button("🚀 Tiếp Tục Chơi Tự Do (Free Play)", UIThemeManager.ButtonStyle, GUILayout.Height(38)))
        {
            OnFreePlayClicked();
        }

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.95f, 0.80f, 0.35f);
        if (GUILayout.Button("🏠 Về Menu Chính", UIThemeManager.ButtonStyle, GUILayout.Height(34)))
        {
            OnReturnToMainMenuClicked();
        }

        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("🔄 Chơi Lại Từ Đầu", UIThemeManager.ButtonStyle, GUILayout.Height(34)))
        {
            OnRestartClicked();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void OnReturnToMainMenuClicked()
    {
        isOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Time.timeScale = 1.0f;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        if (MainMenuUI.Instance != null)
        {
            MainMenuUI.Instance.OpenMenu();
        }
    }
}
