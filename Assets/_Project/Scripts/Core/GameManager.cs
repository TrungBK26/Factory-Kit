using UnityEngine;

/// <summary>
/// Quản lý vòng đời tổng thể của Game (GameManager).
/// Phụ trách cấu hình ứng dụng: Chạy nền khi chuyển cửa sổ (Run In Background),
/// khóa tốc độ khung hình (Frame Rate), và điều phối trạng thái ứng dụng.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager_AutoCreated");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("--- CẤU HÌNH ỨNG DỤNG ---")]
    [Tooltip("Tiếp tục chạy game khi người chơi chuyển sang tab/ứng dụng khác (Cực kỳ quan trọng cho game Idle)")]
    [SerializeField] private bool runInBackground = true;

    [Tooltip("Khóa số khung hình trên giây để tránh ngốn CPU/GPU khi chạy nền (-1: Không giới hạn, 60: Chuẩn mượt)")]
    [SerializeField] private int targetFrameRate = 60;

    /// <summary>
    /// Đảm bảo tự động cấu hình ngay cả trước khi Scene đầu tiên load xong.
    /// Hoạt động an toàn trên Unity 6 và khử lỗi Domain Reload.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreInitialize()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
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
            return;
        }

        ApplyAppSettings();
    }

    /// <summary>
    /// Áp dụng các thiết lập hệ thống ứng dụng.
    /// </summary>
    public void ApplyAppSettings()
    {
        Application.runInBackground = runInBackground;
        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
        Debug.Log($"[GameManager] Cấu hình hoàn tất: RunInBackground={Application.runInBackground}, TargetFrameRate={Application.targetFrameRate}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Khi mất focus (người chơi Alt+Tab hoặc chuyển app), đảm bảo game vẫn không bị đóng băng
        if (runInBackground && !Application.runInBackground)
        {
            Application.runInBackground = true;
        }
    }
}
