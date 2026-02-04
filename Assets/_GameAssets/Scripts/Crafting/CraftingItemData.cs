using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CraftingItemData", menuName = "Crafting/ItemData")]
public class CraftingItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    //items with a crafting alias can be treated as that item during crafting (I'm sure this won't break anything)
    [SerializeField] private List<CraftingItemData> craftingAliases; 
    [SerializeField] private Texture2D thumbnailTex;
    [SerializeField] private CraftingItemWindowContent contentPrefab;
    [SerializeField] private List<CraftingItemData> prerequisites;
    [SerializeField] private List<CraftingItemData> products;

    public string ItemName => itemName;
    public List<CraftingItemData> Aliases => craftingAliases;
    public Texture2D ThumbnailTex => thumbnailTex;
    public CraftingItemWindowContent WindowContent => contentPrefab;
    public List<CraftingItemData> ExtraProducts => products;
    public List<CraftingItemData> Prerequisites => prerequisites;

    public override string ToString()
    {
        return itemName;
    }
}
