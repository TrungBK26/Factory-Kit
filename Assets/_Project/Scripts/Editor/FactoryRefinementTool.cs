using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Công cụ tinh chỉnh toàn diện theo phản hồi mới nhất của người dùng:
/// 1. Chỉ 1 máy duy nhất được FREE khi bắt đầu (Máy Gỗ - Wood). Máy Đồng (Copper) và các máy khác bị khóa theo đúng tiến trình.
/// 2. Thống nhất tọa độ Object cha với Model con cho: Mỏ đá (Quarry), Bảng nâng cấp (Terminal), Két sắt (Vault), và Xưởng (Workshop Shed).
/// 3. Cân chỉnh kích cỡ và góc quay chuẩn xác cho Máy Tier 2 và Tier 3 theo đúng ảnh chụp thực tế của người dùng:
///    - Tier 2 Model: Pos = (0.3592021, -0.218793, -0.360023), Rot = (-90, 0, -90), Scale = (2, 2, 2)
///    - Tier 3 Model: Pos = (0, -0.326551, -0.36), Rot = (-90, 0, 0), Scale = (2, 2, 2) (Căn giữa tuyệt đối với băng chuyền)
/// 4. Đồng bộ tọa độ SpawnPoint của tất cả các máy theo tọa độ mẫu của Máy Gỗ (Machine_Wood/SpawnPoint).
/// 5. Sửa lỗi tấm pallet ván gỗ:
///    - Triệt tiêu biến dạng tỉ lệ (Scale Distortion) của StoragePad bằng cách tạo bệ ItemSlotRoot chuẩn tỉ lệ 1:1.
///    - Căn giữa vật phẩm vào tâm tấm pallet, khoảng cách dãn cách 3x3 vừa khít trong ván gỗ, không bị tràn ra ngoài.
///    - Nâng chiều cao giữa các tầng (Layer 2) chuẩn theo chiều cao khối vật phẩm (0.35m) để tầng 2 xếp thẳng lên trên tầng 1 gọn gàng, không bị đè lún.
/// </summary>
public static class FactoryRefinementTool
{
    private const string SO_MACH_DIR = "Assets/_Project/ScriptableObjects/Machines";
    private const string MACH_PREFAB_DIR = "Assets/_Project/Prefabs/Machines";
    private const string PREFAB_DIR = "Assets/_Project/Prefabs/ProductionLines";

    // ĐÃ TẮT TỰ ĐỘNG CHẠY KHI RELOAD: Bảo toàn 100% vị trí người dùng đã chỉnh thủ công trong Scene.
    // Chỉ chạy khi người dùng chủ động click menu.

    [MenuItem("Factory/Fixes/Apply All Refinements (Fix Hierarchy, Tiers, Spawns, Pallets)", priority = 0)]
    public static void ManualExecute()
    {
        ExecuteRefinement();
        EditorUtility.DisplayDialog(
            "Đã Tinh Chỉnh Thành Công Toàn Diện!",
            "1. Chỉ máy GỖ mở khóa mặc định (miễn phí), các máy khác bị khóa theo tiến trình.\n" +
            "2. Chuẩn hóa kích thước và căn giữa Máy Tier 2 và Tier 3 với băng chuyền.\n" +
            "3. Đồng bộ SpawnPoint tất cả các máy theo Máy Gỗ mẫu.\n" +
            "4. Sửa lỗi bệ Pallet: Đẩy ra ngoài đón hàng ở Z = -1.25m, xếp gọn gàng 3x3 lên tầng 2 chuẩn xác.\n" +
            "5. Tuyệt đối không thay đổi vị trí thủ công của Nhà xưởng và Mỏ đá của bạn.",
            "Tuyệt vời!"
        );
    }

