using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Static Instance
    public static PlayerController Instance { get; private set; }
    #endregion

    #region Components
    private Rigidbody2D rb;
    private PlayerStamina playerStamina;
    private BaseSpeed playerSpeed;
    private KnockbackReceiver knockbackReceiver;
    private BaseHealth playerHealth;
    private Animator animator;

    public BaseHealth Health => playerHealth;
    public bool IsDead => playerHealth != null && playerHealth.IsDead;
    public bool IsKnockedBack => knockbackReceiver != null && knockbackReceiver.IsKnockback;
    #endregion

    #region State Variables
    private bool isSprinting = false;
    private bool isAttacking = false;
    private bool hasEnteredAttackState = false;
    private int attackStateHash;
    private Vector2 moveInput;

    [Header("Footstep Settings (Bước chân)")]
    [SerializeField] private float walkStepInterval = 0.42f;
    [SerializeField] private float runStepInterval = 0.28f;
    private float stepTimer = 0f;

    private Coroutine attackFallbackCoroutine;
    #endregion

    #region Unity Lifecycle (Orchestrator)
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CacheComponents();
        attackStateHash = Animator.StringToHash("Attack");
    }

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<BaseHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
            playerHealth.OnDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // Guard Clauses: Bỏ qua khi game Pause hoặc người chơi đã chết
        if (Time.timeScale == 0f || IsDead) return;

        HandleInput();
        HandleAttackStateMonitor();
    }

    private void FixedUpdate()
    {
        // Guard Clauses: Bỏ qua khi Pause hoặc chết
        if (Time.timeScale == 0f) return;

        if (IsDead)
        {
            StopMovementImmediately();
            return;
        }

        // 1. Quản lý thể lực (Stamina)
        HandleStamina();

        // 2. Quản lý vật lý di chuyển (Bỏ qua nếu đang bị Knockback để Rigidbody tự trôi mượt mà)
        if (!IsKnockedBack)
        {
            HandleMovement();
            HandleFootsteps();
        }

        // 3. Quản lý đồ họa và hoạt ảnh (Animation & Sprite Flip)
        HandleAnimation();
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        playerStamina = GetComponent<PlayerStamina>();
        animator = GetComponent<Animator>();
        playerSpeed = GetComponent<BaseSpeed>();
        knockbackReceiver = GetComponent<KnockbackReceiver>();
        playerHealth = GetComponent<BaseHealth>();
    }
    #endregion

    #region 1. Input Processing (HandleInput)
    private void HandleInput()
    {
        if (IsDead || Time.timeScale == 0f)
        {
            moveInput = Vector2.zero;
        }
    }

    private void OnMove(InputValue inputValue)
    {
        if (IsDead)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = inputValue.Get<Vector2>();
    }

    private void OnSprint(InputValue inputValue)
    {
        if (!inputValue.isPressed) return;
        if (Time.timeScale == 0f || IsDead) return;

        isSprinting = !isSprinting;
        if (moveInput != Vector2.zero)
        {
            UpdateAnimatorState(isSprinting);
        }
    }

    private void OnAttack(InputValue inputValue)
    {
        if (!inputValue.isPressed) return;
        if (Time.timeScale == 0f || IsDead) return;

        ProcessAttackInput();
    }

    private void ProcessAttackInput()
    {
        // 1. Nếu đang cầm Thức Ăn (Food) trên tay -> Tiêu thụ thức ăn
        PlayerEquipment equip = GetComponent<PlayerEquipment>();
        if (equip != null && equip.currentItem != null && equip.currentItem.IsFood)
        {
            if (equip.TryConsumeCurrentFood()) return;
        }

        // 2. Kích hoạt đòn tấn công nếu chưa trong trạng thái tấn công
        if (!isAttacking)
        {
            TriggerAttack(equip);
        }
    }

    private void TriggerAttack(PlayerEquipment equip)
    {
        float lastX = animator != null ? animator.GetFloat("LastInputX") : 0f;
        float lastY = animator != null ? animator.GetFloat("LastInputY") : -1f;
        Vector2 attackDir = new Vector2(lastX, lastY);

        if (attackDir == Vector2.zero)
        {
            attackDir = Vector2.down;
        }
        else
        {
            attackDir = attackDir.normalized;
        }

        //Khóa và truyền hướng tấn công chuẩn vào Animator Blend Tree
        if (animator != null)
        {
            animator.SetFloat("InputX", attackDir.x);
            animator.SetFloat("InputY", attackDir.y);
            animator.SetFloat("LastInputX", Mathf.RoundToInt(attackDir.x));
            animator.SetFloat("LastInputY", Mathf.RoundToInt(attackDir.y));
            animator.SetTrigger("Attack");
        }

        isAttacking = true;

        // Fallback tấn công cận chiến: đề phòng trường hợp file Animation của vũ khí chưa gắn AnimEvent_Attack
        if (attackFallbackCoroutine != null) StopCoroutine(attackFallbackCoroutine);
        attackFallbackCoroutine = StartCoroutine(AttackFallbackRoutine(equip));
    }

    private IEnumerator AttackFallbackRoutine(PlayerEquipment equip)
    {
        yield return new WaitForSeconds(0.12f);
        if (isAttacking && equip != null)
        {
            equip.AnimEvent_Attack();
        }
    }
    #endregion

    #region 2. Movement Logic (HandleMovement)
    private void HandleMovement()
    {
        if (rb == null) return;

        if (moveInput == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float finalSpeed = playerSpeed != null ? playerSpeed.MoveSpeed(isSprinting) : 5f;
        rb.linearVelocity = moveInput.normalized * finalSpeed;
    }

    public void StopMovementImmediately()
    {
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public bool CanMove()
    {
        if (IsDead) return false;
        if (IsKnockedBack) return false;
        if (Time.timeScale == 0f) return false;
        return true;
    }
    #endregion

    #region 3. Stamina Management (HandleStamina)
    private void HandleStamina()
    {
        if (playerStamina == null) return;

        bool isActivelySprinting = isSprinting && moveInput != Vector2.zero;
        if (!isActivelySprinting)
        {
            playerStamina.RegenStamina(playerStamina.Regen * Time.fixedDeltaTime);
            return;
        }

        float staminaCost = playerStamina.Lost * Time.fixedDeltaTime;
        if (playerStamina.HasEnoughStamina(staminaCost))
        {
            playerStamina.UseStamina(staminaCost);
        }
        else
        {
            // Hết thể lực: tự động tắt chạy nhanh
            isSprinting = false;
            UpdateAnimatorState(false);
        }
    }
    #endregion

    #region 4. Animation & Visuals (HandleAnimation)
    private void HandleAnimation()
    {
        if (animator == null) return;

        // 1. Chỉ lật Sprite khi không trong trạng thái tấn công
        if (!isAttacking)
        {
            HandleSpriteFlip();
        }

        // 2. Cập nhật trạng thái di chuyển cơ bản
        if (moveInput == Vector2.zero)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            return;
        }

        UpdateAnimatorState(isSprinting);

        // 3. Đóng băng hoàn toàn Blend Tree (InputX, InputY, LastInputX, LastInputY) khi đang tấn công
        // Chỉ cập nhật hướng nhìn khi người chơi KHÔNG trong trạng thái tấn công
        if (!isAttacking)
        {
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);

            Vector2 norm = moveInput.normalized;
            animator.SetFloat("LastInputX", Mathf.RoundToInt(norm.x));
            animator.SetFloat("LastInputY", Mathf.RoundToInt(norm.y));
        }
    }

    private void UpdateAnimatorState(bool isRunning)
    {
        if (animator == null) return;
        animator.SetBool("isWalking", !isRunning);
        animator.SetBool("isRunning", isRunning);
    }

    private void HandleSpriteFlip()
    {
        Vector3 currentScale = transform.localScale;
        if (moveInput.x > 0)
        {
            currentScale.x = Mathf.Abs(currentScale.x);
        }
        else if (moveInput.x < 0)
        {
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else if (moveInput.y != 0)
        {
            currentScale.x = Mathf.Abs(currentScale.x);
        }
        transform.localScale = currentScale;
    }

    private void HandleAttackStateMonitor()
    {
        if (!isAttacking || animator == null) return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.shortNameHash == attackStateHash)
        {
            hasEnteredAttackState = true;
        }

        bool isTransitioning = animator.IsInTransition(0);
        if (isTransitioning && animator.GetNextAnimatorStateInfo(0).shortNameHash == attackStateHash)
        {
            hasEnteredAttackState = true;
        }

        if (hasEnteredAttackState && !isTransitioning && currentState.shortNameHash != attackStateHash)
        {
            ResetAttackState();
        }
    }

    public void ResetAttackState()
    {
        isAttacking = false;
        hasEnteredAttackState = false;
        if (attackFallbackCoroutine != null)
        {
            StopCoroutine(attackFallbackCoroutine);
            attackFallbackCoroutine = null;
        }
    }
    #endregion

    #region Audio & Footsteps
    private void HandleFootsteps()
    {
        if (moveInput == Vector2.zero || isAttacking || IsDead)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.fixedDeltaTime;
        if (stepTimer <= 0f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFootstep(isSprinting ? 0.9f : 0.65f);
            }
            stepTimer = isSprinting ? runStepInterval : walkStepInterval;
        }
    }

    public void AnimEvent_Footstep()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep(isSprinting ? 0.9f : 0.65f);
        }
    }
    #endregion

    #region Animation Events (Weapon Dispatcher)
    public void AnimEvent_Shoot()
    {
        PlayerEquipment equip = GetComponent<PlayerEquipment>();
        if (equip != null) equip.AnimEvent_Shoot();
    }

    public void AnimEvent_Attack()
    {
        PlayerEquipment equip = GetComponent<PlayerEquipment>();
        if (equip != null) equip.AnimEvent_Attack();
    }

    public void AnimEvent_Hoe()
    {
        PlayerEquipment equip = GetComponent<PlayerEquipment>();
        if (equip != null) equip.AnimEvent_Hoe();
    }
    #endregion

    #region Death & Game Over
    private void HandlePlayerDeath(GameObject killer)
    {
        if (animator != null)
        {
            animator.SetBool("isDeath", true);
        }

        StopMovementImmediately();

        PlayerInput pInput = GetComponent<PlayerInput>();
        if (pInput != null) pInput.enabled = false;

        if (AudioManager.Instance != null && AudioManager.Instance.playerDeathClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeathClip);
        }

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
    }
    #endregion
}
