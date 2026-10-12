using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Công cụ chuẩn hóa toàn bộ 6 dây chuyền sản xuất trong Scene và thư mục Prefabs
/// dựa trên mẫu chuẩn MÁY COPPER (Copper Machine) do người dùng tùy chỉnh.
/// Đảm bảo:
/// 1. Giữ nguyên vị trí/góc xoay mà người dùng đã xếp trên bản đồ.
/// 2. Băng chuyền thon gọn, mỏng nhẹ, vừa khít từ miệng máy đến bệ pallet.
/// 3. Bệ chứa Pallet nhỏ gọn (tỉ lệ 0.25), hitbox vừa vặn.
/// 4. Hitbox máy, hitbox băng tải và hitbox bệ chứa chính xác 100%.
/// 5. Máy Gỗ (Wood) chuyển sang dùng model Lò luyện (Visual_Ore_Tier1) đồng bộ với Copper.
/// 6. Cấu hình isUnlockedByDefault cho máy khởi đầu (Wood & Copper) để khi xóa save máy không bị biến mất.
/// 7. Tự động đồng bộ hóa ngược lại cả 6 file Prefab trong Assets/_Project/Prefabs/ProductionLines/.
/// </summary>
public static class StandardizeProductionLinesTool
{
    private const string PREFAB_DIR = "Assets/_Project/Prefabs/ProductionLines";
    private const string ENV_PREFAB_DIR = "Assets/_Project/Prefabs/Environment";
    private const string MACH_PREFAB_DIR = "Assets/_Project/Prefabs/Machines";
    private const string SO_MACH_DIR = "Assets/_Project/ScriptableObjects/Machines";

    // ĐÃ TẮT TỰ ĐỘNG CHẠY KHI RELOAD: Bảo toàn 100% vị trí trong Scene.
    // Chỉ chạy khi người dùng chủ động click menu "Factory/Production Lines/Standardize All Lines from Copper Sample".

    [MenuItem("Factory/Production Lines/Standardize All Lines from Copper Sample", priority = 1)]
    public static void ManualStandardize()
    {
        ExecuteStandardization();
        EditorUtility.DisplayDialog(
            "Đã Chuẩn Hóa Thành Công 6 Dây Chuyền!",
            "Toàn bộ 6 máy (Gỗ, Đá, Đồng, Sắt, Bạc, Vàng) đã được đồng bộ chuẩn kích thước, hitbox và băng tải thon gọn theo mẫu MÁY COPPER.\n\n" +
            "- Vị trí bạn đã xếp trên Scene được giữ nguyên vẹn.\n" +
            "- Máy Gỗ đã được đổi sang model Lò luyện.\n" +
            "- Máy Đồng & Gỗ được mở khóa mặc định (xóa save không còn bị mất máy).\n" +
            "- Toàn bộ 6 file Prefab trong Assets/_Project/Prefabs/ProductionLines/ đã được cập nhật.",
            "Tuyệt vời!"
        );
    }

    public static void ExecuteStandardization()
    {
        Debug.Log("[StandardizeTool] Bắt đầu chuẩn hóa 6 dây chuyền sản xuất theo mẫu Máy Copper...");

        // 1. Cập nhật Machine_Wood_Config.asset sang model Lò luyện
        UpdateWoodConfigSO();

        // 2. Chuẩn hóa các GameObjects trong Scene đang mở
        StandardizeSceneObjects();

        // 3. Cập nhật toàn bộ 6 file Prefabs trong thư mục Prefabs/ProductionLines
        UpdatePrefabAssets();

        // 4. Lưu Scene và AssetDatabase
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 5. Ghi log kiểm tra mới
        SceneModelInspector.Inspect();

        Debug.Log("[StandardizeTool] HOÀN TẤT CHUẨN HÓA 6 DÂY CHUYỀN VÀ PREFABS!");
    }

