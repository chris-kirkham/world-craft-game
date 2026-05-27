using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    public class CraftingManager : SingletonMonoBehaviour<CraftingManager>
    {
        public enum CraftingResultState
        {
            None,
            PartialIngredientMatch,
            SuccessfulCraft
        }

        [SerializeField] private CraftingItemDatabase itemDatabase;
        [SerializeField] private CraftingItem thumbnailPrefab;
        [SerializeField] private CraftingItemWindow windowPrefab;
        [SerializeField] private GameObject gameOverSequence; //TODO: move this somewhere proper
        [Header("Crafting sequence")]
        [SerializeField] private OnCraftSequence craftSequence;
        [Header("Crafting zones")]
        [SerializeField] private CrafterBoard crafterBoard;
        [SerializeField] private List<CraftingItemData> randomProducts;
        [SerializeField] private bool SpawnHelperIngredientsIfNoCraftPossible = true;
        [SerializeField] private float helperSpawnMinTime = 1f;
        [SerializeField] private float helperSpawnMaxTime = 10f;
        [Header("Debug")]
        [SerializeField] private DebugDisplay debugDisplay;

        //TODO: move this to a more generic ItemManager if keeping functionality
        private HashSet<CraftingItem> activeItems = new HashSet<CraftingItem>(); //all enabled crafting items
        public HashSet<CraftingItem> ActiveItems => activeItems;

        //cached lists of stuff
        private List<CraftingItem> unusedIngredients = new List<CraftingItem>(); //unused ingredients during each craft attempt
        private const int MaxResultsPerCraft = 10; //increase this if we need more
        private CraftingItemData[] craftResults = new CraftingItemData[MaxResultsPerCraft];
        private CraftingResultState[] craftResultStates = new CraftingResultState[MaxResultsPerCraft];

        private CraftingEventTracker craftedTracker = new CraftingEventTracker();
        private ItemContactsTracker itemContactsTracker = new ItemContactsTracker();
        private Coroutine spawnHelperItemCoroutine;

        private void Start()
        {
            //just in case - crafting sequence should never be active when the manager is first enabled
            if(craftSequence)
            {
                craftSequence.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (crafterBoard)
            {
                foreach (var placementPoint in crafterBoard.PlacementPoints)
                {
                    placementPoint.ItemPlaced -= OnItemPlacedOrRemoved;
                    placementPoint.ItemRemoved -= OnItemPlacedOrRemoved;
                }
            }
        }

        private void LateUpdate()
        {
            if (debugDisplay)
            {
                debugDisplay.SetCraftingDebugInfo(itemContactsTracker.ContactGroups);
            }

            itemContactsTracker.LateUpdate();

            if (CheckForPossibleCrafts())
            {
                if (spawnHelperItemCoroutine != null)
                {
                    StopCoroutine(spawnHelperItemCoroutine);
                }
            }
            else //no crafts possible - start spawning helper items
            {
                //TODO: prototype! ugh
                if (!gameOverSequence)
                {
                    gameOverSequence = FindFirstObjectByType<ItemShowcaseSequence>().transform.root.gameObject;
                }

                if (gameOverSequence)
                {
                    gameOverSequence.SetActive(true);
                }

                if (spawnHelperItemCoroutine == null)
                {
                    spawnHelperItemCoroutine = StartCoroutine(SpawnHelperItem());
                }
            }
        }

        private void OnItemPlacedOrRemoved()
        {
            var placedItems = new List<CraftingItem>(); //TODO: allocation
            foreach (var placementPoint in crafterBoard.PlacementPoints)
            {
                if (placementPoint.CurrentItem)
                {
                    placedItems.Add(placementPoint.CurrentItem);
                }
            }

            if(placedItems.Count > 1)
            {
                TryCraft(placedItems);
            }
        }

        public void AddItemContact(CraftingItem item, CraftingItem contactingItem)
        {
            itemContactsTracker.AddItemContact(item, contactingItem);
        }

        public void RemoveItemContact(CraftingItem item, CraftingItem contactingItem)
        {
            itemContactsTracker.RemoveItemContact(item, contactingItem);
        }

        public void RemoveAllItemContactsForItem(CraftingItem item)
        {
            itemContactsTracker.RemoveAllItemContactsForItem(item);
        }

        private int TryCraft(ICollection<CraftingItem> ingredients)
        {
            var numResults = GetCraftResultsAllItems(ingredients, craftResults, craftResultStates);

            //TODO: allocation
            var anySuccessfulCraft = false;
            var anyPartialMatch = false;
            var successfulCrafts = new List<CraftingItemData>();
            for (int i = 0; i < numResults; i++)
            {
                if (craftResultStates[i] == CraftingResultState.SuccessfulCraft)
                {
                    successfulCrafts.Add(craftResults[i]);
                    anySuccessfulCraft = true;
                }
                else if (craftResultStates[i] == CraftingResultState.PartialIngredientMatch)
                {
                    anyPartialMatch = true;
                }
            }

            if (anySuccessfulCraft)
            {
                DoSuccessfulCrafts(ingredients, successfulCrafts);
            }
            else if(anyPartialMatch)
            {
                foreach (var ingredient in ingredients)
                {
                    //TODO FOR PARTIAL CRAFT RESULT: TELL CRAFTING ITEM IF IT IS PART OF THE CRAFT OR NOT
                    ingredient.OnCraftAttempt(CraftingResultState.PartialIngredientMatch);
                }
            }
            else
            {
                foreach(var ingredient in ingredients)
                {
                    ingredient.OnCraftAttempt(CraftingResultState.None);
                }

                //TODO: Fix placement points not removing items correctly when they're moved to grid from here
                MoveItemsToGrid(ingredients);
            }

            return numResults;
        }


        private int GetCraftResultsAllItems(ICollection<CraftingItem> ingredients, CraftingItemData[] results, CraftingResultState[] resultStates)
        {
            if (ingredients.Count < 2)
            {
                return 0;
            }

            int numResults = 0;
            CraftingItemData currentResult = null;
            CraftingResultState currentResultState;
            foreach (var result in itemDatabase.ItemList)
            {
                //exit if item isn't craftable by two or more other items
                if (result.Prerequisites.Count < 2)
                {
                    continue;
                }

                currentResultState = CraftingResultState.None;
                currentResult = result;

                //check number of matching ingredients we have to this item
                int numMatching = GetNumMatchingIngredientsToItemPrereqs(result, ingredients);

                if (numMatching == result.Prerequisites.Count)
                {
                    //if we have -more- ingredients than prerequisites, count it as a partial craft
                    //(so we can display the partial-craft VFX and player might guess they need to remove an item)
                    //TODO: think about this! Should this case count as a partial craft or not? It would confuse players
                    //if they think a partial craft always has -fewer- ingredients than required
                    if (ingredients.Count > result.Prerequisites.Count)
                    {
                        currentResultState = CraftingResultState.PartialIngredientMatch;
                    }
                    else //successful craft
                    {
                        currentResultState = CraftingResultState.SuccessfulCraft;
                    }
                }
                else if (numMatching > 1)
                {
                    currentResultState = CraftingResultState.PartialIngredientMatch;
                }

                //if there was a successful or partial result for this item, add it to the results buffer
                if (currentResultState != CraftingResultState.None)
                {
                    craftResults[numResults] = currentResult;
                    craftResultStates[numResults] = currentResultState;

                    numResults++;

                    if (numResults >= MaxResultsPerCraft)
                    {
                        Debug.LogError($"More craft results created from this craft than space in the results buffer! Increase its size!!!");
                        return numResults;
                    }
                }
            }

            return numResults;
        }

        private int GetNumMatchingIngredientsToItemPrereqs(CraftingItemData item, ICollection<CraftingItem> ingredients)
        {
            unusedIngredients.Clear();
            unusedIngredients.AddRange(ingredients);

            int numMatching = 0;
            foreach (var prereqData in item.Prerequisites)
            {
                for (int i = unusedIngredients.Count - 1; i >= 0; i--)
                {
                    if (unusedIngredients[i].Data == prereqData || unusedIngredients[i].Data.Aliases.Contains(prereqData))
                    {
                        numMatching++;
                        unusedIngredients.RemoveAt(i);
                        break; //avoids matching two of the same ingredient to one of the same prerequisite
                    }
                }
            }

            return numMatching;
        }

        private void DoSuccessfulCrafts(ICollection<CraftingItem> ingredients, List<CraftingItemData> successfulCrafts)
        {
            if (!craftSequence)
            {
                Debug.LogError($"No {nameof(craftSequence)} PlayableDirector set!");
                return;
            }

            craftSequence.gameObject.SetActive(true);
            craftSequence.DoCraftSequence(ingredients, successfulCrafts);
        }

        public CraftingItem SpawnItem(CraftingItemData itemData, Vector3 position, Quaternion rotation, bool doItemOnCraftedCallback = true)
        {
            var item = Instantiate<CraftingItem>(thumbnailPrefab, position, rotation);
            item.Data = itemData;

            if (doItemOnCraftedCallback)
            {
                craftedTracker.OnItemCrafted(itemData);
                item.OnCrafted();
            }

            return item;
        }

        public void MoveItemToGrid(CraftingItem item, bool stackSameItems = true)
        {
            Debug.Assert(crafterBoard);
            if(crafterBoard)
            {
                crafterBoard.MoveItemToGrid(item, stackSameItems);
            }
        }

        public void MoveItemsToGrid(ICollection<CraftingItem> items, bool stackSameItems = true)
        {
            Debug.Assert(crafterBoard);
            if(crafterBoard)
            {
                foreach(var item in items)
                {
                    crafterBoard.MoveItemToGrid(item, stackSameItems);
                }
            }
        }

        public void OnItemDisabledOrDestroyed(CraftingItem item)
        {
            itemContactsTracker.OnItemDisabledOrDestroyed(item);
            DeregisterCraftingItem(item);
        }

        //checks for any possible crafts with the items currently on the board; returns true if any crafts are possible.
        //IDEA: this could be extended to check for crafts which the player is only 1 (or 2 or 3) ingredients or steps away from,
        //and we could spawn specific items to help! Obviously this will get complex fast if we're looking multiple steps ahead e.g. 
        //if player has items a, b, and c, then they could craft d using a and b, which will enable them to craft e using c and d...
        private bool CheckForPossibleCrafts()
        {
            var anyPossibleCraft = false;
            foreach (var item in itemDatabase.ItemList)
            {
                var numMatching = GetNumMatchingIngredientsToItemPrereqs(item, activeItems);
                if (numMatching >= item.Prerequisites.Count)
                {
                    anyPossibleCraft = true;
                    break;
                }
            }

            return anyPossibleCraft;
        }

        public List<CraftingItemData> FindPossibleCrafts(bool includeAlreadyCraftedItems = true)
        {
            var possibleCrafts = new List<CraftingItemData>();
            foreach(var itemData in itemDatabase.ItemList)
            {
                if(itemData.Prerequisites.Count == 0)
                {
                    continue;
                }

                if(!includeAlreadyCraftedItems && crafterBoard.ActiveItemCounts.ContainsKey(itemData))
                {
                    continue;
                }

                var craftPossible = true;
                foreach (var prereq in itemData.Prerequisites)
                {
                    if(!crafterBoard.ActiveItemCounts.ContainsKey(prereq))
                    {
                        craftPossible = false;
                        break;
                    }
                    else if(!GameplaySettings.InfiniteDecks) //do we have enough of each ingredient to craft?
                    {
                        var prereqCounts = itemData.GetPrereqCounts();
                        if(crafterBoard.ActiveItemCounts[prereq] < prereqCounts[prereq])
                        {
                            craftPossible = false;
                            break;
                        }
                    }
                }

                if(craftPossible)
                {
                    possibleCrafts.Add(itemData);
                }
            }

            return possibleCrafts;
        }

        private IEnumerator SpawnHelperItem()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(helperSpawnMinTime, helperSpawnMaxTime));

                SpawnItem(randomProducts[Random.Range(0, randomProducts.Count)],
                    crafterBoard.GetRandomPointOnBoard(padding: 1f) + Vector3.up * 10f,
                    Quaternion.identity);
            }
        }

        public bool WasItemCraftedPreviously(CraftingItemData itemData)
        {
            return craftedTracker.WasItemCraftedPreviously(itemData);
        }

        public List<CraftingItemData> GetUniqueItemsCrafted()
        {
            return craftedTracker.GetUniqueItemsCrafted();
        }

        public void SetBoard(CrafterBoard board)
        {
            crafterBoard = board;
            foreach (var placementPoint in crafterBoard.PlacementPoints)
            {
                placementPoint.ItemPlaced += OnItemPlacedOrRemoved;
                placementPoint.ItemRemoved += OnItemPlacedOrRemoved;
            }
        }

        public void RegisterCraftingItem(CraftingItem item)
        {
            if (activeItems.Contains(item))
            {
                Debug.LogError($"Item {item.name} already registered to the {nameof(CraftingManager)}!");
                return;
            }

            activeItems.Add(item);
        }

        public void DeregisterCraftingItem(CraftingItem item)
        {
            if (!activeItems.Contains(item))
            {
                Debug.LogWarning($"Tried to deregister {item.name} with {nameof(CraftingManager)} but it was not in the registered items list!");
                return;
            }

            activeItems.Remove(item);
        }

        private void OnDrawGizmos()
        {
            itemContactsTracker.OnDrawGizmos();
        }
    }
}