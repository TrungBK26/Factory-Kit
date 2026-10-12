using UnityEngine;

/// <summary>
/// Component đính kèm trên mỗi vật phẩm tài nguyên được sinh ra,
/// lưu trữ tham chiếu đến ResourceDataSO để xác định loại và giá trị khi bán.
/// </summary>
public class ResourceItemHolder : MonoBehaviour
{
    [SerializeField] private ResourceDataSO data;

    public ResourceDataSO Data
    {
        get => data;
        set => data = value;
    }
}
