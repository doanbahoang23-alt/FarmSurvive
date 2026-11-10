using UnityEngine;
using UnityEngine.Tilemaps;

public class HoeWeapon : MonoBehaviour
{
    [Header("Cài Đặt Đất Cuốc")]
    [SerializeField] private TileBase dirtTile;

    public TileBase DirtTile
    {
        get => dirtTile;
        set => dirtTile = value;
    }

    public void TillSoil()
    {
        // Ưu tiên chuyển giao xử lý về PlayerFarming trung tâm
        PlayerFarming farming = transform.root.GetComponent<PlayerFarming>();
        if (farming == null) farming = Object.FindFirstObjectByType<PlayerFarming>();
        if (farming != null)
        {
            farming.TillSoil();
            return;
        }

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
                if (tm.name.Contains("Soil") || tm.name.Contains("Farm") || tm.name.Contains("Dirt") || tm.name.Contains("Ground"))
                {
                    farmTilemap = tm;
                    break;
                }
            }
        }

        if (dirtTile == null && farming != null)
        {
            dirtTile = farming.DirtTile;
        }

        if (farmTilemap == null || dirtTile == null)
        {
            return;
        }

        Animator anim = transform.root.GetComponent<Animator>();
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

        Transform feetTransform = farming != null ? farming.FeetPos : null;
        Vector3 checkPosition = feetTransform != null ? feetTransform.position : transform.root.position;
        Vector3 targetPos = checkPosition + (Vector3)(lookDir * 0.8f);

        Vector3Int cellPosition = farmTilemap.WorldToCell(targetPos);
        Vector3 cellCenter = farmTilemap.GetCellCenterWorld(cellPosition);

        // Đã là đất cuốc rồi thì bỏ qua
        if (farmTilemap.GetTile(cellPosition) == dirtTile) return;

        // Kiểm tra vật cản và vị trí cấm (Cầu / Nước)
        if (IsCellBlockedForTilling(cellCenter, targetPos))
        {
            return;
        }

        // Biến thành đất cuốc
        farmTilemap.SetTile(cellPosition, dirtTile);

        // Âm thanh cuốc đất
        if (AudioManager.Instance != null && AudioManager.Instance.tillSoilClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.tillSoilClip);
        }
    }

    private bool IsCellBlockedForTilling(Vector3 centerPos, Vector3 targetPos)
    {
        // Chặn cuốc trên cầu hoặc mặt nước
        if (!FarmableArea.CanTillAt(centerPos, targetPos))
        {
            return true;
        }

        Physics2D.SyncTransforms();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(centerPos, 0.4f);
        if (colliders == null || colliders.Length == 0) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.gameObject == gameObject || col.transform.root == transform.root) continue;
            if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null) continue;

            if (col.GetComponent<ResourceNode>() != null || col.GetComponentInParent<ResourceNode>() != null)
            {
                return true;
            }

            if (col.CompareTag("Enemy") || col.GetComponentInParent<EnemyController>() != null)
            {
                return true;
            }

            PlantController plant = col.GetComponent<PlantController>();
            if (plant == null) plant = col.GetComponentInParent<PlantController>();
            if (plant != null)
            {
                if (Vector2.Distance(plant.transform.position, centerPos) < 0.5f)
                {
                    return true;
                }
                continue;
            }

            if (col.GetComponent<IPlaceable>() != null || col.GetComponentInParent<IPlaceable>() != null)
            {
                return true;
            }

            if (col.CompareTag("Bridge") || col.gameObject.name.IndexOf("bridge", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (!col.isTrigger && (col.CompareTag("Wall") ||
                col.gameObject.layer == LayerMask.NameToLayer("wall") ||
                col.gameObject.layer == LayerMask.NameToLayer("Building") ||
                col.gameObject.layer == LayerMask.NameToLayer("Water")))
            {
                return true;
            }
        }

        return false;
    }
}
