using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerFarming : MonoBehaviour
{
    [Header("Farm Settings")]
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private Transform feetPos;

    [Header("Mining Settings (Đào đá)")]
    [SerializeField] private float miningHitDistance = 0.8f;
    [SerializeField] private float miningHitRadius = 0.5f;
    [SerializeField] private float defaultMiningDamage = 35f;
    [SerializeField] private LayerMask miningTargetLayers = ~0;

    public TileBase DirtTile => dirtTile;
    public Transform FeetPos => feetPos;

    private PlayerEquipment equipment;
    private PlayerInventory inventory;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        inventory = GetComponent<PlayerInventory>();

        if (GetComponent<PlayerPlacement>() == null)
        {
            gameObject.AddComponent<PlayerPlacement>();
        }
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
        // 1. Kiểm tra logic ô Grid: Mỗi ô đất chỉ được trồng tối đa 1 cây
        PlantController[] plants = Object.FindObjectsByType<PlantController>(FindObjectsSortMode.None);
        for (int i = 0; i < plants.Length; i++)
        {
            PlantController plant = plants[i];
            if (plant == null || !plant.gameObject.activeInHierarchy) continue;

            // Nếu cây này nằm đúng trong ô cellPos này
            if (tilemap.WorldToCell(plant.transform.position) == cellPos) return true;

            if (Vector2.Distance(plant.transform.position, centerPos) < 0.5f)
            {
                return true;
            }
        }

        // 2. Quét vật cản trên ô (bỏ qua PlantController vì đã kiểm tra chính xác bằng Grid ở trên, tránh trigger bán kính 2 của cây hàng xóm gây chặn nhầm)
        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapBoxAll(centerPos, new Vector2(0.6f, 0.6f), 0f);
        if (colliders == null || colliders.Length == 0) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;
            if (col.CompareTag("Player") || col.CompareTag("Enemy")) continue;

            // Bỏ qua PlantController vì đã kiểm tra chính xác theo ô ở trên
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
