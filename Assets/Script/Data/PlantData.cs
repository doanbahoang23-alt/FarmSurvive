using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantData", menuName = "KnightGame/Plant Data")]
public class PlanData : ScriptableObject
{
    public string plantName;
    public Sprite[] growthStageSprites;
    public float timePerStage = 5f;
    public GameObject dropItemPrefab;
    public int dropAmount = 1;
}