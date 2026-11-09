using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [System.Serializable]
    public struct WeaponSlot
    {
        public ToolType toolType;
        public WeaponData weaponData;
        public GameObject weaponObject;
    }

    [Header("Cấu Hình Vũ Khí")]
    public Transform weaponSocket;
    public List<WeaponSlot> weaponList = new List<WeaponSlot>();

    [Header("Vật Phẩm Khác (Hạt giống, Đồ ăn...)")]
    public SpriteRenderer genericItemHold;

    [Header("Vật Phẩm Đang Cầm")]
    public ItemData currentItem;

    private Animator animator;
    private RuntimeAnimatorController baseController;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            baseController = animator.runtimeAnimatorController;
        }

        InitWeapons();
    }

    private void InitWeapons()
    {
        if (weaponList != null && weaponList.Count > 0) return;
        if (weaponSocket == null) return;

        weaponList = new List<WeaponSlot>();
        foreach (Transform child in weaponSocket)
        {
            WeaponSlot slot = new WeaponSlot();
            slot.weaponObject = child.gameObject;

            if (child.name.Contains("Pickaxe") || child.name.Contains("Hoe"))
            {
                slot.toolType = ToolType.PickaxeAndHoe;
                if (child.GetComponent<HoeWeapon>() == null)
                {
                    child.gameObject.AddComponent<HoeWeapon>();
                }

                // Đảm bảo Cuốc có component Weapon để có thể đập đá
                if (child.GetComponent<Weapon>() == null)
                {
                    Weapon wp = child.gameObject.AddComponent<Weapon>();
                    wp.SetWeaponStats(10f, 5f);
                }

                // Đảm bảo có Trigger Collider để va chạm với đá
                Collider2D col = child.GetComponent<Collider2D>();
                if (col == null)
                {
                    BoxCollider2D box = child.gameObject.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.offset = new Vector2(0f, 1.2f);
                    box.size = new Vector2(1f, 0.8f);
                }
                else
                {
                    col.isTrigger = true;
                }
            }
            else if (child.name.Contains("Axe"))
            {
                slot.toolType = ToolType.Axe;
                if (child.GetComponent<Weapon>() == null)
                {
                    child.gameObject.AddComponent<MeleeWeapon>();
                }
                Collider2D col = child.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = true;
            }
            else if (child.name.Contains("Spear"))
            {
                // Đảm bảo Spear có component MeleeWeapon và trigger collider để nhận diện va chạm khi đâm
                if (child.GetComponent<Weapon>() == null)
                {
                    child.gameObject.AddComponent<MeleeWeapon>();
                }
                Collider2D col = child.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = true;
            }
            else if (child.GetComponent<BowWeapon>() == null && child.GetComponent<Weapon>() == null)
            {
                // Bất kỳ vũ khí cận chiến nào khác (Sword...) nếu thiếu component sẽ tự động được thêm MeleeWeapon
                child.gameObject.AddComponent<MeleeWeapon>();
                Collider2D col = child.GetComponent<Collider2D>();
                if (col != null) col.isTrigger = true;
            }

            weaponList.Add(slot);
        }
    }

    public void EquipItem(ItemData newItem)
    {
        currentItem = newItem;

        HideAllWeapons();

        if (newItem == null)
        {
            if (animator != null && baseController != null)
            {
                animator.runtimeAnimatorController = baseController;
            }
            return;
        }

        if (animator != null)
        {
            if (newItem.weaponData != null && newItem.weaponData.overrideController != null)
            {
                animator.runtimeAnimatorController = newItem.weaponData.overrideController;
            }
            else if (baseController != null)
            {
                animator.runtimeAnimatorController = baseController;
            }
        }

        GameObject weaponObj = GetWeaponObject(newItem);

        if (weaponObj != null)
        {
            weaponObj.SetActive(true);
            if (genericItemHold != null) genericItemHold.gameObject.SetActive(false);

            // Đảm bảo Collider của vũ khí là trigger để đâm xuyên quái vật và kích hoạt va chạm
            Collider2D weaponCol = weaponObj.GetComponent<Collider2D>();
            if (weaponCol != null)
            {
                weaponCol.isTrigger = true;
            }

            if (newItem.weaponData != null)
            {
                Weapon weapon = weaponObj.GetComponent<Weapon>();
                if (weapon == null && weaponObj.GetComponent<BowWeapon>() == null)
                {
                    weapon = weaponObj.AddComponent<MeleeWeapon>();
                }

                if (weapon != null)
                {
                    weapon.ApplyWeaponData(newItem.weaponData);
                }
                else
                {
                    BaseDamage baseDamage = weaponObj.GetComponent<BaseDamage>();
                    if (baseDamage != null)
                    {
                        baseDamage.SetDamage(newItem.weaponData.BaseDamage, newItem.weaponData.KnockbackForce);
                    }
                }
            }
        }
        else
        {
            bool isToolOrWeapon = newItem.weaponData != null || newItem.toolType != ToolType.None;
            if (genericItemHold != null && !isToolOrWeapon)
            {
                genericItemHold.gameObject.SetActive(true);
                genericItemHold.sprite = newItem.icon;
            }
            else if (genericItemHold != null)
            {
                genericItemHold.gameObject.SetActive(false);
            }
        }
    }

    private GameObject GetWeaponObject(ItemData item)
    {
        if (item == null || weaponList == null) return null;

        if (item.weaponData != null)
        {
            for (int i = 0; i < weaponList.Count; i++)
            {
                if (weaponList[i].weaponData == item.weaponData && weaponList[i].weaponObject != null)
                {
                    return weaponList[i].weaponObject;
                }
            }
        }

        if (item.toolType != ToolType.None)
        {
            for (int i = 0; i < weaponList.Count; i++)
            {
                if (weaponList[i].toolType == item.toolType && weaponList[i].weaponObject != null)
                {
                    return weaponList[i].weaponObject;
                }
            }
        }

        for (int i = 0; i < weaponList.Count; i++)
        {
            GameObject obj = weaponList[i].weaponObject;
            if (obj == null) continue;

            if (string.Equals(obj.name, item.name, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(obj.name, item.itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return obj;
            }
        }

        if (weaponSocket != null)
        {
            Transform found = weaponSocket.Find(item.name);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    public void HideAllWeapons()
    {
        if (weaponList != null)
        {
            for (int i = 0; i < weaponList.Count; i++)
            {
                if (weaponList[i].weaponObject != null)
                {
                    weaponList[i].weaponObject.SetActive(false);
                }
            }
        }

        if (weaponSocket != null)
        {
            foreach (Transform child in weaponSocket)
            {
                child.gameObject.SetActive(false);
            }
        }

        if (genericItemHold != null)
        {
            genericItemHold.gameObject.SetActive(false);
        }
    }

    public void AnimEvent_Shoot()
    {
        BowWeapon bow = null;
        if (weaponSocket != null)
        {
            bow = weaponSocket.GetComponentInChildren<BowWeapon>(true);
        }

        if (bow == null)
        {
            bow = transform.root.GetComponentInChildren<BowWeapon>(true);
        }

        if (bow != null && bow.gameObject.activeInHierarchy)
        {
            bow.Shoot();
        }
    }

    public void AnimEvent_Hoe()
    {
        HoeWeapon hoe = null;
        if (weaponSocket != null)
        {
            hoe = weaponSocket.GetComponentInChildren<HoeWeapon>(true);
        }

        if (hoe == null)
        {
            hoe = transform.root.GetComponentInChildren<HoeWeapon>(true);
        }

        if (hoe != null && hoe.gameObject.activeInHierarchy)
        {
            hoe.TillSoil();
        }
    }

    public void AnimEvent_Attack()
    {
        Weapon weapon = null;
        if (weaponSocket != null)
        {
            weapon = weaponSocket.GetComponentInChildren<Weapon>(false);
        }

        if (weapon == null)
        {
            weapon = transform.root.GetComponentInChildren<Weapon>(false);
        }

        if (weapon != null && weapon.gameObject.activeInHierarchy && weapon.GetComponent<BowWeapon>() == null)
        {
            weapon.PerformAttack();
        }
    }

    public bool TryConsumeCurrentFood()
    {
        if (currentItem == null) return false;
        // CHỈ cho phép ăn nếu vật phẩm có kiểu là Food (ItemType.Food)
        if (!currentItem.IsFood) return false;

        PlayerFood food = GetComponent<PlayerFood>() ?? PlayerFood.Instance;
        PlayerInventory inv = GetComponent<PlayerInventory>();
        PlayerHotbar hotbar = GetComponent<PlayerHotbar>();

        if (food != null && inv != null)
        {
            float restoreAmount = currentItem.foodRestoreAmount > 0 ? currentItem.foodRestoreAmount : 20f;
            food.RestoreFood(restoreAmount);

            bool consumed = false;
            if (hotbar != null && hotbar.currentHotbarIndex >= 0 && inv.slots != null)
            {
                int realIndex = hotbar.currentHotbarIndex + 30;
                if (realIndex >= 0 && realIndex < inv.slots.Length)
                {
                    var slot = inv.slots[realIndex];
                    if (slot != null && slot.itemData == currentItem && slot.amount > 0)
                    {
                        slot.amount--;
                        consumed = true;
                        if (slot.amount <= 0)
                        {
                            slot.amount = 0;
                            slot.itemData = null;
                            EquipItem(null);
                        }
                        if (inv.uiManager != null) inv.uiManager.UpdateUI(inv.slots);
                    }
                }
            }

            if (!consumed)
            {
                inv.RemoveItem(currentItem, 1);
                if (inv.GetItemAmount(currentItem) <= 0)
                {
                    EquipItem(null);
                }
            }

            return true;
        }

        return false;
    }
}