    public static void ExecuteRefinement()
    {
        Debug.Log("[RefinementTool] Bắt đầu tinh chỉnh Scene và Prefabs theo yêu cầu của người dùng...");

        // 1. Chỉ 1 máy FREE khi vào game (Wood)
        ApplySingleFreeMachineRule();

        // 2. KHÔNG can thiệp vị trí Két, Bảng nâng cấp, Xưởng, Mỏ đá (Bảo lưu toàn bộ vị trí thủ công người dùng đã sắp xếp)
        // UnifyParentCoordinates();

        // 3. Chuẩn hóa Máy Tier 2 và Tier 3
        StandardizeTiers2And3();

        // 4. Đồng bộ SpawnPoint các máy theo Máy Gỗ
        SyncSpawnPointsFromWood();

        // 5. Chuẩn hóa bệ chứa Pallet và cơ chế xếp hàng 3x3 lên tầng 2
        FixPalletStackingAndAlignment();

        // 6. Lưu Scene và AssetDatabase
        var scene = SceneManager.GetActiveScene();
        if (scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 6. Ghi log kiểm tra mới
        SceneModelInspector.Inspect();

        Debug.Log("[RefinementTool] HOÀN TẤT TINH CHỈNH TOÀN DIỆN!");
    }

    /// <summary>
    /// VẤN ĐỀ 1: Chỉ 1 máy duy nhất (Gỗ) là Free khi vào game. Máy Đồng và các máy khác bị khóa.
    /// </summary>
    private static void ApplySingleFreeMachineRule()
    {
        string[] lineTypes = new[] { "Wood", "Stone", "Copper", "Iron", "Silver", "Gold" };

        foreach (var type in lineTypes)
        {
            var lineObj = GameObject.Find($"ProductionLine_{type}");
            if (lineObj == null) continue;

            bool isFree = (type == "Wood"); // Chỉ DUY NHẤT Gỗ là miễn phí khởi đầu!

            var prodLine = lineObj.GetComponent<ProductionLine>();
            if (prodLine != null)
            {
                var lineSO = new SerializedObject(prodLine);
                var slotsProp = lineSO.FindProperty("machineSlots");
                if (slotsProp != null && slotsProp.arraySize > 0)
                {
                    var s0 = slotsProp.GetArrayElementAtIndex(0);
                    s0.FindPropertyRelative("isUnlockedByDefault").boolValue = isFree;
                    s0.FindPropertyRelative("isUnlocked").boolValue = isFree;
                }
                lineSO.ApplyModifiedProperties();
            }

            var machObj = lineObj.transform.Find($"Machine_{type}");
            if (machObj != null)
            {
                // Máy gỗ bật lên, các máy khác tắt đi chờ mở khóa
                machObj.gameObject.SetActive(isFree);
            }

            // Cập nhật Prefabs tương ứng
            string prefabPath = $"{PREFAB_DIR}/ProductionLine_{type}.prefab";
            var prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabObj != null)
            {
                var pLine = prefabObj.GetComponent<ProductionLine>();
                if (pLine != null)
                {
                    var pSO = new SerializedObject(pLine);
                    var pSlots = pSO.FindProperty("machineSlots");
                    if (pSlots != null && pSlots.arraySize > 0)
                    {
                        var s0 = pSlots.GetArrayElementAtIndex(0);
                        s0.FindPropertyRelative("isUnlockedByDefault").boolValue = isFree;
                        s0.FindPropertyRelative("isUnlocked").boolValue = isFree;
                    }
                    pSO.ApplyModifiedProperties();
                }
                var pMach = prefabObj.transform.Find($"Machine_{type}");
                if (pMach != null)
                {
                    pMach.gameObject.SetActive(isFree);
                }
                EditorUtility.SetDirty(prefabObj);
            }
        }

        Debug.Log("[RefinementTool] Đã thiết lập: Chỉ Máy GỖ là máy khởi đầu miễn phí, các máy khác bị khóa theo đúng tiến trình.");
    }

