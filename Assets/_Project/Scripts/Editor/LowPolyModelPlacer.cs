using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bước 3: Đặt các Model 3D đã có ra Bản Đồ Diorama (Placement of Existing 3D Models)
/// Đồng thời dọn dẹp triệt để toàn bộ model cũ (xưởng cũ, hang cũ, cây cũ, khối cube trùng lặp)
/// để khung cảnh hoàn toàn sạch sẽ, không bị lẫn lộn giữa model cũ và mới.
/// </summary>
public static class LowPolyModelPlacer
{
    private const string PREFAB_ENV_DIR = "Assets/_Project/Prefabs/Environment";
    private const string PREFAB_MACH_DIR = "Assets/_Project/Prefabs/Machines";
    private const string PREFAB_RES_DIR = "Assets/_Project/Prefabs/Resources";

    [MenuItem("Factory/Map Setup/Step 3: Place Existing Models on Map", priority = 12)]
    public static void PlaceModelsInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        PlaceModelsForScene(activeScene);
    }

    [MenuItem("Factory/Map Setup/Apply All Steps (Terrain + Roads + Models)", priority = 13)]
    public static void ApplyAllStepsInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        Debug.Log($"[DioramaMaster] === BẮT ĐẦU THIẾT LẬP TOÀN DIỆN DIORAMA CHO SCENE: {activeScene.name} ===");

        // Bước 1: Địa hình đồi núi & hồ nước
        LowPolyTerrainBuilder.GenerateTerrainForScene(activeScene);

        // Bước 2: Hệ thống đường 3D bê tông, đường đất & bãi cát
        LowPolyRoadBuilder.GenerateRoadsForScene(activeScene);

        // Bước 3: Bố trí toàn bộ 3D models và dọn sạch model cũ
        PlaceModelsForScene(activeScene);

        Debug.Log($"[DioramaMaster] === HOÀN TẤT TOÀN DIỆN BẢN ĐỒ DIORAMA THEO ẢNH MẪU! ===");
        EditorUtility.DisplayDialog(
            "Hoàn Tất Bản Đồ Diorama!",
            "Đã thiết lập toàn bộ bản đồ theo ảnh mẫu:\n\n" +
            "⛰️ Bước 1: Địa hình đồi núi & hồ nước với Shader Stylized mới\n" +
            "🛣️ Bước 2: Đường bê tông 3D vát mép nổi bật, đường đất sườn đồi & hoa dại\n" +
            "🏡 Bước 3: Xưởng mộc, mỏ đá, rừng thông bạt ngàn & máy móc nhà máy\n" +
            "🧹 Đã dọn dẹp sạch sẽ toàn bộ model cũ tránh trùng lặp!\n" +
            "✅ Đã nướng xong NavMesh!",
            "Tuyệt vời!"
        );
    }

    public static void PlaceModelsForScene(Scene scene)
    {
        Debug.Log($"[LowPolyModelPlacer] === BẮT ĐẦU ĐẶT MODEL (BƯỚC 3) CHO SCENE: {scene.name} ===");

        // 1. DỌN SẠCH TRIỆT ĐỂ TOÀN BỘ MODEL CŨ VÀ CÁC VISUAL TRÙNG LẶP
        PurgeAllOldObsoleteModels();

        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null)
        {
            envRoot = new GameObject("_Environment_Map");
        }

        // Xóa nhóm decor cũ nếu có trong _Environment_Map
        var oldDecor = envRoot.transform.Find("Decor_Models");
        if (oldDecor != null)
        {
            Object.DestroyImmediate(oldDecor.gameObject);
        }

        GameObject decorRoot = new GameObject("Decor_Models");
        decorRoot.transform.SetParent(envRoot.transform, false);

        // Tải các Prefabs môi trường chuẩn
        var workshopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Workshop.prefab");
        var cavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Cave.prefab");
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Tree.prefab");
        var safePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/BankSafe_Visual.prefab");
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/ControlPanel_Visual.prefab");

        // 2. Đặt Xưởng Gỗ Mộc Mạc tại cuối đường bê tông chính
        PlaceWorkshopShed(decorRoot.transform, workshopPrefab);

        // 3. Đặt Hang Mỏ Đá tại bãi cát khai khoáng kèm các tảng đá props
        PlaceQuarryPit(decorRoot.transform, cavePrefab);

        // 4. Đặt Chòi Canh Ngắm Cảnh trên Đỉnh Đồi Cao (nơi đường đất uốn lượn dẫn lên)
        PlaceWatchtowerHilltop(decorRoot.transform);

        // 5. Bố trí Rừng Thông Phủ Xanh Đồi Núi (Hơn 70 cây thông đa dạng góc xoay & kích thước)
        PlaceDensePineForest(decorRoot.transform, treePrefab);

        // 6. Đặt Két Sắt & Bảng Điều Khiển Nâng Cấp tại Quảng Trường Trung Tâm
        PlaceCentralHubFacilities(decorRoot.transform, safePrefab, panelPrefab);

        // 7. Cập nhật Dây Chuyền Sản Xuất của Nhà Máy (Làm sạch cube cũ, gắn visual mới)
        UpdateSceneFactoryMachines();

        // 8. Cập nhật NavMesh để nhân vật né tránh chính xác tất cả chướng ngại vật
        var terrainObj = envRoot.transform.Find("LowPoly_Terrain");
        if (terrainObj != null)
        {
            var navSurface = terrainObj.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[LowPolyModelPlacer] === BƯỚC 3 HOÀN TẤT: Đã dọn sạch model cũ và đặt xong toàn bộ Model mới! ===");
    }

    /// <summary>
    /// Dọn dẹp sạch sẽ toàn bộ các đối tượng decor cũ, tránh trùng lặp đè lên nhau
    /// </summary>
    public static void PurgeAllOldObsoleteModels()
    {
        // 1. Xóa nhóm _EnvironmentDecor cũ của script trước
        var oldEnvDecor = GameObject.Find("_EnvironmentDecor");
        if (oldEnvDecor != null)
        {
            Object.DestroyImmediate(oldEnvDecor);
        }

        // 2. Xóa các đối tượng decor cũ vương vãi
        string[] oldNames = {
            "Decor_TimberWorkshop", "Decor_Cave_Left", "Decor_Cave_Right",
            "Model_RusticWorkshopShed", "Model_QuarryPitCave", "Model_HilltopWatchtower"
        };
        foreach (var name in oldNames)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        // 3. Xóa các cây thông cũ mang tên Decor_PineTree_*
        for (int i = 1; i <= 20; i++)
        {
            var tree = GameObject.Find($"Decor_PineTree_{i:D2}");
            if (tree != null) Object.DestroyImmediate(tree);
        }

        // 4. Vô hiệu hóa mặt sàn phẳng cũ
        var floor = GameObject.Find("Ground_FactoryFloor");
        if (floor != null) floor.SetActive(false);
        var showroom = GameObject.Find("Showroom_Floor");
        if (showroom != null) showroom.SetActive(false);
    }

    // =========================================================================
    // 2. XƯỞNG GỖ (WORKSHOP SHED)
    // =========================================================================
    private static void PlaceWorkshopShed(Transform parent, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[LowPolyModelPlacer] Không tìm thấy Environment_Workshop.prefab!");
            return;
        }

        Vector3 pos = new Vector3(3.5f, 0f, 4.5f);
        pos.y = LowPolyTerrainBuilder.CalculateHeight(pos.x, pos.z);

        GameObject ws = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, 180f, 0f), parent);
        ws.name = "Model_RusticWorkshopShed";
        ws.transform.localScale = Vector3.one * 1.15f;

        EnsureCollider(ws);
    }

    // =========================================================================
    // 3. HỐ MỎ ĐÁ / HANG ĐÁ (QUARRY PIT / CAVE)
    // =========================================================================
    private static void PlaceQuarryPit(Transform parent, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[LowPolyModelPlacer] Không tìm thấy Environment_Cave.prefab!");
            return;
        }

        Vector3 pos = new Vector3(-8.8f, 0f, 2.8f);
        pos.y = LowPolyTerrainBuilder.CalculateHeight(pos.x, pos.z);

        GameObject cave = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, 75f, 0f), parent);
        cave.name = "Model_QuarryPitCave";
        cave.transform.localScale = Vector3.one * 1.1f;

        EnsureCollider(cave);

        // Thêm đá trang trí ven mỏ
        var stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_RES_DIR}/Stone_Visual.prefab");
        if (stonePrefab != null)
        {
            Vector3[] stoneOffsets = new[]
            {
                new Vector3(-6.2f, 0f, 1.2f),
                new Vector3(-5.6f, 0f, 0.4f),
                new Vector3(-7.2f, 0f, -0.8f)
            };

            for (int i = 0; i < stoneOffsets.Length; i++)
            {
                Vector3 sPos = stoneOffsets[i];
                sPos.y = LowPolyTerrainBuilder.CalculateHeight(sPos.x, sPos.z);
                var st = Object.Instantiate(stonePrefab, sPos, Quaternion.Euler(0f, i * 65f, 0f), parent);
                st.name = $"Decor_QuarryStone_{i + 1}";
                st.transform.localScale = Vector3.one * (1.2f + i * 0.3f);
            }
        }
    }

    // =========================================================================
    // 4. CHÒI CANH NGẮM CẢNH ĐỈNH ĐỒI (WATCHTOWER HILLTOP)
    // =========================================================================
    private static void PlaceWatchtowerHilltop(Transform parent)
    {
        Vector3 peakPos = new Vector3(-8.0f, 0f, 26.0f);
        peakPos.y = LowPolyTerrainBuilder.CalculateHeight(peakPos.x, peakPos.z);

        var shedModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/wooden_workshop_3d_model/wooden_workshop_3d_model.fbx");
        if (shedModel != null)
        {
            GameObject tower = Object.Instantiate(shedModel, peakPos, Quaternion.Euler(0f, 135f, 0f), parent);
            tower.name = "Model_HilltopWatchtower";
            tower.transform.localScale = Vector3.one * 0.75f;
            EnsureCollider(tower);
        }
    }

    // =========================================================================
    // 5. RỪNG THÔNG LOW-POLY PHỦ XANH ĐỒI NÚI (DENSE PINE FOREST)
    // =========================================================================
    private static void PlaceDensePineForest(Transform parent, GameObject treePrefab)
    {
        if (treePrefab == null)
        {
            Debug.LogWarning("[LowPolyModelPlacer] Không tìm thấy Environment_Tree.prefab!");
            return;
        }

        GameObject forestRoot = new GameObject("PineForest_Clusters");
        forestRoot.transform.SetParent(parent, false);

        List<Vector3> treeCoords = new List<Vector3>();

        // Cụm 1: Sườn đồi bên trái (men theo con đường đất uốn lượn lên núi)
        Vector3[] leftRidgeTrees = new[]
        {
            new Vector3(-21f, 0f, -24f),
            new Vector3(-19f, 0f, -20f),
            new Vector3(-24f, 0f, -18f),
            new Vector3(-22f, 0f, -14f),
            new Vector3(-18f, 0f, -10f),
            new Vector3(-25f, 0f, -8f),
            new Vector3(-21f, 0f, -4f),
            new Vector3(-15f, 0f, -6f),
            new Vector3(-23f, 0f, 0f),
            new Vector3(-26f, 0f, 4f),
            new Vector3(-18f, 0f, 6f),
            new Vector3(-22f, 0f, 8f),
            new Vector3(-25f, 0f, 12f),
            new Vector3(-20f, 0f, 14f),
            new Vector3(-16f, 0f, 16f),
            new Vector3(-22f, 0f, 18f),
            new Vector3(-26f, 0f, 22f),
            new Vector3(-19f, 0f, 22f),
            new Vector3(-14f, 0f, 20f)
        };
        treeCoords.AddRange(leftRidgeTrees);

        // Cụm 2: Đỉnh đồi cao phía sau (Bao bọc tháp canh)
        Vector3[] backRidgeTrees = new[]
        {
            new Vector3(-14f, 0f, 25f),
            new Vector3(-12f, 0f, 28f),
            new Vector3(-10f, 0f, 31f),
            new Vector3(-6f, 0f, 31f),
            new Vector3(-4f, 0f, 28f),
            new Vector3(-2f, 0f, 26f),
            new Vector3(0f, 0f, 29f),
            new Vector3(3f, 0f, 27f),
            new Vector3(-8f, 0f, 33f)
        };
        treeCoords.AddRange(backRidgeTrees);

        // Cụm 3: Ven hồ nước xanh biếc Đông-Bắc
        Vector3[] lakeTrees = new[]
        {
            new Vector3(8f, 0f, 18f),
            new Vector3(9f, 0f, 23f),
            new Vector3(11f, 0f, 26f),
            new Vector3(15f, 0f, 27f),
            new Vector3(18f, 0f, 25f),
            new Vector3(20f, 0f, 21f),
            new Vector3(19f, 0f, 17f),
            new Vector3(17f, 0f, 14f),
            new Vector3(13f, 0f, 13f),
            new Vector3(23f, 0f, 23f),
            new Vector3(24f, 0f, 18f)
        };
        treeCoords.AddRange(lakeTrees);

        // Cụm 4: Dãy đồi phía Đông (bên phải)
        Vector3[] eastRidgeTrees = new[]
        {
            new Vector3(21f, 0f, 10f),
            new Vector3(24f, 0f, 6f),
            new Vector3(20f, 0f, 2f),
            new Vector3(25f, 0f, -2f),
            new Vector3(22f, 0f, -6f),
            new Vector3(26f, 0f, -10f),
            new Vector3(21f, 0f, -14f),
            new Vector3(25f, 0f, -18f),
            new Vector3(19f, 0f, -22f),
            new Vector3(23f, 0f, -25f)
        };
        treeCoords.AddRange(eastRidgeTrees);

        // Cụm 5: Gờ đồi phía trước (Cổng vào đường chính)
        Vector3[] frontEntranceTrees = new[]
        {
            new Vector3(-8f, 0f, -26f),
            new Vector3(-6f, 0f, -28f),
            new Vector3(6f, 0f, -27f),
            new Vector3(8f, 0f, -25f),
            new Vector3(12f, 0f, -26f)
        };
        treeCoords.AddRange(frontEntranceTrees);

        System.Random rng = new System.Random(1337);

        for (int i = 0; i < treeCoords.Count; i++)
        {
            Vector3 basePos = treeCoords[i];
            float jitterX = (float)(rng.NextDouble() * 1.6 - 0.8);
            float jitterZ = (float)(rng.NextDouble() * 1.6 - 0.8);
            Vector3 finalPos = new Vector3(basePos.x + jitterX, 0f, basePos.z + jitterZ);
            finalPos.y = LowPolyTerrainBuilder.CalculateHeight(finalPos.x, finalPos.z);

            float scale = 0.85f + (float)rng.NextDouble() * 0.55f;
            float yaw = (float)rng.NextDouble() * 360f;
            float tilt = (float)(rng.NextDouble() * 3.5 - 1.75);

            GameObject tree = Object.Instantiate(treePrefab, finalPos, Quaternion.Euler(tilt, yaw, tilt), forestRoot.transform);
            tree.name = $"PineTree_{i + 1:D2}";
            tree.transform.localScale = Vector3.one * scale;

            EnsureCollider(tree);
        }
    }

    // =========================================================================
    // 6. KÉT SẮT & BẢNG ĐIỀU KHIỂN TẠI QUẢNG TRƯỜNG TRUNG TÂM
    // =========================================================================
    private static void PlaceCentralHubFacilities(Transform parent, GameObject safePrefab, GameObject panelPrefab)
    {
        var existingSafe = GameObject.Find("Bank_Vault") ?? GameObject.Find("Safe") ?? GameObject.Find("MoneyVault_Safe");
        if (existingSafe == null && safePrefab != null)
        {
            Vector3 pos = new Vector3(-2.2f, 0f, -4.5f);
            pos.y = LowPolyTerrainBuilder.CalculateHeight(pos.x, pos.z);
            var safe = Object.Instantiate(safePrefab, pos, Quaternion.Euler(0f, 25f, 0f), parent);
            safe.name = "Decor_CentralBankSafe";
            EnsureCollider(safe);
        }

        var existingTerminal = GameObject.Find("UpgradeStation_Terminal") ?? GameObject.Find("UpgradeStation");
        if (existingTerminal == null && panelPrefab != null)
        {
            Vector3 pos = new Vector3(2.2f, 0f, -4.5f);
            pos.y = LowPolyTerrainBuilder.CalculateHeight(pos.x, pos.z);
            var terminal = Object.Instantiate(panelPrefab, pos, Quaternion.Euler(0f, -25f, 0f), parent);
            terminal.name = "Decor_CentralUpgradeStation";
            EnsureCollider(terminal);
        }
    }

    // =========================================================================
    // 7. CẬP NHẬT DÂY CHUYỀN SẢN XUẤT CỦA NHÀ MÁY (XÓA SẠCH TRÙNG LẶP)
    // =========================================================================
    private static void UpdateSceneFactoryMachines()
    {
        var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/ConveyorBelt_Visual.prefab");
        var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/StoragePallet_Visual.prefab");

        var types = new[] {
            new { type = "Wood", isLeft = true },
            new { type = "Stone", isLeft = true },
            new { type = "Copper", isLeft = true },
            new { type = "Iron", isLeft = false },
            new { type = "Silver", isLeft = false },
            new { type = "Gold", isLeft = false }
        };

        foreach (var def in types)
        {
            var pad = GameObject.Find($"StoragePad_{def.type}");
            if (pad != null && palletPrefab != null)
            {
                DisableMeshRenderer(pad);
                AttachVisualClean(pad, palletPrefab, "Visual_Pallet", new Vector3(0f, -0.15f, 0f), Quaternion.identity);
            }

            var conv = GameObject.Find($"Conveyor_{def.type}");
            if (conv != null && conveyorPrefab != null)
            {
                DisableMeshRenderer(conv);
                AttachVisualClean(conv, conveyorPrefab, "Visual_Conveyor", new Vector3(0f, -0.25f, 0f), Quaternion.identity);
            }

            var mach = GameObject.Find($"Machine_{def.type}");
            if (mach != null)
            {
                DisableMeshRenderer(mach);
                string tier1Path = def.type == "Wood"
                    ? $"{PREFAB_MACH_DIR}/Visual_Wood_Tier1.prefab"
                    : $"{PREFAB_MACH_DIR}/Visual_Ore_Tier1.prefab";
                var t1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(tier1Path);
                if (t1Prefab != null)
                {
                    Quaternion rot = Quaternion.Euler(0f, def.isLeft ? 90f : -90f, 0f);
                    AttachVisualClean(mach, t1Prefab, "Visual_MachineTier1", new Vector3(0f, -1f, 0f), rot);
                }
            }
        }
    }

    /// <summary>
    /// Gắn visual duy nhất và xóa sạch mọi object con visual cũ trùng lặp
    /// </summary>
    private static void AttachVisualClean(GameObject target, GameObject prefab, string childName, Vector3 localPos, Quaternion localRot)
    {
        // Xóa mọi child visual cũ hoặc trùng lặp
        for (int i = target.transform.childCount - 1; i >= 0; i--)
        {
            var child = target.transform.GetChild(i);
            if (child.name.StartsWith("Visual_") || child.name.Contains("Model") || child.name.Contains("Tier"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        var instance = Object.Instantiate(prefab, target.transform, false);
        instance.name = childName;
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
    }

    private static void DisableMeshRenderer(GameObject go)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    private static void EnsureCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() == null && go.GetComponentInChildren<Collider>() == null)
        {
            var mc = go.AddComponent<BoxCollider>();
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                mc.center = go.transform.InverseTransformPoint(b.center);
                mc.size = go.transform.InverseTransformVector(b.size);
            }
        }
    }
}
