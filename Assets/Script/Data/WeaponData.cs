
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "KnightGame/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    [SerializeField] private string weaponName;
    public Sprite icon;

    [Header("Chỉ số chiến đấu")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float knockbackForce = 15f;

    [Header("Loại công cụ khai thác")]
    [SerializeField] private ToolType toolType = ToolType.None;

    [Header("Animation")]
    public AnimatorOverrideController overrideController;

    public string WeaponName => weaponName;
    public float BaseDamage => baseDamage;

    public float KnockbackForce => knockbackForce;
    public ToolType ToolType => toolType;
    public AnimatorOverrideController OverrideController => overrideController;
}
