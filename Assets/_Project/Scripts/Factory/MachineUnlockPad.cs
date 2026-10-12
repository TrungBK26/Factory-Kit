using UnityEngine;

/// <summary>
/// Bệ mở khóa máy in-world (Machine Unlock Pad).
/// Đặt tại vị trí sàn của cỗ máy chưa mở khóa.
/// Khi người chơi lại gần bấm phím F (hoặc tương tác), hệ thống kiểm tra tiền ví
/// và kích hoạt mở khóa thông qua ProductionLine.UnlockSlot.
/// Tự động ẩn đi khi máy đã mở khóa (kể cả khi nạp từ file Save hoặc bấm trong UI).
/// </summary>
public class MachineUnlockPad : MonoBehaviour, IInteractable
{
    [Header("--- THIẾT LẬP MỞ KHÓA ---")]
    [SerializeField] private ProductionLine productionLine;
    [SerializeField] private string targetSlotID;
    [SerializeField] private MachineSlot targetSlot;

    public MachineSlot TargetSlot => targetSlot;

    public void Setup(ProductionLine line, MachineSlot slot)
    {
        this.productionLine = line;
        this.targetSlot = slot;
        if (slot != null) this.targetSlotID = slot.slotID;
        CheckUnlockedState();
    }

    private void Start()
    {
        CheckUnlockedState();
    }

    private void Update()
    {
        CheckUnlockedState();
    }

    public void RefreshState()
    {
        CheckUnlockedState();
        if (targetSlot != null)
        {
            gameObject.SetActive(!targetSlot.isUnlocked);
        }
    }

    private void CheckUnlockedState()
    {
        if (targetSlot == null && productionLine != null && !string.IsNullOrEmpty(targetSlotID))
        {
            foreach (var s in productionLine.MachineSlots)
            {
                if (s != null && s.slotID == targetSlotID)
                {
                    targetSlot = s;
                    break;
                }
            }
        }

        if (targetSlot != null && targetSlot.isUnlocked)
        {
            gameObject.SetActive(false);
        }
    }

    public string GetInteractionPrompt()
    {
        if (targetSlot == null || targetSlot.isUnlocked) return string.Empty;

        int cost = targetSlot.machineConfig != null ? targetSlot.machineConfig.unlockCost : 0;
        string mName = targetSlot.machineConfig != null ? targetSlot.machineConfig.machineName : targetSlot.slotName;
        return cost > 0 ? $"[F] Mở khóa {mName} (${cost:N0})" : $"[F] Mở khóa {mName} (Miễn phí)";
    }

    public void OnInteract(PlayerCarry playerCarry)
    {
        if (targetSlot == null || targetSlot.isUnlocked || productionLine == null) return;

        int cost = targetSlot.machineConfig != null ? targetSlot.machineConfig.unlockCost : 0;
        if (cost <= 0 || (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendMoney(cost)))
        {
            productionLine.UnlockSlot(targetSlot);
            gameObject.SetActive(false);
            Debug.Log($"[Factory] Người chơi đã mở khóa thành công {targetSlot.slotName} với chi phí ${cost}!");
        }
        else
        {
            Debug.Log($"[Factory] Không đủ tiền để mở khóa {targetSlot.slotName}! Cần ${cost}.");
        }
    }
}
