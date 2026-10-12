using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Công cụ Editor nâng cấp tự động các Prefab tài nguyên từ Khối Hộp (Cube Prototype)
/// sang các Model 3D chuyên nghiệp (KayKit / Procedural FBX).
/// </summary>
public static class ResourceModelUpgrader
{
    private const string PREFAB_DIR = "Assets/_Project/Prefabs/Resources";
    private const string MODEL_DIR = "Assets/_Project/Models/Resources";
    private const string SO_DIR = "Assets/_Project/ScriptableObjects/Resources";
    private const string MAT_DIR = "Assets/_Project/Materials";

    private struct ResourceModelMapping
    {
        public string resourceName;
        public ResourceType resourceType;
        public string primaryFbxName;   // File model từ gói KayKit
        public string fallbackFbxName;  // File model tạo từ script Blender
        public string materialName;
        public string prefabFileName;
        public Vector3 visualScale;
        public Vector3 colliderSize;
        public Vector3 colliderCenter;
    }

    private static readonly ResourceModelMapping[] Mappings = new[]
    {
        new ResourceModelMapping
        {
            resourceName = "Gỗ",
            resourceType = ResourceType.Wood,
            primaryFbxName = "Wood_Log_A.fbx",
            fallbackFbxName = "SM_Item_Wood.fbx",
            materialName = "Mat_Wood.mat",
            prefabFileName = "WoodItemPrefab.prefab",
            visualScale = new Vector3(0.7f, 0.7f, 0.7f),
            colliderSize = new Vector3(0.5f, 0.35f, 0.5f),
            colliderCenter = new Vector3(0f, 0.175f, 0f)
        },
        new ResourceModelMapping
        {
            resourceName = "Đá",
            resourceType = ResourceType.Stone,
            primaryFbxName = "Stone_Chunks_Small.fbx",
            fallbackFbxName = "SM_Item_Stone.fbx",
            materialName = "Mat_Stone.mat",
            prefabFileName = "StoneItemPrefab.prefab",
            visualScale = new Vector3(0.8f, 0.8f, 0.8f),
            colliderSize = new Vector3(0.5f, 0.35f, 0.5f),
            colliderCenter = new Vector3(0f, 0.175f, 0f)
        },
        new ResourceModelMapping
        {
            resourceName = "Đồng",
            resourceType = ResourceType.Copper,
            primaryFbxName = "Copper_Bar.fbx",
            fallbackFbxName = "SM_Item_Copper.fbx",
            materialName = "Mat_Copper.mat",
            prefabFileName = "CopperItemPrefab.prefab",
            visualScale = new Vector3(0.75f, 0.75f, 0.75f),
            colliderSize = new Vector3(0.5f, 0.3f, 0.5f),
            colliderCenter = new Vector3(0f, 0.15f, 0f)
        },
        new ResourceModelMapping
        {
            resourceName = "Sắt",
            resourceType = ResourceType.Iron,
            primaryFbxName = "Iron_Bar.fbx",
            fallbackFbxName = "SM_Item_Iron.fbx",
            materialName = "Mat_Iron.mat",
            prefabFileName = "IronItemPrefab.prefab",
            visualScale = new Vector3(0.75f, 0.75f, 0.75f),
            colliderSize = new Vector3(0.5f, 0.3f, 0.5f),
            colliderCenter = new Vector3(0f, 0.15f, 0f)
        },
        new ResourceModelMapping
        {
            resourceName = "Bạc",
            resourceType = ResourceType.Silver,
            primaryFbxName = "Silver_Bar.fbx",
            fallbackFbxName = "SM_Item_Silver.fbx",
            materialName = "Mat_Silver.mat",
            prefabFileName = "SilverItemPrefab.prefab",
            visualScale = new Vector3(0.75f, 0.75f, 0.75f),
            colliderSize = new Vector3(0.5f, 0.3f, 0.5f),
            colliderCenter = new Vector3(0f, 0.15f, 0f)
        },
        new ResourceModelMapping
        {
            resourceName = "Vàng",
            resourceType = ResourceType.Gold,
            primaryFbxName = "Gold_Bar.fbx",
            fallbackFbxName = "SM_Item_Gold.fbx",
            materialName = "Mat_Gold.mat",
            prefabFileName = "GoldItemPrefab.prefab",
            visualScale = new Vector3(0.75f, 0.75f, 0.75f),
            colliderSize = new Vector3(0.5f, 0.3f, 0.5f),
            colliderCenter = new Vector3(0f, 0.15f, 0f)
        }
    };

