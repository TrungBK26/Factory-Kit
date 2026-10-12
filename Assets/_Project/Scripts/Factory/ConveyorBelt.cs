using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Băng chuyền vận chuyển hàng hóa (Conveyor Belt System).
/// Đưa vật phẩm từ họng xả của cỗ máy trượt thẳng tắp tới Bệ chứa thành phẩm (Storage Pad).
/// Hỗ trợ tự động tính hướng về phía bệ chứa, cuộn texture, và dừng lại khi bệ chứa bị đầy (Anti-Jam).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ConveyorBelt : MonoBehaviour
{
    [Header("--- CẤU HÌNH BĂNG TẢI ---")]
    [Tooltip("Tốc độ trượt của vật phẩm trên băng chuyền (units/giây)")]
    [SerializeField] private float speed = 2.5f;

    [Tooltip("Tự động hướng luồng đẩy về phía Bệ Chứa mục tiêu")]
    [SerializeField] private bool autoOrientTowardsPad = true;

    [Tooltip("Hướng vận chuyển hàng thủ công (nếu không dùng tự động hướng)")]
    [SerializeField] private Vector3 conveyDirection = Vector3.forward;

    [Tooltip("Bệ chứa thành phẩm ở cuối băng tải")]
    [SerializeField] private StoragePad targetStoragePad;

    [Header("--- HIỆU ỨNG THỊ GIÁC CUỘN TEXTURE ---")]
    [SerializeField] private Renderer beltRenderer;
    [SerializeField] private float textureScrollSpeed = 0.5f;

    private readonly List<Rigidbody> itemsOnBelt = new List<Rigidbody>();

    public StoragePad TargetStoragePad
    {
        get => targetStoragePad;
        set => targetStoragePad = value;
    }

    private void Awake()
    {
        if (beltRenderer == null) TryGetComponent(out beltRenderer);

        // Đảm bảo Collider của băng tải là Trigger
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        // Hiệu ứng cuộn Texture bề mặt băng tải khi bệ chứa chưa bị dồn ứ
        if (beltRenderer != null && (targetStoragePad == null || !targetStoragePad.IsFull))
        {
            float offset = Time.time * textureScrollSpeed;
            if (beltRenderer.material != null)
            {
                beltRenderer.material.mainTextureOffset = new Vector2(0f, offset);
            }
        }
    }

    private void FixedUpdate()
    {
        // Nếu bệ chứa đã đầy, các vật phẩm dừng lại xếp hàng chờ trên băng tải (Anti-Jam)
        if (targetStoragePad != null && targetStoragePad.IsFull) return;

        // Tính toán hướng vận chuyển
        Vector3 moveDir;
        if (autoOrientTowardsPad && targetStoragePad != null)
        {
            Vector3 toPad = targetStoragePad.transform.position - transform.position;
            toPad.y = 0f;
            moveDir = toPad.sqrMagnitude > 0.001f ? toPad.normalized : transform.forward;
        }
        else
        {
            moveDir = transform.TransformDirection(conveyDirection.normalized);
        }

        Vector3 moveVelocity = moveDir * speed;

        for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
        {
            Rigidbody itemRb = itemsOnBelt[i];

            // Nếu vật phẩm bị xóa, bị ẩn, hoặc đã được Bệ chứa/Player bốc lên (có parent) -> Loại bỏ ngay
            if (itemRb == null || !itemRb.gameObject.activeInHierarchy || itemRb.transform.parent != null)
            {
                itemsOnBelt.RemoveAt(i);
                continue;
            }

            // Tịnh tiến vật phẩm mượt mà dọc theo băng chuyền
            itemRb.MovePosition(itemRb.position + moveVelocity * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other) => RegisterItem(other);
    private void OnTriggerStay(Collider other) => RegisterItem(other);

    private void RegisterItem(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = other.GetComponent<Rigidbody>();

        // Nếu vật phẩm chưa có Rigidbody, tự động thêm dự phòng
        if (rb == null && (other.GetComponentInParent<ResourceItemHolder>() != null || other.CompareTag("ResourceItem")))
        {
            rb = other.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (rb != null && !itemsOnBelt.Contains(rb) && rb.transform.parent == null)
        {
            rb.isKinematic = true;
            itemsOnBelt.Add(rb);
            Debug.Log($"[ConveyorBelt] Đã đón nhận vật phẩm: {rb.gameObject.name} lên băng tải.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = other.GetComponent<Rigidbody>();

        if (rb != null)
        {
            itemsOnBelt.Remove(rb);
        }
    }

    /// <summary>
    /// Xóa và giải phóng toàn bộ vật phẩm đang chạy trên băng chuyền khi Reset/Restart Game.
    /// </summary>
    public void ClearBelt()
    {
        for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
        {
            var rb = itemsOnBelt[i];
            if (rb != null)
            {
                if (ResourcePoolManager.Instance != null) ResourcePoolManager.Instance.Despawn(rb.gameObject);
                else Destroy(rb.gameObject);
            }
        }
        itemsOnBelt.Clear();
    }
}
