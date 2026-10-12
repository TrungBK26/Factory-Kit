using UnityEngine;

/// <summary>
/// Bức thư cốt truyện mở đầu từ người Cha để lại (Intro Story Letter).
/// Kích hoạt khi người chơi bắt đầu game mới (Chơi Mới).
/// Hiển thị trực tiếp trang giấy da cổ phong thư pháp chân thực do User chuẩn bị.
/// </summary>
public class StoryIntroLetter : MonoBehaviour
{
    private static StoryIntroLetter _instance;
    public static StoryIntroLetter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<StoryIntroLetter>();
                if (_instance == null)
                {
                    var go = new GameObject("StoryIntroLetter_Singleton");
                    _instance = go.AddComponent<StoryIntroLetter>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public bool IsOpen => isOpen;

    private bool isOpen = false;
    private Texture2D letterTex;
    private GUIStyle startBtnStyle;
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

        LoadLetterTexture();
    }

    private void LoadLetterTexture()
    {
        letterTex = Resources.Load<Texture2D>("UI/Intro_Letter");
        if (letterTex == null)
        {
            string[] possiblePaths = new string[]
            {
                System.IO.Path.Combine(Application.dataPath, "_Project/Resources/UI/Intro_Letter.png"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/UI/Intro_Letter.png"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Resources/UI/Intro_Letter.jpg"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/UI/Intro_Letter.jpg")
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    letterTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    letterTex.filterMode = FilterMode.Bilinear;
                    letterTex.LoadImage(bytes);
                    break;
                }
            }
        }
    }

    public void ShowLetter()
    {
        if (letterTex == null)
        {
            LoadLetterTexture();
        }

        isOpen = true;
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundType.ItemPickup);
        }

        Debug.Log("[Story] Đã mở Bức Thư Cốt Truyện Mở Đầu từ người Cha.");
    }

    public void CloseLetter()
    {
        isOpen = false;
        Time.timeScale = 1.0f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMovementEnabled(true);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundType.CoinCollect);
        }

        Debug.Log("[Story] Người chơi đã tiếp nhận công xưởng, bắt đầu hành trình!");
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        startBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        startBtnStyle.normal.textColor = Color.white;
        startBtnStyle.hover.textColor = new Color(1.0f, 0.95f, 0.4f);

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        InitStyles();
        GUI.depth = -300; // Hiển thị trên cùng tuyệt đối

        float screenW = Screen.width;
        float screenH = Screen.height;

        // 1. Phông nền tối mờ huyền ảo
        GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 2. Tính toán kích thước Bức Thư Giấy Cổ (Tỉ lệ W/H)
        float letterAspect = (letterTex != null) ? ((float)letterTex.width / letterTex.height) : (1792f / 2400f);
        float maxAvailableH = screenH * 0.86f;
        float letterH = Mathf.Min(maxAvailableH, 760f);
        float letterW = letterH * letterAspect;

        // Đảm bảo không tràn chiều ngang nếu màn hình hẹp
        if (letterW > screenW * 0.92f)
        {
            letterW = screenW * 0.92f;
            letterH = letterW / letterAspect;
        }

        float letterX = (screenW - letterW) / 2f;
        float letterY = (screenH - letterH) / 2f - 24f;
        if (letterY < 10f) letterY = 10f;

        Rect letterRect = new Rect(letterX, letterY, letterW, letterH);

        // Vẽ bức thư phong thư pháp cổ
        if (letterTex != null)
        {
            GUI.DrawTexture(letterRect, letterTex, ScaleMode.ScaleToFit);
        }

        // 3. Nút bấm Tiếp Nhận Công Xưởng đặt ngay dưới bức thư
        float btnW = Mathf.Min(380f, screenW - 40f);
        float btnH = 46f;
        float btnX = (screenW - btnW) / 2f;
        float btnY = letterY + letterH + 12f;

        // Nếu màn hình quá thấp, dời nút neo sát đáy
        if (btnY + btnH > screenH - 8f)
        {
            btnY = screenH - btnH - 8f;
        }

        Rect btnRect = new Rect(btnX, btnY, btnW, btnH);
        UIThemeManager.Initialize();
        bool isHovered = btnRect.Contains(Event.current.mousePosition);
        GUI.backgroundColor = isHovered ? new Color(0.40f, 0.95f, 0.40f) : new Color(0.32f, 0.85f, 0.35f);
        if (GUI.Button(btnRect, "<size=15><b>🏭 TIẾP NHẬN CÔNG XƯỞNG & BẮT ĐẦU</b></size>", UIThemeManager.ButtonStyle))
        {
            CloseLetter();
        }
        GUI.backgroundColor = Color.white;

        // 4. Phím tắt tiện lợi (Space / Enter / Escape) để tiếp tục
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Return ||
                Event.current.keyCode == KeyCode.KeypadEnter ||
                Event.current.keyCode == KeyCode.Space ||
                Event.current.keyCode == KeyCode.Escape)
            {
                CloseLetter();
            }
        }
    }
}
