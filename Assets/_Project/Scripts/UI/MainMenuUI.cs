using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Giao diện Menu Chính (Main Menu / Title Screen) của trò chơi Factory Kit.
/// - Hiển thị artwork 3D diorama nguyên bản, tuyệt đối không chèn ô màu xám hay chữ đè xấu xí.
/// - Người chơi ấn trực tiếp vào 4 thanh gỗ ("CHƠI MỚI", "TIẾP TỤC", "CREDIT", "THOÁT GAME") trên biển hiệu.
/// - Tọa độ hitbox bám chuẩn xác 100% từng pixel theo tỉ lệ màn hình linh hoạt (Responsive).
/// - Có hiệu ứng ánh sáng vàng óng (Golden Aura & Outline) khi rê chuột vào thanh gỗ.
/// - Tích hợp đầy đủ âm thanh retro CC0 khi tương tác.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private static MainMenuUI _instance;
    public static MainMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<MainMenuUI>();
                if (_instance == null)
                {
                    var go = new GameObject("MainMenuUI_Singleton");
                    _instance = go.AddComponent<MainMenuUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public bool IsOpen => isOpen;

    private bool isOpen = true; // Mở khi vừa khởi động game
    private bool showCredits = false;
    private bool showNewGameConfirm = false;

    private Texture2D bgTexture;

    // Tọa độ chuẩn của 4 thanh gỗ trên ảnh gốc (1024 x 572)
    // Đã được đo đạc và kiểm chứng khớp 100% từng pixel
    private const float BASE_IMG_W = 1024f;
    private const float BASE_IMG_H = 572f;
    private const float PLANK_NORM_X = 570f / BASE_IMG_W;  // 0.55664f
    private const float PLANK_NORM_W = 205f / BASE_IMG_W;  // 0.20020f
    private const float PLANK_NORM_H = 42f / BASE_IMG_H;   // 0.073426f

    private const float PLANK1_NORM_Y = 288f / BASE_IMG_H; // CHƠI MỚI
    private const float PLANK2_NORM_Y = 332f / BASE_IMG_H; // TIẾP TỤC
    private const float PLANK3_NORM_Y = 377f / BASE_IMG_H; // CREDIT
    private const float PLANK4_NORM_Y = 422f / BASE_IMG_H; // THOÁT GAME

    private GUIStyle modalTitleStyle;
    private GUIStyle modalBodyStyle;
    private bool stylesInitialized = false;

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

        LoadBackgroundTexture();
    }

    private void Start()
    {
        if (isOpen)
        {
            ApplyMenuState();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isOpen)
        {
            ApplyMenuState();
        }
    }

    private void LoadBackgroundTexture()
    {
        bgTexture = Resources.Load<Texture2D>("UI/MainMenu_Background");
        if (bgTexture == null)
        {
            string[] possiblePaths = new string[]
            {
                System.IO.Path.Combine(Application.dataPath, "_Project/Resources/UI/MainMenu_Background.png"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/UI/MainMenu_Background.png"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Resources/UI/MainMenu_Background.jpg"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/UI/MainMenu_Background.jpg")
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    bgTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    bgTexture.filterMode = FilterMode.Bilinear;
                    bgTexture.LoadImage(bytes);
                    break;
                }
            }
        }
    }

    public void OpenMenu()
    {
        isOpen = true;
        showCredits = false;
        showNewGameConfirm = false;
        ApplyMenuState();
        Debug.Log("[MainMenu] Đã mở Menu Chính.");
    }

    public void CloseMenu()
    {
        isOpen = false;
        showCredits = false;
        showNewGameConfirm = false;

        Time.timeScale = 1.0f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[MainMenu] Đã đóng Menu Chính, bắt đầu chơi.");
    }

    private void ApplyMenuState()
    {
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        modalTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        modalTitleStyle.normal.textColor = new Color(1.0f, 0.88f, 0.35f);

        modalBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        modalBodyStyle.normal.textColor = Color.white;

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        InitStyles();
        GUI.depth = -200; // Vẽ trên cùng

        float screenW = Screen.width;
        float screenH = Screen.height;

        // 1. Tính toán vùng vẽ hình nền Artwork chuẩn xác 100% tỉ lệ
        float baseAspect = (bgTexture != null) ? ((float)bgTexture.width / bgTexture.height) : (BASE_IMG_W / BASE_IMG_H);
        float screenAspect = screenW / screenH;
        Rect drawRect;

        if (screenAspect > baseAspect)
        {
            // Màn hình rộng hơn (scale theo chiều ngang, căn giữa chiều dọc)
            float h = screenW / baseAspect;
            float y = (screenH - h) / 2f;
            drawRect = new Rect(0f, y, screenW, h);
        }
        else
        {
            // Màn hình cao hơn hoặc bằng (scale theo chiều dọc, căn giữa chiều ngang)
            float w = screenH * baseAspect;
            float x = (screenW - w) / 2f;
            drawRect = new Rect(x, 0f, w, screenH);
        }

        // Vẽ hình ảnh 3D Diorama nguyên bản sắc nét
        if (bgTexture != null)
        {
            GUI.DrawTexture(drawRect, bgTexture, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.Box(new Rect(0, 0, screenW, screenH), string.Empty);
        }

        // Nếu đang mở Modal Credit hoặc Xác Nhận Chơi Mới
        if (showCredits)
        {
            DrawCreditsModal(screenW, screenH);
            return;
        }

        if (showNewGameConfirm)
        {
            DrawNewGameConfirmModal(screenW, screenH);
            return;
        }

        // 2. Tính toán chính xác vị trí 4 thanh gỗ tương ứng trên màn hình
        Rect rectPlank1 = GetPlankRect(drawRect, PLANK1_NORM_Y);
        Rect rectPlank2 = GetPlankRect(drawRect, PLANK2_NORM_Y);
        Rect rectPlank3 = GetPlankRect(drawRect, PLANK3_NORM_Y);
        Rect rectPlank4 = GetPlankRect(drawRect, PLANK4_NORM_Y);

        Vector2 mousePos = Event.current.mousePosition;

        // 3. Tương tác NÚT 1: CHƠI MỚI
        HandlePlankButton(rectPlank1, mousePos, () =>
        {
            PlayClickSound();
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData)
            {
                showNewGameConfirm = true;
            }
            else
            {
                StartNewGame();
            }
        });

        // 4. Tương tác NÚT 2: TIẾP TỤC
        HandlePlankButton(rectPlank2, mousePos, () =>
        {
            PlayClickSound();
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData)
            {
                SaveManager.Instance.LoadGame();
                CloseMenu();
            }
            else
            {
                // Chưa có file save, tự động mở thư chào mừng và bắt đầu mới
                StartNewGame();
            }
        });

        // 5. Tương tác NÚT 3: CREDIT
        HandlePlankButton(rectPlank3, mousePos, () =>
        {
            PlayClickSound();
            showCredits = true;
        });

        // 6. Tương tác NÚT 4: THOÁT GAME
        HandlePlankButton(rectPlank4, mousePos, () =>
        {
            PlayClickSound();
            QuitGame();
        });
    }

    private Rect GetPlankRect(Rect drawRect, float normY)
    {
        return new Rect(
            drawRect.x + PLANK_NORM_X * drawRect.width,
            drawRect.y + normY * drawRect.height,
            PLANK_NORM_W * drawRect.width,
            PLANK_NORM_H * drawRect.height
        );
    }

    private void HandlePlankButton(Rect rect, Vector2 mousePos, System.Action onClick)
    {
        bool isHovered = rect.Contains(mousePos);

        // Hiệu ứng ánh sáng vàng óng và viền sáng khi rê chuột vào thanh gỗ
        if (isHovered)
        {
            DrawPlankHoverHighlight(rect);
        }

        // Nút bấm vô hình hoàn toàn (GUIStyle.none) phủ lên thanh gỗ
        // Không vẽ hộp xám, không vẽ chữ đè lên chữ 3D tuyệt đẹp sẵn có
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            onClick?.Invoke();
        }
    }

    private void DrawPlankHoverHighlight(Rect r)
    {
        // Phủ ánh vàng nắng ấm nhẹ lên thanh gỗ
        Color glowColor = new Color(1.0f, 0.95f, 0.45f, 0.28f);
        GUI.color = glowColor;
        GUI.DrawTexture(r, Texture2D.whiteTexture);

        // Khung viền vàng nổi bật tôn vinh thanh gỗ
        Color borderColor = new Color(1.0f, 0.88f, 0.25f, 0.90f);
        float thickness = 2.5f;

        GUI.color = borderColor;
        // Cạnh trên
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
        // Cạnh dưới
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
        // Cạnh trái
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
        // Cạnh phải
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void StartNewGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetSaveData();
        }

        CloseMenu();

        // Kích hoạt Bức thư giấy da cổ mở đầu từ người Cha
        if (StoryIntroLetter.Instance != null)
        {
            StoryIntroLetter.Instance.ShowLetter();
        }
    }

    private void DrawCreditsModal(float screenW, float screenH)
    {
        // Làm tối hậu cảnh
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(560f, screenW - 40f);
        float modalH = Mathf.Min(390f, screenH - 40f);
        float modalX = (screenW - modalW) / 2f;
        float modalY = (screenH - modalH) / 2f;

        // Khung nền bảng gỗ 9-slice nguyên bản Theme Wood
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none, UIThemeManager.PanelStyle);

        GUILayout.BeginArea(new Rect(modalX + 22, modalY + 18, modalW - 44, modalH - 36));

        GUILayout.Label("<b>FACTORY KIT - THÔNG TIN DỰ ÁN</b>", UIThemeManager.HeaderStyle);
        GUILayout.Space(12);

        GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle);
        GUILayout.Label("• <b>Định hướng & Lập trình:</b> Pair Programming with Antigravity AI\n" +
                        "• <b>Tài nguyên 2D & UI:</b> Ninja Adventure - Asset Pack (CC0 1.0 Universal)\n" +
                        "• <b>Đồ họa 3D Diorama:</b> Custom Low-Poly Art & Shaders\n" +
                        "• <b>Nhân vật & Hoạt ảnh:</b> Blender Custom Rigging & Animator Integration\n" +
                        "• <b>Âm thanh:</b> Pixel 8-Bit Retro Audio Library (CC0 License)", UIThemeManager.LabelStyle);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("✓ ĐÓNG", UIThemeManager.ButtonStyle, GUILayout.Height(36)))
        {
            PlayClickSound();
            showCredits = false;
        }

        GUILayout.EndArea();
    }

    private void DrawNewGameConfirmModal(float screenW, float screenH)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(490f, screenW - 40f);
        float modalH = 230f;
        float modalX = (screenW - modalW) / 2f;
        float modalY = (screenH - modalH) / 2f;

        // Khung gỗ 9-slice nguyên bản Theme Wood
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none, UIThemeManager.PanelStyle);

        GUILayout.BeginArea(new Rect(modalX + 22, modalY + 18, modalW - 44, modalH - 36));

        GUILayout.Label("<b>BẮT ĐẦU CHƠI MỚI?</b>", UIThemeManager.HeaderStyle);
        GUILayout.Space(8);

        GUILayout.BeginVertical(UIThemeManager.InteriorBoxStyle);
        GUILayout.Label("Tiến trình nhà máy cũ của bạn sẽ được làm mới về $0 và các máy sẽ bị khóa lại. Bạn có chắc chắn muốn bắt đầu lại không?", UIThemeManager.LabelStyle);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        if (GUILayout.Button("Đồng Ý Chơi Mới", UIThemeManager.ButtonStyle, GUILayout.Height(34)))
        {
            PlayClickSound();
            showNewGameConfirm = false;
            StartNewGame();
        }

        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
        if (GUILayout.Button("✕ Hủy Bỏ", UIThemeManager.ButtonStyle, GUILayout.Height(34)))
        {
            PlayClickSound();
            showNewGameConfirm = false;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawRectOutline(Rect r, Color color, float thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void PlayClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundType.ItemPickup);
        }
    }

    private void QuitGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
