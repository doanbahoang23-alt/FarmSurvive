using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    #region Properties & Settings
    public Transform CurrentTarget { get; private set; }
    public bool HasTarget { get; private set; }

    public Vector3 TargetPosition => CurrentTarget != null ? CurrentTarget.position : Vector3.zero;

    [Header("Vision Settings")]
    [SerializeField] private float scanInterval = 0.5f;

    private static Transform mapCenterTarget;
    private WaitForSeconds scanWait;
    private Coroutine scanRoutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        EnsureMapCenterTarget();
        scanWait = new WaitForSeconds(scanInterval);
    }

    private void Start()
    {
        FindClosestTarget();
    }

    private void OnEnable()
    {
        FindClosestTarget();
        if (scanRoutine != null) StopCoroutine(scanRoutine);
        scanRoutine = StartCoroutine(FindTargetsRoutine());
    }

    private void OnDisable()
    {
        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
            scanRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        if (CurrentTarget != null && IsTargetValid(CurrentTarget))
        {
            HasTarget = true;
            return;
        }

        // Khi mục tiêu bị phá hủy hoặc chết, lập tức tìm mục tiêu mới thay vì đứng im chờ coroutine
        FindClosestTarget();
    }
    #endregion

    #region Target Management (Orchestrator)
    public void FindClosestTarget()
    {
        // 1. Quét tìm cây trồng gần nhất (Ưu tiên số 1)
        if (TryFindClosestPlant(out Transform closestPlant))
        {
            SetTarget(closestPlant);
            return;
        }

        // 2. Quét người chơi (Ưu tiên số 2)
        if (TryGetPlayerTarget(out Transform playerTarget))
        {
            SetTarget(playerTarget);
            return;
        }

        // 3. Fallback: Tâm bản đồ (Ưu tiên số 3)
        SetTarget(EnsureMapCenterTarget());
    }

    public void ClearTarget()
    {
        SetTarget(null);
        FindClosestTarget();
    }

    public bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        // Điểm tâm bản đồ luôn hợp lệ
        if (mapCenterTarget != null && target == mapCenterTarget) return true;

        // Kiểm tra máu nếu có BaseHealth
        if (target.TryGetComponent(out BaseHealth health) && health.IsDead)
        {
            return false;
        }

        // Kiểm tra hợp lệ nếu là cây trồng
        PlantController plant = target.GetComponent<PlantController>();
        if (plant == null)
        {
            plant = target.GetComponentInParent<PlantController>();
        }

        if (plant != null)
        {
            if (!plant.gameObject.activeInHierarchy || !plant.enabled || plant.PlantData == null)
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Sub-methods & Detection
    private bool TryFindClosestPlant(out Transform closestPlant)
    {
        closestPlant = null;
        List<PlantController> activePlants = PlantController.ActivePlants;
        if (activePlants == null || activePlants.Count == 0) return false;

        float closestSqrDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < activePlants.Count; i++)
        {
            PlantController plant = activePlants[i];
            if (plant == null || !plant.gameObject.activeInHierarchy || !plant.enabled || plant.PlantData == null)
            {
                continue;
            }

            float sqrDist = (currentPos - plant.transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestPlant = plant.transform;
            }
        }

        return closestPlant != null;
    }

    private bool TryGetPlayerTarget(out Transform playerTarget)
    {
        playerTarget = null;
        PlayerController player = PlayerController.Instance;
        if (player == null || !player.gameObject.activeInHierarchy || player.IsDead)
        {
            return false;
        }

        playerTarget = player.transform;
        return true;
    }

    private void SetTarget(Transform target)
    {
        CurrentTarget = target;
        HasTarget = target != null;
    }

    private static Transform EnsureMapCenterTarget()
    {
        if (mapCenterTarget == null)
        {
            GameObject obj = GameObject.Find("[MapCenterTarget]");
            if (obj == null)
            {
                obj = new GameObject("[MapCenterTarget]");
                obj.transform.position = Vector3.zero;
            }
            mapCenterTarget = obj.transform;
        }
        return mapCenterTarget;
    }

    private IEnumerator FindTargetsRoutine()
    {
        while (true)
        {
            FindClosestTarget();
            yield return scanWait;
        }
    }
    #endregion
}
