using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingItemDatabase", menuName = "Crafting/Item Database")]
public class CraftingItemDatabase : ScriptableObject
{
    //list of all craftable items
    [SerializeField] private List<CraftingItemData> itemList;

    public List<int> ItemTiers { get; private set; }

    public List<CraftingItemData> ItemList => itemList;

    private void OnValidate()
    {
        UpdateItemTiers();
    }

    public void UpdateItemTiers()
    {
        var uncheckedItems = new List<CraftingItemData>(ItemList);
        var checkedItems = new HashSet<CraftingItemData>();
        var itemsWithPrereqLoop = new HashSet<CraftingItemData>();

        int tier = 0;
        while(uncheckedItems.Count > 0)
        {
            for (int i = uncheckedItems.Count - 1; i >= 0; i--)
            {
                var itemData = uncheckedItems[i];
                var allPrereqsInPrevTier = true;
                if (itemData.Prerequisites.Count > 0)
                {
                    if (itemData.HasPrerequisiteLoop)
                    {
                        Debug.LogError($"{itemData} contains a prerequisite loop! Cannot update its tier.");
                        itemData.Tier = -1;
                        uncheckedItems.RemoveAt(i);
                        itemsWithPrereqLoop.Add(itemData);
                        continue;
                    }

                    foreach (var prereq in itemData.Prerequisites)
                    {
                        if(itemsWithPrereqLoop.Contains(prereq))
                        {
                            Debug.LogError($"{itemData} has a prerequisite which forms a loop ({prereq})! Cannot update its tier.");
                            itemData.Tier = -1;
                            itemsWithPrereqLoop.Add(itemData);
                            uncheckedItems.RemoveAt(i);
                            break;
                        }

                        if (!checkedItems.Contains(prereq))
                        {
                            allPrereqsInPrevTier = false;
                            break;
                        }
                    }
                }

                if (allPrereqsInPrevTier)
                {
                    itemData.Tier = tier;
                    checkedItems.Add(itemData);
                    uncheckedItems.RemoveAt(i);
                    Debug.Log($"{itemData.name} is tier {tier}");
                }
            }

            tier++;
        }
    }
}
