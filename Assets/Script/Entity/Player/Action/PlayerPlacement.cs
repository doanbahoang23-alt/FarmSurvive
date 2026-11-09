using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerPlacement : MonoBehaviour
{
    #region Inspector Fields & Settings
    [Header("Placement Settings")]
    [SerializeField] private float placementCheckRadius = 0.4f;// kiểm tra vị trí đặt đồ
    #endregion

    #region Private Components & Cached State
    private PlayerEquipment equipment;
    private PlayerInventory inventory;
    private PlayerFarming farming;
    private Animator animator;

    private static int cachedObstacleLayerMask = -1;
    private static int ObstacleLayerMask
    {
        get
        {
            if (cachedObstacleLayerMask == -1)
            {
                cachedObstacleLayerMask = LayerMask.GetMask("wall", "Water", "Building");
                if (cachedObstacleLayerMask == 0) cachedObstacleLayerMask = (1 << 4) | (1 << 7);
            }
            return cachedObstacleLayerMask;
        }
    }
    #endregion

    #region Unity Lifecycles
    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        inventory = GetComponent<PlayerInventory>();
        farming = GetComponent<PlayerFarming>();
        animator = GetComponent<Animator>();
    }
    #endregion

    #region Input Handling
    private void OnInteract(InputValue inputValue)
    {
        if (!inputValue.isPressed) return;

        if (equipment != null && equipment.currentItem != null)
        {
            ItemData currentItem = equipment.currentItem;

            // Chỉ xử lý các vật phẩm đặt được (không phải hạt giống vì hạt giống thuộc về PlayerFarming)
            if (currentItem.itemType != ItemType.Seed && currentItem.IsPlaceable && currentItem.placeablePrefab != null)
            {
                PlaceItem(currentItem);
            }
        }
    }
    #endregion

    #region Placement Orchestration (SRP)
    public void PlaceItem(ItemData itemToPlace)
    {
        if (itemToPlace == null || itemToPlace.placeablePrefab == null) return;

        Vector3 spawnPos = CalculateBestPlacementPosition();
        if (float.IsPositiveInfinity(spawnPos.x)) return;

        SpawnPlaceablePrefab(itemToPlace.placeablePrefab, spawnPos);
        ConsumePlacedItem(itemToPlace);
    }

    private void SpawnPlaceablePrefab(GameObject prefab, Vector3 spawnPos)
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        Physics2D.SyncTransforms();
    }

    private void ConsumePlacedItem(ItemData itemToPlace)
    {
        if (inventory == null) return;

        inventory.RemoveItem(itemToPlace, 1);
        if (inventory.GetItemAmount(itemToPlace) <= 0 && equipment != null)
        {
            equipment.EquipItem(null);
        }
    }
    #endregion

    #region Grid Snapping & Position Calculation
    public Vector3 CalculateBestPlacementPosition()
    {
        Vector3 basePos = GetPlayerFeetPosition();
        Vector2 cardinalLookDir = GetPlayerCardinalDirection();

        Vector3 frontTargetPos = basePos + (Vector3)(cardinalLookDir * 0.8f);

        Vector3 frontSnappedPos = GetGridCellCenter(frontTargetPos);
        Vector3 feetSnappedPos = GetGridCellCenter(basePos);

        // 1. Ưu tiên ô phía trước mặt
        if (!IsPlacementBlocked(frontSnappedPos))
        {
            return frontSnappedPos;
        }

        // 2. Thử ô ngay dưới chân người chơi
        if (!IsPlacementBlocked(feetSnappedPos))
        {
            return feetSnappedPos;
        }

        // Không tìm được vị trí hợp lệ
        return Vector3.positiveInfinity;
    }

    private Vector3 GetPlayerFeetPosition()
    {
        if (farming != null && farming.FeetPos != null)
        {
            return farming.FeetPos.position;
        }
        return transform.position;
    }

    private Vector2 GetPlayerCardinalDirection()
    {
        if (animator == null) return Vector2.down;

        float lookX = animator.GetFloat("LastInputX");
        float lookY = animator.GetFloat("LastInputY");
        Vector2 lookDir = new Vector2(lookX, lookY);

        if (lookDir == Vector2.zero) return Vector2.down;

        if (Mathf.Abs(lookDir.y) > Mathf.Abs(lookDir.x))
        {
            return lookDir.y > 0 ? Vector2.up : Vector2.down;
        }

        return lookDir.x >= 0 ? Vector2.right : Vector2.left;
    }

    public Vector3 GetGridCellCenter(Vector3 worldPos)
    {
        Tilemap farmTilemap = FarmableArea.ActiveFarmTilemap;
        if (farmTilemap != null)
        {
            Vector3Int cellPos = farmTilemap.WorldToCell(worldPos);
            return farmTilemap.GetCellCenterWorld(cellPos);
        }

        Grid grid = Object.FindFirstObjectByType<Grid>();
        if (grid != null)
        {
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            return grid.GetCellCenterWorld(cellPos);
        }

        return new Vector3(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f, 0f);
    }
    #endregion

    #region Physics & Obstacle Collision Checks (Tag/Layer Optimized)
    public bool IsPlacementBlocked(Vector3 centerPos)
    {
        Physics2D.SyncTransforms();
        Vector2 checkSize = new Vector2(placementCheckRadius * 1.5f, placementCheckRadius * 1.5f);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(centerPos, checkSize, 0f);

        if (colliders == null || colliders.Length == 0) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (!IsColliderValidForPlacement(col, centerPos))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsColliderValidForPlacement(Collider2D col, Vector3 centerPos)
    {
        // 1. Bỏ qua chính người chơi và các child colliders
        if (col == null || col.gameObject == gameObject || col.transform.root == transform.root)
        {
            return true;
        }

        // 2. Bỏ qua các thực thể sống (Người chơi, Quái vật)
        if (col.CompareTag("Player") || col.CompareTag("Enemy"))
        {
            return true;
        }

        // 3. Lọc nhanh bằng Tag và Layer cản (không tốn chi phí GetComponent)
        if (col.CompareTag("Trap") || col.CompareTag("Wall"))
        {
            return false;
        }

        if (!col.isTrigger && IsObstacleLayer(col.gameObject.layer))
        {
            return false;
        }

        // 4. Kiểm tra Cây trồng (Plant): Chỉ chặn nếu tâm cây nằm trong ô (< 0.5f)
        if (TryGetPlant(col, out PlantController plant))
        {
            return Vector2.Distance(plant.transform.position, centerPos) >= 0.5f;
        }

        // 5. Kiểm tra các công trình đặt được hoặc tài nguyên tự nhiên
        if (IsStructuralOrResourceObstacle(col))
        {
            return false;
        }

        return true;
    }

    private bool IsObstacleLayer(int layer)
    {
        return ((1 << layer) & ObstacleLayerMask) != 0;
    }

    private bool TryGetPlant(Collider2D col, out PlantController plant)
    {
        if (col.TryGetComponent(out plant)) return true;
        plant = col.GetComponentInParent<PlantController>();
        return plant != null;
    }

    private bool IsStructuralOrResourceObstacle(Collider2D col)
    {
        if (col.TryGetComponent(out ResourceNode _) || col.GetComponentInParent<ResourceNode>() != null)
        {
            return true;
        }

        if (col.TryGetComponent(out IPlaceable _) || col.GetComponentInParent<IPlaceable>() != null)
        {
            return true;
        }

        if (col.TryGetComponent(out BurnEffect _) || col.GetComponentInParent<BurnEffect>() != null ||
            col.TryGetComponent(out FreezeZoneEffect _) || col.GetComponentInParent<FreezeZoneEffect>() != null ||
            col.TryGetComponent(out SlowEffect _) || col.GetComponentInParent<SlowEffect>() != null)
        {
            return true;
        }

        return false;
    }
    #endregion
}
