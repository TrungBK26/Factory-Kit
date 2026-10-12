using UnityEditor;
using UnityEngine;

/// <summary>
/// Tự động thiết lập chất lượng tối đa cho UI:
/// - MainMenu_Background.png (2752 x 1536) & Intro_Letter.png (1792 x 2400): 4K, Uncompressed, Bilinear.
/// - Theme_Wood UI Sprites: GUI type, Point filter (Pixel-perfect), No Mipmaps, Uncompressed.
/// </summary>
public class UITextureQualityOptimizer : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath.Contains("MainMenu_Background") || assetPath.Contains("Intro_Letter"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            Apply4KBackgroundSettings(importer);
        }
        else if (assetPath.Contains("Theme_Wood"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            ApplyPixelUISettings(importer);
        }
    }

    private static void Apply4KBackgroundSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.GUI;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
    }

    private static void ApplyPixelUISettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.GUI;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point; // Pixel art sắc nét từng điểm ảnh
        importer.alphaIsTransparency = true;
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool("UI_TEXTURES_OPTIMIZED_4K_V3", false)) return;
            EditorPrefs.SetBool("UI_TEXTURES_OPTIMIZED_4K_V3", true);
            AssetDatabase.Refresh();
            OptimizeAllUITextures();
        };
    }

    [MenuItem("Factory/Tối Ưu Độ Nét Ảnh UI (4K & Pixel Art)")]
    public static void OptimizeAllUITextures()
    {
        string[] targetPaths = new string[]
        {
            "Assets/_Project/Resources/UI/MainMenu_Background.png",
            "Assets/_Project/Textures/UI/MainMenu_Background.png",
            "Assets/_Project/Resources/UI/Intro_Letter.png",
            "Assets/_Project/Textures/UI/Intro_Letter.png"
        };

        foreach (string path in targetPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                Apply4KBackgroundSettings(importer);
                importer.SaveAndReimport();
            }
        }

        // Tối ưu các texture Theme_Wood
        string[] themeGuids = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/_Project/Resources/UI/Theme_Wood" });
        foreach (string guid in themeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                ApplyPixelUISettings(importer);
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("[UI Optimizer] Đã tối ưu hóa toàn bộ UI Sprites và 4K Backgrounds!");
    }
}
