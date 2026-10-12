using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Tạo 6 Prefab Cụm Dây Chuyền Sản Xuất Modular Gọn Gàng (Modular Production Line Prefabs).
/// Mỗi Prefab là một khối thống nhất (Máy + Băng chuyền ngắn 3.5m + Bệ chứa Pallet) đã nối sẵn 100% dây logic.
/// Người dùng chỉ cần kéo thả Prefab vào Scene và xoay/đặt ở bất kỳ góc nào tùy thích.
/// </summary>
public static class ProductionLinePrefabGenerator
{
    private const string PREFAB_DIR = "Assets/_Project/Prefabs/ProductionLines";
    private const string ENV_PREFAB_DIR = "Assets/_Project/Prefabs/Environment";
    private const string MACH_PREFAB_DIR = "Assets/_Project/Prefabs/Machines";
    private const string SO_MACH_DIR = "Assets/_Project/ScriptableObjects/Machines";
    private const string SO_RES_DIR = "Assets/_Project/ScriptableObjects/Resources";

    // ĐÃ TẮT TỰ ĐỘNG CHẠY KHI RELOAD: Bảo toàn 100% vị trí trong Scene.
    // Chỉ chạy khi người dùng chủ động click menu "Factory/Prefabs/Create Modular Production Line Prefabs".

    [MenuItem("Factory/Prefabs/Create Modular Production Line Prefabs", priority = 5)]
    public static void ManualGenerate()
    {
        GenerateAllPrefabs();
        EditorUtility.DisplayDialog(
            "Đã Tạo Xong 6 Prefab Dây Chuyền Modular!",
            "Bạn có thể vào thư mục:\nAssets/_Project/Prefabs/ProductionLines/\n\n" +
            "Kéo thả trực tiếp các Prefab (Gỗ, Đá, Đồng, Sắt, Bạc, Vàng) vào Scene để tự do sắp xếp bất kỳ vị trí nào bạn thích!",
            "Tuyệt vời!"
        );
    }

