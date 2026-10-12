using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Master Tool: Chuẩn hóa toàn bộ Model 3D (Meshy & Tripo & Resources),
/// đóng gói thành Prefab chuẩn (Normalized Pivot Y=0, Scale, Orientation),
/// kết nối tự động vào ScriptableObjects & Scenes (PrototypeScene & SampleScene Showroom),
/// đồng thời tắt toàn bộ MeshRenderer của các khối Cube nguyên mẫu (tránh che khuất model).
/// </summary>
public static class FactoryAssetIntegrationMaster
{
    private const string PREFAB_RES_DIR = "Assets/_Project/Prefabs/Resources";
    private const string PREFAB_MACH_DIR = "Assets/_Project/Prefabs/Machines";
    private const string PREFAB_ENV_DIR = "Assets/_Project/Prefabs/Environment";
    private const string SO_MACH_DIR = "Assets/_Project/ScriptableObjects/Machines";
    private const string SO_RES_DIR = "Assets/_Project/ScriptableObjects/Resources";

    // ĐÃ TẮT TỰ ĐỘNG CHẠY KHI RELOAD: Bảo toàn 100% vị trí trong Scene.
    // Chỉ chạy khi người dùng chủ động click menu "Factory/3D Models/Normalize and Apply All Models".

    [MenuItem("Factory/3D Models/Normalize and Apply All Models", priority = 1)]
    public static void ManualExecute()
    {
        ExecuteAll(silent: false);
    }

    public static void ExecuteAll(bool silent)
    {
        Debug.Log("[FactoryAssetMaster] === BẮT ĐẦU CHUẨN HÓA VÀ TÍCH HỢP TOÀN BỘ 3D MODELS ===");
        EnsureFoldersExist();

        // 1. Tạo các Prefab Tài nguyên (6 Resources)
        CreateResourcePrefabs();

        // 2. Tạo các Prefab Máy móc (Tiers 1, 2, 3)
        CreateMachinePrefabs();

        // 3. Tạo các Prefab Môi trường & Thiết bị
        CreateEnvironmentPrefabs();

        // 4. Kết nối ScriptableObjects
        LinkAllScriptableObjects();

        // 5. Cập nhật Scene Prototype (PrototypeScene.unity)
        UpdatePrototypeScene();

        // 6. Cập nhật Scene Trưng bày (SampleScene.unity Showroom)
        UpdateSampleSceneShowroom();

        // Re-open PrototypeScene as the active playable scene
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/PrototypeScene.unity", OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FactoryAssetMaster] === HOÀN TẤT CHUẨN HÓA VÀ TÍCH HỢP 3D MODELS THÀNH CÔNG! ===");

