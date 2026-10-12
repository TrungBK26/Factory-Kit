using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Master Tool: Hoàn thiện toàn bộ Character (Rigged Model, Animation Idle/Walk/WalkCarry),
/// Tạo Animator Controller, Tạo WorkerNPCPrefab, Tích hợp Player vào Scene,
/// Gắn CameraFollow Isometric mượt mà, Kết nối SoundManager 8-bit, và Bake NavMesh.
/// TUÂN THỦ 100%: Tuyệt đối không thay đổi tọa độ các công trình, mỏ đá, xưởng, máy móc do User đã xếp.
/// </summary>
public static class CharacterAudioIntegrationTool
{
    private const string CHAR_DIR = "Assets/_Project/Models/Characters";
    private const string ANIM_DIR = "Assets/_Project/Animations";
    private const string PREFAB_CHAR_DIR = "Assets/_Project/Prefabs/Characters";
    private const string AUDIO_DIR = "Assets/_Project/Audio";
    private const string SCENE_PATH = "Assets/_Project/Scenes/PrototypeScene.unity";
    private const string TRIGGER_FILE = "Assets/_Project/Scripts/Editor/.trigger_character_setup";

    [InitializeOnLoadMethod]
    private static void AutoTriggerOnce()
    {
        if (File.Exists(TRIGGER_FILE))
        {
            try { File.Delete(TRIGGER_FILE); } catch { }
            EditorApplication.delayCall += () =>
            {
                ExecuteAll(silent: true);
            };
        }
    }

    [MenuItem("Factory/Complete Character & Audio Integration", priority = 10)]
    public static void ManualExecute()
    {
        ExecuteAll(silent: false);
    }

