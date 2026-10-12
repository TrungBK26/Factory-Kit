using UnityEngine;

/// <summary>
/// Interface quy chuẩn hành vi vác đồ trên đầu dùng chung cho cả Người chơi và Công nhân NPC.
/// </summary>
public interface ICarryable
{
    int CurrentCount { get; }
    int MaxCapacity { get; }
    bool CanCarryMore { get; }
    void AddItem(GameObject itemObj);
    GameObject PopTopItem();
}
