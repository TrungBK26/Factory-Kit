using UnityEngine;

public enum SoundType
{
    ItemPickup,
    CoinCollect,
    ItemSell,
    MachineDrop,
    UpgradeSuccess,
    VictoryFanfare
}

/// <summary>
/// Quản lý Hệ thống Âm Thanh (SoundManager).
/// Tách biệt 2 kênh BGM (Nhạc nền) và SFX (Hiệu ứng) theo Feature 9 của GFD & TDD.
/// Lắng nghe gián tiếp qua GameEvents, kiểm tra an toàn 100% không văng lỗi kể cả khi chưa import file âm thanh.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SoundManager>();
                if (_instance == null)
                {
                    var go = new GameObject("SoundManager_Singleton");
                    _instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("--- KÊNH ÂM THANH AUDIOSOURCE ---")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("--- DANH MỤC AUDIO CLIPS (TÙY CHỌN GẮN FILE) ---")]
    [Tooltip("Nhạc nền trong game (Track 36: Village)")]
    [SerializeField] private AudioClip bgmGameplay;

    [Tooltip("Nhạc nền Menu Chính (Track 38: Intro)")]
    [SerializeField] private AudioClip bgmMenu;

    [Tooltip("Nhạc nền chủ đạo (Tương thích ngược)")]
    [SerializeField] private AudioClip bgmFactoryGroove;

    [Tooltip("Tiếng máy nhả vật phẩm")]
    [SerializeField] private AudioClip sfxMachineDrop;

    [Tooltip("Tiếng bốc hàng lên đầu")]
    [SerializeField] private AudioClip sfxItemPickup;

    [Tooltip("Tiếng bán hàng leng keng tại SellZone")]
    [SerializeField] private AudioClip sfxItemSell;

    [Tooltip("Tiếng rút tiền két Ka-ching!")]
    [SerializeField] private AudioClip sfxVaultCollect;

    [Tooltip("Tiếng nâng cấp máy / lên Tier")]
    [SerializeField] private AudioClip sfxUpgradeSuccess;

    [Tooltip("Tiếng chiến thắng game")]
    [SerializeField] private AudioClip sfxVictoryFanfare;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
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
            return;
        }

        // Tự động tạo 2 AudioSource nếu chưa có
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        ApplyVolumes();
        EnsureBGMClipsLoaded();

        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen)
        {
            PlayMenuBGM();
        }
        else
        {
            PlayGameplayBGM();
        }
    }

    private void Start()
    {
        EnsureBGMClipsLoaded();
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen)
        {
            PlayMenuBGM();
        }
        else
        {
            PlayGameplayBGM();
        }
    }

    private void OnEnable()
    {
        GameEvents.OnItemsSold += HandleItemSold;
        GameEvents.OnMoneyVaultWithdrawn += HandleVaultWithdrawn;
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
        GameEvents.OnItemProduced += HandleItemProduced;
        GameEvents.OnMachineTierUpgraded += HandleMachineTierUpgraded;
        GameEvents.OnGameVictory += HandleGameVictory;
    }

    private void OnDisable()
    {
        GameEvents.OnItemsSold -= HandleItemSold;
        GameEvents.OnMoneyVaultWithdrawn -= HandleVaultWithdrawn;
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
        GameEvents.OnItemProduced -= HandleItemProduced;
        GameEvents.OnMachineTierUpgraded -= HandleMachineTierUpgraded;
        GameEvents.OnGameVictory -= HandleGameVictory;
    }

    public void ApplyVolumes()
    {
        float bgmVol = PlayerPrefs.GetFloat("BGM_Volume", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("SFX_Volume", 1.0f);

        if (bgmSource != null) bgmSource.volume = bgmVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
    }

    private void EnsureBGMClipsLoaded()
    {
        if (bgmGameplay == null)
        {
            bgmGameplay = Resources.Load<AudioClip>("Audio/BGM_Gameplay");
            if (bgmGameplay == null) bgmGameplay = bgmFactoryGroove;
        }

        if (bgmMenu == null)
        {
            bgmMenu = Resources.Load<AudioClip>("Audio/BGM_Menu");
        }
    }

    public void PlayMenuBGM()
    {
        EnsureBGMClipsLoaded();
        AudioClip target = bgmMenu != null ? bgmMenu : (bgmGameplay != null ? bgmGameplay : bgmFactoryGroove);
        PlayBGMClip(target);
    }

    public void PlayGameplayBGM()
    {
        EnsureBGMClipsLoaded();
        AudioClip target = bgmGameplay != null ? bgmGameplay : bgmFactoryGroove;
        PlayBGMClip(target);
    }

    public void PlayBGM()
    {
        PlayGameplayBGM();
    }

    private void PlayBGMClip(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip, float pitch = 1.0f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(SoundType type, float pitch = 1.0f)
    {
        AudioClip clip = type switch
        {
            SoundType.ItemPickup => sfxItemPickup,
            SoundType.CoinCollect => sfxVaultCollect,
            SoundType.ItemSell => sfxItemSell,
            SoundType.MachineDrop => sfxMachineDrop,
            SoundType.UpgradeSuccess => sfxUpgradeSuccess,
            SoundType.VictoryFanfare => sfxVictoryFanfare,
            _ => null
        };
        if (clip != null) PlaySFX(clip, pitch);
    }

    // --- CÁC SỰ KIỆN LẮNG NGHE GAMEPLAY ---

    private void HandleItemSold(int value)
    {
        PlaySFX(sfxItemSell);
    }

    private void HandleVaultWithdrawn(int amount)
    {
        PlaySFX(sfxVaultCollect);
    }

    private void HandleItemPickedUp(int currentStack)
    {
        // Âm vực (Pitch) tăng dần theo số lượng hàng xếp trên đầu (1/5 -> 5/5)
        float pitch = 1.0f + (currentStack * 0.1f);
        PlaySFX(sfxItemPickup, pitch);
    }

    private void HandleItemProduced()
    {
        PlaySFX(sfxMachineDrop);
    }

    private void HandleMachineTierUpgraded(ResourceType type, int tier)
    {
        PlaySFX(sfxUpgradeSuccess);
    }

    private void HandleGameVictory()
    {
        PlaySFX(sfxVictoryFanfare);
    }
}
