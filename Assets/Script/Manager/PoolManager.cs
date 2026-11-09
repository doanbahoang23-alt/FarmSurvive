using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    private Dictionary<int, ObjectPool<GameObject>> pools = new Dictionary<int, ObjectPool<GameObject>>();

    [Header("Coin Pool Settings")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinDefaultCapacity = 20;
    [SerializeField] private int coinMaxSize = 100;

    public GameObject CoinPrefab => coinPrefab;

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            if (gameObject.name == "PoolManager")
            {
                Destroy(gameObject);
            }
            else
            {
                // Cập nhật instance cho scene mới nếu gắn chung trên GameManager
                Instance = this;
            }
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        pools.Clear();
        if (coinPrefab != null)
        {
            PrewarmPool(coinPrefab, coinDefaultCapacity, coinMaxSize);
        }
    }

    private void Start()
    {
        if (coinPrefab != null)
        {
            PrewarmPool(coinPrefab, coinDefaultCapacity, coinMaxSize);
        }
    }

    public void PrewarmPool(GameObject prefab, int defaultCapacity = 20, int maxSize = 200)
    {
        if (prefab == null) return;
        int prefabId = prefab.GetInstanceID();

        GetOrCreatePool(prefab, defaultCapacity, maxSize);

        // Khởi tạo trước (prewarm) các object để sẵn sàng trong pool
        List<GameObject> prewarmed = new List<GameObject>();
        for (int i = 0; i < defaultCapacity; i++)
        {
            GameObject obj = pools[prefabId].Get();
            prewarmed.Add(obj);
        }

        foreach (GameObject obj in prewarmed)
        {
            pools[prefabId].Release(obj);
        }
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab, int defaultCapacity = 20, int maxSize = 200)
    {
        int prefabId = prefab.GetInstanceID();

        if (!pools.ContainsKey(prefabId))
        {
            pools[prefabId] = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject obj = Instantiate(prefab);
                    PoolReturner returner = obj.AddComponent<PoolReturner>();
                    returner.prefabId = prefabId;
                    return obj;
                },
                actionOnGet: (obj) =>
                {
                    if (obj.TryGetComponent(out Rigidbody2D rb))
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                },
                actionOnRelease: (obj) =>
                {
                    obj.SetActive(false);
                },
                actionOnDestroy: (obj) =>
                {
                    Destroy(obj);
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        return pools[prefabId];
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        int prefabId = prefab.GetInstanceID();
        if (!pools.ContainsKey(prefabId))
        {
            GetOrCreatePool(prefab);
        }

        GameObject spawned = pools[prefabId].Get();
        spawned.transform.position = position;
        spawned.transform.rotation = rotation;
        spawned.SetActive(true);
        return spawned;
    }

    public GameObject SpawnCoin(Vector3 position, Quaternion rotation = default)
    {
        if (coinPrefab != null)
        {
            return Spawn(coinPrefab, position, rotation == default ? Quaternion.identity : rotation);
        }
        Debug.LogWarning("[PoolManager] coinPrefab chưa được gán trong Inspector!");
        return null;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (obj.TryGetComponent<PoolReturner>(out PoolReturner returner))
        {
            if (pools.ContainsKey(returner.prefabId))
            {
                pools[returner.prefabId].Release(obj);
                return;
            }
        }
        Destroy(obj);
    }
}

public class PoolReturner : MonoBehaviour
{
    public int prefabId;
}