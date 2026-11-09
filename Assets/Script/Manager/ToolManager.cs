using UnityEngine;

public enum ToolType
{
    None = 0,         
    Axe = 1,         
    PickaxeAndHoe = 2
}

public static class ToolManager
{
    public static bool CanTillSoil(ToolType tool)
    {
        return tool == ToolType.PickaxeAndHoe;
    }

    public static bool CanChopTree(ToolType tool)
    {
        return tool == ToolType.Axe;
    }

    public static bool CanMineRock(ToolType tool)
    {
        return tool == ToolType.PickaxeAndHoe;
    }

    public static bool CanHarvestResource(ToolType tool, ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Tree:
                return CanChopTree(tool);

            case ResourceType.Rock:
                return CanMineRock(tool);

            default:
                return false;
        }
    }

    public static bool CanAttackEnemies(ToolType tool)
    {
        return true;
    }

    public static bool IsCombatOnly(ToolType tool)
    {
        return tool == ToolType.None;
    }

    public static ToolType GetEquippedTool(GameObject user)
    {
        if (user == null) return ToolType.None;

        PlayerEquipment equipment = user.GetComponent<PlayerEquipment>() ?? user.GetComponentInParent<PlayerEquipment>();
        if (equipment != null && equipment.currentItem != null)
        {
            return equipment.currentItem.EffectiveToolType;
        }

        return ToolType.None;
    }
}

