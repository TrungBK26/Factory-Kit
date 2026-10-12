using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Khu vực bán hàng tự động (Sell Zone - Auto Trigger).
/// Hỗ trợ dỡ hàng lần lượt cho bất kỳ đối tượng nào mang interface ICarryable (Cả Player lẫn NPC),
/// nhịp xả 0.1s/món, chuyển doanh thu phát sinh thành sự kiện OnItemsSold nạp vào Két tiền.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SellZone : MonoBehaviour
{
    [Header("--- CẤU HÌNH BÁN HÀNG ---")]
    [Tooltip("Nhịp dỡ hàng xuống sàn bán (giây/món, mặc định 0.5s)")]
    [SerializeField] private float unloadInterval = 0.5f;

    private readonly HashSet<ICarryable> activeCarriers = new HashSet<ICarryable>();

    private void Awake()
    {
        // Đảm bảo Collider là Trigger
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = true;
        }

        // Đảm bảo Rigidbody là Kinematic để tương tác vật lý Trigger trong Unity hoạt động 100%
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other) => TryStartUnload(other);
    private void OnTriggerStay(Collider other) => TryStartUnload(other);

    private void TryStartUnload(Collider other)
    {
        var carryable = other.GetComponentInParent<ICarryable>();
        if (carryable == null) carryable = other.GetComponent<ICarryable>();

        if (carryable != null && carryable.CurrentCount > 0)
        {
            StartUnload(carryable);
        }
    }

    /// <summary>
    /// Cho phép NPC hoặc hệ thống chủ động gọi dỡ hàng khi đã tiến vào phạm vi SellZone
    /// </summary>
    public void StartUnload(ICarryable carryable)
    {
        if (carryable != null && carryable.CurrentCount > 0)
        {
            if (!activeCarriers.Contains(carryable))
            {
                StartCoroutine(UnloadRoutine(carryable));
            }
        }
    }

    private IEnumerator UnloadRoutine(ICarryable carry)
    {
        activeCarriers.Add(carry);

        while (carry != null && carry.CurrentCount > 0)
        {
            // Kiểm tra nếu người mang vác đã di chuyển ra quá xa khỏi SellZone (> 3.5m) thì dừng
            if (carry is Component comp && Vector3.Distance(transform.position, comp.transform.position) > 3.5f)
            {
                break;
            }
            GameObject item = carry.PopTopItem();
            if (item != null)
            {
                int itemValue = 2; // Mặc định nếu không tìm thấy dữ liệu
                if (item.TryGetComponent<ResourceItemHolder>(out var holder) && holder.Data != null)
                {
                    itemValue = holder.Data.baseValue;
                }

                // Phát tín hiệu bán hàng để Két tiền tích lũy và SoundManager phát âm thanh
                GameEvents.OnItemsSold?.Invoke(itemValue);

                // Thu hồi vật phẩm về Object Pool để khử 100% rác RAM
                if (ResourcePoolManager.Instance != null)
                {
                    ResourcePoolManager.Instance.Despawn(item);
                }
                else
                {
                    Destroy(item);
                }

                Debug.Log($"[SellZone] Đã dỡ và bán 1 món hàng. Giá trị: ${itemValue}");
            }

            yield return new WaitForSeconds(unloadInterval);
        }

        activeCarriers.Remove(carry);
    }
}
