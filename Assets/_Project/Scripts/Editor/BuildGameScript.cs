using System.IO;
using UnityEditor;
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

    [MenuItem("FactoryKit/🚀 Build Game PC Standalone (.exe)", false, 10)]
    public static void BuildWindowsPC()
    {
        Debug.Log("[BuildGameScript] Bắt đầu quá trình Build Game PC Standalone cho cuộc thi Game Sprint 2026...");

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
