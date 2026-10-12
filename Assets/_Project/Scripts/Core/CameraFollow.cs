using UnityEngine;

/// <summary>
/// Camera Follow bám theo Người chơi theo góc nhìn Isometric / Top-Down.
/// Cập nhật tại LateUpdate với Vector3.SmoothDamp để đảm bảo chuyển động mượt mà 60fps+, không bị giật lag.
/// Tự động tìm Player nếu chưa được gán thủ công trong Inspector.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("--- MỤC TIÊU THEO DÕI ---")]
    [Tooltip("Transform của Nhân vật Người chơi")]
    [SerializeField] private Transform target;

    [Header("--- THÔNG SỐ VỊ TRÍ & ĐỘ MƯỢT ---")]
    [Tooltip("Khoảng cách lệch vị trí so với nhân vật (Isometric)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -14f);

    [Tooltip("Thời gian làm mượt chuyển động của Camera (giây)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float smoothTime = 0.2f;

    [Tooltip("Góc quay cố định của Camera (X = độ chúi xuống)")]
    [SerializeField] private Vector3 cameraEuler = new Vector3(48f, 0f, 0f);

    private Vector3 currentVelocity = Vector3.zero;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        FindPlayerTarget();
        transform.rotation = Quaternion.Euler(cameraEuler);
        
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null) return;
        }

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }

    private void FindPlayerTarget()
    {
        if (target != null) return;

        var playerMove = FindAnyObjectByType<PlayerMovement>();
        if (playerMove != null)
        {
            target = playerMove.transform;
            return;
        }

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
