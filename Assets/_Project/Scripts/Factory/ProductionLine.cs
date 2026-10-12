using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý một Cụm Dây Chuyền Sản Xuất Modular (Production Line / Machine Cluster).
/// Cho phép Level Designer cấu hình hoàn toàn qua kéo thả Inspector:
/// các slot vị trí máy (MachineSlot), bệ chứa thành phẩm, điểm sinh công nhân.
/// </summary>
public class ProductionLine : MonoBehaviour
{
    [Header("--- THIẾT LẬP CỤM DÂY CHUYỀN ---")]
    [Tooltip("Định danh duy nhất của cụm dây chuyền (VD: Cluster_Wood, Cluster_Gold)")]
    [SerializeField] private string clusterID = "Cluster_Wood";

    [Tooltip("Bệ chứa thành phẩm tập trung của cụm")]
    [SerializeField] private StoragePad storagePad;

    [Tooltip("Danh sách các slot đặt máy trong cụm")]
    [SerializeField] private List<MachineSlot> machineSlots = new List<MachineSlot>();

    [Tooltip("Danh sách các vị trí xuất hiện của công nhân NPC phụ trách cụm này")]
    [SerializeField] private List<Transform> workerSpawnPoints = new List<Transform>();

    [Tooltip("Vị trí bãi bán hàng SellZone để NPC di chuyển tới")]
    [SerializeField] private Transform sellZoneTransform;

    [Header("--- PREFABS ---")]
    [Tooltip("Prefab gốc của cỗ máy sản xuất")]
    [SerializeField] private GameObject machineBasePrefab;

    [Tooltip("Prefab của công nhân NPC")]
    [SerializeField] private GameObject workerNPCPrefab;

    public string ClusterID => clusterID;
    public StoragePad TargetStoragePad => storagePad;
    public IReadOnlyList<MachineSlot> MachineSlots => machineSlots;
    public int WorkerCount => currentHiredWorkers.Count;
    public int MaxWorkers => workerSpawnPoints.Count > 0 ? workerSpawnPoints.Count : 2;

    private readonly List<WorkerNPC> currentHiredWorkers = new List<WorkerNPC>();

    private void Awake()
    {
        InitializeSlots();
    }

    private void Start()
    {
        // Tự động tìm sellZoneTransform nếu chưa gán
        if (sellZoneTransform == null)
        {
            var sz = FindAnyObjectByType<SellZone>();
            if (sz != null) sellZoneTransform = sz.transform;
        }

        // Đăng ký với FactoryManager
        if (FactoryManager.Instance != null)
        {
            FactoryManager.Instance.RegisterProductionLine(this);
        }
    }

