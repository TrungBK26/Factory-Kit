using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Công cụ dọn dẹp Scene: Xóa sạch toàn bộ các cụm dây chuyền cũ bị trùng lặp,
/// trả lại Scene sạch sẽ, chỉ giữ lại Bản đồ Diorama (Đồi núi, Đường xá, Rừng thông, Xưởng, Mỏ đá)
/// và các tiện ích trung tâm (Player, Bán hàng, Két sắt, Bảng nâng cấp).
/// </summary>
public static class DioramaSceneCleaner
{
    [MenuItem("Factory/Clean Scene/Remove All Duplicate Production Lines", priority = 20)]
    public static void CleanAllProductionLines()
    {
        var scene = SceneManager.GetActiveScene();
        int count = 0;

        // Tìm tất cả các GameObject bắt đầu bằng ProductionLine_
        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjs)
        {
            if (go == null) continue;
            if (go.name.StartsWith("ProductionLine_"))
            {
                Object.DestroyImmediate(go);
                count++;
            }
        }

        // Tìm và xóa các máy lẻ, băng chuyền lẻ, bệ chứa lẻ nếu còn sót bên ngoài
        string[] prefixes = { "Machine_", "Conveyor_", "StoragePad_" };
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go == null) continue;
            foreach (var pref in prefixes)
            {
                if (go.name.StartsWith(pref))
                {
                    Object.DestroyImmediate(go);
                    count++;
                    break;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[DioramaCleaner] Đã xóa sạch {count} đối tượng dây chuyền cũ/trùng lặp!");

        EditorUtility.DisplayDialog(
            "Dọn Dẹp Thành Công!",
            $"Đã xóa sạch {count} đối tượng dây chuyền cũ và trùng lặp!\n\n" +
            "Scene hiện tại đã hoàn toàn sạch sẽ.\n" +
            "Bây giờ bạn có thể vào thư mục:\nAssets/_Project/Prefabs/ProductionLines/\n" +
            "để kéo thả các dây chuyền mới vào mà không sợ bất kỳ xung đột nào!",
            "Tuyệt vời!"
        );
    }
}