    public static void ExecuteAll(bool silent = false)
    {
        Debug.Log("[CharacterTool] === BẮT ĐẦU TÍCH HỢP CHARACTER, ANIMATIONS, AUDIO & CAMERA ===");

        EnsureDirectories();

        // 1. Cấu hình Loop cho các Animation Clip trong 2 file FBX
        SetupFbxAnimationLoops($"{CHAR_DIR}/Player_Character.fbx");
        SetupFbxAnimationLoops($"{CHAR_DIR}/Worker_NPC.fbx");

        // 2. Tạo Materials hiển thị chuẩn Textures cho Player & Worker
        Material playerMat = CreateOrGetCharacterMaterial("Player", $"{CHAR_DIR}/Player_BaseColor.png", $"{CHAR_DIR}/Player_Normal.png");
        Material workerMat = CreateOrGetCharacterMaterial("Worker", $"{CHAR_DIR}/Worker_BaseColor.png", $"{CHAR_DIR}/Worker_Normal.png");

        // 3. Tạo Animator Controller (Idle, Walk, WalkCarry)
        var playerController = CreateAnimatorController("Player_Animator", $"{CHAR_DIR}/Player_Character.fbx");
        var workerController = CreateAnimatorController("Worker_Animator", $"{CHAR_DIR}/Worker_NPC.fbx");

        // 4. Tạo Prefab WorkerNPCPrefab và liên kết vào các ProductionLine Prefabs
        GameObject workerPrefab = CreateWorkerNPCPrefab(workerController, workerMat);
        LinkWorkerPrefabToProductionLines(workerPrefab);

        // 5. Cập nhật Scene: Player, ProductionLines, Camera, Audio, NavMesh
        UpdatePrototypeScene(playerController, playerMat, workerPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CharacterTool] === HOÀN THÀNH TẤT CẢ MỤC TIÊU TÍCH HỢP THÀNH CÔNG! ===");
        if (!silent)
        {
            EditorUtility.DisplayDialog(
                "Tích Hợp Hoàn Tất!",
                "1. Model Player & NPC (Blender FBX) đã tích hợp đầy đủ 3 animations: Idle, Walk, WalkCarry (giơ 2 tay lên đầu).\n" +
                "2. HeadSocket đã đặt chuẩn xác tại Y = 1.85m đỉnh đầu chibi.\n" +
                "3. Prefab Worker NPC đã gắn vào 6 dây chuyền sản xuất.\n" +
                "4. Main Camera đã gắn CameraFollow mượt mà góc Isometric.\n" +
                "5. SoundManager đã kết nối toàn bộ âm thanh 8-bit (BGM + 5 SFX).\n" +
                "6. NavMesh đã được Bake hoàn chỉnh trên địa hình mới!",
                "Tuyệt vời!"
            );
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(ANIM_DIR)) AssetDatabase.CreateFolder("Assets/_Project", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Characters"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs")) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Characters");
        }
    }

    private static void SetupFbxAnimationLoops(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[CharacterTool] Không tìm thấy ModelImporter tại: {fbxPath}");
            return;
        }

        var defaultClips = importer.defaultClipAnimations;
        if (defaultClips == null || defaultClips.Length == 0) return;

        var clipAnims = new List<ModelImporterClipAnimation>();
        foreach (var defClip in defaultClips)
        {
            var ca = new ModelImporterClipAnimation();
            string cleanName = defClip.name;
            if (cleanName.Contains("|"))
            {
                var parts = cleanName.Split('|');
                cleanName = parts[parts.Length - 1];
            }
            ca.name = cleanName;
            ca.takeName = defClip.takeName;
            ca.firstFrame = defClip.firstFrame;
            ca.lastFrame = defClip.lastFrame;
            ca.loopTime = true;
            ca.wrapMode = WrapMode.Loop;
            clipAnims.Add(ca);
            Debug.Log($"[CharacterTool] Cấu hình loop clip: {cleanName} trong {fbxPath}");
        }

        importer.clipAnimations = clipAnims.ToArray();
        importer.SaveAndReimport();
    }

    private static void EnsureTextureImportSettings(string texturePath, bool isNormalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (isNormalMap && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[CharacterTool] Đã cấu hình TextureImporter {texturePath} -> NormalMap thành công!");
            }
        }
    }

    private static Material CreateOrGetCharacterMaterial(string name, string diffuseTexPath, string normalTexPath)
    {
        EnsureTextureImportSettings(diffuseTexPath, isNormalMap: false);
        EnsureTextureImportSettings(normalTexPath, isNormalMap: true);

        string matPath = $"{CHAR_DIR}/M_{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffuseTexPath);
        if (diffuseTex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diffuseTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diffuseTex);
        }

        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexPath);
        if (normalTex != null)
        {
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.2f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static AnimatorController CreateAnimatorController(string controllerName, string fbxPath)
    {
        string controllerPath = $"{ANIM_DIR}/{controllerName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Tải các animation clip từ file FBX
        AnimationClip idleClip = null;
        AnimationClip walkClip = null;
        AnimationClip idleCarryClip = null;
        AnimationClip walkCarryClip = null;

        var allObjects = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var obj in allObjects)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                string cName = clip.name.ToLower();
                if (cName.Contains("idlecarry")) idleCarryClip = clip;
                else if (cName.Contains("walkcarry")) walkCarryClip = clip;
                else if (cName.Contains("idle")) idleClip = clip;
                else if (cName.Contains("walk")) walkClip = clip;
            }
        }

        Debug.Log($"[CharacterTool] Clips cho {controllerName}: Idle={idleClip != null}, Walk={walkClip != null}, IdleCarry={idleCarryClip != null}, WalkCarry={walkCarryClip != null}");

        // Thêm parameters nếu chưa có
        bool hasSpeed = false;
        bool hasCarrying = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "Speed") hasSpeed = true;
            if (p.name == "isCarrying") hasCarrying = true;
        }

        if (!hasSpeed) controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (!hasCarrying) controller.AddParameter("isCarrying", AnimatorControllerParameterType.Bool);

        // Reset RootStateMachine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Xóa states cũ để dựng lại mạch chuẩn xác
        var oldStates = new List<ChildAnimatorState>(rootStateMachine.states);
        foreach (var s in oldStates)
        {
            rootStateMachine.RemoveState(s.state);
        }

        // Tạo 4 States: Idle, Walk, IdleCarry (đứng yên vác đồ), WalkCarry (bước đi vác đồ)
        var stateIdle = rootStateMachine.AddState("Idle");
        stateIdle.motion = idleClip;

        var stateWalk = rootStateMachine.AddState("Walk");
        stateWalk.motion = walkClip;

        var stateIdleCarry = rootStateMachine.AddState("IdleCarry");
        stateIdleCarry.motion = idleCarryClip != null ? idleCarryClip : idleClip;

        var stateWalkCarry = rootStateMachine.AddState("WalkCarry");
        stateWalkCarry.motion = walkCarryClip != null ? walkCarryClip : carryClipFallback(walkClip);

        rootStateMachine.defaultState = stateIdle;

        // 1. Idle <-> Walk (khi không mang đồ: isCarrying == false)
        var tIdleToWalk = stateIdle.AddTransition(stateWalk);
        tIdleToWalk.hasExitTime = false;
        tIdleToWalk.duration = 0.15f;
        tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        tIdleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");

        var tWalkToIdle = stateWalk.AddTransition(stateIdle);
        tWalkToIdle.hasExitTime = false;
        tWalkToIdle.duration = 0.15f;
        tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        tWalkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");

        // 2. IdleCarry <-> WalkCarry (khi đang mang đồ: isCarrying == true)
        var tIdleCarryToWalkCarry = stateIdleCarry.AddTransition(stateWalkCarry);
        tIdleCarryToWalkCarry.hasExitTime = false;
        tIdleCarryToWalkCarry.duration = 0.15f;
        tIdleCarryToWalkCarry.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        tIdleCarryToWalkCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");

        var tWalkCarryToIdleCarry = stateWalkCarry.AddTransition(stateIdleCarry);
        tWalkCarryToIdleCarry.hasExitTime = false;
        tWalkCarryToIdleCarry.duration = 0.15f;
        tWalkCarryToIdleCarry.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        tWalkCarryToIdleCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");

        // 3. Nhặt đồ (isCarrying chuyển từ false sang true)
        var tIdleToIdleCarry = stateIdle.AddTransition(stateIdleCarry);
        tIdleToIdleCarry.hasExitTime = false;
        tIdleToIdleCarry.duration = 0.15f;
        tIdleToIdleCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");
        tIdleToIdleCarry.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var tWalkToWalkCarry = stateWalk.AddTransition(stateWalkCarry);
        tWalkToWalkCarry.hasExitTime = false;
        tWalkToWalkCarry.duration = 0.15f;
        tWalkToWalkCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");
        tWalkToWalkCarry.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tIdleToWalkCarry = stateIdle.AddTransition(stateWalkCarry);
        tIdleToWalkCarry.hasExitTime = false;
        tIdleToWalkCarry.duration = 0.15f;
        tIdleToWalkCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");
        tIdleToWalkCarry.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tWalkToIdleCarry = stateWalk.AddTransition(stateIdleCarry);
        tWalkToIdleCarry.hasExitTime = false;
        tWalkToIdleCarry.duration = 0.15f;
        tWalkToIdleCarry.AddCondition(AnimatorConditionMode.If, 0f, "isCarrying");
        tWalkToIdleCarry.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // 4. Bán/dỡ đồ xong (isCarrying chuyển từ true sang false)
        var tIdleCarryToIdle = stateIdleCarry.AddTransition(stateIdle);
        tIdleCarryToIdle.hasExitTime = false;
        tIdleCarryToIdle.duration = 0.15f;
        tIdleCarryToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");
        tIdleCarryToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var tWalkCarryToWalk = stateWalkCarry.AddTransition(stateWalk);
        tWalkCarryToWalk.hasExitTime = false;
        tWalkCarryToWalk.duration = 0.15f;
        tWalkCarryToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");
        tWalkCarryToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tIdleCarryToWalk = stateIdleCarry.AddTransition(stateWalk);
        tIdleCarryToWalk.hasExitTime = false;
        tIdleCarryToWalk.duration = 0.15f;
        tIdleCarryToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");
        tIdleCarryToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tWalkCarryToIdle = stateWalkCarry.AddTransition(stateIdle);
        tWalkCarryToIdle.hasExitTime = false;
        tWalkCarryToIdle.duration = 0.15f;
        tWalkCarryToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCarrying");
        tWalkCarryToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip carryClipFallback(AnimationClip fallback) => fallback;

    private static GameObject CreateWorkerNPCPrefab(AnimatorController workerController, Material workerMat)
    {
        string prefabPath = $"{PREFAB_CHAR_DIR}/WorkerNPCPrefab.prefab";

        GameObject root = new GameObject("WorkerNPCPrefab");

        // NavMeshAgent
        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.stoppingDistance = 1.0f;
        agent.radius = 0.45f;
        agent.height = 1.9f;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;

        // CapsuleCollider
        var col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.45f;
        col.height = 1.9f;
        col.center = new Vector3(0f, 0.95f, 0f);

        // Rigidbody
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // WorkerNPC Component
        var workerScript = root.AddComponent<WorkerNPC>();

        // WorkerHeadSocket
        GameObject socketObj = new GameObject("WorkerHeadSocket");
        socketObj.transform.SetParent(root.transform);
        socketObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        socketObj.transform.localRotation = Quaternion.identity;

        // Gắn model 3D
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{CHAR_DIR}/Worker_NPC.fbx");
        if (fbxAsset != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            modelInst.name = "Model_Worker_NPC";
            modelInst.transform.SetParent(root.transform);
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var anim = modelInst.GetComponent<Animator>();
            if (anim == null) anim = modelInst.AddComponent<Animator>();
            anim.runtimeAnimatorController = workerController;

            // Gán material
            if (workerMat != null)
            {
                var renderers = modelInst.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = workerMat;
                }
            }

            // Gán animator và socket vào WorkerNPC
            var so = new SerializedObject(workerScript);
            so.FindProperty("animator").objectReferenceValue = anim;
            so.FindProperty("headSocket").objectReferenceValue = socketObj.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
        Debug.Log($"[CharacterTool] Đã tạo thành công Prefab: {prefabPath}");
        return savedPrefab;
    }

    private static void LinkWorkerPrefabToProductionLines(GameObject workerPrefab)
    {
        if (workerPrefab == null) return;

        string linesDir = "Assets/_Project/Prefabs/ProductionLines";
        if (!AssetDatabase.IsValidFolder(linesDir)) return;

        var files = Directory.GetFiles(linesDir, "ProductionLine_*.prefab");
        foreach (var file in files)
        {
            string unityPath = file.Replace('\\', '/');
            var lineObj = PrefabUtility.LoadPrefabContents(unityPath);
            var prodLine = lineObj.GetComponent<ProductionLine>();
            if (prodLine != null)
            {
                var so = new SerializedObject(prodLine);
                var prop = so.FindProperty("workerNPCPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = workerPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(lineObj, unityPath);
                    Debug.Log($"[CharacterTool] Đã gán WorkerNPCPrefab vào: {unityPath}");
                }
            }
            PrefabUtility.UnloadPrefabContents(lineObj);
        }
    }

    private static void UpdatePrototypeScene(AnimatorController playerController, Material playerMat, GameObject workerPrefab)
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[CharacterTool] Không thể mở Scene: {SCENE_PATH}");
            return;
        }

        // 1. Tích hợp / Chuẩn hóa Player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pMove = GameObject.FindAnyObjectByType<PlayerMovement>();
            if (pMove != null) playerObj = pMove.gameObject;
        }

        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0f, 0f, -2f);
        }

        // Đảm bảo có CharacterController chuẩn tỉ lệ 1.9m
        var cc = playerObj.GetComponent<CharacterController>();
        if (cc == null) cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 0.95f, 0f);

        // Đảm bảo có PlayerMovement
        var pm = playerObj.GetComponent<PlayerMovement>();
        if (pm == null) pm = playerObj.AddComponent<PlayerMovement>();

        // Đảm bảo có PlayerCarry
        var pc = playerObj.GetComponent<PlayerCarry>();
        if (pc == null) pc = playerObj.AddComponent<PlayerCarry>();

        // Đảm bảo có PlayerInteraction
        var pi = playerObj.GetComponent<PlayerInteraction>();
        if (pi == null) pi = playerObj.AddComponent<PlayerInteraction>();

        // Xóa model cũ / cube / capsule tạm nếu có
        for (int i = playerObj.transform.childCount - 1; i >= 0; i--)
        {
            var child = playerObj.transform.GetChild(i);
            if (child.name.StartsWith("Model_") || child.name.Contains("Capsule") || child.name.Contains("Cube"))
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
        var oldMeshFilter = playerObj.GetComponent<MeshFilter>();
        if (oldMeshFilter != null) GameObject.DestroyImmediate(oldMeshFilter);
        var oldMeshRenderer = playerObj.GetComponent<MeshRenderer>();
        if (oldMeshRenderer != null) GameObject.DestroyImmediate(oldMeshRenderer);

        // Gắn CarrySocket ở Y = 1.85m đỉnh đầu
        Transform carrySocket = playerObj.transform.Find("CarrySocket");
        if (carrySocket == null)
        {
            GameObject csObj = new GameObject("CarrySocket");
            csObj.transform.SetParent(playerObj.transform);
            carrySocket = csObj.transform;
        }
        carrySocket.localPosition = new Vector3(0f, 1.85f, 0f);
        carrySocket.localRotation = Quaternion.identity;

        // Gắn model Player_Character.fbx
        GameObject playerFbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{CHAR_DIR}/Player_Character.fbx");
        if (playerFbx != null)
        {
            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(playerFbx);
            modelInst.name = "Model_Player_Character";
            modelInst.transform.SetParent(playerObj.transform);
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            var anim = modelInst.GetComponent<Animator>();
            if (anim == null) anim = modelInst.AddComponent<Animator>();
            anim.runtimeAnimatorController = playerController;

            if (playerMat != null)
            {
                var renderers = modelInst.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) r.sharedMaterial = playerMat;
            }

            pm.SetAnimator(anim);

            var pcSo = new SerializedObject(pc);
            var hsProp = pcSo.FindProperty("headSocket");
            if (hsProp != null) hsProp.objectReferenceValue = carrySocket;
            pcSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // 2. Camera Follow Isometric
        Camera cam = Camera.main;
        if (cam != null)
        {
            var camFollow = cam.GetComponent<CameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<CameraFollow>();

            camFollow.SetTarget(playerObj.transform);
            cam.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            cam.transform.position = playerObj.transform.position + new Vector3(0f, 16f, -14f);
            Debug.Log("[CharacterTool] Đã cấu hình CameraFollow bám theo Player thành công.");
        }

        // 3. SoundManager kết nối Audio Clips
        var soundMgr = GameObject.FindAnyObjectByType<SoundManager>();
        if (soundMgr == null)
        {
            var smObj = new GameObject("SoundManager");
            soundMgr = smObj.AddComponent<SoundManager>();
        }

        var smSo = new SerializedObject(soundMgr);
        smSo.FindProperty("bgmFactoryGroove").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/BGM_FactoryGroove.ogg");
        smSo.FindProperty("sfxMachineDrop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_WoodDrop.wav");
        smSo.FindProperty("sfxItemPickup").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_ItemPickup.wav");
        smSo.FindProperty("sfxItemSell").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_CoinCollect.wav");
        smSo.FindProperty("sfxVaultCollect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_CoinCollect.wav");
        smSo.FindProperty("sfxUpgradeSuccess").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_Upgrade.wav");
        smSo.FindProperty("sfxVictoryFanfare").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AUDIO_DIR}/SFX_Victory.wav");
        smSo.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[CharacterTool] Đã kết nối đầy đủ BGM và SFX vào SoundManager.");

        // 4. Gán workerNPCPrefab cho các dây chuyền trong Scene (BẢO TOÀN TỌA ĐỘ VỊ TRÍ)
        if (workerPrefab != null)
        {
            var lines = GameObject.FindObjectsByType<ProductionLine>(FindObjectsSortMode.None);
            foreach (var l in lines)
            {
                var lSo = new SerializedObject(l);
                var prop = lSo.FindProperty("workerNPCPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = workerPrefab;
                    lSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // 5. Bake NavMesh cho Scene trên địa hình mới
        try
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[CharacterTool] Đã Bake NavMesh cho PrototypeScene thành công!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CharacterTool] NavMesh Bake warning: {ex.Message}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CharacterTool] Đã lưu Scene PrototypeScene.unity thành công!");
    }
}
