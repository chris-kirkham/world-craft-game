using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

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
    [Header("Crafting VFX/animations")]
    [SerializeField] private PlayableDirector duringCraftFX;
    [SerializeField] private bool inspectItemOnFirstCraft = true;
    [SerializeField] private float onSpawnInspectTime = 0.1f;
    [SerializeField] private float onSpawnMoveToEndPosTime = 1f;
    [SerializeField] private float multiItemSpawnDelay = 0.1f;
    [Header("Crafting zones")]
    [SerializeField] private CrafterBoard crafterBoard;
    [SerializeField] private List<CraftingItemData> randomProducts;
    [SerializeField] private bool SpawnHelperIngredientsIfNoCraftPossible = true;
    [SerializeField] private float helperSpawnMinTime = 1f;
    [SerializeField] private float helperSpawnMaxTime = 10f;
    [Header("Debug")]
    [SerializeField] private DebugDisplay debugDisplay;

    //TODO: move this to a more generic ItemManager if keeping functionality
    private HashSet<CraftingItem> activeItems = new HashSet<CraftingItem>(); //list of all active crafting items currently on the board
    public HashSet<CraftingItem> ActiveItems => activeItems;

    //cached lists of stuff
    private List<CraftingItem> unusedIngredients = new List<CraftingItem>(); //unused ingredients during each craft attempt
    private const int MaxResultsPerCraft = 10; //increase this if we need more
    private CraftingItemData[] craftResults = new CraftingItemData[MaxResultsPerCraft];
    private CraftingResultState[] craftResultStates = new CraftingResultState[MaxResultsPerCraft];

    //item spawn raycasts
    private LayerMask itemLayerMask;
    private const float MaxRaycastDist = 20f;
    private readonly Vector3 RaycastStartUpOffset = Vector3.up * 10f;
    
    private bool canCraft;

    public CraftingEventTracker CraftedTracker { get; } = new CraftingEventTracker();
    private ItemContactsTracker itemContactsTracker = new ItemContactsTracker();
    private Coroutine spawnHelperItemCoroutine;

    private void Start()
    {
        itemLayerMask = LayerMask.GetMask("Item");
    }

    private void OnDisable()
    {
        if(crafterBoard)
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

        if(CheckForPossibleCrafts())
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

            if(gameOverSequence)
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
        var placedItems = new List<CraftingItem>();
        foreach(var placementPoint in crafterBoard.PlacementPoints)
        {
            if(placementPoint.CurrentItem)
            {
                placedItems.Add(placementPoint.CurrentItem);
            }
        }

        TryCraft(placedItems);
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
        var successfulCrafts = new List<CraftingItemData>();
        for(int i = 0; i < numResults; i++)
        {
            if (craftResultStates[i] == CraftingResultState.SuccessfulCraft)
            {
                successfulCrafts.Add(craftResults[i]);
                anySuccessfulCraft = true;
            }
        }

        if(anySuccessfulCraft)
        {
            StartCoroutine(DoSuccessfulCrafts(ingredients, successfulCrafts));
        }
        else
        {
            foreach(var ingredient in ingredients)
            {
                //TODO FOR PARTIAL CRAFT RESULT: TELL CRAFTING ITEM IF IT IS PART OF THE CRAFT OR NOT
                ingredient.OnCraftAttempt(CraftingResultState.PartialIngredientMatch);
            }
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
        foreach (var item in itemDatabase.ItemList)
        {
            //exit if item isn't craftable by two or more other items
            if (item.Prerequisites.Count < 2)
            {
                continue;
            }

            currentResultState = CraftingResultState.None;
            currentResult = item;

            //check number of matching ingredients we have to this item
            int numMatching = GetNumMatchingIngredientsToItemPrereqs(item, ingredients);

            if (numMatching == item.Prerequisites.Count)
            {
                //if we have -more- ingredients than prerequisites, count it as a partial craft
                //(so we can display the partial-craft VFX and player might guess they need to remove an item)
                //TODO: think about this! Should this case count as a partial craft or not? It would confuse players
                //if they think a partial craft always has -fewer- ingredients than required
                if (ingredients.Count > item.Prerequisites.Count)
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

                if(numResults >= MaxResultsPerCraft)
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

    //Placeholder sequence! TODO: refactor and optimise
    private IEnumerator DoSuccessfulCrafts(ICollection<CraftingItem> ingredients, List<CraftingItemData> successfulCrafts)
    {
        if(ingredients.Count == 0 || successfulCrafts.Count == 0)
        {
            yield break;
        }

        foreach (var ingredient in ingredients)
        {
            ingredient.SetState(CraftingItem.State.Animatable);
        }

        var ingredientsList = new List<CraftingItem>(ingredients);
        var numIngredients = ingredients.Count;

        var fusionPos = crafterBoard ? crafterBoard.CraftIngredientFusionPos : Vector3.zero;

        var angleInc = 360f / ingredients.Count;
        var upInc = Vector3.up * 0.1f; //add a small vertical increment to each card to avoid z-fighting (and to look nice)

        //lerp ingredients to outer positions
        const float radius = 1f;
        var startPositions = new Vector3[numIngredients];
        var targetPositions = new Vector3[numIngredients];
        for (int i = 0; i < numIngredients; i++)
        {
            startPositions[i] = ingredientsList[i].transform.position + (upInc * i); 
            targetPositions[i] = fusionPos + (upInc * i) + (Quaternion.Euler(0f, angleInc * i, 0f) * Vector3.left * radius);
        }

        const float lerpToOuterTime = 0.75f;
        var t = 0f;
        do
        {
            t = Mathf.Clamp01(t + (Time.deltaTime / lerpToOuterTime));
            for(int i = 0; i < numIngredients; i++)
            {
                ingredientsList[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            yield return null;
        }
        while (t < 1f);

        //make ingredients converge in the centre!
        const float convergeInCentreTime = 0.5f;
        t = 0f;
        do
        {
            t = Mathf.Clamp01(t + (Time.deltaTime / convergeInCentreTime));
            for(int i = 0; i < numIngredients; i++)
            {
                ingredientsList[i].transform.position = Vector3.Slerp(targetPositions[i], fusionPos + (upInc * i), t);
            }

            yield return null;
        }
        while (t < 1f);

        if(duringCraftFX)
        {
            duringCraftFX.gameObject.transform.position = fusionPos;
            duringCraftFX.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
        }

        //Spawn new items - angle stuff is jank lmao
        var newItemStartAngle = Random.Range(0f, 360f);
        var newItemOffset = new Vector3(0f, 0.5f, 0.5f); //forward and up
        var numToSpawn = successfulCrafts.Count;
        foreach(var result in successfulCrafts)
        {
            numToSpawn += result.ExtraProducts.Count;
        }
        var craftInstantiateAngleInc = 360f / numToSpawn;
        int spawnCount = 0;

        yield return new WaitForSeconds(multiItemSpawnDelay);

        foreach (var ingredient in ingredientsList)
        {
            ingredient.OnCraftAttempt(CraftingResultState.SuccessfulCraft);
        }

        var spawnOrigin = crafterBoard ? crafterBoard.CraftResultSpawnPos : Vector3.zero;
        for(int i = 0; i < successfulCrafts.Count; i++)
        {
            Debug.Log($"Successfully crafted {successfulCrafts[i].ItemName} from ingredients " + string.Join(", ", ingredients) + "!");

            //Instantiate new item
            var result = successfulCrafts[i];
            var posOffset = Vector3.right * 1.25f;

            //yield return SpawnItemAnimated(result, fusionPos, spawnOrigin + (posOffset * spawnCount));
            yield return SpawnItemToGrid(result, fusionPos, Quaternion.identity);
            spawnCount++;

            //TODO: "new WaitForSeconds" allocates - make a helper class to get WaitForSecondses without allocation
            yield return new WaitForSeconds(multiItemSpawnDelay); 

            //Instantiate any extra products
            if (result.ExtraProducts.Count > 0)
            {
                foreach (var extraProduct in result.ExtraProducts)
                {
                    //spawn item
                    //yield return SpawnItemAnimated(extraProduct, fusionPos, spawnOrigin + (posOffset * spawnCount));
                    yield return SpawnItemToGrid(extraProduct, fusionPos, Quaternion.identity);
                    spawnCount++;
                    Debug.Log($"Spawned extra product {extraProduct.ItemName} from craft result {result.ItemName}!");
                    yield return new WaitForSeconds(multiItemSpawnDelay);
                }
            }
        }

        if(duringCraftFX)
        {
            duringCraftFX.gameObject.SetActive(false);
        }
    }

    private IEnumerator SpawnItemAnimated(
        CraftingItemData itemData,
        Vector3 startPos_WS, 
        Vector3 endPos_WS,
        bool wasCrafted = true)
    {
        var wasCraftedPreviously = CraftedTracker.WasItemCraftedPreviously(itemData);
        var item = SpawnItem(itemData, startPos_WS, Quaternion.identity, wasCrafted);
        item.SetState(CraftingItem.State.Animatable);

        if (wasCrafted && inspectItemOnFirstCraft && !wasCraftedPreviously)
        {
            var cam = Cursor.Inst.Cam;
            yield return item.AnimateToRoutine(cam.transform.position + (cam.transform.forward * 2f), Quaternion.identity, onSpawnMoveToEndPosTime, false);
            item.SetOnInspectVFX(true);
            yield return new WaitForSeconds(onSpawnInspectTime);
            item.SetOnInspectVFX(false);
        }

        yield return item.AnimateToRoutine(endPos_WS, Quaternion.identity, onSpawnMoveToEndPosTime, false);
        item.SetState(CraftingItem.State.Active);
    }

    private IEnumerator SpawnItemToGrid(CraftingItemData itemData, Vector3 startPos_WS, Quaternion startRotation_WS, bool wasCrafted = true)
    {
        var wasCraftedPreviously = CraftedTracker.WasItemCraftedPreviously(itemData);
        var item = SpawnItem(itemData, startPos_WS, startRotation_WS, wasCrafted);
        item.SetState(CraftingItem.State.Animatable);
        if (wasCrafted && inspectItemOnFirstCraft && !wasCraftedPreviously)
        {
            var cam = Cursor.Inst.Cam;
            yield return item.AnimateToRoutine(cam.transform.position + (cam.transform.forward * 2f), Quaternion.identity, onSpawnMoveToEndPosTime, false);
            item.SetOnInspectVFX(true);
            yield return new WaitForSeconds(onSpawnInspectTime);
            item.SetOnInspectVFX(false);
        }

        if (crafterBoard)
        {
            crafterBoard.MoveItemToGrid(item);
        }
    }

    /// <summary>Instantiate a crafting item directly.</summary>
    /// <param name="wasCrafted">if true, call the item's on-crafted callback</param>
    public CraftingItem SpawnItem(CraftingItemData itemData, Vector3 position, Quaternion rotation, bool wasCrafted = true)
    {
        var item = Instantiate<CraftingItem>(thumbnailPrefab, position, rotation);
        item.Data = itemData;

        if(wasCrafted)
        {
            CraftedTracker.OnItemCrafted(itemData);
            item.OnCrafted();
        }

        return item;
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
        foreach(var item in itemDatabase.ItemList)
        {
            var numMatching = GetNumMatchingIngredientsToItemPrereqs(item, activeItems);
            if(numMatching >= item.Prerequisites.Count)
            {
                anyPossibleCraft = true;
                break;
            }
        }

        return anyPossibleCraft;
    }

    private IEnumerator SpawnHelperItem()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(helperSpawnMinTime, helperSpawnMaxTime));

            SpawnItem(randomProducts[Random.Range(0, randomProducts.Count)],
                crafterBoard.GetRandomPointOnBoard(padding: 1f) + Vector3.up * 10f,
                Quaternion.identity);
        }
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
        if(activeItems.Contains(item))
        {
            Debug.LogError($"Item {item.name} already registered to the {nameof(CraftingManager)}!");
            return;
        }

        activeItems.Add(item);
    }

    public void DeregisterCraftingItem(CraftingItem item)
    {
        if(!activeItems.Contains(item))
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
