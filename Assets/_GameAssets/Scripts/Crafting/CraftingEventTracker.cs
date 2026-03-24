using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingEventTracker
{
    private HashSet<CraftingItemData> uniqueItemsCrafted;
    private List<CraftingItemData> uniqueItemsCraftedInCraftOrder;

    public event Action<CraftingItemData> itemFirstCrafted;

    public CraftingEventTracker()
    {
        uniqueItemsCrafted = new HashSet<CraftingItemData>();
        uniqueItemsCraftedInCraftOrder = new List<CraftingItemData>();
    }

    public void OnItemCrafted(CraftingItemData itemData)
    {
        if(!uniqueItemsCrafted.Contains(itemData))
        {
            uniqueItemsCrafted.Add(itemData);
            uniqueItemsCraftedInCraftOrder.Add(itemData);
            itemFirstCrafted?.Invoke(itemData);
        }
    }

    public List<CraftingItemData> GetUniqueItemsCrafted()
    {
        return uniqueItemsCraftedInCraftOrder;
    }

    public bool WasItemCraftedPreviously(CraftingItemData itemData)
    {
        return uniqueItemsCrafted.Contains(itemData);
    }
}
