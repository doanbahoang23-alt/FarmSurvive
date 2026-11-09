using System.Collections.Generic;
using UnityEngine;
public class EnemyController : MonoBehaviour, IBurnable, IFreezable, ISlowable, IKnockbackable
{
    #region Serialized Fields & Inspector Configuration
    [Header("Combat Config")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackRange = 1.2f;

    [Header("Enemy Coin Loot")]
    [SerializeField] private int minCoinDrop = 8;
    [SerializeField] private int maxCoinDrop = 10;

    [Header("A* obstacle avoidance")]
    [SerializeField] private LayerMask obstacleLayers;
    [Tooltip("khoảng cách thấy vật cản")]
    [SerializeField] private float obstacleCheckDistance = 1.0f;
    [Tooltip("bán kính quét")]
    [SerializeField] private float obstacleCheckRadius = 0.35f;
    [SerializeField] private float steeringSmoothSpeed = 12f;

    [Header("A-Star Pathfinding 2D")]
    [SerializeField] private bool useAStarPathfinding = true;
    [SerializeField] private float aStarRefreshInterval = 0.5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    #endregion

    #region Private Components & Runtime State
    private Rigidbody2D rb;
    private Animator animator;
    private BaseSpeed enemySpeed;
    private BaseDamage enemyAttackDamage;
    private EnemyVision enemyVision;
    private KnockbackReceiver knockbackReceiver;
    private BurnReceiver burnReceiver;
    private FreezeReceiver freezeReceiver;
    private SlowReceiver slowReceiver;
    private BaseHealth enemyHealth;
    private Collider2D enemyCollider;

    private float pauseTimer;
    private float attackTimer = 0f;
    private Vector2 currentMoveDir = Vector2.zero;

    private List<Vector2> currentAStarPath = null;
    private int currentWaypointIndex = 0;
    private float aStarPathTimer = 0f;

    // Bộ góc quét cảm biến tìm lối thoát né cản 
    private static readonly float[] AvoidanceScanAngles = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f };
    #endregion

    #region Unity Lifecycle (Orchestrator)
    private void Awake()
    {
        BindComponents();
        EnsureStatusReceivers();
        ConfigurePhysics();
        InitObstacleLayers();
    }

    private void OnEnable()
    {
        ResetEnemyState();
        SetColliderActive(true);
        SubscribeHealthEvents();
    }

    private void OnDisable()
    {
        UnsubscribeHealthEvents();
    }

    private void FixedUpdate()
    {
        if (IsActionBlocked()) return;

        UpdateTimers();
        UpdateMovement();
        UpdateCombat();
    }
    #endregion

