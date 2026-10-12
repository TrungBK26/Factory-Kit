using UnityEngine;

/// <summary>
/// Quản lý việc dò tìm và tương tác với các trạm chức năng (IInteractable)
/// xung quanh người chơi trong bán kính 1.5m khi nhận tín hiệu phím F từ InputManager.
/// </summary>
[RequireComponent(typeof(PlayerCarry))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("--- CẤU HÌNH TƯƠNG TÁC ---")]
    [Tooltip("Bán kính phát hiện trạm tương tác (m)")]
    [Range(0.5f, 4.0f)]
    [SerializeField] private float interactRadius = 2.6f;

    [Tooltip("LayerMask lọc đối tượng tương tác (Mặc định quét tất cả nếu để Everything)")]
    [SerializeField] private LayerMask interactableLayers = ~0;

    [SerializeField] private PlayerCarry playerCarry;

    private IInteractable currentInteractable;
    private readonly Collider[] hitColliders = new Collider[10];

    [Tooltip("Khoảng cách tối thiểu giữa 2 lần tương tác (giây)")]
    [SerializeField] private float interactCooldown = 0.5f;
    private float lastInteractTime = -10f;

    public string CurrentPrompt => currentInteractable != null ? currentInteractable.GetInteractionPrompt() : string.Empty;
    public bool HasInteractableTarget => currentInteractable != null;

    private void Reset()
    {
        playerCarry = GetComponent<PlayerCarry>();
    }

    private void Awake()
    {
        if (playerCarry == null) playerCarry = GetComponent<PlayerCarry>();
    }

    private void OnEnable()
    {
        InputManager.OnInteractPressed += HandleInteract;
    }

    private void OnDisable()
    {
        InputManager.OnInteractPressed -= HandleInteract;
    }

    private void Update()
    {
        FindClosestInteractable();

        // Hỗ trợ giữ phím F để nhặt liên tục đều đặn mỗi 0.5s
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.fKey.isPressed && Time.time - lastInteractTime >= interactCooldown)
        {
            HandleInteract();
        }
#else
        if (Input.GetKey(KeyCode.F) && Time.time - lastInteractTime >= interactCooldown)
        {
            HandleInteract();
        }
#endif
    }

    private void FindClosestInteractable()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, hitColliders, interactableLayers);
        IInteractable closest = null;
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitColliders[i];
            if (col.gameObject == gameObject) continue;

            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closest = interactable;
                }
            }
        }

        currentInteractable = closest;
    }

    private void HandleInteract()
    {
        if (Time.time - lastInteractTime < interactCooldown) return;

        if (currentInteractable != null && playerCarry != null)
        {
            lastInteractTime = Time.time;
            currentInteractable.OnInteract(playerCarry);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
