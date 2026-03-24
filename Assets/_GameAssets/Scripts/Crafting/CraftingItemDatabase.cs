using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingItemDatabase", menuName = "Crafting/Item Database")]
public class CraftingItemDatabase : ScriptableObject
{
    //list of all craftable items
    [SerializeField] private List<CraftingItemData> itemList;

    public List<CraftingItemData> ItemList => itemList;
}