    /// <summary>
    /// VẤN ĐỀ 2: Thống nhất tọa độ Object cha với vị trí Model con cho:
    /// - Két sắt (MoneyVault_Safe)
    /// - Bảng nâng cấp (UpgradeStation_Terminal)
    /// - Nhà xưởng (Model_RusticWorkshopShed)
    /// - Mỏ đá (Model_QuarryPitCave)
    /// </summary>
    private static void UnifyParentCoordinates()
    {
        // 1. Két sắt (MoneyVault_Safe)
        var safeRoot = GameObject.Find("MoneyVault_Safe");
        if (safeRoot != null)
        {
            var modelT = safeRoot.transform.Find("Visual_Safe/Visual/Model");
            if (modelT != null)
            {
                Vector3 targetWorldPos = modelT.position;
                Quaternion targetWorldRot = modelT.rotation;

                // Đưa Root về đúng tâm của Model
                safeRoot.transform.position = targetWorldPos;

                // Đưa các cấp con về Vector3.zero
                var visSafe = safeRoot.transform.Find("Visual_Safe");
                if (visSafe != null)
                {
                    visSafe.localPosition = Vector3.zero;
                    visSafe.localRotation = Quaternion.identity;
                }
                var visual = visSafe != null ? visSafe.Find("Visual") : null;
                if (visual != null)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                }
                modelT.localPosition = Vector3.zero;
                modelT.localRotation = Quaternion.Euler(270f, 180f, 0f);

                // Cân lại BoxCollider của Root bao quanh két
                var col = safeRoot.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 0.45f, 0f);
                    col.size = new Vector3(1.0f, 0.9f, 1.0f);
                }
                EditorUtility.SetDirty(safeRoot);
                Debug.Log($"[RefinementTool] Đã thống nhất tọa độ Két sắt (MoneyVault_Safe) tại {safeRoot.transform.position}.");
            }
        }

        // 2. Bảng nâng cấp (UpgradeStation_Terminal)
        var terminalRoot = GameObject.Find("UpgradeStation_Terminal");
        if (terminalRoot != null)
        {
            var modelT = terminalRoot.transform.Find("Visual_Terminal/Visual/Model");
            if (modelT != null)
            {
                Vector3 targetWorldPos = modelT.position;

                terminalRoot.transform.position = targetWorldPos;

                var visTerm = terminalRoot.transform.Find("Visual_Terminal");
                if (visTerm != null)
                {
                    visTerm.localPosition = Vector3.zero;
                    visTerm.localRotation = Quaternion.identity;
                }
                var visual = visTerm != null ? visTerm.Find("Visual") : null;
                if (visual != null)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                }
                modelT.localPosition = Vector3.zero;
                modelT.localRotation = Quaternion.Euler(270f, 180f, 0f);

                var col = terminalRoot.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 0.5f, 0f);
                    col.size = new Vector3(1.2f, 1.0f, 1.0f);
                }
                EditorUtility.SetDirty(terminalRoot);
                Debug.Log($"[RefinementTool] Đã thống nhất tọa độ Bảng nâng cấp (UpgradeStation_Terminal) tại {terminalRoot.transform.position}.");
            }
        }

        // 3. Nhà xưởng (Model_RusticWorkshopShed)
        var shedRoot = GameObject.Find("Model_RusticWorkshopShed");
        if (shedRoot != null)
        {
            var modelT = shedRoot.transform.Find("Visual/Model");
            if (modelT != null)
            {
                Vector3 targetWorldPos = modelT.position;
                targetWorldPos.y = 0f; // Đặt gốc chân nhà sát mặt đất

                shedRoot.transform.position = targetWorldPos;

                var visual = shedRoot.transform.Find("Visual");
                if (visual != null)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                }
                modelT.localPosition = Vector3.zero;
                modelT.localRotation = Quaternion.Euler(270f, 0f, 0f);

                var col = shedRoot.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 2.5f, 0f);
                    col.size = new Vector3(5.5f, 5.0f, 5.0f);
                }
                EditorUtility.SetDirty(shedRoot);
                Debug.Log($"[RefinementTool] Đã thống nhất tọa độ Nhà xưởng (Model_RusticWorkshopShed) tại {shedRoot.transform.position}.");
            }
        }

        // 4. Mỏ đá (Model_QuarryPitCave)
        var quarryRoot = GameObject.Find("Model_QuarryPitCave");
        if (quarryRoot != null)
        {
            var modelT = quarryRoot.transform.Find("Visual/Model");
            if (modelT != null)
            {
                Vector3 targetWorldPos = modelT.position;
                targetWorldPos.y = 0f;

                quarryRoot.transform.position = targetWorldPos;

                var visual = quarryRoot.transform.Find("Visual");
                if (visual != null)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                }
                modelT.localPosition = Vector3.zero;
                modelT.localRotation = Quaternion.Euler(270f, 0f, 0f);

                var col = quarryRoot.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 1.5f, 0f);
                    col.size = new Vector3(4.0f, 3.0f, 4.0f);
                }
                EditorUtility.SetDirty(quarryRoot);
                Debug.Log($"[RefinementTool] Đã thống nhất tọa độ Mỏ đá (Model_QuarryPitCave) tại {quarryRoot.transform.position}.");
            }
        }
    }

    /// <summary>
    /// VẤN ĐỀ 3: Cân chỉnh Máy Tier 2 và Tier 3 chuẩn xác theo đúng ảnh chụp thực tế của người dùng:
    /// - Tier 2 Model: Pos = (0.3592021, -0.218793, -0.360023), Rot = (-90, 0, -90), Scale = (2, 2, 2)
    /// - Tier 3 Model: Pos = (-2.283703, -0.326551, -0.36), Rot = (-90, 0, 0), Scale = (2, 2, 2)
    /// </summary>
    private static void StandardizeTiers2And3()
    {
        // 1. Cập nhật file Prefab Visual_Ore_Tier2.prefab
        string t2Path = $"{MACH_PREFAB_DIR}/Visual_Ore_Tier2.prefab";
        var t2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(t2Path);
        if (t2Prefab != null)
        {
            var visT = t2Prefab.transform.Find("Visual");
            if (visT != null)
            {
                visT.localPosition = new Vector3(0.01f, 0.69f, 0f);
                visT.localRotation = Quaternion.identity;
                visT.localScale = new Vector3(1.16f, 1.16f, 1.16f);

                var modelT = visT.Find("Model");
                if (modelT != null)
                {
                    modelT.localPosition = new Vector3(0.3592021f, -0.218793f, -0.360023f);
                    modelT.localRotation = Quaternion.Euler(-90f, 0f, -90f);
                    modelT.localScale = new Vector3(2f, 2f, 2f);
                }
            }
            EditorUtility.SetDirty(t2Prefab);
            Debug.Log("[RefinementTool] Đã cập nhật file Prefab Visual_Ore_Tier2 chuẩn theo ảnh người dùng!");
        }

        // 2. Cập nhật file Prefab Visual_Ore_Tier3.prefab
        string t3Path = $"{MACH_PREFAB_DIR}/Visual_Ore_Tier3.prefab";
        var t3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(t3Path);
        if (t3Prefab != null)
        {
            var visT = t3Prefab.transform.Find("Visual");
            if (visT != null)
            {
                visT.localPosition = new Vector3(0.01f, 0.69f, 0f);
                visT.localRotation = Quaternion.identity;
                visT.localScale = new Vector3(1.16f, 1.16f, 1.16f);

                var modelT = visT.Find("Model");
                if (modelT != null)
                {
                    modelT.localPosition = new Vector3(0f, -0.326551f, -0.36f);
                    modelT.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    modelT.localScale = new Vector3(2f, 2f, 2f);
                }
            }
            EditorUtility.SetDirty(t3Prefab);
            Debug.Log("[RefinementTool] Đã cập nhật file Prefab Visual_Ore_Tier3 chuẩn theo ảnh người dùng!");
        }

        // 3. Cập nhật các instance Visual_Ore_Tier2 và Visual_Ore_Tier3 đang nằm sẵn trong Scene (trên Machine_Wood...)
        foreach (var t2Instance in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (t2Instance.name == "Visual_Ore_Tier2" && t2Instance.scene.isLoaded)
            {
                var visT = t2Instance.transform.Find("Visual");
                if (visT != null)
                {
                    visT.localPosition = new Vector3(0.01f, 0.69f, 0f);
                    visT.localScale = new Vector3(1.16f, 1.16f, 1.16f);
                    var modelT = visT.Find("Model");
                    if (modelT != null)
                    {
                        modelT.localPosition = new Vector3(0.3592021f, -0.218793f, -0.360023f);
                        modelT.localRotation = Quaternion.Euler(-90f, 0f, -90f);
                        modelT.localScale = new Vector3(2f, 2f, 2f);
                    }
                }
                EditorUtility.SetDirty(t2Instance);
            }
            else if (t2Instance.name == "Visual_Ore_Tier3" && t2Instance.scene.isLoaded)
            {
                var visT = t2Instance.transform.Find("Visual");
                if (visT != null)
                {
                    visT.localPosition = new Vector3(0.01f, 0.69f, 0f);
                    visT.localScale = new Vector3(1.16f, 1.16f, 1.16f);
                    var modelT = visT.Find("Model");
                    if (modelT != null)
                    {
                        modelT.localPosition = new Vector3(0f, -0.326551f, -0.36f);
                        modelT.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                        modelT.localScale = new Vector3(2f, 2f, 2f);
                    }
                }
                EditorUtility.SetDirty(t2Instance);
            }
        }

        // 4. Ẩn toàn bộ các visual cấp cao (Tier 2, Tier 3, model cũ) trên các máy trong Scene, chỉ giữ lại Tier 1 khi khởi động
        foreach (var mc in Resources.FindObjectsOfTypeAll<MachineController>())
        {
            if (mc.gameObject.scene.isLoaded)
            {
                for (int i = 0; i < mc.transform.childCount; i++)
                {
                    var child = mc.transform.GetChild(i);
                    if (child.name.StartsWith("Visual"))
                    {
                        bool isTier1 = child.name == "Visual_Ore_Tier1";
                        child.gameObject.SetActive(isTier1);
                    }
                }
                EditorUtility.SetDirty(mc);
            }
        }
    }

    /// <summary>
    /// VẤN ĐỀ 4: Tọa độ SpawnPoint nâng lên cao đẹp mắt, lấy Máy Gỗ làm chuẩn và áp dụng cho tất cả các máy khác.
    /// Tọa độ chuẩn từ ảnh chụp của người dùng: X = 0, Y = -0.275, Z = -0.105555475
    /// </summary>
    private static void SyncSpawnPointsFromWood()
    {
        Vector3 targetLocalPos = new Vector3(0f, -0.275f, -0.105555475f);

        // 1. Quét tìm tất cả các Machine trong Scene (kể cả các máy đang bị khóa/inactive)
        foreach (var rootGo in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var machControllers = rootGo.GetComponentsInChildren<MachineController>(true);
            foreach (var mc in machControllers)
            {
                var sp = mc.transform.Find("SpawnPoint");
                if (sp != null)
                {
                    sp.localPosition = targetLocalPos;
                    sp.localRotation = Quaternion.identity;
                    sp.localScale = Vector3.one;
                    EditorUtility.SetDirty(mc.gameObject);
                    Debug.Log($"[RefinementTool] Đã cập nhật SpawnPoint cho {mc.gameObject.name} -> {targetLocalPos}");
                }
            }
        }

        // 2. Cập nhật vào toàn bộ các file Prefab
        string[] machTypes = new[] { "Wood", "Stone", "Copper", "Iron", "Silver", "Gold" };
        foreach (var type in machTypes)
        {
            string prefabPath = $"{PREFAB_DIR}/ProductionLine_{type}.prefab";
            var pObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pObj != null)
            {
                var sp = pObj.transform.Find($"Machine_{type}/SpawnPoint");
                if (sp != null)
                {
                    sp.localPosition = targetLocalPos;
                    sp.localRotation = Quaternion.identity;
                    sp.localScale = Vector3.one;
                    EditorUtility.SetDirty(pObj);
                }
            }
        }

        Debug.Log($"[RefinementTool] Đã đồng bộ SpawnPoint chuẩn (0, -0.275, -0.1055) cho toàn bộ 6 máy trong Scene và Prefabs!");
    }

    /// <summary>
    /// VẤN ĐỀ 5: Sửa bệ chứa Pallet:
    /// - Không bị méo scale (Scale = 1:1)
    /// - Tạo bệ ItemSlotRoot nằm chuẩn giữa mặt ván gỗ
    /// - Khoảng cách dãn cách 3x3 vừa khít trong ván gỗ
    /// - Chiều cao tầng 2 (Y-step = 0.35m) chuẩn xác, tầng 2 xếp lên trên tầng 1 gọn gàng.
    /// </summary>
    private static void FixPalletStackingAndAlignment()
    {
        string[] types = new[] { "Wood", "Stone", "Copper", "Iron", "Silver", "Gold" };

        foreach (var type in types)
        {
            var storageObj = GameObject.Find($"StoragePad_{type}");
            if (storageObj != null)
            {
                // Đặt StoragePad về vị trí chuẩn Z = -1.25 đón hàng theo ảnh 4 mẫu của người dùng
                storageObj.transform.localPosition = new Vector3(0f, 0f, -1.25f);
                storageObj.transform.localScale = Vector3.one;

                var boxCol = storageObj.GetComponent<BoxCollider>();
                if (boxCol != null)
                {
                    boxCol.center = new Vector3(0f, 0.75f, 0f);
                    boxCol.size = new Vector3(1.5f, 1.5f, 1.5f);
                    boxCol.isTrigger = true;
                }

                // Căn Visual_Pallet nằm chính xác tại tâm sàn
                var visPallet = storageObj.transform.Find("Visual_Pallet");
                if (visPallet != null)
                {
                    visPallet.localPosition = Vector3.zero;
                    visPallet.localRotation = Quaternion.identity;
                    visPallet.localScale = Vector3.one;

                    var visChild = visPallet.Find("Visual");
                    if (visChild != null)
                    {
                        visChild.localPosition = Vector3.zero;
                        visChild.localScale = new Vector3(1.42f, 1.42f, 1.42f);

                        var modelChild = visChild.Find("Model");
                        if (modelChild != null)
                        {
                            modelChild.localPosition = Vector3.zero;
                            modelChild.localRotation = Quaternion.identity;
                            modelChild.localScale = new Vector3(0.65f, 0.65f, 0.65f); // Kích thước ván gỗ vừa vặn đẹp mắt
                        }
                    }
                }

                // Tạo điểm gốc ItemSlotRoot chuẩn đặt trên mặt pallet
                var slotRoot = storageObj.transform.Find("ItemSlotRoot");
                if (slotRoot == null)
                {
                    var srGo = new GameObject("ItemSlotRoot");
                    srGo.transform.SetParent(storageObj.transform, false);
                    slotRoot = srGo.transform;
                }
                slotRoot.localPosition = new Vector3(0f, 0.15f, 0f);
                slotRoot.localRotation = Quaternion.identity;
                slotRoot.localScale = Vector3.one;

                var sp = storageObj.GetComponent<StoragePad>();
                if (sp != null)
                {
                    var spSO = new SerializedObject(sp);
                    var rootProp = spSO.FindProperty("itemSlotRoot");
                    if (rootProp != null) rootProp.objectReferenceValue = slotRoot;

                    // Dãn cách chuẩn: 3x3 vừa khít mặt pallet (0.28m) và chiều cao tầng 0.35m
                    var offsetProp = spSO.FindProperty("itemOffset");
                    if (offsetProp != null) offsetProp.vector3Value = new Vector3(0.28f, 0.35f, 0.28f);

                    spSO.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(storageObj);
            }

            // Cập nhật cả file Prefab tương ứng
            string prefabPath = $"{PREFAB_DIR}/ProductionLine_{type}.prefab";
            var pObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pObj != null)
            {
                var pStorage = pObj.transform.Find($"StoragePad_{type}");
                if (pStorage != null)
                {
                    pStorage.localPosition = new Vector3(0f, 0f, -1.25f);
                    pStorage.localScale = Vector3.one;

                    var pCol = pStorage.GetComponent<BoxCollider>();
                    if (pCol != null)
                    {
                        pCol.center = new Vector3(0f, 0.75f, 0f);
                        pCol.size = new Vector3(1.5f, 1.5f, 1.5f);
                    }

                    var pSlotRoot = pStorage.Find("ItemSlotRoot");
                    if (pSlotRoot == null)
                    {
                        var srGo = new GameObject("ItemSlotRoot");
                        srGo.transform.SetParent(pStorage, false);
                        pSlotRoot = srGo.transform;
                    }
                    pSlotRoot.localPosition = new Vector3(0f, 0.15f, 0f);
                    pSlotRoot.localScale = Vector3.one;

                    var pSP = pStorage.GetComponent<StoragePad>();
                    if (pSP != null)
                    {
                        var pSO = new SerializedObject(pSP);
                        var rProp = pSO.FindProperty("itemSlotRoot");
                        if (rProp != null) rProp.objectReferenceValue = pSlotRoot;
                        var oProp = pSO.FindProperty("itemOffset");
                        if (oProp != null) oProp.vector3Value = new Vector3(0.28f, 0.35f, 0.28f);
                        pSO.ApplyModifiedProperties();
                    }

                    EditorUtility.SetDirty(pObj);
                }
            }
        }

        Debug.Log("[RefinementTool] Đã chuẩn hóa bệ chứa Pallet: Không méo scale, căn giữa ván gỗ, xếp tầng 2 cao 0.35m chuẩn xác!");
    }
}
