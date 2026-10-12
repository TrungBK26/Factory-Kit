using UnityEngine;

/// <summary>
/// Trạm Bảng Quản Lý Nhà Máy in-world (Upgrade Station / Management Terminal).
/// Người chơi tiến lại gần bấm phím F để mở menu nâng cấp, mua máy mới hoặc thuê công nhân.
/// </summary>
public class UpgradeStation : MonoBehaviour, IInteractable
{
    [Header("--- THÔNG TIN TRẠM ---")]
    [SerializeField] private string stationName = "Bảng Quản Lý Trung Tâm";

    public string GetInteractionPrompt() => $"[F] Mở {stationName}";

    public void OnInteract(PlayerCarry playerCarry)
    {
        Debug.Log("[UpgradeStation] Người chơi tương tác mở Bảng Quản Lý Nhà Máy.");
        FactoryManagementUI.Instance.OpenMenu();
    }
}