    public static void GenerateAllPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PREFAB_DIR))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "ProductionLines");
        }

        var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ENV_PREFAB_DIR}/StoragePallet_Visual.prefab");
        var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ENV_PREFAB_DIR}/ConveyorBelt_Visual.prefab");

        var types = new[]
        {
            new { type = "Wood", name = "Gỗ", isDefault = true },
            new { type = "Stone", name = "Đá", isDefault = false },
            new { type = "Copper", name = "Đồng", isDefault = false },
            new { type = "Iron", name = "Sắt", isDefault = false },
            new { type = "Silver", name = "Bạc", isDefault = false },
            new { type = "Gold", name = "Vàng", isDefault = false }
        };

        foreach (var def in types)
        {
            try
            {
                CreateSingleLinePrefab(def.type, def.name, def.isDefault, palletPrefab, conveyorPrefab);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProductionLinePrefabs] Lỗi khi tạo Prefab cho {def.type}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProductionLinePrefabs] Đã tạo thành công toàn bộ 6 Prefab Modular Production Lines tại Assets/_Project/Prefabs/ProductionLines/!");
    }

    private static void CreateSingleLinePrefab(
        string type, string displayName, bool isDefaultUnlocked,
        GameObject palletPrefab, GameObject conveyorPrefab)
    {
        string prefabPath = $"{PREFAB_DIR}/ProductionLine_{type}.prefab";

        var config = AssetDatabase.LoadAssetAtPath<MachineConfigSO>($"{SO_MACH_DIR}/Machine_{type}_Config.asset");
        string tier1Path = $"{MACH_PREFAB_DIR}/Visual_Ore_Tier1.prefab";
        var t1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(tier1Path);

        // 1. Root Object
        GameObject root = new GameObject($"ProductionLine_{type}");
        var prodLine = root.AddComponent<ProductionLine>();

        // Thiết kế bố cục thẳng hàng gọn gàng:
        // Bệ chứa: (0, 0, 0)
        // Băng chuyền: (0, 0.25, 1.85)
        // Máy nhả: (0, 1.0, 3.8)

        // 2. StoragePad (Chuẩn tỉ lệ 1:1 chống méo mó vật phẩm)
        GameObject storageObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        storageObj.name = $"StoragePad_{type}";
        storageObj.transform.SetParent(root.transform, false);
        storageObj.transform.localPosition = new Vector3(0f, 0f, -1.25f);
        storageObj.transform.localScale = Vector3.one;
        var padMr = storageObj.GetComponent<MeshRenderer>();
        if (padMr != null) padMr.enabled = false;

        if (palletPrefab != null)
        {
            var palInstance = Object.Instantiate(palletPrefab, storageObj.transform, false);
            palInstance.name = "Visual_Pallet";
            palInstance.transform.localPosition = Vector3.zero;

            var visChild = palInstance.transform.Find("Visual");
            if (visChild != null)
            {
                visChild.localPosition = Vector3.zero;
                visChild.localScale = new Vector3(1.42f, 1.42f, 1.42f);
                var modelChild = visChild.Find("Model");
                if (modelChild != null)
                {
                    modelChild.localPosition = Vector3.zero;
                    modelChild.localScale = new Vector3(0.65f, 0.65f, 0.65f);
                }
            }
        }

        var padCol = storageObj.GetComponent<BoxCollider>();
        padCol.isTrigger = true;
        padCol.center = new Vector3(0f, 0.75f, 0f);
        padCol.size = new Vector3(1.5f, 1.5f, 1.5f);

        GameObject slotRootObj = new GameObject("ItemSlotRoot");
        slotRootObj.transform.SetParent(storageObj.transform, false);
        slotRootObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        var storagePad = storageObj.AddComponent<StoragePad>();
        storagePad.SetCapacity(10);

        var spSO = new SerializedObject(storagePad);
        var rProp = spSO.FindProperty("itemSlotRoot");
        if (rProp != null) rProp.objectReferenceValue = slotRootObj.transform;
        var oProp = spSO.FindProperty("itemOffset");
        if (oProp != null) oProp.vector3Value = new Vector3(0.28f, 0.35f, 0.28f);
        spSO.ApplyModifiedProperties();

        // 3. ConveyorBelt (Dài 3.2m thon gọn nối từ máy vào bệ chứa theo chuẩn Copper)
        GameObject convObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        convObj.name = $"Conveyor_{type}";
        convObj.transform.SetParent(root.transform, false);
        convObj.transform.localPosition = new Vector3(0f, 0.25f, 1.85f);
        convObj.transform.localScale = new Vector3(1.4f, 0.5f, 3.2f);
        var convMr = convObj.GetComponent<MeshRenderer>();
        if (convMr != null) convMr.enabled = false;

        if (conveyorPrefab != null)
        {
            var cInstance = Object.Instantiate(conveyorPrefab, convObj.transform, false);
            cInstance.name = "Visual_Conveyor";
            cInstance.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            cInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            cInstance.transform.localScale = new Vector3(0.5f, 1f, 1f);

            var visChild = cInstance.transform.Find("Visual");
            if (visChild != null)
            {
                visChild.localPosition = new Vector3(0f, 0.25f, 0f);
                visChild.localScale = new Vector3(3.68f, 1.02f, 1.73f);
                var modelChild = visChild.Find("Model");
                if (modelChild != null)
                {
                    modelChild.localPosition = new Vector3(0.12773855f, 0.00056135183f, -0.00028387684f);
                    modelChild.localRotation = Quaternion.Euler(270f, 0f, 0f);
                    modelChild.localScale = new Vector3(26.71089f, 44.06257f, 182.49254f);
                }
            }
        }

        var convCol = convObj.GetComponent<BoxCollider>();
        convCol.isTrigger = true;
        convCol.center = new Vector3(0f, 1.0f, -0.04f);
        convCol.size = new Vector3(1.0f, 3.0f, 1.32f);

        var conveyor = convObj.AddComponent<ConveyorBelt>();
        conveyor.TargetStoragePad = storagePad;

        var convSO = new SerializedObject(conveyor);
        var convPadProp = convSO.FindProperty("targetStoragePad");
        if (convPadProp != null) convPadProp.objectReferenceValue = storagePad;
        var autoOrientProp = convSO.FindProperty("autoOrientTowardsPad");
        if (autoOrientProp != null) autoOrientProp.boolValue = true;
        convSO.ApplyModifiedProperties();

        // 4. MachineController
        GameObject machineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        machineObj.name = $"Machine_{type}";
        machineObj.transform.SetParent(root.transform, false);
        machineObj.transform.localPosition = new Vector3(0f, 1.0f, 3.8f);
        machineObj.transform.localScale = new Vector3(1.8f, 2f, 1.8f);
        var machMr = machineObj.GetComponent<MeshRenderer>();
        if (machMr != null) machMr.enabled = false;

        if (t1Prefab != null)
        {
            var mViz = Object.Instantiate(t1Prefab, machineObj.transform, false);
            mViz.name = "Visual_MachineTier1";
            mViz.transform.localPosition = new Vector3(0f, -1f, 0f);
            mViz.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            mViz.transform.localScale = Vector3.one;

            var visChild = mViz.transform.Find("Visual");
            if (visChild != null)
            {
                visChild.localPosition = new Vector3(0.01f, 0.69f, 0f);
                visChild.localScale = new Vector3(1.16f, 1.16f, 1.16f);
                var modelChild = visChild.Find("Model");
                if (modelChild != null)
                {
                    modelChild.localPosition = new Vector3(0f, 0.32f, -0.36f);
                    modelChild.localRotation = Quaternion.Euler(270f, 0f, 0f);
                    modelChild.localScale = new Vector3(100f, 100f, 100f);
                }
            }
        }

        var machCol = machineObj.GetComponent<BoxCollider>();
        machCol.isTrigger = false;
        machCol.center = new Vector3(0f, 0.03f, 0.56f);
        machCol.size = new Vector3(1.5f, 0.95f, 1.66f);

        GameObject spawnPt = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(machineObj.transform, false);
        spawnPt.transform.localPosition = new Vector3(0f, -0.275f, -0.105555475f);

        var machine = machineObj.AddComponent<MachineController>();
        machine.Initialize(config, conveyor, storagePad);

        var machSO = new SerializedObject(machine);
        var mCfgProp = machSO.FindProperty("machineConfig");
        if (mCfgProp != null) mCfgProp.objectReferenceValue = config;
        var mConvProp = machSO.FindProperty("targetConveyor");
        if (mConvProp != null) mConvProp.objectReferenceValue = conveyor;
        var mPadProp = machSO.FindProperty("targetStoragePad");
        if (mPadProp != null) mPadProp.objectReferenceValue = storagePad;
        var mSpawnProp = machSO.FindProperty("spawnPoint");
        if (mSpawnProp != null) mSpawnProp.objectReferenceValue = spawnPt.transform;
        machSO.ApplyModifiedProperties();

        // 5. Nối dữ liệu vào ProductionLine Root trực tiếp qua C#
        var slot = new MachineSlot
        {
            slotID = $"{type}_Slot01",
            slotName = $"Máy {displayName}",
            machineConfig = config,
            spawnPoint = machineObj.transform,
            targetConveyor = conveyor,
            isUnlockedByDefault = isDefaultUnlocked,
            isUnlocked = isDefaultUnlocked,
            currentTier = 1,
            currentSpeedLevel = 0,
            spawnedMachineInstance = machine
        };
        prodLine.AddSlotDirectly(slot);

        var lineSO = new SerializedObject(prodLine);
        var cIdProp = lineSO.FindProperty("clusterID");
        if (cIdProp != null) cIdProp.stringValue = $"Cluster_{type}";
        var linePadProp = lineSO.FindProperty("storagePad");
        if (linePadProp != null) linePadProp.objectReferenceValue = storagePad;
        lineSO.ApplyModifiedProperties();

        // 6. Lưu thành Prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
    }
}
