using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerFarming : MonoBehaviour
{
    [Header("Farm Settings")]
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private Transform feetPos;

    [Header("Tilling Validation Settings (Cuốc Đất & Hạn Chế Cầu/Nước/Vật Cản)")]
    [Tooltip("Khoảng cách vị trí cuốc đất trước mặt theo hướng nhìn")]
    [SerializeField] private float tillingDistance = 0.8f;
    [Tooltip("Bán kính quét kiểm tra vật cản và layer hạn chế khi cuốc đất")]
    [SerializeField] private float tillingCheckRadius = 0.4f;
    [Tooltip("LayerMask các layer cấm cuốc đất: Building (Cầu, công trình), wall, Water...")]
    [SerializeField] private LayerMask restrictedTillingLayers;

    [Header("Tile-based Validation (Kiểm tra theo Tilemap nền - Tùy chọn)")]
    [Tooltip("Tilemap nền đất gốc (ví dụ: floor / Ground). Tự động tìm 'floor' nếu để trống")]
    [SerializeField] private Tilemap baseTilemap;
    [Tooltip("Danh sách Tile nền hợp lệ cho phép cuốc thành đất. Nếu danh sách này trống, hệ thống sẽ chỉ dùng Physics & Tilemap Cầu/Nước để chặn")]
    [SerializeField] private List<TileBase> tillableTiles = new List<TileBase>();

    [Header("Mining Settings (Đào đá)")]
    [SerializeField] private float miningHitDistance = 0.8f;
    [SerializeField] private float miningHitRadius = 0.5f;
    [SerializeField] private float defaultMiningDamage = 35f;
    [SerializeField] private LayerMask miningTargetLayers = ~0;

    public TileBase DirtTile => dirtTile;
    public Transform FeetPos => feetPos;

    private PlayerEquipment equipment;
    private PlayerInventory inventory;
    private int lastTilledFrame = -1;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        inventory = GetComponent<PlayerInventory>();

        if (GetComponent<PlayerPlacement>() == null)
        {
            gameObject.AddComponent<PlayerPlacement>();
        }

        InitRestrictedLayersIfEmpty();
    }

    private void InitRestrictedLayersIfEmpty()
    {
        // Khởi tạo LayerMask cấm cuốc đất nếu chưa được thiết lập trong Inspector
        if (restrictedTillingLayers.value == 0)
        {
            restrictedTillingLayers = LayerMask.GetMask("Building", "wall", "Water", "Bridge");
            if (restrictedTillingLayers.value == 0)
            {
                // Fallback theo index layer mặc định: Layer 4 (Water), Layer 7 (wall), Layer 10 (Building)
                restrictedTillingLayers = (1 << 4) | (1 << 7) | (1 << 10);
            }
        }
    }

    private Tilemap GetBaseTilemap()
    {
        if (baseTilemap == null)
        {
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tm in tilemaps)
            {
                if (tm == null) continue;
                string lower = tm.name.ToLower();
                if (lower == "floor" || lower == "ground" || lower.Contains("floor"))
                {
                    baseTilemap = tm;
                    break;
                }
            }
        }
        return baseTilemap;
    }

    private void OnInteract(InputValue inputValue)
    {
        if (!inputValue.isPressed) return;

        // 1. Ưu tiên thu hoạch cây đã chín nếu đứng gần
        if (TryHarvestNearbyPlant())
        {
            return;
        }

        // 2. Trồng cây nếu đang cầm hạt giống
        if (equipment != null && equipment.currentItem != null)
        {
            ItemData currentItem = equipment.currentItem;

            if (currentItem.itemType == ItemType.Seed && currentItem.plantPrefab != null)
            {
                PlantSeed(currentItem);
            }
        }
    }

    private bool TryHarvestNearbyPlant()
    {
        Vector3 checkPos = feetPos != null ? feetPos.position : transform.position;
        PlantController[] plants = Object.FindObjectsByType<PlantController>(FindObjectsSortMode.None);

        PlantController closestHarvestable = null;
        float closestDist = 1.3f; // Tầm tương tác tay để thu hoạch

        for (int i = 0; i < plants.Length; i++)
        {
            PlantController plant = plants[i];
            if (plant == null || !plant.gameObject.activeInHierarchy || !plant.IsFullyGrown) continue;

            float dist = Vector2.Distance(checkPos, plant.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestHarvestable = plant;
            }
        }

        if (closestHarvestable != null)
        {
            closestHarvestable.Harvest(gameObject);
            return true;
        }

        return false;
    }

    private void PlantSeed(ItemData seedItem)
    {
        Tilemap farmTilemap = FarmableArea.ActiveFarmTilemap;
        if (farmTilemap == null)
        {
            FarmableArea farmArea = Object.FindFirstObjectByType<FarmableArea>();
            if (farmArea != null) farmTilemap = farmArea.GetComponent<Tilemap>();
        }

        if (farmTilemap == null)
        {
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tm in tilemaps)
            {
                if (tm.name.Contains("Soil") || tm.name.Contains("Farm"))
                {
                    farmTilemap = tm;
                    break;
                }
            }
        }

        if (farmTilemap == null)
        {
            PlayPlantFail();
            return;
        }

        Vector3 checkPosition = feetPos != null ? feetPos.position : transform.position;

        Animator anim = GetComponent<Animator>();
        float lookX = anim != null ? anim.GetFloat("LastInputX") : 0f;
        float lookY = anim != null ? anim.GetFloat("LastInputY") : 0f;
        Vector2 lookDir = new Vector2(lookX, lookY);
        if (lookDir == Vector2.zero) lookDir = Vector2.down;
        if (Mathf.Abs(lookDir.y) > Mathf.Abs(lookDir.x))
            lookDir = lookDir.y > 0 ? Vector2.up : Vector2.down;
        else
            lookDir = lookDir.x >= 0 ? Vector2.right : Vector2.left;

        Vector3 frontPos = checkPosition + (Vector3)(lookDir * 0.8f);
        Vector3Int frontCell = farmTilemap.WorldToCell(frontPos);
        Vector3Int feetCell = farmTilemap.WorldToCell(checkPosition);

        Vector3Int chosenCell = Vector3Int.zero;
        bool foundValidCell = false;

        // Ưu tiên 1: Ô đất phía trước mặt nếu là đất cuốc và chưa có cây
        if (farmTilemap.GetTile(frontCell) == dirtTile)
        {
            Vector3 frontCenter = farmTilemap.GetCellCenterWorld(frontCell);
            if (!IsCellOccupiedByPlant(frontCenter, frontCell, farmTilemap))
            {
                chosenCell = frontCell;
                foundValidCell = true;
            }
        }

        // Ưu tiên 2: Ô đất dưới chân nếu ô trước mặt không trồng được
        if (!foundValidCell && farmTilemap.GetTile(feetCell) == dirtTile)
        {
            Vector3 feetCenter = farmTilemap.GetCellCenterWorld(feetCell);
            if (!IsCellOccupiedByPlant(feetCenter, feetCell, farmTilemap))
            {
                chosenCell = feetCell;
                foundValidCell = true;
            }
        }

        if (!foundValidCell)
        {
            PlayPlantFail();
            return;
        }

        Vector3 spawnPos = farmTilemap.GetCellCenterWorld(chosenCell);

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(seedItem.plantPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Instantiate(seedItem.plantPrefab, spawnPos, Quaternion.identity);
        }

        Physics2D.SyncTransforms();

        PlayPlantSuccess();

        if (inventory != null)
        {
            inventory.RemoveItem(seedItem, 1);
            if (inventory.GetItemAmount(seedItem) <= 0 && equipment != null)
            {
                equipment.EquipItem(null);
            }
        }
    }

    private bool IsCellOccupiedByPlant(Vector3 centerPos, Vector3Int cellPos, Tilemap tilemap)
    {
        // Kiểm tra vị trí không được nằm trên mặt cầu hoặc nước
        if (!FarmableArea.CanTillAt(centerPos, centerPos))
        {
            return true;
        }

        // 1. Kiểm tra logic ô Grid: Mỗi ô đất chỉ được trồng tối đa 1 cây
        PlantController[] plants = Object.FindObjectsByType<PlantController>(FindObjectsSortMode.None);
        for (int i = 0; i < plants.Length; i++)
        {
            PlantController plant = plants[i];
            if (plant == null || !plant.gameObject.activeInHierarchy) continue;

            if (tilemap.WorldToCell(plant.transform.position) == cellPos) return true;

            if (Vector2.Distance(plant.transform.position, centerPos) < 0.5f)
            {
                return true;
            }
        }

        // 2. Quét vật cản trên ô
        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapBoxAll(centerPos, new Vector2(0.6f, 0.6f), 0f);
        if (colliders == null || colliders.Length == 0) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;
            if (col.CompareTag("Player") || col.CompareTag("Enemy")) continue;

            if (col.GetComponent<PlantController>() != null || col.GetComponentInParent<PlantController>() != null ||
                col.GetComponent<IHaverstable>() != null || col.GetComponentInParent<IHaverstable>() != null)
            {
                continue;
            }

            if (col.GetComponent<ResourceNode>() != null || col.GetComponentInParent<ResourceNode>() != null)
                return true;

            if (col.GetComponent<IPlaceable>() != null || col.GetComponentInParent<IPlaceable>() != null)
                return true;

            if (col.CompareTag("Trap"))
                return true;

            if (!col.isTrigger && (col.CompareTag("Wall") ||
                col.gameObject.layer == LayerMask.NameToLayer("Water") ||
                col.gameObject.layer == LayerMask.NameToLayer("wall") ||
                col.gameObject.layer == LayerMask.NameToLayer("Building")))
            {
                return true;
            }
        }

        return false;
    }

    private void PlayPlantSuccess()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.plantSuccessClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.plantSuccessClip);
        }
    }

    private void PlayPlantFail()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.plantFailClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.plantFailClip);
        }
    }

    #region Farming & Tilling Logic (Animation Events & Validation)
    public void AnimEvent_Hoe()
    {
        TillSoil();
    }

    public void TillSoil()
    {
        if (Time.frameCount == lastTilledFrame) return;
        lastTilledFrame = Time.frameCount;

        if (equipment == null) equipment = GetComponent<PlayerEquipment>();

        // 1. Kiểm tra cầm Cuốc 
        bool isHoe = equipment != null && equipment.currentItem != null &&
                     (equipment.currentItem.isHoe ||
                      equipment.currentItem.toolType == ToolType.PickaxeAndHoe ||
                      equipment.currentItem.EffectiveToolType == ToolType.PickaxeAndHoe ||
                      (equipment.currentItem.itemName != null && equipment.currentItem.itemName.ToLower().Contains("hoe")));

        if (!isHoe) return;

        // 2. Tìm Farm Tilemap
        Tilemap farmTilemap = FarmableArea.ActiveFarmTilemap;
        if (farmTilemap == null)
        {
            FarmableArea farmArea = Object.FindFirstObjectByType<FarmableArea>();
            if (farmArea != null) farmTilemap = farmArea.GetComponent<Tilemap>();
        }

        if (farmTilemap == null)
        {
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tm in tilemaps)
            {
                if (tm.name.Contains("Soil") || tm.name.Contains("Farm") || tm.name.Contains("Dirt"))
                {
                    farmTilemap = tm;
                    break;
                }
            }
        }

        if (farmTilemap == null || dirtTile == null) return;

        // 3. Tính toán vị trí ô nhắm tới theo hướng nhìn của người chơi
        Vector3 checkPosition = feetPos != null ? feetPos.position : transform.position;

        Animator anim = GetComponent<Animator>();
        float lookX = anim != null ? anim.GetFloat("LastInputX") : 0f;
        float lookY = anim != null ? anim.GetFloat("LastInputY") : 0f;
        Vector2 lookDir = new Vector2(lookX, lookY);

        if (lookDir == Vector2.zero) lookDir = Vector2.down;

        if (Mathf.Abs(lookDir.y) > Mathf.Abs(lookDir.x))
        {
            lookDir = lookDir.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            lookDir = lookDir.x >= 0 ? Vector2.right : Vector2.left;
        }

        Vector3 targetPos = checkPosition + (Vector3)(lookDir * tillingDistance);
        Vector3Int cellPosition = farmTilemap.WorldToCell(targetPos);
        Vector3 cellCenter = farmTilemap.GetCellCenterWorld(cellPosition);

        // Đã là đất cuốc rồi thì bỏ qua
        if (farmTilemap.GetTile(cellPosition) == dirtTile) return;

        // 4. KIỂM TRA HỢP LỆ: Chặn cuốc đất trên cầu, trên nước và các bề mặt không hợp lệ
        if (!CanTillSoil(cellCenter, targetPos))
        {
            return;
        }

        // 5. Đặt ô đất cuốc thành công
        farmTilemap.SetTile(cellPosition, dirtTile);

        // 6. Phát âm thanh cuốc đất
        if (AudioManager.Instance != null && AudioManager.Instance.tillSoilClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.tillSoilClip);
        }
    }

    public bool CanTillSoil(Vector3 targetCenter, Vector3 targetPos)
    {
        InitRestrictedLayersIfEmpty();

        // 1. Tilemap Bridge & Water Check (Vì sàn cầu Tilemap không có Collider2D để nhân vật có thể đi lại)
        if (!FarmableArea.CanTillAt(targetCenter, targetPos))
        {
            return false;
        }

        // 2. Physics-based Validation (Dùng Physics2D.OverlapCircle quét LayerMask và Collider vật cản)
        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(targetCenter, tillingCheckRadius);
        if (colliders != null && colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;
                if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null) continue;

                // Kiểm tra theo restrictedTillingLayers (Building, wall, Water, Bridge...)
                if (((1 << col.gameObject.layer) & restrictedTillingLayers.value) != 0)
                {
                    return false;
                }

                // Kiểm tra tên hoặc tag Bridge/Wall/Building
                string colName = col.gameObject.name.ToLower();
                if (col.CompareTag("Bridge") || col.CompareTag("Wall") || colName.Contains("bridge"))
                {
                    return false;
                }

                // Không cuốc chồng lên mỏ đá, thân cây, quái vật, hoặc vật phẩm đã đặt
                if (col.GetComponent<ResourceNode>() != null || col.GetComponentInParent<ResourceNode>() != null) return false;
                if (col.CompareTag("Enemy") || col.GetComponentInParent<EnemyController>() != null) return false;
                if (col.GetComponent<IPlaceable>() != null || col.GetComponentInParent<IPlaceable>() != null) return false;

                PlantController plant = col.GetComponent<PlantController>() ?? col.GetComponentInParent<PlantController>();
                if (plant != null && Vector2.Distance(plant.transform.position, targetCenter) < 0.5f) return false;
            }
        }

        // 3. Tile-based Ground Validation (nếu có cấu hình danh sách tillableTiles trên baseTilemap)
        Tilemap groundMap = GetBaseTilemap();
        if (groundMap != null && tillableTiles != null && tillableTiles.Count > 0)
        {
            Vector3Int groundCell = groundMap.WorldToCell(targetCenter);
            TileBase tileOnGround = groundMap.GetTile(groundCell);
            if (tileOnGround == null || !tillableTiles.Contains(tileOnGround))
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Mining & Resource Logic (Animation Events)
    public void AnimEvent_Mine()
    {
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();

        // 1. Kiểm tra người chơi có đang cầm Cúp (Pickaxe) không
        if (equipment == null || equipment.currentItem == null || !equipment.currentItem.isPickaxe)
        {
            return;
        }

        // 2. Xác định hướng nhìn và tâm hitbox đào đá trước mặt
        Vector3 originPos = feetPos != null ? feetPos.position : transform.position;

        Animator anim = GetComponent<Animator>();
        float lookX = anim != null ? anim.GetFloat("LastInputX") : 0f;
        float lookY = anim != null ? anim.GetFloat("LastInputY") : 0f;
        Vector2 lookDir = new Vector2(lookX, lookY);

        if (lookDir == Vector2.zero) lookDir = Vector2.down;

        if (Mathf.Abs(lookDir.y) > Mathf.Abs(lookDir.x))
        {
            lookDir = lookDir.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            lookDir = lookDir.x >= 0 ? Vector2.right : Vector2.left;
        }

        Vector3 hitCenter = originPos + (Vector3)(lookDir * miningHitDistance);

        // 3. Quét va chạm
        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hitCenter, miningHitRadius, miningTargetLayers);
        if (colliders == null || colliders.Length == 0) return;

        bool hitRock = false;
        float damageToDeal = (equipment.currentItem.weaponData != null && equipment.currentItem.weaponData.BaseDamage > 0)
            ? equipment.currentItem.weaponData.BaseDamage
            : defaultMiningDamage;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;

            ResourceNode node = col.GetComponent<ResourceNode>() ?? col.GetComponentInParent<ResourceNode>();
            bool isRock = (node != null && node.Type == ResourceType.Rock) || col.CompareTag("Rock");

            if (isRock)
            {
                if (node != null)
                {
                    node.TakeDamage(damageToDeal);
                }
                else if (col.TryGetComponent(out IDamageable damageable) || (damageable = col.GetComponentInParent<IDamageable>()) != null)
                {
                    damageable.TakeDamage(damageToDeal);
                }

                hitRock = true;
                break;
            }
        }

        // 4. Phản hồi âm thanh đào đá
        if (hitRock && AudioManager.Instance != null && AudioManager.Instance.mineRockClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.mineRockClip);
        }
    }

    public void AnimEvent_Chop()
    {
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();

        // 1. Kiểm tra người chơi có đang cầm Rìu (Axe) không
        bool isAxe = equipment != null && equipment.currentItem != null && 
                     (equipment.currentItem.EffectiveToolType == ToolType.Axe || 
                      equipment.currentItem.toolType == ToolType.Axe ||
                      (equipment.currentItem.itemName != null && equipment.currentItem.itemName.ToLower().Contains("axe")));

        if (!isAxe) return;

        // 2. Xác định hướng nhìn và tâm hitbox trước mặt
        Vector3 originPos = feetPos != null ? feetPos.position : transform.position;

        Animator anim = GetComponent<Animator>();
        float lookX = anim != null ? anim.GetFloat("LastInputX") : 0f;
        float lookY = anim != null ? anim.GetFloat("LastInputY") : 0f;
        Vector2 lookDir = new Vector2(lookX, lookY);

        if (lookDir == Vector2.zero) lookDir = Vector2.down;

        if (Mathf.Abs(lookDir.y) > Mathf.Abs(lookDir.x))
        {
            lookDir = lookDir.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            lookDir = lookDir.x >= 0 ? Vector2.right : Vector2.left;
        }

        Vector3 hitCenter = originPos + (Vector3)(lookDir * miningHitDistance);

        // 3. Quét va chạm
        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hitCenter, miningHitRadius, miningTargetLayers);
        if (colliders == null || colliders.Length == 0) return;

        bool hitTree = false;
        float damageToDeal = (equipment.currentItem.weaponData != null && equipment.currentItem.weaponData.BaseDamage > 0)
            ? equipment.currentItem.weaponData.BaseDamage
            : defaultMiningDamage;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;

            ResourceNode node = col.GetComponent<ResourceNode>() ?? col.GetComponentInParent<ResourceNode>();
            bool isTree = (node != null && node.Type == ResourceType.Tree) || col.CompareTag("Tree");

            if (isTree)
            {
                if (node != null)
                {
                    node.TakeDamage(damageToDeal);
                }
                else if (col.TryGetComponent(out IDamageable damageable) || (damageable = col.GetComponentInParent<IDamageable>()) != null)
                {
                    damageable.TakeDamage(damageToDeal);
                }

                hitTree = true;
                break;
            }
        }

        if (hitTree && AudioManager.Instance != null && AudioManager.Instance.chopWoodClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.chopWoodClip);
        }
    }
    #endregion
}
