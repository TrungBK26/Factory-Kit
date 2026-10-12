using UnityEngine;

/// <summary>
/// Hệ thống Quản Lý Giao Diện Gỗ Retro (Theme Wood UI Manager).
/// Trích xuất và áp dụng bộ UI Theme Wood từ Ninja Adventure Asset Pack theo phong cách pixel ấm áp.
/// Cung cấp 9-slice panel, button cam có viền đổ bóng, tab, progress bar, và hộp thoại gỗ cho toàn game.
/// </summary>
public static class UIThemeManager
{
    private static bool initialized = false;

    // Textures
    public static Texture2D PanelTex { get; private set; }
    public static Texture2D PanelInteriorTex { get; private set; }
    public static Texture2D PanelAltTex { get; private set; }
    public static Texture2D MachineCardTex { get; private set; }
    public static Texture2D StatsPillTex { get; private set; }
    public static Texture2D ButtonRoundedNormalTex { get; private set; }
    public static Texture2D TabPillActiveTex { get; private set; }
    public static Texture2D TabPillInactiveTex { get; private set; }
    public static Texture2D ButtonNormalTex { get; private set; }
    public static Texture2D ButtonHoverTex { get; private set; }
    public static Texture2D ButtonPressedTex { get; private set; }
    public static Texture2D ButtonDisabledTex { get; private set; }
    public static Texture2D TabNormalTex { get; private set; }
    public static Texture2D TabSelectedTex { get; private set; }
    public static Texture2D ProgressFillTex { get; private set; }
    public static Texture2D ProgressTrackTex { get; private set; }
    public static Texture2D CellTex { get; private set; }
    public static Font RetroFont { get; private set; }

