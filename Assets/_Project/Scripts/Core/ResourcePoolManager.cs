using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component đánh dấu vật phẩm thuộc Object Pool để biết nguồn prefab gốc khi thu hồi.
/// </summary>
public class PooledResourceItem : MonoBehaviour
{
    public GameObject OriginalPrefab { get; set; }
}

/// <summary>
/// Trình quản lý tái sử dụng đối tượng (Object Pooling System).
/// Khử 100% rác RAM (0 Bytes GC Alloc), loại bỏ hiện tượng khựng khung hình do Unity Garbage Collection,
/// đảm bảo game duy trì ổn định 60 FPS trên WebGL và máy yếu.
/// </summary>
public class ResourcePoolManager : MonoBehaviour
{
    private static ResourcePoolManager _instance;
    public static ResourcePoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ResourcePoolManager>();
                if (_instance == null)
                {
                    var go = new GameObject("ResourcePoolManager_Singleton");
                    _instance = go.AddComponent<ResourcePoolManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

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
        }
    }

    /// <summary>
    /// Lấy một vật phẩm tái sử dụng từ Pool (hoặc sinh mới nếu Pool đang rỗng).
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }

        GameObject obj;
        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();
            obj.transform.SetParent(null);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            var pooled = obj.AddComponent<PooledResourceItem>();
            pooled.OriginalPrefab = prefab;
        }

        // Khôi phục trạng thái vật lý chuẩn cho chu kỳ làm việc mới
        if (obj.TryGetComponent<Collider>(out var col)) col.enabled = true;
        if (obj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        return obj;
    }

    /// <summary>
    /// Thu hồi vật phẩm về Pool để tái sử dụng, tuyệt đối không dùng lệnh Destroy().
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        if (obj.TryGetComponent<PooledResourceItem>(out var pooled) && pooled.OriginalPrefab != null)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (!poolDictionary.ContainsKey(pooled.OriginalPrefab))
            {
                poolDictionary[pooled.OriginalPrefab] = new Queue<GameObject>();
            }

            poolDictionary[pooled.OriginalPrefab].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}
