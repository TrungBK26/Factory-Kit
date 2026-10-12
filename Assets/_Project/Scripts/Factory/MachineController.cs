using UnityEngine;

/// <summary>
/// Điều khiển cỗ máy sản xuất tài nguyên (Resource Spawner & 3 Tiers Machine).
/// Nhả hàng hóa lên băng chuyền theo chu kỳ spawnInterval,
/// chuyển đổi ngoại hình theo 3 cấp (Tier 1 -> 2 -> 3)
/// và tự động ngắt nhả khi Bệ chứa hoặc Băng tải dồn ứ (Backpressure Cap).
/// </summary>
public class MachineController : MonoBehaviour, IInteractable
{
    [Header("--- CẤU HÌNH MÁY SẢN XUẤT ---")]
    [SerializeField] private MachineConfigSO machineConfig;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ConveyorBelt targetConveyor;
    [SerializeField] private StoragePad targetStoragePad;

    [Header("--- ĐIỂM GẮN NGOẠI HÌNH (VISUAL ROOT) ---")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject currentVisualInstance;

    [Header("--- TRẠNG THÁI HIỆN TẠI ---")]
    [SerializeField] private int currentTier = 1;
    [SerializeField] private int currentSpeedLevel = 0;

    private float spawnTimer = 0f;

    public MachineConfigSO Config => machineConfig;
    public int CurrentTier => currentTier;
    public int CurrentSpeedLevel => currentSpeedLevel;

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;

        // Tự động tìm nạp cấu hình nếu chưa gán
        if (machineConfig == null)
        {
            var configs = Resources.FindObjectsOfTypeAll<MachineConfigSO>();
            if (configs != null && configs.Length > 0)
            {
                machineConfig = configs[0];
            }
        }
    }

    private void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Khởi tạo thông số máy từ Slot cụm dây chuyền (ProductionLine).
    /// </summary>
    public void Initialize(MachineConfigSO config, ConveyorBelt conveyor, StoragePad storagePad)
    {
        this.machineConfig = config;
        this.targetConveyor = conveyor;
        this.targetStoragePad = storagePad;

        if (conveyor != null && storagePad != null && conveyor.TargetStoragePad == null)
        {
            conveyor.TargetStoragePad = storagePad;
        }

        UpdateVisuals();
    }

