using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlantController : MonoBehaviour, IHaverstable, IPlaceable
{
    #region Static Registry
    public static readonly List<PlantController> ActivePlants = new List<PlantController>();
    #endregion

    [SerializeField] private PlanData plantData;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentStage = 0; // giai đoạn
    private float growthTimer = 0f; // Thời gian chín
    private bool isFullyGrown = false;

    public bool IsFullyGrown => isFullyGrown;
    public PlanData PlantData => plantData;
    public int CurrentStage => currentStage;
    public float GrowthTimer => growthTimer;

    private void OnEnable()
    {
        if (!ActivePlants.Contains(this))
        {
            ActivePlants.Add(this);
        }

        currentStage = 0;
        growthTimer = 0f;
        isFullyGrown = false;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        UpdatePlantSprite();
    }

    private void OnDisable()
    {
        ActivePlants.Remove(this);
    }

    private void OnDestroy()
    {
        ActivePlants.Remove(this);
    }

    void Update()
    {
        HandleGrowth();
    }

    private void HandleGrowth()
    {
        if (isFullyGrown || plantData == null) return;

        growthTimer += Time.fixedDeltaTime;
        if (growthTimer >= plantData.timePerStage)
        {
            growthTimer = 0f;
            currentStage++;

            if (currentStage >= plantData.growthStageSprites.Length - 1)
            {
                currentStage = plantData.growthStageSprites.Length - 1;
                isFullyGrown = true;
            }
            UpdatePlantSprite();
        }
    }

    private void UpdatePlantSprite()
    {
        if (plantData != null && plantData.growthStageSprites.Length > 0)
        {
            spriteRenderer.sprite = plantData.growthStageSprites[currentStage];
        }
    }

    public void SetPlantState(int stage, float timer, bool fullyGrown)
    {
        currentStage = stage;
        growthTimer = timer;
        isFullyGrown = fullyGrown;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        UpdatePlantSprite();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFullyGrown && collision.CompareTag("Player"))
        {
            Harvest(collision.gameObject);
        }
    }

    public void Harvest(GameObject harvester)
    {
        if (!isFullyGrown) return;

        if (plantData.dropItemPrefab != null)
        {
            for (int i = 0; i < plantData.dropAmount; i++)
            {
                Vector3 spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
                PoolManager.Instance.Spawn(plantData.dropItemPrefab, transform.position + spawnOffset, Quaternion.identity);
            }
        }

        // Tự động cộng thẳng EXP cho người thu hoạch nếu cây có gắn BaseExpReward hoặc PlantExpReward
        if (TryGetComponent(out ExpReward expReward))
        {
            expReward.GiveExp(harvester);
        }

        Tilemap parentTilemap = GetComponentInParent<Tilemap>();
        if (parentTilemap != null)
        {
            Vector3Int cellPos = parentTilemap.WorldToCell(transform.position);
            parentTilemap.SetTile(cellPos, null);
        }
        else
        {
            PoolManager.Instance.Release(gameObject);
        }
    }
}