    private static void UpdateWoodConfigSO()
    {
        string woodCfgPath = $"{SO_MACH_DIR}/Machine_Wood_Config.asset";
        var woodCfg = AssetDatabase.LoadAssetAtPath<MachineConfigSO>(woodCfgPath);
        if (woodCfg != null)
        {
            var t1Ore = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACH_PREFAB_DIR}/Visual_Ore_Tier1.prefab");
            var t2Ore = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACH_PREFAB_DIR}/Visual_Ore_Tier2.prefab");
            var t3Ore = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACH_PREFAB_DIR}/Visual_Ore_Tier3.prefab");

            woodCfg.tier1VisualPrefab = t1Ore;
            woodCfg.tier2VisualPrefab = t2Ore != null ? t2Ore : t1Ore;
            woodCfg.tier3VisualPrefab = t3Ore != null ? t3Ore : t1Ore;
            EditorUtility.SetDirty(woodCfg);
            Debug.Log("[StandardizeTool] Đã cập nhật Machine_Wood_Config sang model Lò luyện (Visual_Ore_Tier1/2/3).");
        }
    }

    private static void StandardizeSceneObjects()
    {
        string[] lineTypes = new[] { "Wood", "Stone", "Copper", "Iron", "Silver", "Gold" };

        var oreT1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACH_PREFAB_DIR}/Visual_Ore_Tier1.prefab");

        foreach (var type in lineTypes)
        {
            string lineName = $"ProductionLine_{type}";
            var lineObj = GameObject.Find(lineName);
            if (lineObj == null)
            {
                Debug.LogWarning($"[StandardizeTool] Không tìm thấy {lineName} trong Scene!");
                continue;
            }

            Debug.Log($"[StandardizeTool] Đang chuẩn hóa {lineName} tại pos={lineObj.transform.position}...");

            var prodLine = lineObj.GetComponent<ProductionLine>();
            if (prodLine == null) prodLine = lineObj.AddComponent<ProductionLine>();

            var config = AssetDatabase.LoadAssetAtPath<MachineConfigSO>($"{SO_MACH_DIR}/Machine_{type}_Config.asset");

            // --- A. CHUẨN HÓA STORAGE PAD ---
            var storageObj = lineObj.transform.Find($"StoragePad_{type}")?.gameObject;
            if (storageObj != null)
            {
                storageObj.transform.localPosition = new Vector3(0f, 0f, -1.25f);
                storageObj.transform.localRotation = Quaternion.identity;
                storageObj.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);

                var padCol = storageObj.GetComponent<BoxCollider>();
                if (padCol != null)
                {
                    padCol.isTrigger = true;
                    padCol.center = new Vector3(0f, 1.13f, -0.46f);
                    padCol.size = new Vector3(0.5f, 3.0f, 0.5f);
                }

                var padMr = storageObj.GetComponent<MeshRenderer>();
                if (padMr != null) padMr.enabled = false;

                // Chuẩn hóa Visual_Pallet con
                var visPallet = storageObj.transform.Find("Visual_Pallet");
                if (visPallet != null)
                {
                    visPallet.localPosition = new Vector3(0f, -0.15f, 0f);
                    visPallet.localRotation = Quaternion.identity;
                    visPallet.localScale = Vector3.one;

                    var visChild = visPallet.Find("Visual");
                    if (visChild != null)
                    {
                        visChild.localPosition = Vector3.zero;
                        visChild.localRotation = Quaternion.identity;
                        visChild.localScale = new Vector3(1.42f, 1.42f, 1.42f);

                        var modelChild = visChild.Find("Model");
                        if (modelChild != null)
                        {
                            modelChild.localPosition = new Vector3(0f, 0.11f, -0.32f);
                            modelChild.localRotation = Quaternion.identity;
                            modelChild.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                        }
                    }
                }

                var sp = storageObj.GetComponent<StoragePad>();
                if (sp != null)
                {
                    sp.SetCapacity(10);
                }
            }

            // --- B. CHUẨN HÓA BĂNG TẢI (CONVEYOR) ---
            var convObj = lineObj.transform.Find($"Conveyor_{type}")?.gameObject;
            if (convObj != null)
            {
                convObj.transform.localPosition = new Vector3(0f, 0.25f, 1.85f);
                convObj.transform.localRotation = Quaternion.identity;
                convObj.transform.localScale = new Vector3(1.4f, 0.5f, 3.2f);

                var convCol = convObj.GetComponent<BoxCollider>();
                if (convCol != null)
                {
                    convCol.isTrigger = true;
                    convCol.center = new Vector3(0f, 1.0f, -0.04f);
                    convCol.size = new Vector3(1.0f, 3.0f, 1.32f);
                }

                var convMr = convObj.GetComponent<MeshRenderer>();
                if (convMr != null) convMr.enabled = false;

                // Chuẩn hóa Visual_Conveyor con
                var visConv = convObj.transform.Find("Visual_Conveyor");
                if (visConv != null)
                {
                    visConv.localPosition = new Vector3(0f, -0.25f, 0f);
                    visConv.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    visConv.localScale = new Vector3(0.5f, 1f, 1f);

                    var visChild = visConv.Find("Visual");
                    if (visChild != null)
                    {
                        visChild.localPosition = new Vector3(0f, 0.25f, 0f);
                        visChild.localRotation = Quaternion.identity;
                        visChild.localScale = new Vector3(3.68f, 1.02f, 1.73f);

                        var modelChild = visChild.Find("Model");
                        if (modelChild != null)
                        {
                            // Tỷ lệ vàng thon gọn lấy từ Máy Copper của bạn!
                            modelChild.localPosition = new Vector3(0.12773855f, 0.00056135183f, -0.00028387684f);
                            modelChild.localRotation = Quaternion.Euler(270f, 0f, 0f);
                            modelChild.localScale = new Vector3(26.71089f, 44.06257f, 182.49254f);
                        }
                    }
                }

                var cb = convObj.GetComponent<ConveyorBelt>();
                if (cb != null && storageObj != null)
                {
                    cb.TargetStoragePad = storageObj.GetComponent<StoragePad>();
                }
            }

            // --- C. CHUẨN HÓA MÁY (MACHINE) ---
            var machObj = lineObj.transform.Find($"Machine_{type}")?.gameObject;
            if (machObj != null)
            {
                machObj.transform.localPosition = new Vector3(0f, 1.0f, 3.8f);
                machObj.transform.localRotation = Quaternion.identity;
                machObj.transform.localScale = new Vector3(1.8f, 2.0f, 1.8f);

                var machCol = machObj.GetComponent<BoxCollider>();
                if (machCol != null)
                {
                    machCol.isTrigger = false;
                    machCol.center = new Vector3(0f, 0.03f, 0.56f);
                    machCol.size = new Vector3(1.5f, 0.95f, 1.66f);
                }

                var machMr = machObj.GetComponent<MeshRenderer>();
                if (machMr != null) machMr.enabled = false;

                // Chuẩn hóa Visual_MachineTier1
                var visMach = machObj.transform.Find("Visual_MachineTier1");
                if (type == "Wood" || (visMach != null && visMach.Find("Visual/Model") != null && visMach.Find("Visual/Model").localScale.x < 10f))
                {
                    // Nếu là máy Gỗ đang dùng nhà xưởng cũ -> thay bằng lò luyện Ore_Tier1
                    if (visMach != null)
                    {
                        UnityEngine.Object.DestroyImmediate(visMach.gameObject);
                    }
                    if (oreT1Prefab != null)
                    {
                        var newVis = UnityEngine.Object.Instantiate(oreT1Prefab, machObj.transform, false);
                        newVis.name = "Visual_MachineTier1";
                        visMach = newVis.transform;
                    }
                }

                if (visMach != null)
                {
                    visMach.localPosition = new Vector3(0f, -1.0f, 0f);
                    visMach.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    visMach.localScale = Vector3.one;

                    var visChild = visMach.Find("Visual");
                    if (visChild != null)
                    {
                        visChild.localPosition = new Vector3(0.01f, 0.69f, 0f);
                        visChild.localRotation = Quaternion.identity;
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

                // Chuẩn hóa SpawnPoint
                var spawnPt = machObj.transform.Find("SpawnPoint");
                if (spawnPt == null)
                {
                    var spGo = new GameObject("SpawnPoint");
                    spGo.transform.SetParent(machObj.transform, false);
                    spawnPt = spGo.transform;
                }
                spawnPt.localPosition = new Vector3(0f, -0.275f, -0.105555475f);
                spawnPt.localRotation = Quaternion.identity;
                spawnPt.localScale = Vector3.one;

                var mc = machObj.GetComponent<MachineController>();
                if (mc != null)
                {
                    var cb = convObj != null ? convObj.GetComponent<ConveyorBelt>() : null;
                    var sp = storageObj != null ? storageObj.GetComponent<StoragePad>() : null;
                    mc.Initialize(config, cb, sp);

                    var machSO = new SerializedObject(mc);
                    var mCfgProp = machSO.FindProperty("machineConfig");
                    if (mCfgProp != null) mCfgProp.objectReferenceValue = config;
                    var mConvProp = machSO.FindProperty("targetConveyor");
                    if (mConvProp != null) mConvProp.objectReferenceValue = cb;
                    var mPadProp = machSO.FindProperty("targetStoragePad");
                    if (mPadProp != null) mPadProp.objectReferenceValue = sp;
                    var mSpawnProp = machSO.FindProperty("spawnPoint");
                    if (mSpawnProp != null) mSpawnProp.objectReferenceValue = spawnPt;
                    machSO.ApplyModifiedProperties();
                }
            }

            // --- D. CHUẨN HÓA PRODUCTIONLINE ROOT VÀ CỜ UNLOCKED BY DEFAULT ---
            bool isDefaultUnlocked = (type == "Wood");

            var spComponent = storageObj != null ? storageObj.GetComponent<StoragePad>() : null;
            var cbComponent = convObj != null ? convObj.GetComponent<ConveyorBelt>() : null;
            var mcComponent = machObj != null ? machObj.GetComponent<MachineController>() : null;

            var lineSO = new SerializedObject(prodLine);
            var cIdProp = lineSO.FindProperty("clusterID");
            if (cIdProp != null) cIdProp.stringValue = $"Cluster_{type}";
            var linePadProp = lineSO.FindProperty("storagePad");
            if (linePadProp != null) linePadProp.objectReferenceValue = spComponent;

            // Xây dựng lại slot sạch sẽ
            var slotsProp = lineSO.FindProperty("machineSlots");
            if (slotsProp != null)
            {
                slotsProp.ClearArray();
                slotsProp.InsertArrayElementAtIndex(0);
                var s0 = slotsProp.GetArrayElementAtIndex(0);
                s0.FindPropertyRelative("slotID").stringValue = $"{type}_Slot01";
                s0.FindPropertyRelative("slotName").stringValue = $"Máy {type}";
                s0.FindPropertyRelative("machineConfig").objectReferenceValue = config;
                s0.FindPropertyRelative("spawnPoint").objectReferenceValue = machObj != null ? machObj.transform : null;
                s0.FindPropertyRelative("targetConveyor").objectReferenceValue = cbComponent;
                s0.FindPropertyRelative("isUnlockedByDefault").boolValue = isDefaultUnlocked;
                s0.FindPropertyRelative("isUnlocked").boolValue = isDefaultUnlocked;
                s0.FindPropertyRelative("currentTier").intValue = 1;
                s0.FindPropertyRelative("currentSpeedLevel").intValue = 0;
                s0.FindPropertyRelative("currentStorageLevel").intValue = 0;
                s0.FindPropertyRelative("spawnedMachineInstance").objectReferenceValue = mcComponent;
            }
            lineSO.ApplyModifiedProperties();

            // Đảm bảo máy hiển thị đúng trạng thái mở khóa (chỉ bật máy Gỗ)
            if (machObj != null)
            {
                machObj.SetActive(isDefaultUnlocked);
            }

            EditorUtility.SetDirty(lineObj);
        }
    }

    private static void UpdatePrefabAssets()
    {
        var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ENV_PREFAB_DIR}/StoragePallet_Visual.prefab");
        var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ENV_PREFAB_DIR}/ConveyorBelt_Visual.prefab");
        var t1OrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACH_PREFAB_DIR}/Visual_Ore_Tier1.prefab");

        var types = new[]
        {
            new { type = "Wood", name = "Gỗ", isDefault = true },
            new { type = "Stone", name = "Đá", isDefault = false },
            new { type = "Copper", name = "Đồng", isDefault = true },
            new { type = "Iron", name = "Sắt", isDefault = false },
            new { type = "Silver", name = "Bạc", isDefault = false },
            new { type = "Gold", name = "Vàng", isDefault = false }
        };

        foreach (var def in types)
        {
            string prefabPath = $"{PREFAB_DIR}/ProductionLine_{def.type}.prefab";
            var config = AssetDatabase.LoadAssetAtPath<MachineConfigSO>($"{SO_MACH_DIR}/Machine_{def.type}_Config.asset");

            GameObject root = new GameObject($"ProductionLine_{def.type}");
            var prodLine = root.AddComponent<ProductionLine>();

            // 1. StoragePad
            GameObject storageObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storageObj.name = $"StoragePad_{def.type}";
            storageObj.transform.SetParent(root.transform, false);
            storageObj.transform.localPosition = new Vector3(0f, 0f, -1.25f);
            storageObj.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);
            var padMr = storageObj.GetComponent<MeshRenderer>();
            if (padMr != null) padMr.enabled = false;

            if (palletPrefab != null)
            {
                var palInstance = UnityEngine.Object.Instantiate(palletPrefab, storageObj.transform, false);
                palInstance.name = "Visual_Pallet";
                palInstance.transform.localPosition = new Vector3(0f, -0.15f, 0f);

                var visChild = palInstance.transform.Find("Visual");
                if (visChild != null)
                {
                    visChild.localPosition = Vector3.zero;
                    visChild.localScale = new Vector3(1.42f, 1.42f, 1.42f);
                    var modelChild = visChild.Find("Model");
                    if (modelChild != null)
                    {
                        modelChild.localPosition = new Vector3(0f, 0.11f, -0.32f);
                        modelChild.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    }
                }
            }

            var padCol = storageObj.GetComponent<BoxCollider>();
            padCol.isTrigger = true;
            padCol.center = new Vector3(0f, 1.13f, -0.46f);
            padCol.size = new Vector3(0.5f, 3.0f, 0.5f);

            var storagePad = storageObj.AddComponent<StoragePad>();
            storagePad.SetCapacity(10);

            // 2. Conveyor
            GameObject convObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            convObj.name = $"Conveyor_{def.type}";
            convObj.transform.SetParent(root.transform, false);
            convObj.transform.localPosition = new Vector3(0f, 0.25f, 1.85f);
            convObj.transform.localScale = new Vector3(1.4f, 0.5f, 3.2f);
            var convMr = convObj.GetComponent<MeshRenderer>();
            if (convMr != null) convMr.enabled = false;

            if (conveyorPrefab != null)
            {
                var cInstance = UnityEngine.Object.Instantiate(conveyorPrefab, convObj.transform, false);
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

            // 3. Machine
            GameObject machObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machObj.name = $"Machine_{def.type}";
            machObj.transform.SetParent(root.transform, false);
            machObj.transform.localPosition = new Vector3(0f, 1.0f, 3.8f);
            machObj.transform.localScale = new Vector3(1.8f, 2.0f, 1.8f);
            var machMr = machObj.GetComponent<MeshRenderer>();
            if (machMr != null) machMr.enabled = false;

            if (t1OrePrefab != null)
            {
                var mViz = UnityEngine.Object.Instantiate(t1OrePrefab, machObj.transform, false);
                mViz.name = "Visual_MachineTier1";
                mViz.transform.localPosition = new Vector3(0f, -1.0f, 0f);
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

            var machCol = machObj.GetComponent<BoxCollider>();
            machCol.isTrigger = false;
            machCol.center = new Vector3(0f, 0.03f, 0.56f);
            machCol.size = new Vector3(1.5f, 0.95f, 1.66f);

            GameObject spawnPt = new GameObject("SpawnPoint");
            spawnPt.transform.SetParent(machObj.transform, false);
            spawnPt.transform.localPosition = new Vector3(0f, -0.275f, -0.105555475f);

            var machine = machObj.AddComponent<MachineController>();
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

            // 4. Slot
            var slot = new MachineSlot
            {
                slotID = $"{def.type}_Slot01",
                slotName = $"Máy {def.name}",
                machineConfig = config,
                spawnPoint = machObj.transform,
                targetConveyor = conveyor,
                isUnlockedByDefault = def.isDefault,
                isUnlocked = def.isDefault,
                currentTier = 1,
                currentSpeedLevel = 0,
                spawnedMachineInstance = machine
            };
            prodLine.AddSlotDirectly(slot);

            var lineSO = new SerializedObject(prodLine);
            var cIdProp = lineSO.FindProperty("clusterID");
            if (cIdProp != null) cIdProp.stringValue = $"Cluster_{def.type}";
            var linePadProp = lineSO.FindProperty("storagePad");
            if (linePadProp != null) linePadProp.objectReferenceValue = storagePad;
            lineSO.ApplyModifiedProperties();

            // Lưu Prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        Debug.Log("[StandardizeTool] Đã cập nhật xong toàn bộ 6 file Prefabs trong thư mục ProductionLines!");
    }
}
