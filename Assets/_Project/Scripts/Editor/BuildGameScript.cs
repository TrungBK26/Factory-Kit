using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Script tự động hóa Build Game PC Standalone (.exe) cho cuộc thi Game Sprint 2026.
/// Cung cấp 1-click build chuẩn 64-bit, tự động gom scene PrototypeScene và mở thư mục đầu ra.
/// </summary>
public static class BuildGameScript
{
    private const string ScenePath = "Assets/_Project/Scenes/PrototypeScene.unity";
    private const string OutputFolder = "Builds/PC";
    private const string ExeName = "FactoryKit.exe";
    private const string IconPath = "Assets/_Project/Textures/UI/GameIcon.png";
    private const string SplashBgPath = "Assets/_Project/Textures/UI/MainMenu_Background.png";

    [MenuItem("FactoryKit/🎨 Cập Nhật Icon Game & Màn Hình Khởi Động (Splash Screen)", false, 5)]
    public static void ConfigureGameIconAndSplash()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // 1. Cấu hình Icon ứng dụng (.exe)
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null)
        {
            try
            {
                int[] defaultSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Application);
                if (defaultSizes != null && defaultSizes.Length > 0)
                {
                    Texture2D[] defaultIcons = new Texture2D[defaultSizes.Length];
                    for (int i = 0; i < defaultIcons.Length; i++) defaultIcons[i] = icon;
                    PlayerSettings.SetIcons(NamedBuildTarget.Unknown, defaultIcons, IconKind.Application);
                }

                int[] standaloneSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
                if (standaloneSizes != null && standaloneSizes.Length > 0)
                {
                    Texture2D[] standaloneIcons = new Texture2D[standaloneSizes.Length];
                    for (int i = 0; i < standaloneIcons.Length; i++) standaloneIcons[i] = icon;
                    PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Application);
                }

                Debug.Log($"[BuildGameScript] ✅ Đã cập nhật Icon Game thành công ({IconPath})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildGameScript] Gán Icon: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[BuildGameScript] Chưa tìm thấy texture icon tại: {IconPath}");
        }

        // 2. Cấu hình Ảnh nền màn hình chờ khởi động (Splash Screen Background)
        Sprite splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SplashBgPath);
        if (splashSprite != null)
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.background = splashSprite;
            PlayerSettings.SplashScreen.blurBackgroundImage = false;
            Debug.Log($"[BuildGameScript] ✅ Đã cập nhật Splash Background thành công ({SplashBgPath})");
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("FactoryKit/🚀 Build Game PC Standalone (.exe)", false, 10)]
    public static void BuildWindowsPC()
    {
        Debug.Log("[BuildGameScript] Bắt đầu quá trình Build Game PC Standalone cho cuộc thi Game Sprint 2026...");

        // Tự động đồng bộ Icon và Màn hình chờ đẹp mắt trước khi Build
        ConfigureGameIconAndSplash();

        // 1. Kiểm tra Scene tồn tại
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Lỗi Build", $"Không tìm thấy file Scene chính tại: {ScenePath}", "OK");
            Debug.LogError($"[BuildGameScript] Scene chính không tồn tại: {ScenePath}");
            return;
        }

        // 2. Thiết lập cấu hình PlayerSettings tối ưu cho cuộc thi
        PlayerSettings.companyName = "Kuro";
        PlayerSettings.productName = "Factory Kit - Dream Factory";
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;

        // 3. Chuẩn bị thư mục đầu ra
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullOutputDir = Path.Combine(projectRoot, OutputFolder);
        if (!Directory.Exists(fullOutputDir))
        {
            Directory.CreateDirectory(fullOutputDir);
        }

        string fullExePath = Path.Combine(fullOutputDir, ExeName);

        // 4. Cấu hình BuildPlayerOptions
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = fullExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // 5. Tiến hành Build
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        BuildReport report = BuildPipeline.BuildPlayer(options);
        stopwatch.Stop();

        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            double sizeMb = (double)summary.totalSize / (1024 * 1024);
            string msg = $"🎉 BUILD THÀNH CÔNG!\n\n" +
                         $"• File thực thi: {fullExePath}\n" +
                         $"• Dung lượng: {sizeMb:F2} MB\n" +
                         $"• Thời gian build: {stopwatch.Elapsed.TotalSeconds:F1} giây\n" +
                         $"• Cảnh báo: {summary.totalWarnings}\n\n" +
                         $"Thư mục chứa file exe sẽ được mở ngay bây giờ.";

            Debug.Log($"[BuildGameScript] {msg}");
            EditorUtility.DisplayDialog("Build Thành Công", msg, "Mở Thư Mục");

            // Mở thư mục chứa file build trong Windows Explorer
            EditorUtility.RevealInFinder(fullExePath);
        }
        else
        {
            string errMsg = $"❌ BUILD THẤT BẠI!\nKết quả: {summary.result}\nLỗi: {summary.totalErrors}\nXem chi tiết tại Console log.";
            Debug.LogError($"[BuildGameScript] {errMsg}");
            EditorUtility.DisplayDialog("Build Thất Bại", errMsg, "Đóng");
        }
    }
}
