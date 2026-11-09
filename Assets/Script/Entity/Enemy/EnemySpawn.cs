using System.Collections;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    #region Inspector Configuration & Fields
    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 0; // wave đầu
    [SerializeField] private float initialGracePeriod = 20f;// thời gian trc khi wave 1
    [SerializeField] private WaveData infiniteWaveData;
    [SerializeField] private bool startSpawning = true;
    [SerializeField] private float statIncreasePerWave = 0.15f; // chỉ số hp tăng mỗi wave

    [Header("Dynamic Spawning Settings")]
    [SerializeField] private bool useDynamicSpawning = true;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 8f; //min spawn distance around player
    [SerializeField] private float maxSpawnDistance = 15f;//max spawn distance around player
    [SerializeField] private int maxSampleAttempts = 30; // số lần thử spawn

    [Header("A* setting")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float obstacleCheckRadius = 0.5f; // bán kính kiểm tra vật cản

    [Header("Cave Spawning")]
    [SerializeField] private bool enableCaveSpawn = false;
    [SerializeField] private Transform[] caveSpawnPoints;

    [Header("NavMesh Fallback Option")]
    [SerializeField] private bool spawnEvenWithoutNavMesh = true;

    [Header("Wave State Tracking")]
    [SerializeField] private int currentWaveCount = 0;
    [SerializeField] private float waveCooldownTimer = 0f;
    [SerializeField] private bool isWaitingForNextWave = false;
    [SerializeField] private int spawnedInCurrentWave = 0;
    #endregion

    #region Singleton & Properties
    public static EnemySpawn Instance { get; private set; }

    public int CurrentWaveCount
    {
        get => currentWaveCount;
        set => currentWaveCount = value;
    }

    public float WaveCooldownTimer
    {
        get => waveCooldownTimer;
        set => waveCooldownTimer = value;
    }

    public bool IsWaitingForNextWave
    {
        get => isWaitingForNextWave;
        set => isWaitingForNextWave = value;
    }

    public int SpawnedInCurrentWave
    {
        get => spawnedInCurrentWave;
        set => spawnedInCurrentWave = value;
    }

    public GameObject EnemyPrefab => (infiniteWaveData != null) ? infiniteWaveData.enemyPrefab : null;

    private Coroutine waveCoroutine;
    #endregion

    #region Unity Lifecycles
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        FindPlayerIfNull();

        if (startSpawning)
        {
            StartSpawning();
        }
    }
    #endregion

    #region Public Management API
    public void SetWave(int wave)
    {
        currentWaveCount = Mathf.Max(0, wave);
    }

    public void ClearAllEnemies()
    {
        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(e.gameObject);
            }
            else
            {
                Destroy(e.gameObject);
            }
        }
    }

    public void RestartWave(int wave, bool isWaiting = false, float cooldownTimer = 0f)
    {
        currentWaveCount = Mathf.Max(0, wave);
        isWaitingForNextWave = isWaiting;
        waveCooldownTimer = cooldownTimer;
        spawnedInCurrentWave = 0;
        ClearAllEnemies();
        StartSpawning();
    }

    public void ResetToInitialState()
    {
        RestartWave(startingWave, true, initialGracePeriod);
    }

    public void StartSpawning()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        if (!IsWaveDataValid())
        {
            return;
        }

        waveCoroutine = StartCoroutine(InfiniteWaveRoutine());
    }

    public void SetCaveSpawnActive(bool active) => enableCaveSpawn = active;

    public void SetSpawnDistance(float min, float max)
    {
        minSpawnDistance = min;
        maxSpawnDistance = max;
    }

    public void UpdateSpawnPoints(Transform[] newPoints) => caveSpawnPoints = newPoints;
    #endregion

    #region Wave Lifecycle Orchestrator
    private IEnumerator InfiniteWaveRoutine()
    {
        yield return ProcessInitialGracePeriod();

        while (true)
        {
            while (Time.timeScale <= 0f) yield return null;

            FindPlayerIfNull();

            if (!IsWaveDataValid())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return WaitProcessCooldown();

            yield return SpawnCurrentWaveEnemies();

            CompleteWaveAndStartCooldown();

            yield return null;
        }
    }

    private bool IsWaveDataValid()
    {
        return infiniteWaveData != null && infiniteWaveData.enemyPrefab != null;
    }

    private IEnumerator ProcessInitialGracePeriod()
    {
        if (currentWaveCount != 0 || isWaitingForNextWave || initialGracePeriod <= 0f) yield break;

        float graceTimer = initialGracePeriod;
        while (graceTimer > 0f)
        {
            while (Time.timeScale <= 0f) yield return null;

            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
            graceTimer -= dt;
            yield return null;
        }
    }

    private IEnumerator WaitProcessCooldown()
    {
        if (!isWaitingForNextWave) yield break;

        float safetyTimeout = 120f; // Timeout an toàn 2 phút chống kẹt
        while (waveCooldownTimer > 0f && safetyTimeout > 0f)
        {
            while (Time.timeScale <= 0f) yield return null;

            float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.02f;
            waveCooldownTimer -= dt;
            safetyTimeout -= dt;
            yield return null;
        }

        AdvanceToNextWave();
    }

    private void AdvanceToNextWave()
    {
        isWaitingForNextWave = false;
        waveCooldownTimer = 0f;
        currentWaveCount++;
        spawnedInCurrentWave = 0;
    }

    private IEnumerator SpawnCurrentWaveEnemies()
    {
        int enemiesToSpawn = CalculateEnemiesToSpawn();
        int failedSpawnAttempts = 0;

        while (spawnedInCurrentWave < enemiesToSpawn)
        {
            while (Time.timeScale <= 0f) yield return null;

            if (TryGetValidSpawnPosition(out Vector3 spawnPos))
            {
                SpawnAndScaleEnemy(infiniteWaveData.enemyPrefab, spawnPos);
                spawnedInCurrentWave++;
                failedSpawnAttempts = 0;
            }
            else
            {
                failedSpawnAttempts++;
                if (failedSpawnAttempts >= 5)
                {
                    yield return new WaitForSeconds(0.5f);
                    failedSpawnAttempts = 0;
                }
            }

            yield return WaitForSpawnDelay();
        }
    }

    private int CalculateEnemiesToSpawn()
    {
        if (infiniteWaveData == null) return 1;
        return Mathf.Max(1, infiniteWaveData.baseEnemyCount + currentWaveCount);
    }

    private void CompleteWaveAndStartCooldown()
    {
        isWaitingForNextWave = true;
        waveCooldownTimer = Mathf.Max(1f, infiniteWaveData.timeBetweenWaves);
    }

    private IEnumerator WaitForSpawnDelay()
    {
        float delay = infiniteWaveData != null ? infiniteWaveData.spawnDelay : 0f;
        if (delay > 0.02f)
        {
            yield return new WaitForSeconds(delay);
        }
        else
        {
            yield return null;
        }
    }
    #endregion

    #region Spawn Position Calculation (3-Level Fallback)
    private bool TryGetValidSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        if (useDynamicSpawning && TryGetDynamicSpawnPosition(out spawnPos))
        {
            return true;
        }

        if (enableCaveSpawn && TryGetCaveSpawnPosition(out spawnPos))
        {
            return true;
        }

        if (spawnEvenWithoutNavMesh && TryGetFallbackRandomPosition(out spawnPos))
        {
            return true;
        }

        return false;
    }

    private bool TryGetDynamicSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (playerTransform == null)
        {
            FindPlayerIfNull();
            if (playerTransform == null) return false;
        }

        for (int i = 0; i < maxSampleAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.up;

            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 candidatePos = playerTransform.position + (Vector3)(randomDirection * randomDistance);
            candidatePos.z = 0f;

            if (IsPositionBlocked(candidatePos))
            {
                continue;
            }

            spawnPosition = candidatePos;
            return true;
        }

        return false;
    }

    private bool TryGetCaveSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;
        if (caveSpawnPoints == null || caveSpawnPoints.Length == 0) return false;

        Transform spawnPoint = caveSpawnPoints[Random.Range(0, caveSpawnPoints.Length)];
        if (spawnPoint == null || IsPositionBlocked(spawnPoint.position)) return false;

        spawnPos = spawnPoint.position;
        spawnPos.z = 0f;
        return true;
    }

    private bool TryGetFallbackRandomPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;
        if (playerTransform == null) return false;

        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (randomDir == Vector2.zero) randomDir = Vector2.up;

            float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 testPos = playerTransform.position + (Vector3)(randomDir * randomDist);
            testPos.z = 0f;

            if (!IsPositionBlocked(testPos))
            {
                spawnPos = testPos;
                return true;
            }
        }

        return false;
    }

    private bool IsPositionBlocked(Vector3 position)
    {
        if (obstacleLayers.value == 0) return false;

        Collider2D hit = Physics2D.OverlapCircle(position, obstacleCheckRadius, obstacleLayers);
        return hit != null && !hit.isTrigger;
    }

    private void FindPlayerIfNull()
    {
        if (playerTransform == null)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    #endregion

    #region Enemy Instantiation & Stat Scaling
    private void SpawnAndScaleEnemy(GameObject prefab, Vector3 pos)
    {
        if (prefab == null || PoolManager.Instance == null) return;

        pos.z = 0f;
        if (IsPositionBlocked(pos)) return;

        GameObject enemy = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
        if (enemy == null) return;

        ApplyWaveScaling(enemy);
        enemy.transform.position = pos;
    }

    private void ApplyWaveScaling(GameObject enemy)
    {
        float waveMultiplier = 1f + (currentWaveCount * statIncreasePerWave);

        if (enemy.TryGetComponent(out EnemyHealth enemyHealth))
        {
            enemyHealth.ApplyWaveMultiplier(waveMultiplier);
        }

        if (enemy.TryGetComponent(out EnemyAttackDamage enemyDamage))
        {
            enemyDamage.ApplyDamageMultiplier(waveMultiplier);
        }

        if (enemy.TryGetComponent(out EnemyController enemyController))
        {
            int waveBonus = Mathf.Max(0, currentWaveCount - 1);
            enemyController.SetCoinDropRange(8 + waveBonus, 10 + waveBonus);
        }
    }
    #endregion
}
