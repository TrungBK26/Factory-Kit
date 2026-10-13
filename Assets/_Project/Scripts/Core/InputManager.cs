using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Bộ quản lý tín hiệu đầu vào tập trung (InputManager).
/// Đóng vai trò adapter chuẩn hóa phần cứng (Bàn phím, Gamepad, Touch Mobile)
/// và cung cấp dữ liệu sạch cho các component logic (PlayerMovement, UIManager...).
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<InputManager>();
                if (_instance == null)
                {
                    var go = new GameObject("InputManager_Singleton");
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("--- TRẠNG THÁI INPUT ---")]
    public Vector2 MovementInput { get; private set; }
    public bool IsMovementEnabled { get; private set; } = true;

    // Các sự kiện C# Actions phát tín hiệu toàn cục
    public static event Action OnInteractPressed;
    public static event Action OnMenuTogglePressed;
    public static event Action OnSettingsTogglePressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
        OnInteractPressed = null;
        OnMenuTogglePressed = null;
        OnSettingsTogglePressed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        var _ = Instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Vector2 inputVector = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // 1. Đọc từ New Input System (Keyboard)
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (IsMovementEnabled)
            {
                float h = 0f;
                float v = 0f;

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;

                inputVector = new Vector2(h, v);

                // Bắt sự kiện phím tương tác F
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    OnInteractPressed?.Invoke();
                }
            }

            // Phím mở menu Quản lý Nhà máy (Tab hoặc M) - luôn đọc được kể cả khi mở UI để người chơi đóng lại
            if (keyboard.tabKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame)
            {
                if (FactoryManagementUI.Instance != null)
                {
                    FactoryManagementUI.Instance.ToggleMenu();
                }
                GameEvents.OnMenuTogglePressed?.Invoke();
                OnMenuTogglePressed?.Invoke();
            }

            // Phím mở Cài đặt (Esc)
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }

            // Phím mở nhanh Hướng dẫn chơi (H)
            if (keyboard.hKey.wasPressedThisFrame)
            {
                HandleHelpKey();
            }
        }

        // Hỗ trợ Gamepad
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (IsMovementEnabled)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f)
                {
                    inputVector = stick;
                }

                if (gamepad.buttonSouth.wasPressedThisFrame) // Nút A / X
                {
                    OnInteractPressed?.Invoke();
                }
            }

            if (gamepad.selectButton.wasPressedThisFrame)
            {
                if (FactoryManagementUI.Instance != null)
                {
                    FactoryManagementUI.Instance.ToggleMenu();
                }
                GameEvents.OnMenuTogglePressed?.Invoke();
                OnMenuTogglePressed?.Invoke();
            }

            if (gamepad.startButton.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }
#else
        // Fallback đọc từ Legacy Input Manager
        if (IsMovementEnabled)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            inputVector = new Vector2(horizontal, vertical);

            if (Input.GetKeyDown(KeyCode.F))
            {
                OnInteractPressed?.Invoke();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M))
        {
            if (FactoryManagementUI.Instance != null)
            {
                FactoryManagementUI.Instance.ToggleMenu();
            }
            GameEvents.OnMenuTogglePressed?.Invoke();
            OnMenuTogglePressed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            HandleHelpKey();
        }
#endif

        MovementInput = inputVector.sqrMagnitude > 1f ? inputVector.normalized : inputVector;
    }

    /// <summary>
    /// Cho phép UI tạm khóa di chuyển khi đang mở menu hoặc bảng hội thoại.
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        IsMovementEnabled = enabled;
        if (!enabled)
        {
            MovementInput = Vector2.zero;
        }
    }

    /// <summary>
    /// Hỗ trợ cập nhật vector di chuyển ảo từ UI Joystick trên màn hình cảm ứng Mobile.
    /// </summary>
    public void SetVirtualJoystickInput(Vector2 joystickDirection)
    {
        if (!IsMovementEnabled) return;
        MovementInput = joystickDirection.sqrMagnitude > 1f ? joystickDirection.normalized : joystickDirection;
    }

    private void HandleEscapeKey()
    {
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) return;

        if (StoryIntroLetter.Instance != null && StoryIntroLetter.Instance.IsOpen)
        {
            StoryIntroLetter.Instance.CloseLetter();
            return;
        }

        if (FactoryManagementUI.Instance != null && FactoryManagementUI.Instance.IsOpen)
        {
            FactoryManagementUI.Instance.CloseMenu();
            return;
        }

        if (SettingsPopup.Instance != null && SettingsPopup.Instance.IsOpen)
        {
            SettingsPopup.Instance.CloseMenu();
            return;
        }

        if (SettingsPopup.Instance != null)
        {
            SettingsPopup.Instance.OpenSettings();
        }
        else
        {
            GameEvents.OnSettingsTogglePressed?.Invoke();
            OnSettingsTogglePressed?.Invoke();
        }
    }

    private void HandleHelpKey()
    {
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) return;
        if (StoryIntroLetter.Instance != null && StoryIntroLetter.Instance.IsOpen) return;

        if (FactoryManagementUI.Instance != null && FactoryManagementUI.Instance.IsOpen)
        {
            FactoryManagementUI.Instance.CloseMenu();
        }

        if (SettingsPopup.Instance != null)
        {
            if (SettingsPopup.Instance.IsOpen && SettingsPopup.Instance.CurrentTab == SettingsPopup.MenuTab.Tutorial)
            {
                SettingsPopup.Instance.CloseMenu();
            }
            else
            {
                SettingsPopup.Instance.OpenTutorial();
            }
        }
    }
}
