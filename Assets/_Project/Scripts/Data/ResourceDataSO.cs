using UnityEngine;

public enum ResourceType
{
    Wood,   // Gỗ
    Stone,  // Đá
    Copper, // Đồng
    Iron,   // Sắt
    Silver, // Bạc
    Gold    // Vàng
}

[CreateAssetMenu(fileName = "NewResource", menuName = "Factory/Resource Data")]
public class ResourceDataSO : ScriptableObject
{
    [Header("--- THÔNG TIN TÀI NGUYÊN ---")]
    [Tooltip("Tên hiển thị của tài nguyên")]
    public string resourceName = "Gỗ";

    [Tooltip("Loại tài nguyên")]
    public ResourceType resourceType = ResourceType.Wood;

    [Tooltip("Cấp bậc phẩm chất (1, 2, 3)")]
    [Range(1, 3)]
    public int tier = 1;

    [Tooltip("Giá bán lẻ cơ bản ($)")]
    public int baseValue = 2;

    [Tooltip("Prefab hiển thị của vật phẩm (Khối hộp / Model KayKit)")]
    public GameObject itemPrefab;

    [Header("--- HÌNH ẢNH / UI ICON (Dễ dàng thay thế sau này) ---")]
    [Tooltip("Sprite hình ảnh của tài nguyên khi hiển thị trên UI (Để trống sẽ dùng ký tự Emoji)")]
    public Sprite resourceIcon;
}
