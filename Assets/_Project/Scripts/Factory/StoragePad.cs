using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bệ chứa thành phẩm (Storage Pad) đặt tại điểm kết thúc của băng chuyền.
/// Là nơi duy nhất Người chơi và Công nhân NPC có thể tương tác bốc hàng đi bán.
/// Quản lý sức chứa, xếp gọn trên pallet và cơ chế chống tràn (Anti-Jam).
/// </summary>
public class StoragePad : MonoBehaviour, IInteractable
{
    [Header("--- SỨC CHỨA BỆ PALLET ---")]
    [Tooltip("Sức chứa tối đa của bệ chứa (mặc định 10, nâng cấp 15, 20, 30)")]
    [SerializeField] private int maxCapacity = 10;

    [Tooltip("Điểm gốc xếp hàng (Empty GameObject con trên bệ)")]
    [SerializeField] private Transform itemSlotRoot;

    [Tooltip("Khoảng cách dãn cách xếp hàng theo lưới 3x3 trên pallet (0.28m vừa khít pallet, cao tầng 0.35m)")]
    [SerializeField] private Vector3 itemOffset = new Vector3(0.28f, 0.35f, 0.28f);

    private readonly List<GameObject> storedItems = new List<GameObject>();

    public bool IsFull => storedItems.Count >= maxCapacity;
    public int CurrentCount => storedItems.Count;
    public int MaxCapacity => maxCapacity;

    public void SetCapacity(int newCapacity)
    {
        maxCapacity = Mathf.Max(1, newCapacity);
    }

    private void Awake()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other) => TryAcceptCollider(other);
    private void OnTriggerStay(Collider other) => TryAcceptCollider(other);

    private void TryAcceptCollider(Collider other)
    {
        if (IsFull) return;

        // Tìm GameObject gốc của vật phẩm (qua attachedRigidbody hoặc parent)
        GameObject rootObj = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : (other.GetComponentInParent<ResourceItemHolder>() != null ? other.GetComponentInParent<ResourceItemHolder>().gameObject : other.gameObject);

        // Kiểm tra xem đối tượng có phải là hàng hóa tài nguyên không
        if (rootObj.TryGetComponent<ResourceItemHolder>(out _) || rootObj.CompareTag("ResourceItem"))
        {
            // Nếu chưa nằm trong bệ chứa và chưa được ai bốc lên đầu
            if (!storedItems.Contains(rootObj) && rootObj.transform.parent != (itemSlotRoot != null ? itemSlotRoot : transform))
            {
                AcceptItem(rootObj);
            }
        }
    }

    /// <summary>
    /// Đón nhận một vật phẩm từ băng chuyền và xếp gọn gàng trên pallet.
    /// </summary>
    public void AcceptItem(GameObject itemObj)
    {
        if (IsFull || itemObj == null) return;

        // Triệt tiêu tải trọng vật lý và tắt va chạm để tiết kiệm 100% CPU Physics
        if (itemObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider[] colliders = itemObj.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            c.enabled = false;
        }

        if (itemSlotRoot == null)
        {
            var child = transform.Find("ItemSlotRoot");
            if (child != null) itemSlotRoot = child;
        }

        storedItems.RemoveAll(x => x == null);

        Transform root = itemSlotRoot != null ? itemSlotRoot : transform;
        itemObj.transform.SetParent(root);

        // Xếp hàng ngay ngắn theo dạng lưới 3x3 mỗi tầng
        int index = storedItems.Count;
        float x = ((index % 3) - 1f) * itemOffset.x;
        float y = (index / 9) * itemOffset.y;
        float z = (((index / 3) % 3) - 1f) * itemOffset.z;

        itemObj.transform.localPosition = new Vector3(x, y + 0.05f, z);
        itemObj.transform.localRotation = Quaternion.identity;
        itemObj.transform.localScale = Vector3.one;

        storedItems.Add(itemObj);
        Debug.Log($"[StoragePad] Đã tiếp nhận vật phẩm: {itemObj.name}. Hiện có: {storedItems.Count}/{maxCapacity}");
    }

    /// <summary>
    /// Lấy món hàng trên cùng ra khỏi bệ chứa.
    /// </summary>
    public GameObject PopItem()
    {
        if (storedItems.Count == 0) return null;

        int lastIndex = storedItems.Count - 1;
        GameObject item = storedItems[lastIndex];
        storedItems.RemoveAt(lastIndex);
        return item;
    }

    // --- TRIỂN KHAI INTERFACE IINTERACTABLE ---

    public string GetInteractionPrompt()
    {
        return storedItems.Count > 0
            ? $"[F] Nhặt hàng từ Bệ chứa ({storedItems.Count}/{maxCapacity})"
            : $"Bệ chứa rỗng (0/{maxCapacity})";
    }

    public void OnInteract(PlayerCarry playerCarry)
    {
        if (storedItems.Count == 0 || playerCarry == null || !playerCarry.CanCarryMore) return;

        // Bốc từng món một cho mỗi lần tương tác thay vì hút sạch cả bệ
        GameObject item = PopItem();
        if (item != null)
        {
            playerCarry.AddItem(item);
        }
    }
}
