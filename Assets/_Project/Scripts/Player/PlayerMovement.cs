using UnityEngine;

/// <summary>
/// Điều khiển nhân vật di chuyển trên mặt phẳng 3D dựa trên góc nhìn Camera Top-down/Isometric.
/// Nhận tín hiệu Vector2 chuẩn hóa từ InputManager.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("--- CẤU HÌNH DI CHUYỂN ---")]
    [Tooltip("Tốc độ di chuyển của nhân vật (units/giây)")]
    [Range(2.0f, 10.0f)]
    [SerializeField] private float moveSpeed = 5.0f;

    [Tooltip("Tốc độ xoay mượt mặt nhân vật theo hướng di chuyển")]
    [SerializeField] private float rotationSpeed = 12.0f;

    [Header("--- HOẠT ẢNH ---")]
    [SerializeField] private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("--- COMPONENT BẮT BUỘC ---")]
    [SerializeField] private CharacterController controller;

    private Camera mainCamera;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        mainCamera = Camera.main;
    }

    public void SetAnimator(Animator anim)
    {
        animator = anim;
    }

    private void Update()
    {
        if (InputManager.Instance == null || !InputManager.Instance.IsMovementEnabled) return;

        Vector2 input = InputManager.Instance.MovementInput;

        // Tính toán hướng di chuyển dựa trên góc nhìn thực tế của Camera
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 camForward = mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
        Vector3 camRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // Di chuyển bằng CharacterController để chống xuyên tường
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Xoay mặt mượt mà theo hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 1.0f);
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0.0f);
            }
        }

        // Đảm bảo luôn dính sàn (Trọng lực cơ bản)
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }

        // Giới hạn vùng di chuyển an toàn của nhân vật, tránh tuyệt đối chạy lọt ra ngoài bản đồ
        Vector3 curPos = transform.position;
        float clampedX = Mathf.Clamp(curPos.x, -21.0f, 21.0f);
        float clampedZ = Mathf.Clamp(curPos.z, -21.0f, 16.0f);
        if (Mathf.Abs(curPos.x - clampedX) > 0.001f || Mathf.Abs(curPos.z - clampedZ) > 0.001f)
        {
            transform.position = new Vector3(clampedX, curPos.y, clampedZ);
        }
    }
}
