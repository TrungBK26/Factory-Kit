using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI Công nhân chuyên trách (Dedicated Worker NPC).
/// Điều hướng bằng NavMesh, gắn cố định vào đúng 1 bệ chứa duy nhất (loại bỏ tranh chấp tài nguyên),
/// tích hợp vòng lặp hành vi chống kẹt chờ (Patience Timeout 1.5s),
/// và dỡ hàng tự động tại SellZone qua interface ICarryable.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WorkerNPC : MonoBehaviour, ICarryable
{
    public enum WorkerState
    {
        Idle,               // Đứng chờ ở bệ máy
        MovingToStorage,    // Đang đi bộ lại bệ chứa để nhặt hàng
        PickingItems,       // Đang bốc hàng từ bệ chứa lên đầu
        MovingToSell,       // Đang mang hàng ra bãi SellZone
        Unloading           // Đang chờ SellZone dỡ hết hàng
    }

    [Header("--- PHÂN QUYỀN CHUYÊN TRÁCH ---")]
    [Tooltip("Gán cố định vào đúng Bệ chứa thành phẩm được phân công")]
    [SerializeField] private StoragePad assignedStoragePad;

    [Tooltip("Vị trí bãi bán hàng SellZone")]
    [SerializeField] private Transform sellZoneTransform;

    [Header("--- VỊ TRÍ VÁC ĐỒ & NĂNG SUẤT ---")]
    [Tooltip("Điểm neo vật phẩm trên đỉnh đầu công nhân")]
    [SerializeField] private Transform headSocket;

    [Tooltip("Sức chứa tối đa của công nhân (mặc định 3 món)")]
    [SerializeField] private int maxCarryCapacity = 3;

    [Tooltip("Thời gian kiên nhẫn tối đa khi bệ chỉ có 1-2 món (giây)")]
    [SerializeField] private float patienceTimeout = 1.5f;

    [Tooltip("Khoảng cách thời gian bốc từng món hàng lên đầu (giây)")]
    [SerializeField] private float pickInterval = 0.5f;

    [Header("--- HOẠT ẢNH (TÙY CHỌN GẮN MODEL KAYKIT) ---")]
    [SerializeField] private Animator animator;
    private static readonly int IsCarryingHash = Animator.StringToHash("isCarrying");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private NavMeshAgent agent;
    private WorkerState currentState = WorkerState.Idle;
    private readonly List<GameObject> carriedItems = new List<GameObject>();
    private float waitTimer = 0f;
    private float pickTimer = 0f;

    public WorkerState CurrentState => currentState;
    public StoragePad AssignedStoragePad => assignedStoragePad;

    // --- TRIỂN KHAI INTERFACE ICARRYABLE ---
    public int CurrentCount => carriedItems.Count;
    public int MaxCapacity => maxCarryCapacity;
    public bool CanCarryMore => carriedItems.Count < maxCarryCapacity;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.stoppingDistance = 1.0f;
            agent.speed = 3.5f;
        }

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Tự tạo headSocket nếu chưa gán
        if (headSocket == null)
        {
            GameObject socket = new GameObject("WorkerHeadSocket");
            socket.transform.SetParent(transform);
            socket.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            headSocket = socket.transform;
        }
    }

    /// <summary>
    /// Gán công nhân chuyên trách cho một bệ máy/bệ chứa cụ thể.
    /// </summary>
    public void AssignToStoragePad(StoragePad storagePad, Transform sellZone)
    {
        this.assignedStoragePad = storagePad;
        this.sellZoneTransform = sellZone;
        Debug.Log($"[AI] Công nhân {name} đã được phân công chuyên trách Bệ: {(storagePad != null ? storagePad.name : "None")}");
    }

    public void AddItem(GameObject itemObj)
    {
        if (!CanCarryMore || itemObj == null) return;

        // Triệt tiêu tải trọng vật lý
        if (itemObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        itemObj.transform.SetParent(headSocket != null ? headSocket : transform);
        itemObj.transform.localPosition = new Vector3(0f, carriedItems.Count * 0.4f, 0f);
        itemObj.transform.localRotation = Quaternion.identity;

        carriedItems.Add(itemObj);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(IsCarryingHash, true);
        }
    }

    public GameObject PopTopItem()
    {
        if (carriedItems.Count == 0) return null;

        int lastIndex = carriedItems.Count - 1;
        GameObject item = carriedItems[lastIndex];
        carriedItems.RemoveAt(lastIndex);

        if (carriedItems.Count == 0 && animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(IsCarryingHash, false);
        }

        return item;
    }

    private void Update()
    {
        if (animator != null && animator.runtimeAnimatorController != null && agent != null)
        {
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }
        switch (currentState)
        {
            case WorkerState.Idle:
                // Nếu bệ chứa được phân công có hàng và còn chỗ vác, di chuyển lại lấy
                if (assignedStoragePad != null && assignedStoragePad.CurrentCount > 0 && CanCarryMore)
                {
                    agent.SetDestination(assignedStoragePad.transform.position);
                    currentState = WorkerState.MovingToStorage;
                }
                break;

            case WorkerState.MovingToStorage:
                if (assignedStoragePad == null)
                {
                    currentState = WorkerState.Idle;
                    break;
                }

                float distToPad = Vector3.Distance(transform.position, assignedStoragePad.transform.position);
                // Bệ pallet 2.2m x 2.2m: NPC đứng cách tâm bệ <= 2.2m là đã tiếp cận bệ thành công
                bool reachedPad = distToPad <= 2.2f || (!agent.pathPending && agent.remainingDistance <= 1.5f);

                if (reachedPad)
                {
                    currentState = WorkerState.PickingItems;
                    waitTimer = 0f;
                    pickTimer = pickInterval; // Sẵn sàng bốc món đầu tiên ngay khi tiếp cận
                }
                break;

            case WorkerState.PickingItems:
                if (assignedStoragePad == null)
                {
                    currentState = WorkerState.Idle;
                    break;
                }

                // 1. Nhặt từng món đồ từ bệ chứa đưa lên đầu theo nhịp 0.5s/món
                if (assignedStoragePad.CurrentCount > 0 && CanCarryMore)
                {
                    pickTimer += Time.deltaTime;
                    if (pickTimer >= pickInterval)
                    {
                        pickTimer = 0f;
                        GameObject item = assignedStoragePad.PopItem();
                        if (item != null)
                        {
                            AddItem(item);
                        }
                    }
                }

                // 2. Nếu đã đầy tải (3/3 món) -> đi bán ngay
                if (!CanCarryMore)
                {
                    pickTimer = 0f;
                    GoToSell();
                }
                // 3. Nếu bệ hết hàng nhưng đã vác được ít nhất 1 món: chờ tối đa 1.5s xem máy có nhả thêm không rồi đi bán
                else if (CurrentCount > 0 && assignedStoragePad.CurrentCount == 0)
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= patienceTimeout)
                    {
                        pickTimer = 0f;
                        GoToSell();
                    }
                }
                // 4. Bệ rỗng hoàn toàn và chưa nhặt được gì -> Quay lại Idle
                else if (assignedStoragePad.CurrentCount == 0 && CurrentCount == 0)
                {
                    pickTimer = 0f;
                    currentState = WorkerState.Idle;
                }
                break;

            case WorkerState.MovingToSell:
                float distToSell = sellZoneTransform != null
                    ? Vector3.Distance(transform.position, sellZoneTransform.position)
                    : 999f;
                bool reachedSell = distToSell <= 2.5f || (!agent.pathPending && agent.remainingDistance <= 1.5f);

                if (reachedSell)
                {
                    currentState = WorkerState.Unloading;
                }
                break;

            case WorkerState.Unloading:
                // Kích hoạt dỡ hàng chủ động nếu SellZone chưa kịp nhận Trigger vật lý
                if (CurrentCount > 0)
                {
                    SellZone sz = null;
                    if (sellZoneTransform != null) sz = sellZoneTransform.GetComponent<SellZone>();
                    if (sz == null) sz = FindAnyObjectByType<SellZone>();

                    if (sz != null)
                    {
                        sz.StartUnload(this);
                    }
                }

                // Đứng tại SellZone chờ vùng bán hút hết hàng (CurrentCount == 0)
                if (CurrentCount == 0)
                {
                    // Sau khi bán hết, tự động quay trở lại bệ chứa của mình (GFD Feature 6 - Bước 5: RETURN)
                    if (assignedStoragePad != null)
                    {
                        agent.SetDestination(assignedStoragePad.transform.position);
                        currentState = WorkerState.MovingToStorage;
                    }
                    else
                    {
                        currentState = WorkerState.Idle;
                    }
                }
                break;
        }
    }

    private void GoToSell()
    {
        if (sellZoneTransform != null)
        {
            agent.SetDestination(sellZoneTransform.position);
            currentState = WorkerState.MovingToSell;
        }
        else
        {
            // Dự phòng nếu chưa gán sellZoneTransform thì tự tìm SellZone trên Scene
            var sz = FindAnyObjectByType<SellZone>();
            if (sz != null)
            {
                sellZoneTransform = sz.transform;
                agent.SetDestination(sellZoneTransform.position);
                currentState = WorkerState.MovingToSell;
            }
        }
    }
}