        if (!silent)
        {
            EditorUtility.DisplayDialog(
                "Chuẩn Hóa 3D Models Thành Công!",
                "Đã chuẩn hóa toàn bộ Model 3D:\n\n" +
                "✨ 6 Dòng Tài Nguyên (Gỗ, Đá, Đồng, Sắt, Bạc, Vàng)\n" +
                "🏭 Máy Móc 3 Tiers (Rusty Oven -> Steampunk -> Industrial)\n" +
                "🏦 Két Sắt, Bảng Điều Khiển, Băng Chuyền, Bệ Chứa Pallet\n" +
                "🌲 Môi Trường Decor: Xưởng Gỗ, Hang Động Đá, Rừng Thông\n\n" +
                "✅ Đã ẩn toàn bộ khối Cube che khuất!\n" +
                "✅ Đã chuẩn hóa tỉ lệ kích thước (Scale) và hướng xoay (Rotation)!\n" +
                "✅ Đã tạo Gallery Showroom tại Assets/Scenes/SampleScene.unity!",
                "Tuyệt vời!"
            );
        }
    }

    private static void EnsureFoldersExist()
    {
        string[] dirs = { PREFAB_RES_DIR, PREFAB_MACH_DIR, PREFAB_ENV_DIR };
        foreach (var dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        AssetDatabase.Refresh();
    }

    // =========================================================================
    // 1. TẠO TÀI NGUYÊN PREFAB
    // =========================================================================
    private struct ResourceDef
    {
        public ResourceType type;
        public string modelPath;
        public string prefabName;
        public Vector3 targetSize;
        public float yawOffset;
    }

    private static void CreateResourcePrefabs()
    {
        var list = new[]
        {
            new ResourceDef
            {
                type = ResourceType.Wood,
                modelPath = "Assets/_Project/Models/Resources/Wood/SM_Item_Wood_A.fbx",
                prefabName = "WoodItemPrefab.prefab",
                targetSize = new Vector3(0.5f, 0.35f, 0.5f),
                yawOffset = 0f
            },
            new ResourceDef
            {
                type = ResourceType.Stone,
                modelPath = "Assets/_Project/Models/Resources/Stone/SM_Item_Stone.fbx",
                prefabName = "StoneItemPrefab.prefab",
                targetSize = new Vector3(0.45f, 0.35f, 0.45f),
                yawOffset = 0f
            },
            new ResourceDef
            {
                type = ResourceType.Copper,
                modelPath = "Assets/_Project/Models/Resources/Copper/SM_Item_Copper.fbx",
                prefabName = "CopperItemPrefab.prefab",
                targetSize = new Vector3(0.45f, 0.3f, 0.45f),
                yawOffset = 0f
            },
            new ResourceDef
            {
                type = ResourceType.Iron,
                modelPath = "Assets/_Project/Models/Resources/Iron/SM_Item_Iron.fbx",
                prefabName = "IronItemPrefab.prefab",
                targetSize = new Vector3(0.45f, 0.3f, 0.45f),
                yawOffset = 0f
            },
            new ResourceDef
            {
                type = ResourceType.Silver,
                modelPath = "Assets/_Project/Models/Resources/Sliver/SM_Item_Silver.fbx",
                prefabName = "SilverItemPrefab.prefab",
                targetSize = new Vector3(0.45f, 0.3f, 0.45f),
                yawOffset = 0f
            },
            new ResourceDef
            {
                type = ResourceType.Gold,
                modelPath = "Assets/_Project/Models/Resources/Gold/SM_Item_Gold.fbx",
                prefabName = "GoldItemPrefab.prefab",
                targetSize = new Vector3(0.45f, 0.3f, 0.45f),
                yawOffset = 0f
            }
        };

        foreach (var def in list)
        {
            var so = AssetDatabase.LoadAssetAtPath<ResourceDataSO>($"{SO_RES_DIR}/Resource_{def.type}.asset");
            string prefabPath = $"{PREFAB_RES_DIR}/{def.prefabName}";
            CreateNormalizedPrefab(
                prefabPath: prefabPath,
                modelPath: def.modelPath,
                targetSize: def.targetSize,
                yawDegrees: def.yawOffset,
                isUniformScale: true,
                isResourceItem: true,
                resourceData: so
            );
        }
    }

    // =========================================================================
    // 2. TẠO MÁY MÓC PREFAB
    // =========================================================================
    private static void CreateMachinePrefabs()
    {
        // Ore Machines (Tiers 1, 2, 3)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Ore_Tier1.prefab",
            modelPath: "Assets/MeshyImports/Rusty Gearbox Oven_20261009_211907/Meshy_AI_Rusty_Gearbox_Oven_1009141854_texture.fbx",
            targetSize: new Vector3(2.0f, 2.2f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Ore_Tier2.prefab",
            modelPath: "Assets/TripoModels/steampunk_machine_3d_model/steampunk_machine_3d_model.fbx",
            targetSize: new Vector3(2.0f, 2.2f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Ore_Tier3.prefab",
            modelPath: "Assets/TripoModels/industrial_machine_3d_model/industrial_machine_3d_model.fbx",
            targetSize: new Vector3(2.0f, 2.2f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Wood Machine (Tier 1 Wooden Workshop, T2 Gearbox Oven, T3 Industrial)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Wood_Tier1.prefab",
            modelPath: "Assets/TripoModels/wooden_workshop_3d_model/wooden_workshop_3d_model.fbx",
            targetSize: new Vector3(2.2f, 2.2f, 2.2f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Wood_Tier2.prefab",
            modelPath: "Assets/MeshyImports/Rusty Gearbox Oven_20261009_211907/Meshy_AI_Rusty_Gearbox_Oven_1009141854_texture.fbx",
            targetSize: new Vector3(2.0f, 2.2f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_MACH_DIR}/Visual_Wood_Tier3.prefab",
            modelPath: "Assets/TripoModels/industrial_machine_3d_model/industrial_machine_3d_model.fbx",
            targetSize: new Vector3(2.0f, 2.2f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );
    }

    // =========================================================================
    // 3. TẠO MÔI TRƯỜNG & THIẾT BỊ PREFAB
    // =========================================================================
    private static void CreateEnvironmentPrefabs()
    {
        // Két Sắt Bank Safe (1.5m x 1.5m x 1.5m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/BankSafe_Visual.prefab",
            modelPath: "Assets/MeshyImports/Fortified Lockbox_20261009_213203/Meshy_AI_Fortified_Lockbox_1009143153_texture.fbx",
            targetSize: new Vector3(1.5f, 1.5f, 1.5f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Bảng Điều Khiển Control Panel / Upgrade Station
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/ControlPanel_Visual.prefab",
            modelPath: "Assets/MeshyImports/Upgrade Station_20261009_213936/Meshy_AI_Upgrade_Station_1009143918_texture.fbx",
            targetSize: new Vector3(1.4f, 1.8f, 1.2f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Băng Chuyền Conveyor Belt (Dài 7.0m, Cao 0.5m, Rộng 1.4m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/ConveyorBelt_Visual.prefab",
            modelPath: "Assets/MeshyImports/Conveyor Belt_20261009_213958/Meshy_AI_Conveyor_Belt_1009143945_texture.fbx",
            targetSize: new Vector3(7.0f, 0.5f, 1.4f),
            yawDegrees: 0f,
            isUniformScale: false,
            autoAlignConveyorLength: true
        );

        // Bệ Chứa Storage Pallet (2.2m x 0.35m x 2.2m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/StoragePallet_Visual.prefab",
            modelPath: "Assets/_Project/Models/Environment/Pallet/SM_Storage_Pallet.fbx",
            targetSize: new Vector3(2.2f, 0.35f, 2.2f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Cây Thông Evergreen Pine Tree (2m x 4m x 2m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/Environment_Tree.prefab",
            modelPath: "Assets/MeshyImports/Low Poly Evergreen_20261009_214102/Meshy_AI_Low_Poly_Evergreen_1009144053_texture.fbx",
            targetSize: new Vector3(2.0f, 4.0f, 2.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Xưởng Gỗ Workshop Shed (6m x 4.5m x 6m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/Environment_Workshop.prefab",
            modelPath: "Assets/MeshyImports/Rustic Timber Workshop_20261009_212905/Meshy_AI_Rustic_Timber_Worksho_1009142847_texture.fbx",
            targetSize: new Vector3(6.0f, 4.5f, 6.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );

        // Hang Đá Cave / Mine Pit (7m x 4.5m x 7m)
        CreateNormalizedPrefab(
            prefabPath: $"{PREFAB_ENV_DIR}/Environment_Cave.prefab",
            modelPath: "Assets/TripoModels/rocky_cave_3d_model/rocky_cave_3d_model.fbx",
            targetSize: new Vector3(7.0f, 4.5f, 7.0f),
            yawDegrees: 0f,
            isUniformScale: true
        );
    }

    // =========================================================================
    // CORE HELPER: TẠO NORMALIZED PREFAB (TWO-LEVEL WRAPPER)
    // =========================================================================
    private static void CreateNormalizedPrefab(
        string prefabPath,
        string modelPath,
        Vector3 targetSize,
        float yawDegrees,
        bool isUniformScale,
        bool isResourceItem = false,
        ResourceDataSO resourceData = null,
        bool autoAlignConveyorLength = false)
    {
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (fbxAsset == null)
        {
            Debug.LogWarning($"[FactoryAssetMaster] Không tìm thấy model FBX: {modelPath}");
            return;
        }

        string rootName = Path.GetFileNameWithoutExtension(prefabPath);
        GameObject root = new GameObject(rootName);

        // 1. Tạo wrapper Visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        // 2. Tạo Model instance bên trong Visual để giữ nguyên native FBX rotation
        GameObject model = Object.Instantiate(fbxAsset, visual.transform);
        model.name = "Model";

        // Gỡ bỏ bất kỳ Collider thừa bên trong file FBX con
        var childCols = visual.GetComponentsInChildren<Collider>();
        foreach (var c in childCols)
        {
            Object.DestroyImmediate(c);
        }

        // Gán Material và Texture chính xác cho tất cả Renderers
        Material modelMat = FindModelMaterial(modelPath);
        if (modelMat != null)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = modelMat;
            }
        }

        // 3. Nếu là băng chuyền, tự động phát hiện xem chiều dài của FBX chạy dọc theo Z hay X
        if (autoAlignConveyorLength)
        {
            Bounds checkB = CalculateBounds(visual);
            if (checkB.size.z > checkB.size.x)
            {
                yawDegrees += 90f; // Xoay 90 độ để trục dài trùng với trục X (kết nối máy -> bệ chứa)
            }
        }

        // 4. Áp dụng góc xoay Yaw lên Visual
        visual.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

        // 5. Tính Bounds sau khi áp dụng góc xoay
        Bounds rawBounds = CalculateBounds(visual);
        if (rawBounds.size.sqrMagnitude > 0.0001f)
        {
            if (isUniformScale)
            {
                float maxDim = Mathf.Max(rawBounds.size.x, Mathf.Max(rawBounds.size.y, rawBounds.size.z));
                float targetMax = Mathf.Max(targetSize.x, Mathf.Max(targetSize.y, targetSize.z));
                float s = targetMax / (maxDim > 0.001f ? maxDim : 1f);
                visual.transform.localScale = Vector3.one * s;
            }
            else
            {
                float sx = targetSize.x / (rawBounds.size.x > 0.001f ? rawBounds.size.x : 1f);
                float sy = targetSize.y / (rawBounds.size.y > 0.001f ? rawBounds.size.y : 1f);
                float sz = targetSize.z / (rawBounds.size.z > 0.001f ? rawBounds.size.z : 1f);
                visual.transform.localScale = new Vector3(sx, sy, sz);
            }

            // 6. Tính Bounds sau khi scale để căn chuẩn vị trí tiếp đất (Y=0) và tâm (X=0, Z=0)
            Bounds scaledBounds = CalculateBounds(visual);
            visual.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z
            );
        }

        // 7. Thiết lập Resource Item nếu là Prefab tài nguyên
        if (isResourceItem)
        {
            root.tag = "ResourceItem";

            var box = root.AddComponent<BoxCollider>();
            box.size = targetSize;
            box.center = new Vector3(0f, targetSize.y * 0.5f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var holder = root.AddComponent<ResourceItemHolder>();
            if (resourceData != null) holder.Data = resourceData;
        }

        // 8. Lưu Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (isResourceItem && resourceData != null && savedPrefab != null)
        {
            resourceData.itemPrefab = savedPrefab;
            EditorUtility.SetDirty(resourceData);
        }

        Debug.Log($"[FactoryAssetMaster] Đã đóng gói Normalized Prefab: {prefabPath}");
    }

    private static Bounds CalculateBounds(GameObject obj)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!hasBounds)
            {
                b = r.bounds;
                hasBounds = true;
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }
        return b;
    }

    private static Material FindModelMaterial(string modelPath)
    {
        // 1. Resources và Pallet dùng chung bảng màu low-poly Mat_ResourceBits.mat
        if (modelPath.Contains("Resources") || modelPath.Contains("Pallet"))
        {
            var resMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_ResourceBits.mat");
            if (resMat != null) return resMat;
        }

        // 2. Meshy models: Material.001.mat trong cùng thư mục
        string dir = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(dir))
        {
            string directMat = Path.Combine(dir, "Material.001.mat").Replace('\\', '/');
            if (File.Exists(directMat))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(directMat);
                if (mat != null) return mat;
            }

            // 3. Tripo models: Materials/<name>.mat
            string subDir = Path.Combine(dir, "Materials").Replace('\\', '/');
            if (Directory.Exists(subDir))
            {
                var subMatFiles = Directory.GetFiles(subDir, "*.mat");
                if (subMatFiles.Length > 0)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(subMatFiles[0].Replace('\\', '/'));
                    if (mat != null) return mat;
                }
            }

            // Bất kỳ file .mat nào trong thư mục model
            var matFiles = Directory.GetFiles(dir, "*.mat");
            if (matFiles.Length > 0)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matFiles[0].Replace('\\', '/'));
                if (mat != null) return mat;
            }
        }

        return null;
    }

    // =========================================================================
    // 4. KẾT NỐI SCRIPTABLE OBJECTS
    // =========================================================================
    private static void LinkAllScriptableObjects()
    {
        // 1. Resources
        var types = new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Copper, ResourceType.Iron, ResourceType.Silver, ResourceType.Gold };
        foreach (var t in types)
        {
            string prefabPath = $"{PREFAB_RES_DIR}/{t}ItemPrefab.prefab";
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (itemPrefab == null) continue;

            string[] soNames = { $"Resource_{t}.asset", $"Resource_{t}_T2.asset", $"Resource_{t}_T3.asset" };
            foreach (var soName in soNames)
            {
                var so = AssetDatabase.LoadAssetAtPath<ResourceDataSO>($"{SO_RES_DIR}/{soName}");
                if (so != null)
                {
                    so.itemPrefab = itemPrefab;
                    EditorUtility.SetDirty(so);
                }
            }
        }

        // 2. Machines
        var oreT1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Ore_Tier1.prefab");
        var oreT2 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Ore_Tier2.prefab");
        var oreT3 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Ore_Tier3.prefab");

        var woodT1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Wood_Tier1.prefab");
        var woodT2 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Wood_Tier2.prefab");
        var woodT3 = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_MACH_DIR}/Visual_Wood_Tier3.prefab");

        foreach (var t in types)
        {
            string configPath = $"{SO_MACH_DIR}/MachineConfig_{t}.asset";
            var mConfig = AssetDatabase.LoadAssetAtPath<MachineConfigSO>(configPath);
            if (mConfig == null) continue;

            if (t == ResourceType.Wood)
            {
                mConfig.tier1VisualPrefab = woodT1;
                mConfig.tier2VisualPrefab = woodT2;
                mConfig.tier3VisualPrefab = woodT3;
            }
            else
            {
                mConfig.tier1VisualPrefab = oreT1;
                mConfig.tier2VisualPrefab = oreT2;
                mConfig.tier3VisualPrefab = oreT3;
            }

            EditorUtility.SetDirty(mConfig);
        }

        Debug.Log("[FactoryAssetMaster] Đã liên kết đủ Visual Prefabs vào 6 MachineConfigSO & 18 ResourceDataSO.");
    }

    // =========================================================================
    // 5. CẬP NHẬT SCENE PROTOTYPE (ẨN CUBE & GẮN MODEL 3D)
    // =========================================================================
    private static void UpdatePrototypeScene()
    {
        string scenePath = "Assets/_Project/Scenes/PrototypeScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogWarning($"[FactoryAssetMaster] Không thể mở scene tại: {scenePath}");
            return;
        }

        // Bước 1: Tạo Địa hình Low-Poly Faceted nhấp nhô & hồ nước
        LowPolyTerrainBuilder.GenerateTerrainForScene(scene);

        var safePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/BankSafe_Visual.prefab");
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/ControlPanel_Visual.prefab");
        var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/ConveyorBelt_Visual.prefab");
        var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/StoragePallet_Visual.prefab");
        var workshopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Workshop.prefab");
        var cavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Cave.prefab");
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_ENV_DIR}/Environment_Tree.prefab");

        // 1. MoneyVault_Safe
        var vault = GameObject.Find("MoneyVault_Safe");
        if (vault != null)
        {
            DisableMeshRenderer(vault);
            AttachVisualChild(vault, safePrefab, "Visual_Safe", new Vector3(0f, -0.6f, 0f), Quaternion.identity);
        }

        // 2. UpgradeStation_Terminal
        var terminal = GameObject.Find("UpgradeStation_Terminal");
        if (terminal != null)
        {
            DisableMeshRenderer(terminal);
            AttachVisualChild(terminal, panelPrefab, "Visual_Terminal", new Vector3(0f, -0.6f, 0f), Quaternion.identity);
        }

        // 3. 6 Cụm Dây Chuyền Sản Xuất
        var types = new[] {
            new { type = ResourceType.Wood, isLeft = true },
            new { type = ResourceType.Stone, isLeft = true },
            new { type = ResourceType.Copper, isLeft = true },
            new { type = ResourceType.Iron, isLeft = false },
            new { type = ResourceType.Silver, isLeft = false },
            new { type = ResourceType.Gold, isLeft = false }
        };

        foreach (var def in types)
        {
            // StoragePad
            var pad = GameObject.Find($"StoragePad_{def.type}");
            if (pad != null)
            {
                DisableMeshRenderer(pad);
                AttachVisualChild(pad, palletPrefab, "Visual_Pallet", new Vector3(0f, -0.15f, 0f), Quaternion.identity);
            }

            // Conveyor
            var conv = GameObject.Find($"Conveyor_{def.type}");
            if (conv != null)
            {
                DisableMeshRenderer(conv);
                AttachVisualChild(conv, conveyorPrefab, "Visual_Conveyor", new Vector3(0f, -0.25f, 0f), Quaternion.identity);
            }

            // Machine
            var mach = GameObject.Find($"Machine_{def.type}");
            if (mach != null)
            {
                DisableMeshRenderer(mach);
                string tier1Path = def.type == ResourceType.Wood
                    ? $"{PREFAB_MACH_DIR}/Visual_Wood_Tier1.prefab"
                    : $"{PREFAB_MACH_DIR}/Visual_Ore_Tier1.prefab";
                var t1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(tier1Path);
                Quaternion rot = Quaternion.Euler(0f, def.isLeft ? 90f : -90f, 0f);
                AttachVisualChild(mach, t1Prefab, "Visual_MachineTier1", new Vector3(0f, -1f, 0f), rot);
            }
        }

        // 4. Dọn sạch triệt để toàn bộ Decor cũ (_EnvironmentDecor) tránh trùng lặp
        LowPolyModelPlacer.PurgeAllOldObsoleteModels();

        // 5. Áp dụng Hệ Thống Bản Đồ Diorama Mới (Địa hình + Đường 3D + Bố trí Model)
        LowPolyTerrainBuilder.GenerateTerrainForScene(scene);
        LowPolyRoadBuilder.GenerateRoadsForScene(scene);
        LowPolyModelPlacer.PlaceModelsForScene(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FactoryAssetMaster] Đã nâng cấp toàn bộ PrototypeScene.unity với Models 3D & Bản đồ Diorama chuẩn mực!");
    }

    // =========================================================================
    // 6. CẬP NHẬT SCENE TRƯNG BÀY (SAMPLE SCENE SHOWROOM)
    // =========================================================================
    private static void UpdateSampleSceneShowroom()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogWarning($"[FactoryAssetMaster] Không tìm thấy SampleScene tại: {scenePath}");
            return;
        }

        // Bước 1: Tạo Địa hình Low-Poly Faceted cho SampleScene
        LowPolyTerrainBuilder.GenerateTerrainForScene(scene);

        // Định vị 10 model thành 2 hàng ngay ngắn, chuẩn kích thước, mặt hướng về phía trước
        var showroomItems = new[]
        {
            // Hàng 1 (Z = 3.5m)
            new { name = "SM_Machine_Ore_Tier1", path = $"{PREFAB_MACH_DIR}/Visual_Ore_Tier1.prefab", pos = new Vector3(-6f, 0f, 3.5f), rot = 0f },
            new { name = "SM_Machine_Ore_Tier2", path = $"{PREFAB_MACH_DIR}/Visual_Ore_Tier2.prefab", pos = new Vector3(-2f, 0f, 3.5f), rot = 0f },
            new { name = "SM_Machine_Ore_Tier3", path = $"{PREFAB_MACH_DIR}/Visual_Ore_Tier3.prefab", pos = new Vector3(2f, 0f, 3.5f), rot = 0f },
            new { name = "SM_Conveyor_Belt", path = $"{PREFAB_ENV_DIR}/ConveyorBelt_Visual.prefab", pos = new Vector3(6f, 0f, 3.5f), rot = 90f },

            // Hàng 2 (Z = -2.5m)
            new { name = "SM_Bank_Safe", path = $"{PREFAB_ENV_DIR}/BankSafe_Visual.prefab", pos = new Vector3(-7f, 0f, -2.5f), rot = 0f },
            new { name = "SM_Control_Panel", path = $"{PREFAB_ENV_DIR}/ControlPanel_Visual.prefab", pos = new Vector3(-3.5f, 0f, -2.5f), rot = 0f },
            new { name = "SM_Storage_Pallet", path = $"{PREFAB_ENV_DIR}/StoragePallet_Visual.prefab", pos = new Vector3(0f, 0f, -2.5f), rot = 0f },
            new { name = "SM_Workshop_Shed", path = $"{PREFAB_ENV_DIR}/Environment_Workshop.prefab", pos = new Vector3(4f, 0f, -2.5f), rot = 0f },
            new { name = "SM_Mine_Pit", path = $"{PREFAB_ENV_DIR}/Environment_Cave.prefab", pos = new Vector3(9f, 0f, -2.5f), rot = 0f },
            new { name = "SM_Pine_Tree", path = $"{PREFAB_ENV_DIR}/Environment_Tree.prefab", pos = new Vector3(-10f, 0f, 0f), rot = 0f },
        };

        var rootGallery = GameObject.Find("_Showroom_Gallery");
        if (rootGallery == null)
        {
            rootGallery = new GameObject("_Showroom_Gallery");
        }

        // Xóa các object cũ do người dùng kéo vào mà chưa chuẩn hóa
        string[] oldNames = {
            "SM_Machine_Ore_Tier1", "SM_Machine_Ore_Tier2", "SM_Machine_Ore_Tier3",
            "SM_Conveyor_Belt", "SM_Bank_Safe", "SM_Control_Panel", "SM_Workshop_Shed",
            "SM_Mine_Pit", "SM_Pine_Tree"
        };
        foreach (var oldName in oldNames)
        {
            var oldObj = GameObject.Find(oldName);
            if (oldObj != null) Object.DestroyImmediate(oldObj);
        }

        while (rootGallery.transform.childCount > 0)
        {
            Object.DestroyImmediate(rootGallery.transform.GetChild(0).gameObject);
        }

        // Instantiates các model đã được chuẩn hóa
        foreach (var item in showroomItems)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.path);
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab, item.pos, Quaternion.Euler(0f, item.rot, 0f), rootGallery.transform);
                instance.name = item.name;
            }
        }

        // Đặt Camera bao quát góc nhìn triển lãm
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 10f, -14f);
            cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FactoryAssetMaster] Đã cập nhật SampleScene.unity thành Triển Lãm Showroom Model chuẩn hóa!");
    }

    private static void DisableMeshRenderer(GameObject target)
    {
        if (target == null) return;
        var mr = target.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = false;
        }
    }

    private static void AttachVisualChild(GameObject parent, GameObject prefab, string childName, Vector3 localPos, Quaternion localRot)
    {
        if (parent == null || prefab == null) return;

        // Xóa mọi visual cũ hoặc trùng lặp
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            var child = parent.transform.GetChild(i);
            if (child.name.StartsWith("Visual_") || child.name.Contains("Model") || child.name.Contains("Tier"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        var instance = Object.Instantiate(prefab, parent.transform);
        instance.name = childName;
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;
    }
}