    private void Update()
    {
        if (machineConfig == null)
        {
            var configs = Resources.FindObjectsOfTypeAll<MachineConfigSO>();
            if (configs != null && configs.Length > 0)
            {
                machineConfig = configs[0];
            }
            if (machineConfig == null) return;
        }

        // 1. Kiểm tra cơ chế chống tràn (Backpressure): nếu bệ chứa đã đầy, tạm ngưng chu kỳ nhả hàng
        if (targetStoragePad != null && targetStoragePad.IsFull)
        {
            return;
        }

        float currentInterval = GetCurrentSpawnInterval();
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;
            ProduceItem();
        }
    }

    private void ProduceItem()
    {
        ResourceDataSO resData = GetCurrentResourceData();
        if (resData == null) return;

        // Nếu ResourceDataSO chưa gán itemPrefab, tự động tạo khối cube mẫu dự phòng
        GameObject prefabToSpawn = resData.itemPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = CreateDefaultCubeItem();
        }

        // Tính toán vị trí nhả: đảm bảo rơi chính xác lên bề mặt băng tải
        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.position;
            spawnRot = spawnPoint.rotation;
        }
        else if (targetConveyor != null)
        {
            // Tự động nhả lên đầu băng chuyền
            Vector3 convPos = targetConveyor.transform.position;
            Vector3 convScale = targetConveyor.transform.lossyScale;
            spawnPos = new Vector3(convPos.x, convPos.y + 0.35f, convPos.z + convScale.z * 0.35f);
        }
        else
        {
            spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
        }

        // Sinh vật phẩm từ ObjectPool để đạt chuẩn 0 Bytes GC Alloc
        GameObject itemObj = null;
        if (ResourcePoolManager.Instance != null)
        {
            itemObj = ResourcePoolManager.Instance.Spawn(prefabToSpawn, spawnPos, spawnRot);
        }
        else
        {
            itemObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);
        }

        if (itemObj != null)
        {
            itemObj.tag = "ResourceItem";

            // Gán dữ liệu tài nguyên
            if (!itemObj.TryGetComponent<ResourceItemHolder>(out var holder))
            {
                holder = itemObj.AddComponent<ResourceItemHolder>();
            }
            holder.Data = resData;

            // Đảm bảo Rigidbody và Collider sẵn sàng trượt trên băng chuyền
            if (!itemObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = itemObj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (itemObj.TryGetComponent<Collider>(out var col))
            {
                col.enabled = true;
            }
            else
            {
                var box = itemObj.AddComponent<BoxCollider>();
                box.enabled = true;
            }

            GameEvents.OnItemProduced?.Invoke();
            Debug.Log($"[Machine] {machineConfig.machineName} đã nhả 1 kiện {resData.resourceName} ({resData.baseValue}$) tại {spawnPos}.");
        }
    }

    private GameObject CreateDefaultCubeItem()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Default_ResourceCube";
        cube.tag = "ResourceItem";
        cube.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
        var rb = cube.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        cube.AddComponent<ResourceItemHolder>();
        return cube;
    }

    public void SetTier(int newTier)
    {
        currentTier = Mathf.Clamp(newTier, 1, 3);
        UpdateVisuals();

        if (machineConfig != null)
        {
            GameEvents.OnMachineTierUpgraded?.Invoke(machineConfig.resourceType, currentTier);
        }

        Debug.Log($"[Machine] {machineConfig?.machineName} đã nâng cấp lên Tier {currentTier}.");
    }

    public void SetSpeedLevel(int newSpeedLevel)
    {
        currentSpeedLevel = Mathf.Clamp(newSpeedLevel, 0, (machineConfig?.spawnIntervalLevels?.Length ?? 4) - 1);
        Debug.Log($"[Machine] {machineConfig?.machineName} nâng cấp tốc độ cấp {currentSpeedLevel + 1}. Chu kỳ: {GetCurrentSpawnInterval()}s.");
    }

    public float GetCurrentSpawnInterval()
    {
        if (machineConfig != null && machineConfig.spawnIntervalLevels != null && machineConfig.spawnIntervalLevels.Length > 0)
        {
            int index = Mathf.Clamp(currentSpeedLevel, 0, machineConfig.spawnIntervalLevels.Length - 1);
            return machineConfig.spawnIntervalLevels[index];
        }
        return 2.5f; // Mặc định 2.5 giây
    }

    public ResourceDataSO GetCurrentResourceData()
    {
        ResourceDataSO res = null;
        if (machineConfig != null)
        {
            res = currentTier switch
            {
                1 => machineConfig.tier1Resource,
                2 => machineConfig.tier2Resource != null ? machineConfig.tier2Resource : machineConfig.tier1Resource,
                3 => machineConfig.tier3Resource != null ? machineConfig.tier3Resource : (machineConfig.tier2Resource != null ? machineConfig.tier2Resource : machineConfig.tier1Resource),
                _ => machineConfig.tier1Resource
            };

            if (res == null) res = machineConfig.tier1Resource;
        }

        // Tự động tìm ResourceDataSO trên toàn bộ Project nếu chưa có
        if (res == null)
        {
            var allRes = Resources.FindObjectsOfTypeAll<ResourceDataSO>();
            if (allRes != null && allRes.Length > 0)
            {
                res = allRes[0];
            }
        }

        return res;
    }

    private void UpdateVisuals()
    {
        if (machineConfig == null) return;

        GameObject prefabToUse = currentTier switch
        {
            1 => machineConfig.tier1VisualPrefab,
            2 => machineConfig.tier2VisualPrefab,
            3 => machineConfig.tier3VisualPrefab,
            _ => machineConfig.tier1VisualPrefab
        };

        if (prefabToUse != null)
        {
            if (TryGetComponent<MeshRenderer>(out var mr))
            {
                mr.enabled = false;
            }

            Transform parentT = visualRoot != null ? visualRoot : transform;

            // 1. Quét tìm tất cả các visual model con dưới parentT:
            // Ẩn toàn bộ model cấp khác, chỉ kích hoạt model trùng tên prefabToUse
            GameObject matchedInstance = null;
            for (int i = 0; i < parentT.childCount; i++)
            {
                var child = parentT.GetChild(i);
                if (child.name.StartsWith("Visual"))
                {
                    if (child.name == prefabToUse.name && matchedInstance == null)
                    {
                        matchedInstance = child.gameObject;
                        matchedInstance.SetActive(true);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // 2. Nếu chưa có model cho Tier hiện tại thì Instantiate mới
            if (matchedInstance == null)
            {
                matchedInstance = Instantiate(prefabToUse, parentT);
                matchedInstance.name = prefabToUse.name;
                matchedInstance.SetActive(true);
            }

            currentVisualInstance = matchedInstance;
            currentVisualInstance.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            currentVisualInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            currentVisualInstance.transform.localScale = Vector3.one;

            // 3. Chuẩn hóa vị trí model con bên trong
            var visChild = currentVisualInstance.transform.Find("Visual");
            if (visChild != null)
            {
                visChild.localPosition = new Vector3(0.01f, 0.69f, 0f);
                visChild.localScale = new Vector3(1.16f, 1.16f, 1.16f);
                var modelChild = visChild.Find("Model");
                if (modelChild != null)
                {
                    if (currentTier == 2)
                    {
                        // Model Tier 2:
                        modelChild.localPosition = new Vector3(0.3592021f, -0.218793f, -0.360023f);
                        modelChild.localRotation = Quaternion.Euler(-90f, 0f, -90f);
                        modelChild.localScale = new Vector3(2f, 2f, 2f);
                    }
                    else if (currentTier == 3)
                    {
                        // Model Tier 3: Căn giữa tuyệt đối với băng chuyền (X = 0) theo ảnh 3 của người dùng
                        modelChild.localPosition = new Vector3(0f, -0.326551f, -0.36f);
                        modelChild.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                        modelChild.localScale = new Vector3(2f, 2f, 2f);
                    }
                    else // Tier 1 (Lò nung chuẩn)
                    {
                        modelChild.localPosition = new Vector3(0f, 0.32f, -0.36f);
                        modelChild.localRotation = Quaternion.Euler(270f, 0f, 0f);
                        modelChild.localScale = new Vector3(100f, 100f, 100f);
                    }
                }
            }
        }
    }

    // --- IINTERACTABLE: TƯƠNG TÁC PHÍM F ĐỂ MỞ BẢNG NÂNG CẤP ---
    public string GetInteractionPrompt()
    {
        string mName = machineConfig != null ? machineConfig.machineName : name;
        return $"[F] Bảng Nâng Cấp {mName} (Tier {currentTier}/3)";
    }

    public void OnInteract(PlayerCarry playerCarry)
    {
        Debug.Log($"[MachineController] Người chơi bấm F mở bảng nâng cấp cho {name}.");
        FactoryManagementUI.Instance.OpenMenu();
    }
}