    // GUIStyles
    public static GUIStyle PanelStyle { get; private set; }
    public static GUIStyle InteriorBoxStyle { get; private set; }
    public static GUIStyle CardStyle { get; private set; }
    public static GUIStyle MachineCardStyle { get; private set; }
    public static GUIStyle StatsBarStyle { get; private set; }
    public static GUIStyle MoneyBadgeStyle { get; private set; }
    public static GUIStyle ButtonStyle { get; private set; }
    public static GUIStyle ButtonSmallStyle { get; private set; }
    public static GUIStyle ButtonRoundedStyle { get; private set; }
    public static GUIStyle TabActiveStyle { get; private set; }
    public static GUIStyle TabInactiveStyle { get; private set; }
    public static GUIStyle MiniBadgeStyle { get; private set; }
    public static GUIStyle HeaderStyle { get; private set; }
    public static GUIStyle SubHeaderStyle { get; private set; }
    public static GUIStyle LabelStyle { get; private set; }
    public static GUIStyle ValueStyle { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetTheme()
    {
        initialized = false;
        PanelTex = null;
        PanelInteriorTex = null;
        PanelAltTex = null;
        MachineCardTex = null;
        StatsPillTex = null;
        ButtonRoundedNormalTex = null;
        TabPillActiveTex = null;
        TabPillInactiveTex = null;
        ButtonNormalTex = null;
        ButtonHoverTex = null;
        ButtonPressedTex = null;
    }

    public static void Initialize()
    {
        if (initialized && PanelTex != null) return;

        LoadTextures();
        InitStyles();
        initialized = true;
    }

    private static void LoadTextures()
    {
        // Ưu tiên bản 3x để có viền pixel chunky chuẩn retro
        PanelTex = LoadTex("nine_path_panel_3x", "nine_path_panel");
        PanelInteriorTex = LoadTex("nine_path_bg_3x", "nine_path_bg");
        PanelAltTex = LoadTex("nine_path_panel_2_3x", "nine_path_panel_2");

        MachineCardTex = LoadTex("machine_card_bg_3x", "machine_card_bg_3x");
        StatsPillTex = LoadTex("stats_pill_3x", "stats_pill");
        ButtonRoundedNormalTex = LoadTex("button_rounded_3x", "button_rounded");
        TabPillActiveTex = LoadTex("tab_pill_active_3x", "tab_pill_active_3x");
        TabPillInactiveTex = LoadTex("tab_pill_inactive_3x", "tab_pill_inactive_3x");

        ButtonNormalTex = LoadTex("button_normal_3x", "button_normal");
        ButtonHoverTex = LoadTex("button_hover_3x", "button_hover");
        ButtonPressedTex = LoadTex("button_pressed_3x", "button_pressed");
        ButtonDisabledTex = LoadTex("button_disabled_3x", "button_disabled");

        TabNormalTex = LoadTex("tab_3x", "tab");
        TabSelectedTex = LoadTex("tab_selected_3x", "tab_selected");

        ProgressFillTex = LoadTex("slider_progress_3x", "slider_progress");
        ProgressTrackTex = PanelInteriorTex;
        CellTex = LoadTex("inventory_cell_3x", "inventory_cell");

        RetroFont = Resources.Load<Font>("UI/Fonts/NormalFont");

        // Fallback procedural an toàn tuyệt đối nếu asset chưa nạp
        Color darkBorder = new Color(0.075f, 0.106f, 0.106f);
        if (PanelInteriorTex == null) PanelInteriorTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, new Color(0.275f, 0.251f, 0.180f), new Color(0.32f, 0.29f, 0.22f), new Color(0.22f, 0.20f, 0.14f));
        if (MachineCardTex == null) MachineCardTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, new Color(0.220f, 0.196f, 0.149f), new Color(0.27f, 0.24f, 0.18f), new Color(0.18f, 0.16f, 0.12f));
        if (StatsPillTex == null) StatsPillTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, new Color(0.149f, 0.129f, 0.102f), new Color(0.20f, 0.17f, 0.14f), new Color(0.11f, 0.09f, 0.07f));
        if (ButtonRoundedNormalTex == null) ButtonRoundedNormalTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, Color.white, Color.white, new Color(0.85f, 0.85f, 0.85f));
        if (TabPillActiveTex == null) TabPillActiveTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, new Color(0.91f, 0.61f, 0.22f), new Color(1f, 0.68f, 0.29f), new Color(0.78f, 0.50f, 0.15f));
        if (TabPillInactiveTex == null) TabPillInactiveTex = CreatePixelRoundedBox(48, 48, 3, darkBorder, new Color(0.23f, 0.20f, 0.16f), new Color(0.29f, 0.26f, 0.20f), new Color(0.18f, 0.16f, 0.12f));
    }

    private static Texture2D LoadTex(string name3x, string fallbackName)
    {
        Texture2D tex = Resources.Load<Texture2D>("UI/Theme_Wood/" + name3x);
        if (tex == null)
        {
            tex = Resources.Load<Texture2D>("UI/Theme_Wood/" + fallbackName);
        }

        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
        }
        return tex;
    }

    private static void InitStyles()
    {
        // 1. Khung cửa sổ chính 9-slice gỗ (Panel)
        PanelStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(12, 12, 12, 12),
            padding = new RectOffset(16, 16, 16, 16)
        };
        PanelStyle.normal.background = PanelTex;

        // 2. Vùng lõi tối màu (Interior Container) - Bo góc 4 cạnh khép kín chuẩn retro
        InteriorBoxStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(10, 10, 10, 10)
        };
        InteriorBoxStyle.normal.background = PanelInteriorTex;

        // 3. Khung thẻ máy riêng biệt (Machine Card) - Bo góc 4 cạnh sắc nét
        MachineCardStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(14, 14, 10, 10)
        };
        MachineCardStyle.normal.background = MachineCardTex != null ? MachineCardTex : PanelInteriorTex;

        CardStyle = new GUIStyle(MachineCardStyle);

        // 4. Băng thông số vận hành (Stats Bar) - Pill tối màu tương phản cao
        StatsBarStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(12, 12, 5, 5),
            alignment = TextAnchor.MiddleCenter
        };
        StatsBarStyle.normal.background = StatsPillTex != null ? StatsPillTex : PanelInteriorTex;

        // 5. Huy hiệu số dư ví tiền (Money Badge) - Bo góc 4 cạnh hoàn hảo
        MoneyBadgeStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(14, 14, 5, 5),
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        MoneyBadgeStyle.normal.background = StatsPillTex != null ? StatsPillTex : PanelInteriorTex;
        MoneyBadgeStyle.normal.textColor = new Color(1.0f, 0.85f, 0.0f);

        // 6. Nút bấm bo tròn 4 góc (Button Rounded) - Hỗ trợ tint màu tươi sáng không bị đục
        ButtonRoundedStyle = new GUIStyle(GUI.skin.button)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(10, 10, 6, 6),
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        ButtonRoundedStyle.normal.background = ButtonRoundedNormalTex;
        ButtonRoundedStyle.normal.textColor = Color.white;
        ButtonRoundedStyle.hover.background = ButtonRoundedNormalTex;
        ButtonRoundedStyle.hover.textColor = new Color(1.0f, 1.0f, 0.80f);
        ButtonRoundedStyle.active.background = ButtonRoundedNormalTex;
        ButtonRoundedStyle.active.textColor = new Color(0.90f, 0.90f, 0.90f);

        // Nút bấm phong cách gỗ cam (Button)
        ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(12, 12, 7, 7),
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        ButtonStyle.normal.background = ButtonNormalTex;
        ButtonStyle.normal.textColor = new Color(0.98f, 0.95f, 0.88f);

        ButtonStyle.hover.background = ButtonHoverTex;
        ButtonStyle.hover.textColor = new Color(1.0f, 1.0f, 0.70f);

        ButtonStyle.active.background = ButtonPressedTex;
        ButtonStyle.active.textColor = new Color(0.85f, 0.80f, 0.65f);

        // Nút bấm nhỏ (Small Button)
        ButtonSmallStyle = new GUIStyle(ButtonStyle)
        {
            fontSize = 12,
            padding = new RectOffset(6, 6, 4, 4)
        };

        // 7. Tab đang chọn (Pill bo tròn)
        TabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(12, 12, 5, 5),
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        TabActiveStyle.normal.background = TabPillActiveTex != null ? TabPillActiveTex : ButtonNormalTex;
        TabActiveStyle.normal.textColor = Color.white;

        // Tab chưa chọn (Pill bo tròn)
        TabInactiveStyle = new GUIStyle(TabActiveStyle)
        {
            fontStyle = FontStyle.Normal
        };
        TabInactiveStyle.normal.background = TabPillInactiveTex != null ? TabPillInactiveTex : ButtonNormalTex;
        TabInactiveStyle.normal.textColor = new Color(0.88f, 0.84f, 0.76f);

        // 8. Huy hiệu mini (Mini Badge - Dùng cho [ ĐANG HOẠT ĐỘNG ], [ CẤP TIER ])
        MiniBadgeStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(9, 9, 9, 9),
            padding = new RectOffset(10, 10, 3, 3),
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        MiniBadgeStyle.normal.background = StatsPillTex != null ? StatsPillTex : PanelInteriorTex;

        // 8. Tiêu đề (Header)
        HeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        HeaderStyle.normal.textColor = new Color(1.0f, 0.88f, 0.35f);

        SubHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        SubHeaderStyle.normal.textColor = new Color(1.0f, 0.95f, 0.80f);

        // 9. Nhãn chữ thông thường
        LabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true
        };
        LabelStyle.normal.textColor = new Color(0.92f, 0.90f, 0.85f);

        ValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        ValueStyle.normal.textColor = new Color(0.40f, 0.95f, 0.50f);
    }

    /// <summary>
    /// Thuật toán sinh Texture 9-slice bo tròn 4 góc pixel-art chuẩn đối xứng (không bao giờ bị mất viền hay cắt cụt góc).
    /// </summary>
    public static Texture2D CreatePixelRoundedBox(
        int width, int height, int blockSize,
        Color borderColor, Color fillColor,
        Color? topHighlight = null, Color? bottomShadow = null)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        int blocksX = width / blockSize;
        int blocksY = height / blockSize;
        int bMaxX = blocksX - 1;
        int bMaxY = blocksY - 1;

        Color clear = Color.clear;
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Color topHi = topHighlight ?? fillColor;
        Color botSh = bottomShadow ?? fillColor;

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                // 1. 4 góc trong suốt
                bool isCornerTrans = false;
                if ((bx == 0 && by == 0) || (bx == 1 && by == 0) || (bx == 0 && by == 1)) isCornerTrans = true;
                else if ((bx == bMaxX && by == 0) || (bx == bMaxX - 1 && by == 0) || (bx == bMaxX && by == 1)) isCornerTrans = true;
                else if ((bx == 0 && by == bMaxY) || (bx == 1 && by == bMaxY) || (bx == 0 && by == bMaxY - 1)) isCornerTrans = true;
                else if ((bx == bMaxX && by == bMaxY) || (bx == bMaxX - 1 && by == bMaxY) || (bx == bMaxX && by == bMaxY - 1)) isCornerTrans = true;

                if (isCornerTrans) continue;

                // 2. Khối viền (Border)
                bool isBorder = false;
                if (by == 0 || by == bMaxY || bx == 0 || bx == bMaxX) isBorder = true;
                else if (bx == 1 && by == 1) isBorder = true;
                else if (bx == bMaxX - 1 && by == 1) isBorder = true;
                else if (bx == 1 && by == bMaxY - 1) isBorder = true;
                else if (bx == bMaxX - 1 && by == bMaxY - 1) isBorder = true;

                Color c = fillColor;
                if (isBorder)
                {
                    c = borderColor;
                }
                else if (by == 1)
                {
                    c = topHi;
                }
                else if (by == bMaxY - 1)
                {
                    c = botSh;
                }

                for (int py = 0; py < blockSize; py++)
                {
                    int y = by * blockSize + py;
                    for (int px = 0; px < blockSize; px++)
                    {
                        int x = bx * blockSize + px;
                        pixels[y * width + x] = c;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Vẽ một thanh tiến trình (Progress Bar) chuẩn pixel art Theme Wood với chữ hiển thị ở giữa.
    /// </summary>
    public static void DrawProgressBar(Rect rect, float progress, string text = "")
    {
        Initialize();
        progress = Mathf.Clamp01(progress);

        // Vẽ máng trượt nền tối
        if (ProgressTrackTex != null)
        {
            GUI.Box(rect, GUIContent.none, InteriorBoxStyle);
        }

        // Vẽ vệt tiến trình màu cam ấm
        if (progress > 0.02f && ProgressFillTex != null)
        {
            float fillW = (rect.width - 6f) * progress;
            Rect fillRect = new Rect(rect.x + 3f, rect.y + 3f, fillW, rect.height - 6f);
            
            // Vẽ 9-slice progress fill
            GUIStyle fillStyle = new GUIStyle();
            fillStyle.normal.background = ProgressFillTex;
            fillStyle.border = new RectOffset(6, 6, 6, 6);
            GUI.Box(fillRect, GUIContent.none, fillStyle);
        }

        // Chữ % ở giữa
        if (!string.IsNullOrEmpty(text))
        {
            GUIStyle centerText = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            centerText.normal.textColor = Color.white;
            GUI.Label(rect, text, centerText);
        }
    }
}
