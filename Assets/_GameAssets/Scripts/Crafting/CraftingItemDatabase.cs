using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingItemDatabase", menuName = "Crafting/Item Database")]
public class CraftingItemDatabase : ScriptableObject
{
    //list of all craftable items
    [SerializeField] private List<CraftingItemData> itemList; 

    //TODO: PROTOTYPE, optimise!!!!
    public Crafter.CraftingResultState GetCraftResult(HashSet<CraftingItem> ingredients, out CraftingItemData result)
    {
        result = null;
        var resultState = Crafter.CraftingResultState.NoIngredientMatch;

        if (ingredients.Count < 2)
        {
            Debug.LogError($"Crafting attempted with <2 ingredients! This shouldn't happen");
            return Crafter.CraftingResultState.NoIngredientMatch;
        }

        foreach (var item in itemList)
        {
            //exit if item isn't craftable by two or more other items
            if (item.Prerequisites.Count < 2)
            {
                continue;
            }

            //check number of matching ingredients we have to this item
            int numMatching = 0;
            var unusedIngredients = new List<CraftingItem>(ingredients); //TODO: ALLOCATION
            foreach (var prereqData in item.Prerequisites)
            {
                for(int i = unusedIngredients.Count -1; i >= 0; i--)
                {
                    if (unusedIngredients[i].Data == prereqData)
                    {
                        numMatching++;
                        //Debug.Log($"Checked {prereqData.ItemName} against {unusedIngredients[i].Data}, num matching: {numMatching}");
                        unusedIngredients.RemoveAt(i);
                        break; //avoids matching two of the same ingredient to one of the same prerequisite
                    }
                }
            }

            if (numMatching == item.Prerequisites.Count) //if we have the same number of matches as prerequisites...
            {
                //if we have -more- ingredients than prerequisites, count it as a partial craft
                //(so we can display the partial-craft VFX and player might guess they need to remove an item)
                //TODO: think about this! Should this case count as a partial craft or not? It would confuse players
                //if they think a partial craft always has -fewer- ingredients than required
                if (ingredients.Count > item.Prerequisites.Count)
                {
                    resultState = Crafter.CraftingResultState.PartialIngredientMatch;
                }
                else //successful craft
                {
                    result = item;
                    return Crafter.CraftingResultState.SuccessfulCraft;
                }
            }

            if (numMatching > 1)
            {
                resultState = Crafter.CraftingResultState.PartialIngredientMatch;
            }
        }

        return resultState;
    }
}