    private void InitializeSlots()
    {
        // Tự động liên kết nếu cụm có sẵn MachineController con mà chưa gán vào machineSlots
        if (machineSlots.Count == 0)
        {
            var childMachine = GetComponentInChildren<MachineController>();
            if (childMachine != null)
            {
                var defaultSlot = new MachineSlot
                {
                    slotID = $"{clusterID}_Slot01",
                    slotName = childMachine.name,
                    machineConfig = childMachine.Config,
                    spawnPoint = childMachine.transform,
                    targetConveyor = FindAnyObjectByType<ConveyorBelt>(),
                    isUnlockedByDefault = true,
                    isUnlocked = true,
                    currentTier = childMachine.CurrentTier,
                    currentSpeedLevel = childMachine.CurrentSpeedLevel,
                    spawnedMachineInstance = childMachine
                };
                machineSlots.Add(defaultSlot);
            }
        }

        for (int i = 0; i < machineSlots.Count; i++)
        {
            var slot = machineSlots[i];
            if (string.IsNullOrEmpty(slot.slotID))
            {
                slot.slotID = $"{clusterID}_Slot{i:D2}";
            }

            slot.isUnlocked = slot.isUnlockedByDefault;
            if (slot.isUnlocked)
            {
                SpawnMachineAtSlot(slot);
            }
            else
            {
                if (slot.spawnPoint != null)
                {
                    if (slot.spawnPoint.TryGetComponent<MachineController>(out var mc))
                    {
                        mc.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Mở khóa một slot máy mới trong cụm.
    /// </summary>
    public bool UnlockSlot(MachineSlot slot)
    {
        if (slot == null || slot.isUnlocked) return false;

        slot.isUnlocked = true;
        if (slot.spawnPoint != null)
        {
            slot.spawnPoint.gameObject.SetActive(true);
        }
        SpawnMachineAtSlot(slot);

        if (slot.spawnedMachineInstance != null)
        {
            slot.spawnedMachineInstance.gameObject.SetActive(true);
            slot.spawnedMachineInstance.SetTier(slot.currentTier);
            slot.spawnedMachineInstance.SetSpeedLevel(slot.currentSpeedLevel);
        }

        if (slot.machineConfig != null)
        {
            GameEvents.OnMachineCountChanged?.Invoke(slot.machineConfig.resourceType, GetActiveMachineCount());
        }

        Debug.Log($"[ProductionLine] Đã mở khóa máy: {slot.slotID} trong cụm {clusterID}.");
        return true;
    }

    /// <summary>
    /// Khóa lại một slot máy (phục vụ Reset hoặc Load lại bản Save cũ).
    /// </summary>
    public bool LockSlot(MachineSlot slot)
    {
        if (slot == null || !slot.isUnlocked) return false;

        slot.isUnlocked = false;
        slot.currentTier = 1;
        slot.currentSpeedLevel = 0;
        slot.currentStorageLevel = 0;

        if (slot.spawnedMachineInstance != null)
        {
            slot.spawnedMachineInstance.SetTier(1);
            slot.spawnedMachineInstance.SetSpeedLevel(0);
            slot.spawnedMachineInstance.gameObject.SetActive(false);
        }

        if (slot.spawnPoint != null)
        {
            slot.spawnPoint.gameObject.SetActive(false);
        }

        if (slot.machineConfig != null)
        {
            GameEvents.OnMachineCountChanged?.Invoke(slot.machineConfig.resourceType, GetActiveMachineCount());
        }

        Debug.Log($"[ProductionLine] Đã khóa lại máy: {slot.slotID} trong cụm {clusterID}.");
        return true;
    }

    public void SpawnMachineAtSlot(MachineSlot slot)
    {
        if (slot == null || slot.spawnPoint == null) return;

        // Nếu slot đã có sẵn MachineController trên Scene
        if (slot.spawnPoint.TryGetComponent<MachineController>(out var existingController))
        {
            slot.spawnedMachineInstance = existingController;
            existingController.gameObject.SetActive(true);
            existingController.Initialize(slot.machineConfig, slot.targetConveyor, storagePad);
            return;
        }

        // Sinh mới từ Prefab hoặc tạo GameObject
        GameObject machineObj;
        if (machineBasePrefab != null)
        {
            machineObj = Instantiate(machineBasePrefab, slot.spawnPoint.position, slot.spawnPoint.rotation, transform);
        }
        else
        {
            machineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machineObj.name = $"Machine_{slot.slotID}";
            machineObj.transform.position = slot.spawnPoint.position;
            machineObj.transform.rotation = slot.spawnPoint.rotation;
            machineObj.transform.SetParent(transform);
        }

        if (!machineObj.TryGetComponent<MachineController>(out var controller))
        {
            controller = machineObj.AddComponent<MachineController>();
        }

        controller.Initialize(slot.machineConfig, slot.targetConveyor, storagePad);
        slot.spawnedMachineInstance = controller;
    }

    /// <summary>
    /// Thuê thêm 1 công nhân NPC phụ trách cụm này (Tối đa MaxWorkers).
    /// </summary>
    public bool HireWorker()
    {
        if (currentHiredWorkers.Count >= MaxWorkers)
        {
            Debug.Log($"[ProductionLine] Cụm {clusterID} đã đạt tối đa công nhân ({MaxWorkers}/{MaxWorkers})!");
            return false;
        }

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = Quaternion.identity;

        if (workerSpawnPoints.Count > currentHiredWorkers.Count && workerSpawnPoints[currentHiredWorkers.Count] != null)
        {
            spawnPos = workerSpawnPoints[currentHiredWorkers.Count].position;
            spawnRot = workerSpawnPoints[currentHiredWorkers.Count].rotation;
        }
        else if (storagePad != null)
        {
            spawnPos = storagePad.transform.position + Vector3.right * 1.5f;
        }

        GameObject npcObj;
        if (workerNPCPrefab != null)
        {
            npcObj = Instantiate(workerNPCPrefab, spawnPos, spawnRot);
        }
        else
        {
            // Fallback tạo NPC Prototype
            npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObj.name = $"WorkerNPC_{clusterID}_{currentHiredWorkers.Count + 1}";
            npcObj.tag = "WorkerNPC";
            npcObj.transform.position = spawnPos;
            npcObj.transform.rotation = spawnRot;
        }

        if (!npcObj.TryGetComponent<WorkerNPC>(out var worker))
        {
            worker = npcObj.AddComponent<WorkerNPC>();
        }

        worker.AssignToStoragePad(storagePad, sellZoneTransform);
        currentHiredWorkers.Add(worker);

        if (machineSlots.Count > 0 && machineSlots[0].machineConfig != null)
        {
            GameEvents.OnWorkerHired?.Invoke(machineSlots[0].machineConfig.resourceType, currentHiredWorkers.Count);
        }

        Debug.Log($"[ProductionLine] Đã thuê thêm công nhân cho cụm {clusterID}. Hiện có: {currentHiredWorkers.Count}/{MaxWorkers}");
        return true;
    }

    public int GetActiveMachineCount()
    {
        int count = 0;
        foreach (var s in machineSlots)
        {
            if (s.isUnlocked) count++;
        }
        return count;
    }

    public void AddSlotDirectly(MachineSlot slot)
    {
        if (slot != null && !machineSlots.Contains(slot))
        {
            machineSlots.Add(slot);
        }
    }

    public void AddWorkerSpawnPoint(Transform t)
    {
        if (t != null && !workerSpawnPoints.Contains(t))
        {
            workerSpawnPoints.Add(t);
        }
    }

    public void DismissAllWorkers()
    {
        foreach (var worker in currentHiredWorkers)
        {
            if (worker != null)
            {
                Destroy(worker.gameObject);
            }
        }
        currentHiredWorkers.Clear();
    }
}
