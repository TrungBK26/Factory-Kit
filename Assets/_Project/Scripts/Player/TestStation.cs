using UnityEngine;

/// <summary>
/// Trạm thử nghiệm tương tác trong Scene Prototype.
/// Cho phép người chơi kiểm tra việc nhặt đồ lên đầu (PlayerCarry)
/// và bán đồ quy đổi ra tiền (EconomyManager) thông qua phím F.
/// </summary>
public class TestStation : MonoBehaviour, IInteractable
{
    public enum StationType
    {
        ItemDispenser,  // Trạm nhả đồ mẫu (Bấm F để nhặt kiện hàng)
        BuyerStation    // Trạm thu mua (Bấm F để bán kiện hàng lấy tiền)
    }

    [Header("--- CẤU HÌNH TRẠM THỬ NGHIỆM ---")]
    [Tooltip("Loại trạm: ItemDispenser để lấy đồ, BuyerStation để bán đồ")]
    [SerializeField] private StationType stationType = StationType.ItemDispenser;

    [Tooltip("Dữ liệu tài nguyên mẫu (tùy chọn)")]
    [SerializeField] private ResourceDataSO sampleResource;

    private void Awake()
    {
        if (sampleResource == null)
        {
            var allRes = Resources.FindObjectsOfTypeAll<ResourceDataSO>();
            if (allRes != null && allRes.Length > 0)
            {
                sampleResource = allRes[0];
            }
        }
    }

    public StationType CurrentStationType
    {
        get => stationType;
        set => stationType = value;
    }

    public string GetInteractionPrompt()
    {
        if (stationType == StationType.ItemDispenser)
        {
            return "[F] Lấy 1 kiện hàng mẫu lên đầu";
        }
        else
        {
            return "[F] Bán 1 kiện hàng (+$10 vào ví)";
        }
    }

    public void OnInteract(PlayerCarry playerCarry)
    {
        if (playerCarry == null) return;

        if (stationType == StationType.ItemDispenser)
        {
            if (!playerCarry.CanCarryMore)
            {
                Debug.Log("[Player] Đã đầy sức chứa (5/5), không thể nhặt thêm!");
                return;
            }

            // Tạo một khối hộp nhỏ làm vật phẩm trực quan
            GameObject itemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObj.name = "Test_ResourceItem";
            itemObj.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);

            // Gắn component lưu trữ dữ liệu
            var holder = itemObj.AddComponent<ResourceItemHolder>();
            holder.Data = sampleResource;

            // Đưa lên đầu xếp chồng
            playerCarry.AddItem(itemObj);
        }
        else if (stationType == StationType.BuyerStation)
        {
            if (playerCarry.CurrentCount == 0)
            {
                Debug.Log("[Player] Không có kiện hàng nào trên đầu để bán!");
                return;
            }

            GameObject item = playerCarry.PopTopItem();
            if (item != null)
            {
                int itemValue = 10;
                if (item.TryGetComponent<ResourceItemHolder>(out var holder) && holder.Data != null)
                {
                    itemValue = holder.Data.baseValue;
                }

                Destroy(item);

                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddMoney(itemValue);
                }

                Debug.Log($"[Economy] Đã bán 1 kiện hàng. Thu về +${itemValue}");
            }
        }
    }
}