    [MenuItem("Factory/3D Models/Apply 3D Models to Resource Prefabs", priority = 10)]
    public static void UpgradeAllResourcePrefabs()
    {
        Debug.Log("[ResourceModelUpgrader] Bắt đầu nâng cấp Prefab tài nguyên sang Model 3D...");

        if (!AssetDatabase.IsValidFolder(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
            AssetDatabase.Refresh();
        }

        int successCount = 0;

        foreach (var mapping in Mappings)
        {
            // 1. Tìm model FBX (ưu tiên KayKit, fallback sang Blender procedural)
            string fbxPath = $"{MODEL_DIR}/{mapping.primaryFbxName}";
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                fbxPath = $"{MODEL_DIR}/{mapping.fallbackFbxName}";
                fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            }

            if (fbxAsset == null)
            {
                Debug.LogWarning($"[ResourceModelUpgrader] Không tìm thấy file FBX cho {mapping.resourceName} tại {fbxPath}!");
                continue;
            }

            // 2. Tìm Material
            string matPath = $"{MAT_DIR}/{mapping.materialName}";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            // 3. Tìm ScriptableObject ResourceDataSO
            string soPath = $"{SO_DIR}/Resource_{mapping.resourceType}.asset";
            ResourceDataSO resourceData = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(soPath);

            // 4. Tạo cấu trúc Prefab mới
            string prefabPath = $"{PREFAB_DIR}/{mapping.prefabFileName}";
            GameObject root = new GameObject(mapping.prefabFileName.Replace(".prefab", ""));
            root.tag = "ResourceItem";

            // Collider & Physics
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = mapping.colliderSize;
            col.center = mapping.colliderCenter;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // ResourceItemHolder
            ResourceItemHolder holder = root.AddComponent<ResourceItemHolder>();
            if (resourceData != null)
            {
                holder.Data = resourceData;
            }

            // Model con (Visual)
            GameObject visual = Object.Instantiate(fbxAsset, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = mapping.visualScale;

            // Áp dụng Material nếu có
            if (mat != null)
            {
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.sharedMaterial = mat;
                }
            }

            // Gỡ bỏ bất kỳ Collider thừa bên trong file FBX con (để tránh xung đột với root collider)
            Collider[] childColliders = visual.GetComponentsInChildren<Collider>();
            foreach (var c in childColliders)
            {
                Object.DestroyImmediate(c);
            }

            // 5. Lưu thành Prefab Asset
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            // 6. Gán ngược lại vào ResourceDataSO
            if (resourceData != null && savedPrefab != null)
            {
                resourceData.itemPrefab = savedPrefab;
                EditorUtility.SetDirty(resourceData);
            }

            // Cập nhật các Tier 2 và Tier 3 nếu có
            string soPathT2 = $"{SO_DIR}/Resource_{mapping.resourceType}_T2.asset";
            ResourceDataSO resT2 = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(soPathT2);
            if (resT2 != null)
            {
                resT2.itemPrefab = savedPrefab;
                EditorUtility.SetDirty(resT2);
            }

            string soPathT3 = $"{SO_DIR}/Resource_{mapping.resourceType}_T3.asset";
            ResourceDataSO resT3 = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(soPathT3);
            if (resT3 != null)
            {
                resT3.itemPrefab = savedPrefab;
                EditorUtility.SetDirty(resT3);
            }

            successCount++;
            Debug.Log($"[ResourceModelUpgrader] Đã tạo thành công Prefab 3D: {mapping.prefabFileName} (Model: {fbxAsset.name})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ResourceModelUpgrader] HOÀN TẤT! Đã nâng cấp {successCount}/{Mappings.Length} loại tài nguyên sang Model 3D thực thụ.");
    }
}
