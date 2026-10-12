using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script Editor tự động thiết lập Scene Prototype & Toàn Bộ 6 Dây Chuyền Sản Xuất chỉ với 1 cú click.
/// Giúp người chơi trải nghiệm trọn vẹn chu trình kinh tế từ Gỗ đến Vàng,
/// mở khóa máy in-world, nâng cấp 3 Tiers và kiểm tra điều kiện Chiến Thắng $50,000.
/// </summary>
public static class PrototypeSceneSetup
{
    private static Material GetOrCreateMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.5f)
    {
        string matDir = "Assets/_Project/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Materials");
        }
        string matPath = $"{matDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static GameObject GetOrCreateItemPrefab(string name, ResourceDataSO data, Material mat)
    {
        string prefabDir = "Assets/_Project/Prefabs/Resources";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Resources");
        }
        string prefabPath = $"{prefabDir}/{name}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.name = name;
            temp.tag = "ResourceItem";
            temp.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
            var mr = temp.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            var rb = temp.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var holder = temp.AddComponent<ResourceItemHolder>();
            holder.Data = data;

            prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
        }
        else
        {
            var mr = prefab.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;
            var holder = prefab.GetComponent<ResourceItemHolder>();
            if (holder != null) holder.Data = data;
            EditorUtility.SetDirty(prefab);
        }

        if (data != null && data.itemPrefab != prefab)
        {
            data.itemPrefab = prefab;
            EditorUtility.SetDirty(data);
        }

        return prefab;
    }

    [MenuItem("Factory/Create All 6 Default Resource Assets", priority = 2)]
    public static void CreateDefaultResources()
    {
        string resPath = "Assets/_Project/ScriptableObjects/Resources";
        if (!AssetDatabase.IsValidFolder(resPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Resources");
        }

        var list = new[]
        {
            new { name = "Gỗ", type = ResourceType.Wood, val = 2 },
            new { name = "Đá", type = ResourceType.Stone, val = 5 },
            new { name = "Đồng", type = ResourceType.Copper, val = 15 },
            new { name = "Sắt", type = ResourceType.Iron, val = 40 },
            new { name = "Bạc", type = ResourceType.Silver, val = 100 },
            new { name = "Vàng", type = ResourceType.Gold, val = 250 }
        };

        foreach (var item in list)
        {
            string assetFile = $"{resPath}/Resource_{item.type}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(assetFile);
            if (existing == null)
            {
                var so = ScriptableObject.CreateInstance<ResourceDataSO>();
                so.resourceName = item.name;
                so.resourceType = item.type;
                so.tier = 1;
                so.baseValue = item.val;
                AssetDatabase.CreateAsset(so, assetFile);
            }
            else
            {
                existing.resourceName = item.name;
                existing.resourceType = item.type;
                existing.baseValue = item.val;
                EditorUtility.SetDirty(existing);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Factory] Đã cấu hình đủ 6 ResourceDataSO chuẩn theo GDD!");
    }

    [MenuItem("Factory/Setup Complete Production Chain (1-Click)", priority = 1)]
    public static void SetupCompleteProductionChain()
    {
        // 1. Khởi tạo vật liệu màu cho 6 dòng tài nguyên & môi trường
        Material matGround = GetOrCreateMaterial("Mat_Ground", new Color(0.24f, 0.26f, 0.28f));
        Material matWood = GetOrCreateMaterial("Mat_Wood", new Color(0.58f, 0.36f, 0.18f), 0.1f, 0.3f);
        Material matStone = GetOrCreateMaterial("Mat_Stone", new Color(0.52f, 0.55f, 0.58f), 0.1f, 0.2f);
        Material matCopper = GetOrCreateMaterial("Mat_Copper", new Color(0.85f, 0.44f, 0.20f), 0.6f, 0.7f);
        Material matIron = GetOrCreateMaterial("Mat_Iron", new Color(0.30f, 0.38f, 0.46f), 0.7f, 0.6f);
        Material matSilver = GetOrCreateMaterial("Mat_Silver", new Color(0.86f, 0.89f, 0.93f), 0.85f, 0.85f);
        Material matGold = GetOrCreateMaterial("Mat_Gold", new Color(1.00f, 0.82f, 0.08f), 0.9f, 0.9f);

        Material matConveyor = GetOrCreateMaterial("Mat_Conveyor", new Color(0.16f, 0.16f, 0.18f));
        Material matStorage = GetOrCreateMaterial("Mat_StoragePad", new Color(0.38f, 0.28f, 0.18f));
        Material matSell = GetOrCreateMaterial("Mat_SellZone", new Color(0.15f, 0.82f, 0.42f));
        Material matVault = GetOrCreateMaterial("Mat_MoneyVault", new Color(0.96f, 0.72f, 0.12f));
        Material matUnlock = GetOrCreateMaterial("Mat_UnlockPad", new Color(0.12f, 0.65f, 0.95f));

        // 2. Tạo ScriptableObjects và Prefabs cho 6 dòng tài nguyên
        CreateDefaultResources();

        string resPath = "Assets/_Project/ScriptableObjects/Resources";
        string machineDir = "Assets/_Project/ScriptableObjects/Machines";
        if (!AssetDatabase.IsValidFolder(machineDir))
        {
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Machines");
        }

        var lineConfigs = new[]
        {
            new {
                type = ResourceType.Wood, name = "Gỗ", machineTitle = "Máy Cưa Gỗ",
                zPos = 9f, isLeft = true,
                unlockCost = 0, isDefault = true,
                t1Val = 2, t2Val = 6, t3Val = 20,
                t2Cost = 150, t3Cost = 400,
                multiplier = 1.0f,
                intervals = new float[] { 2.5f, 2.0f, 1.5f, 1.0f },
                mat = matWood
            },
            new {
                type = ResourceType.Stone, name = "Đá", machineTitle = "Máy Nghiền Đá",
                zPos = 0f, isLeft = true,
                unlockCost = 200, isDefault = false,
                t1Val = 5, t2Val = 15, t3Val = 50,
                t2Cost = 350, t3Cost = 900,
                multiplier = 2.5f,
                intervals = new float[] { 2.8f, 2.2f, 1.6f, 1.0f },
                mat = matStone
            },
            new {
                type = ResourceType.Copper, name = "Đồng", machineTitle = "Lò Luyện Đồng",
                zPos = -9f, isLeft = true,
                unlockCost = 800, isDefault = false,
                t1Val = 15, t2Val = 45, t3Val = 150,
                t2Cost = 1200, t3Cost = 3000,
                multiplier = 6.0f,
                intervals = new float[] { 3.0f, 2.4f, 1.8f, 1.2f },
                mat = matCopper
            },
            new {
                type = ResourceType.Iron, name = "Sắt", machineTitle = "Lò Đúc Thép",
                zPos = 9f, isLeft = false,
                unlockCost = 3000, isDefault = false,
                t1Val = 40, t2Val = 120, t3Val = 400,
                t2Cost = 4500, t3Cost = 10000,
                multiplier = 15.0f,
                intervals = new float[] { 3.2f, 2.5f, 1.8f, 1.2f },
                mat = matIron
            },
            new {
                type = ResourceType.Silver, name = "Bạc", machineTitle = "Máy Tinh Chế Bạc",
                zPos = 0f, isLeft = false,
                unlockCost = 10000, isDefault = false,
                t1Val = 100, t2Val = 300, t3Val = 1000,
                t2Cost = 12000, t3Cost = 25000,
                multiplier = 35.0f,
                intervals = new float[] { 3.5f, 2.8f, 2.0f, 1.4f },
                mat = matSilver
            },
            new {
                type = ResourceType.Gold, name = "Vàng", machineTitle = "Máy Chiết Xuất Vàng",
                zPos = -9f, isLeft = false,
                unlockCost = 25000, isDefault = false,
                t1Val = 250, t2Val = 750, t3Val = 2500,
                t2Cost = 30000, t3Cost = 60000,
                multiplier = 80.0f,
                intervals = new float[] { 4.0f, 3.0f, 2.2f, 1.5f },
                mat = matGold
            }
        };

        var machineConfigs = new Dictionary<ResourceType, MachineConfigSO>();

        foreach (var def in lineConfigs)
        {
            // Resource Tiers
            string resT1Path = $"{resPath}/Resource_{def.type}.asset";
            var r1 = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(resT1Path);
            if (r1 == null)
            {
                r1 = ScriptableObject.CreateInstance<ResourceDataSO>();
                r1.resourceName = def.name;
                r1.resourceType = def.type;
                r1.tier = 1;
                r1.baseValue = def.t1Val;
                AssetDatabase.CreateAsset(r1, resT1Path);
            }
            r1.baseValue = def.t1Val;
            GetOrCreateItemPrefab($"{def.type}ItemPrefab", r1, def.mat);

            // Tier 2 Resource
            string resT2Path = $"{resPath}/Resource_{def.type}_T2.asset";
            var r2 = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(resT2Path);
            if (r2 == null)
            {
                r2 = ScriptableObject.CreateInstance<ResourceDataSO>();
                r2.resourceName = $"{def.name} Cấp 2";
                r2.resourceType = def.type;
                r2.tier = 2;
                r2.baseValue = def.t2Val;
                r2.itemPrefab = r1.itemPrefab;
                AssetDatabase.CreateAsset(r2, resT2Path);
            }
            else
            {
                r2.baseValue = def.t2Val;
                r2.itemPrefab = r1.itemPrefab;
                EditorUtility.SetDirty(r2);
            }

            // Tier 3 Resource
            string resT3Path = $"{resPath}/Resource_{def.type}_T3.asset";
            var r3 = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(resT3Path);
            if (r3 == null)
            {
                r3 = ScriptableObject.CreateInstance<ResourceDataSO>();
                r3.resourceName = $"{def.name} Cấp 3";
                r3.resourceType = def.type;
                r3.tier = 3;
                r3.baseValue = def.t3Val;
                r3.itemPrefab = r1.itemPrefab;
                AssetDatabase.CreateAsset(r3, resT3Path);
            }
            else
            {
                r3.baseValue = def.t3Val;
                r3.itemPrefab = r1.itemPrefab;
                EditorUtility.SetDirty(r3);
            }

            // Machine Config SO
            string configPath = $"{machineDir}/Machine_{def.type}_Config.asset";
            var mConfig = AssetDatabase.LoadAssetAtPath<MachineConfigSO>(configPath);
            if (mConfig == null)
            {
                mConfig = ScriptableObject.CreateInstance<MachineConfigSO>();
                mConfig.machineName = def.machineTitle;
                mConfig.resourceType = def.type;
                mConfig.unlockCost = def.unlockCost;
                mConfig.tier1Resource = r1;
                mConfig.tier2Resource = r2;
                mConfig.tier3Resource = r3;
                mConfig.tierUpgradeCosts = new int[] { 0, def.t2Cost, def.t3Cost };
                mConfig.spawnIntervalLevels = def.intervals;
                mConfig.economyMultiplier = def.multiplier;
                AssetDatabase.CreateAsset(mConfig, configPath);
            }
            else
            {
                mConfig.machineName = def.machineTitle;
                mConfig.resourceType = def.type;
                mConfig.unlockCost = def.unlockCost;
                mConfig.tier1Resource = r1;
                mConfig.tier2Resource = r2;
                mConfig.tier3Resource = r3;
                mConfig.tierUpgradeCosts = new int[] { 0, def.t2Cost, def.t3Cost };
                mConfig.spawnIntervalLevels = def.intervals;
                mConfig.economyMultiplier = def.multiplier;
                EditorUtility.SetDirty(mConfig);
            }

            machineConfigs[def.type] = mConfig;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Khởi tạo Scene mới
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Mặt sàn Ground (55m x 55m)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_FactoryFloor";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5.5f, 1f, 5.5f);
        var groundMr = ground.GetComponent<MeshRenderer>();
        if (groundMr != null) groundMr.sharedMaterial = matGround;

        var navSurface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        navSurface.BuildNavMesh();

        // Ánh sáng Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Camera Top-Down bao quát toàn bộ 6 cụm
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0f, 22f, -17f);
        camObj.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

        // Managers
        GameObject managers = new GameObject("_Managers");
        managers.AddComponent<InputManager>();
        managers.AddComponent<EconomyManager>();
        managers.AddComponent<ResourcePoolManager>();
        var factoryManager = managers.AddComponent<FactoryManager>();
        managers.AddComponent<SaveManager>();
        managers.AddComponent<WinConditionChecker>();
        managers.AddComponent<FactoryManagementUI>();

        // Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, -2f);

        var oldCol = player.GetComponent<CapsuleCollider>();
        if (oldCol != null) Object.DestroyImmediate(oldCol);

        var cc = player.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.radius = 0.5f;
        cc.height = 2f;

        player.AddComponent<PlayerCarry>();
        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerInteraction>();

        // HUD
        GameObject hudObj = new GameObject("_TestHUD");
        hudObj.AddComponent<PrototypeTestHUD>();

        // --- KHU VỰC TRUNG TÂM (CENTRAL HUB) ---
        // 1. SellZone tại (0, 0.05, 0)
        GameObject sellObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sellObj.name = "SellZone_Central";
        sellObj.transform.position = new Vector3(0f, 0.05f, 0f);
        sellObj.transform.localScale = new Vector3(3f, 0.05f, 3f);
        var sellMr = sellObj.GetComponent<MeshRenderer>();
        if (sellMr != null) sellMr.sharedMaterial = matSell;
        var sellCol = sellObj.GetComponent<Collider>();
        sellCol.isTrigger = true;
        sellObj.AddComponent<SellZone>();
        var sellRb = sellObj.GetComponent<Rigidbody>();
        if (sellRb != null)
        {
            sellRb.isKinematic = true;
            sellRb.useGravity = false;
        }

        // 2. Két Tiền MoneyVault tại (0, 0.6, -5)
        GameObject vaultObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vaultObj.name = "MoneyVault_Safe";
        vaultObj.transform.position = new Vector3(0f, 0.6f, -5f);
        vaultObj.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
        var vaultMr = vaultObj.GetComponent<MeshRenderer>();
        if (vaultMr != null) vaultMr.enabled = false;
        var safePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/BankSafe_Visual.prefab");
        if (safePrefab != null)
        {
            var sInstance = Object.Instantiate(safePrefab, vaultObj.transform);
            sInstance.name = "Visual_Safe";
            sInstance.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        }
        vaultObj.AddComponent<MoneyVault>();

        // 3. Trạm Quản Lý Nâng Cấp UpgradeStation tại (0, 0.6, 5)
        GameObject stationObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stationObj.name = "UpgradeStation_Terminal";
        stationObj.transform.position = new Vector3(0f, 0.6f, 5f);
        stationObj.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
        var stationMr = stationObj.GetComponent<MeshRenderer>();
        if (stationMr != null) stationMr.enabled = false;
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/ControlPanel_Visual.prefab");
        if (panelPrefab != null)
        {
            var pInstance = Object.Instantiate(panelPrefab, stationObj.transform);
            pInstance.name = "Visual_Terminal";
            pInstance.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        }
        stationObj.AddComponent<UpgradeStation>();

        // Load Prefabs môi trường & băng chuyền
        var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/StoragePallet_Visual.prefab");
        var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/ConveyorBelt_Visual.prefab");

        // --- DỰNG 6 CỤM DÂY CHUYỀN SẢN XUẤT ĐỐI XỨNG ---
        foreach (var def in lineConfigs)
        {
            float dirSign = def.isLeft ? -1f : 1f;
            float padX = 5f * dirSign;
            float conveyorX = 9.5f * dirSign;
            float machineX = 14f * dirSign;
            float spawnItemX = 12.5f * dirSign;

            GameObject lineObj = new GameObject($"ProductionLine_{def.type}");
            var prodLine = lineObj.AddComponent<ProductionLine>();
            var lineSO = new SerializedObject(prodLine);
            lineSO.FindProperty("clusterID").stringValue = $"Cluster_{def.type}";
            lineSO.FindProperty("sellZoneTransform").objectReferenceValue = sellObj.transform;
            lineSO.ApplyModifiedProperties();

            // 1. StoragePad
            GameObject storageObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storageObj.name = $"StoragePad_{def.type}";
            storageObj.transform.position = new Vector3(padX, 0.15f, def.zPos);
            storageObj.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);
            storageObj.transform.SetParent(lineObj.transform);
            var padMr = storageObj.GetComponent<MeshRenderer>();
            if (padMr != null) padMr.enabled = false;
            if (palletPrefab != null)
            {
                var palInstance = Object.Instantiate(palletPrefab, storageObj.transform);
                palInstance.name = "Visual_Pallet";
                palInstance.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            }
            var padCol = storageObj.GetComponent<BoxCollider>();
            padCol.isTrigger = true;
            padCol.size = new Vector3(1f, 3.5f, 1f);
            padCol.center = new Vector3(0f, 1f, 0f);
            var storagePad = storageObj.AddComponent<StoragePad>();

            // 2. ConveyorBelt (Dài 7m kết nối trực tiếp từ họng máy đến bệ chứa)
            GameObject convObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            convObj.name = $"Conveyor_{def.type}";
            convObj.transform.position = new Vector3(conveyorX, 0.25f, def.zPos);
            convObj.transform.localScale = new Vector3(7f, 0.5f, 1.4f);
            convObj.transform.SetParent(lineObj.transform);
            var convMr = convObj.GetComponent<MeshRenderer>();
            if (convMr != null) convMr.enabled = false;
            if (conveyorPrefab != null)
            {
                var cInstance = Object.Instantiate(conveyorPrefab, convObj.transform);
                cInstance.name = "Visual_Conveyor";
                cInstance.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            }
            var convCol = convObj.GetComponent<BoxCollider>();
            convCol.isTrigger = true;
            convCol.size = new Vector3(1f, 3f, 1f);
            convCol.center = new Vector3(0f, 1f, 0f);
            var conveyor = convObj.AddComponent<ConveyorBelt>();
            conveyor.TargetStoragePad = storagePad;

            // 3. MachineController
            GameObject machineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machineObj.name = $"Machine_{def.type}";
            machineObj.transform.position = new Vector3(machineX, 1f, def.zPos);
            machineObj.transform.localScale = new Vector3(1.8f, 2f, 1.8f);
            machineObj.transform.SetParent(lineObj.transform);
            var machMr = machineObj.GetComponent<MeshRenderer>();
            if (machMr != null) machMr.enabled = false;
            var machine = machineObj.AddComponent<MachineController>();

            // Gắn visual Tier 1
            var mConfig = machineConfigs[def.type];
            if (mConfig != null && mConfig.tier1VisualPrefab != null)
            {
                var mViz = Object.Instantiate(mConfig.tier1VisualPrefab, machineObj.transform);
                mViz.name = "Visual_MachineTier1";
                mViz.transform.localPosition = new Vector3(0f, -1f, 0f);
                // Xoay hướng mặt máy về phía băng chuyền
                mViz.transform.localRotation = Quaternion.Euler(0f, def.isLeft ? 90f : -90f, 0f);
            }

            // Điểm nhả hàng rơi lên đầu băng chuyền
            GameObject spawnPt = new GameObject("SpawnPoint");
            spawnPt.transform.SetParent(machineObj.transform);
            spawnPt.transform.position = new Vector3(spawnItemX, 0.6f, def.zPos);

            var machSO = new SerializedObject(machine);
            machSO.FindProperty("machineConfig").objectReferenceValue = machineConfigs[def.type];
            machSO.FindProperty("targetConveyor").objectReferenceValue = conveyor;
            machSO.FindProperty("targetStoragePad").objectReferenceValue = storagePad;
            machSO.FindProperty("spawnPoint").objectReferenceValue = spawnPt.transform;
            machSO.ApplyModifiedProperties();

            machine.Initialize(machineConfigs[def.type], conveyor, storagePad);

            // MachineSlot
            var slot = new MachineSlot
            {
                slotID = $"{def.type}_Slot01",
                slotName = def.machineTitle,
                machineConfig = machineConfigs[def.type],
                spawnPoint = machineObj.transform,
                targetConveyor = conveyor,
                isUnlockedByDefault = def.isDefault,
                isUnlocked = def.isDefault,
                currentTier = 1,
                currentSpeedLevel = 0,
                spawnedMachineInstance = machine
            };
            prodLine.AddSlotDirectly(slot);

            // 4. Bệ mở khóa in-world (MachineUnlockPad) cho các máy chưa mở
            if (!def.isDefault)
            {
                machineObj.SetActive(false);

                GameObject unlockObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                unlockObj.name = $"UnlockPad_{def.type}";
                unlockObj.transform.position = new Vector3(machineX, 0.08f, def.zPos);
                unlockObj.transform.localScale = new Vector3(2.5f, 0.06f, 2.5f);
                unlockObj.transform.SetParent(lineObj.transform);
                var unMr = unlockObj.GetComponent<MeshRenderer>();
                if (unMr != null) unMr.sharedMaterial = matUnlock;
                var unCol = unlockObj.GetComponent<Collider>();
                unCol.isTrigger = true;

                var unlockPad = unlockObj.AddComponent<MachineUnlockPad>();
                unlockPad.Setup(prodLine, slot);
            }

            // 5. Worker Spawn Points
            GameObject sp1 = new GameObject("WorkerSpawn_01");
            sp1.transform.SetParent(lineObj.transform);
            sp1.transform.position = new Vector3(padX, 0f, def.zPos + 1.8f);
            prodLine.AddWorkerSpawnPoint(sp1.transform);

            GameObject sp2 = new GameObject("WorkerSpawn_02");
            sp2.transform.SetParent(lineObj.transform);
            sp2.transform.position = new Vector3(padX, 0f, def.zPos - 1.8f);
            prodLine.AddWorkerSpawnPoint(sp2.transform);

            // Gán storagePad vào ProductionLine
            var pLineSO = new SerializedObject(prodLine);
            pLineSO.FindProperty("storagePad").objectReferenceValue = storagePad;
            pLineSO.ApplyModifiedProperties();

            // Đăng ký với FactoryManager
            factoryManager.RegisterProductionLine(prodLine);
        }

        EditorUtility.SetDirty(factoryManager);

        // 6. Trang trí Môi Trường (_EnvironmentDecor)
        var decorRoot = new GameObject("_EnvironmentDecor");
        var wsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Environment_Workshop.prefab");
        if (wsPrefab != null)
        {
            var ws = Object.Instantiate(wsPrefab, new Vector3(-19f, 0f, 9f), Quaternion.Euler(0f, 90f, 0f), decorRoot.transform);
            ws.name = "Decor_TimberWorkshop";
        }
        var cavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Environment_Cave.prefab");
        if (cavePrefab != null)
        {
            var c1 = Object.Instantiate(cavePrefab, new Vector3(-19f, 0f, -9f), Quaternion.Euler(0f, 90f, 0f), decorRoot.transform);
            c1.name = "Decor_Cave_Left";
            var c2 = Object.Instantiate(cavePrefab, new Vector3(19f, 0f, -9f), Quaternion.Euler(0f, -90f, 0f), decorRoot.transform);
            c2.name = "Decor_Cave_Right";
        }
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Environment_Tree.prefab");
        if (treePrefab != null)
        {
            Vector3[] treePositions = new[]
            {
                new Vector3(-23f, 0f, 20f), new Vector3(-20f, 0f, 23f),
                new Vector3(23f, 0f, 20f), new Vector3(20f, 0f, 23f),
                new Vector3(-23f, 0f, -20f), new Vector3(23f, 0f, -20f),
                new Vector3(-19f, 0f, 0f), new Vector3(19f, 0f, 0f)
            };
            for (int i = 0; i < treePositions.Length; i++)
            {
                var tree = Object.Instantiate(treePrefab, treePositions[i], Quaternion.Euler(0f, i * 45f, 0f), decorRoot.transform);
                tree.name = $"Decor_PineTree_{i + 1:D2}";
            }
        }

        // Lưu Scene
        string sceneDirectory = "Assets/_Project/Scenes";
        string scenePath = $"{sceneDirectory}/PrototypeScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Factory] Đã thiết lập thành công toàn bộ 6 Dây Chuyền Sản Xuất tại: {scenePath}");
        EditorUtility.DisplayDialog(
            "Factory Reborn - Toàn Bộ 6 Dây Chuyền",
            "Đã tự động khởi tạo thành công 6 Dây Chuyền Sản Xuất Đối Xứng:\n\n" +
            "🪵 Gỗ (Miễn phí - Đã mở sẵn)\n" +
            "🪨 Đá ($200)\n" +
            "🥉 Đồng ($800)\n" +
            "🔩 Sắt ($3,000)\n" +
            "🥈 Bạc ($10,000)\n" +
            "🥇 Vàng ($25,000)\n\n" +
            "👉 Hãy bấm nút PLAY trên Unity để trải nghiệm đế chế nhà máy ngay!",
            "Tuyệt vời!"
        );
    }
}
