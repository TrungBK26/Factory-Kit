/// <summary>
/// Interface chung cho tất cả các trạm hoặc vật thể có thể tương tác bằng phím F trong game
/// (Bệ chứa hàng, Két tiền, Bảng quản lý nâng cấp...).
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Chuỗi văn bản hiển thị trên bảng gợi ý nổi 3D (VD: "[F] Nhặt Gỗ (5/10)", "[F] Rút tiền (+$150)").
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Xử lý logic khi người chơi nhấn phím tương tác F.
    /// </summary>
    /// <param name="playerCarry">Tham chiếu đến component vác đồ của người chơi.</param>
    void OnInteract(PlayerCarry playerCarry);
}