    #region Initialization & Configuration
    private void BindComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemySpeed = GetComponent<BaseSpeed>();
        enemyAttackDamage = GetComponent<BaseDamage>();
        enemyVision = GetComponent<EnemyVision>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
        enemyHealth = GetComponent<BaseHealth>();
        enemyCollider = GetComponent<Collider2D>();
    }

    private void EnsureStatusReceivers()
    {
        burnReceiver = GetComponent<BurnReceiver>() ?? gameObject.AddComponent<BurnReceiver>();
        freezeReceiver = GetComponent<FreezeReceiver>() ?? gameObject.AddComponent<FreezeReceiver>();
        slowReceiver = GetComponent<SlowReceiver>() ?? gameObject.AddComponent<SlowReceiver>();
    }

    private void ConfigurePhysics()
    {
        if (rb == null) return;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void InitObstacleLayers()
    {
        if (obstacleLayers.value != 0) return;

        obstacleLayers = LayerMask.GetMask("wall", "Water", "Building");
        if (obstacleLayers.value == 0) obstacleLayers = LayerMask.GetMask("wall", "Water");
        if (obstacleLayers.value == 0) obstacleLayers = (1 << 4) | (1 << 7);
    }

    private void ResetEnemyState()
    {
        attackTimer = 0f;
        pauseTimer = 0f;
        currentMoveDir = Vector2.zero;
        currentAStarPath = null;
        currentWaypointIndex = 0;
        aStarPathTimer = 0f;
    }

    private void SetColliderActive(bool isActive)
    {
        if (enemyCollider != null)
        {
            enemyCollider.enabled = isActive;
        }
    }

    private void SubscribeHealthEvents()
    {
        if (enemyHealth == null) return;
        enemyHealth.OnDeath -= HandleDeath;
        enemyHealth.OnDeath += HandleDeath;
    }

    private void UnsubscribeHealthEvents()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleDeath;
        }
    }
    #endregion

    #region State Checks & Timers
    private bool IsActionBlocked()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            SafeStopMovement();
            return true;
        }

        if (freezeReceiver != null && freezeReceiver.IsFrozen)
        {
            SafeStopMovement();
            return true;
        }

        if (knockbackReceiver != null && knockbackReceiver.IsKnockback)
        {
            return true;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            SafeStopMovement();
            return true;
        }

        return false;
    }

    private void UpdateTimers()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.fixedDeltaTime;
        }
    }
    #endregion

    #region Movement Orchestration & Physics
    private void UpdateMovement()
    {
        if (enemyVision == null || !enemyVision.HasTarget)
        {
            StopMovementAndIdle();
            return;
        }

        Vector3 targetPos = enemyVision.TargetPosition;
        float distanceToTarget = Vector2.Distance(transform.position, targetPos);

        // Đã đến tầm đánh -> Dừng di chuyển
        if (distanceToTarget <= attackRange)
        {
            SafeStopMovement();
            return;
        }

        // Đang ở ngoài tầm đánh -> Di chuyển áp sát mục tiêu
        MoveTowardsTarget(targetPos);
    }

    private void MoveTowardsTarget(Vector3 targetPos)
    {
        Vector2 targetMoveDir = DetermineSmartMovementDirection(targetPos);
        currentMoveDir = Vector2.MoveTowards(currentMoveDir, targetMoveDir, Time.fixedDeltaTime * steeringSmoothSpeed);

        float currentSpeed = enemySpeed != null ? enemySpeed.MoveSpeed(false) : 3f;
        ApplyMovementVelocity(currentSpeed);
        HandleAnimation(currentMoveDir, currentSpeed > 0.05f && currentMoveDir.sqrMagnitude > 0f);
    }

    private void ApplyMovementVelocity(float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = currentMoveDir * speed;
        }
    }

    private void StopMovementAndIdle()
    {
        SafeStopMovement();
        HandleAnimation(Vector2.zero, false);
    }

    private void SafeStopMovement()
    {
        currentMoveDir = Vector2.zero;
        if (knockbackReceiver != null && knockbackReceiver.IsKnockback) return;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void HandleMovementAttack()
    {
        UpdateMovement();
        UpdateCombat();
    }
    #endregion

    #region Pathfinding & Context Steering 2D
    private Vector2 DetermineSmartMovementDirection(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        InitObstacleLayers();

        // 1. Kiểm tra tầm nhìn thẳng tới mục tiêu
        bool hasClearLine = Pathfinding2D.HasDirectLineOfSight(currentPos, targetPos, obstacleLayers, obstacleCheckRadius);

        if (hasClearLine || !useAStarPathfinding || Pathfinding2D.Instance == null)
        {
            ResetAStarPath();
            return CalculateSteeringDirection(targetPos);
        }

        // 2. Bị vật cản lớn ngăn cách -> Bám theo đường đi A*
        return FollowAStarPath(targetPos);
    }

    private void ResetAStarPath()
    {
        currentAStarPath = null;
        currentWaypointIndex = 0;
    }

    private Vector2 FollowAStarPath(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        RefreshAStarPathIfNeeded(currentPos, targetPos);

        Vector2 nextWaypoint = GetCurrentOrNextWaypoint(currentPos, targetPos);
        return CalculateSteeringDirection(nextWaypoint);
    }

    private void RefreshAStarPathIfNeeded(Vector2 currentPos, Vector2 targetPos)
    {
        aStarPathTimer -= Time.fixedDeltaTime;

        bool needsRecalculation = currentAStarPath == null ||
                                  currentWaypointIndex >= currentAStarPath.Count ||
                                  aStarPathTimer <= 0f;

        if (!needsRecalculation) return;

        aStarPathTimer = aStarRefreshInterval;
        currentAStarPath = Pathfinding2D.Instance.FindPath(currentPos, targetPos);
        currentWaypointIndex = 0;

        // Bỏ qua waypoint đầu tiên nếu quái đã đứng quá sát điểm đó
        if (currentAStarPath != null && currentAStarPath.Count > 1)
        {
            if (Vector2.Distance(currentPos, currentAStarPath[0]) < waypointReachDistance)
            {
                currentWaypointIndex = 1;
            }
        }
    }

    private Vector2 GetCurrentOrNextWaypoint(Vector2 currentPos, Vector2 targetPos)
    {
        if (currentAStarPath == null || currentWaypointIndex >= currentAStarPath.Count)
        {
            return targetPos;
        }

        Vector2 targetWaypoint = currentAStarPath[currentWaypointIndex];
        if (Vector2.Distance(currentPos, targetWaypoint) <= waypointReachDistance)
        {
            currentWaypointIndex++;
            targetWaypoint = currentWaypointIndex < currentAStarPath.Count
                ? currentAStarPath[currentWaypointIndex]
                : targetPos;
        }

        return targetWaypoint;
    }

    private Vector2 CalculateSteeringDirection(Vector2 targetPos)
    {
        Vector2 currentPos = transform.position;
        Vector2 desiredDir = (targetPos - currentPos).normalized;

        if (desiredDir == Vector2.zero) return Vector2.zero;

        InitObstacleLayers();

        // 1. Kiểm tra trực diện đường đi phía trước bằng hàm dò tách riêng
        if (!IsPathObstructed(currentPos, desiredDir, obstacleCheckDistance))
        {
            return desiredDir;
        }

        // 2. Phía trước có vật cản: quét các góc cảm biến tìm lối thoát
        return FindBestAvoidanceDirection(currentPos, desiredDir);
    }

    private bool IsPathObstructed(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D hit = Physics2D.CircleCast(origin, obstacleCheckRadius, direction, distance, obstacleLayers);
        return hit.collider != null;
    }

    private Vector2 FindBestAvoidanceDirection(Vector2 currentPos, Vector2 desiredDir)
    {
        float bestScore = -float.MaxValue;
        Vector2 bestDir = desiredDir;

        for (int i = 0; i < AvoidanceScanAngles.Length; i++)
        {
            Vector2 candidateDir = RotateVector2(desiredDir, AvoidanceScanAngles[i]);

            // Bỏ qua hướng nếu bị vướng cản
            if (IsPathObstructed(currentPos, candidateDir, obstacleCheckDistance)) continue;

            float score = CalculateDirectionScore(candidateDir, desiredDir);
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = candidateDir;
            }
        }

        return bestDir;
    }

    private float CalculateDirectionScore(Vector2 candidateDir, Vector2 desiredDir)
    {
        float score = Vector2.Dot(candidateDir, desiredDir);
        if (currentMoveDir != Vector2.zero)
        {
            score += Vector2.Dot(candidateDir, currentMoveDir) * 0.35f;
        }
        return score;
    }

    private static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    #endregion

    #region Combat & Attack Mechanics
    private void UpdateCombat()
    {
        if (enemyVision == null || !enemyVision.HasTarget) return;

        Vector3 targetPos = enemyVision.TargetPosition;
        float distanceToTarget = Vector2.Distance(transform.position, targetPos);

        if (distanceToTarget > attackRange) return;

        // Cập nhật hướng quay mặt về mục tiêu khi đứng đánh
        Vector2 aimDir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        HandleAnimation(aimDir, false);

        if (attackTimer <= 0f && enemyVision.CurrentTarget != null)
        {
            TryAttack(enemyVision.CurrentTarget.gameObject, targetPos);
        }
    }

    private void TryAttack(GameObject targetObj, Vector3 targetPos)
    {
        // Guard Clauses 1: Điều kiện tiên quyết của bản thân và mục tiêu
        if (targetObj == null) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        if (freezeReceiver != null && freezeReceiver.IsFrozen) return;
        if (attackTimer > 0f) return;
        if (IsWeaponOrProjectile(targetObj)) return;

        // Nhánh 1: Tấn công Người Chơi (Player)
        if (IsPlayerTarget(targetObj))
        {
            ExecutePlayerAttack(targetObj, targetPos);
            return;
        }

        // Nhánh 2: Tấn công Nông sản nông trại (Plant)
        PlantController plant = GetPlantTarget(targetObj);
        if (plant != null)
        {
            ExecutePlantAttack(plant);
            return;
        }
    }

    private bool IsWeaponOrProjectile(GameObject targetObj)
    {
        return targetObj.GetComponent<Weapon>() != null ||
               targetObj.GetComponentInParent<Weapon>() != null ||
               targetObj.GetComponent<ProjectileDamage>() != null ||
               targetObj.GetComponentInParent<ProjectileDamage>() != null;
    }

    private bool IsPlayerTarget(GameObject targetObj)
    {
        return targetObj.GetComponentInParent<PlayerController>() != null ||
               targetObj.CompareTag("Player") ||
               (targetObj.transform.root != null && targetObj.transform.root.CompareTag("Player"));
    }

    private PlantController GetPlantTarget(GameObject targetObj)
    {
        return targetObj.GetComponent<PlantController>() ?? targetObj.GetComponentInParent<PlantController>();
    }

    private void ExecutePlayerAttack(GameObject targetObj, Vector3 targetPos)
    {
        IDamageable damageable = targetObj.GetComponentInParent<IDamageable>();
        if (damageable != null && enemyAttackDamage != null)
        {
            damageable.TakeDamage(enemyAttackDamage.AttackDamage());
        }

        IKnockbackable knockbackableTarget = targetObj.GetComponentInParent<IKnockbackable>();
        if (knockbackableTarget != null && enemyAttackDamage != null)
        {
            Vector2 knockbackDir = ((Vector2)targetPos - (Vector2)transform.position).normalized;
            if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;
            knockbackableTarget.ApplyKnockback(knockbackDir, enemyAttackDamage.KnockbackForce);
        }

        attackTimer = attackCooldown;

        if (targetObj.TryGetComponent(out BaseHealth health) && health.IsDead)
        {
            if (enemyVision != null) enemyVision.ClearTarget();
        }
    }

    private void ExecutePlantAttack(PlantController plant)
    {
        if (PoolManager.Instance != null && plant.GetComponent<PoolReturner>() != null)
        {
            PoolManager.Instance.Release(plant.gameObject);
        }
        else
        {
            Destroy(plant.gameObject);
        }

        attackTimer = attackCooldown;

        if (enemyVision != null)
        {
            enemyVision.ClearTarget();
            enemyVision.FindClosestTarget();
        }
    }

    private void HandleTargetExit(GameObject targetObj) { }

    private void OnCollisionEnter2D(Collision2D collision) => TryAttack(collision.gameObject, collision.transform.position);
    private void OnCollisionStay2D(Collision2D collision) => TryAttack(collision.gameObject, collision.transform.position);
    private void OnCollisionExit2D(Collision2D collision) => HandleTargetExit(collision.gameObject);

    private void OnTriggerEnter2D(Collider2D other) => TryAttack(other.gameObject, other.transform.position);
    private void OnTriggerStay2D(Collider2D other) => TryAttack(other.gameObject, other.transform.position);
    private void OnTriggerExit2D(Collider2D other) => HandleTargetExit(other.gameObject);
    #endregion

    #region Animation & Visual Presentation
    private void HandleAnimation(Vector2 dir, bool isMoving)
    {
        if (animator == null) return;
        animator.SetBool("isMoving", isMoving);

        if (isMoving && dir.sqrMagnitude > 0f)
        {
            UpdateFacingDirection(dir.x);
        }
    }

    private void UpdateFacingDirection(float dirX)
    {
        Vector3 currentScale = transform.localScale;
        if (dirX > 0f) currentScale.x = Mathf.Abs(currentScale.x);
        else if (dirX < 0f) currentScale.x = -Mathf.Abs(currentScale.x);
        transform.localScale = currentScale;
    }
    #endregion

    #region Death & Loot Drop Mechanics
    private void HandleDeath(GameObject source)
    {
        SafeStopMovement();
        SetColliderActive(false);
        StopMoveAnimation();
        DropCoins();
        PlayDeathAudio();
        StartCoroutine(DeathReleaseRoutine());
    }

    private void StopMoveAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }
    }

    private void PlayDeathAudio()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.enemyDeathClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.enemyDeathClip);
        }
    }

    private void DropCoins()
    {
        int coinAmount = Random.Range(minCoinDrop, maxCoinDrop + 1);
        if (coinAmount <= 0) return;

        if (TrySpawnCoinPickup(coinAmount)) return;

        AddCoinsDirectlyToPlayer(coinAmount);
    }

    private bool TrySpawnCoinPickup(int coinAmount)
    {
        if (PoolManager.Instance == null || PoolManager.Instance.CoinPrefab == null) return false;

        GameObject coinObj = PoolManager.Instance.Spawn(PoolManager.Instance.CoinPrefab, transform.position, Quaternion.identity);
        if (coinObj != null && coinObj.TryGetComponent(out ItemPickUp pickUp))
        {
            pickUp.SetCoinAmount(coinAmount);
            return true;
        }

        return false;
    }

    private void AddCoinsDirectlyToPlayer(int coinAmount)
    {
        PlayerInventory playerInv = Object.FindFirstObjectByType<PlayerInventory>();
        if (playerInv == null) return;

        playerInv.AddCoins(coinAmount);
        PlayCoinAudio();
    }

    private void PlayCoinAudio()
    {
        if (AudioManager.Instance == null) return;

        AudioClip clip = AudioManager.Instance.coinPickUpClip != null
            ? AudioManager.Instance.coinPickUpClip
            : AudioManager.Instance.pickUpClip;

        if (clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    private System.Collections.IEnumerator DeathReleaseRoutine()
    {
        yield return new WaitForSeconds(0.15f);

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public API & Status Effect Interfaces
    public void SetCoinDropRange(int min, int max)
    {
        minCoinDrop = min;
        maxCoinDrop = Mathf.Max(min, max);
    }

    public void PauseMovement(float time) => pauseTimer = time;

    public void TakeKnockback(Vector2 force) => ApplyKnockback(force.normalized, force.magnitude);

    public void ApplyKnockback(Vector2 direction, float force)
    {
        PauseMovement(0.35f);
        if (knockbackReceiver != null)
        {
            knockbackReceiver.ApplyKnockback(direction, force);
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    public void ApplyBurn(float damePerSecond, float duration)
    {
        if (burnReceiver != null) burnReceiver.ApplyBurn(damePerSecond, duration);
    }

    public void ApplyFreeze(float stunDuration, float frostDuration, float damagePerSec)
    {
        if (freezeReceiver != null) freezeReceiver.ApplyFreeze(stunDuration, frostDuration, damagePerSec);
    }

    public void ApplySlow(float percentageSlow, float duration)
    {
        if (slowReceiver != null) slowReceiver.ApplySlow(percentageSlow, duration);
    }

    public void SetZoneSlow(float percentageSlow, bool isInsideZone)
    {
        if (slowReceiver != null) slowReceiver.SetZoneSlow(percentageSlow, isInsideZone);
    }
    #endregion

    #region Debug & Gizmos
    private void OnDrawGizmosSelected()
    {
        if (currentAStarPath == null || currentAStarPath.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector3 prev = transform.position;
        for (int i = currentWaypointIndex; i < currentAStarPath.Count; i++)
        {
            Vector3 pt = currentAStarPath[i];
            Gizmos.DrawSphere(pt, 0.15f);
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }
    }
    #endregion
}
