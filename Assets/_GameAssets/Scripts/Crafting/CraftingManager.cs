using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CraftingManager : SingletonMonoBehaviour<CraftingManager>, ICursorEventListener
{
    public class ItemContactsGroup
    {
        private HashSet<CraftingItem> items;

        public HashSet<CraftingItem> Items => items;
        public int Count => items.Count;

        public ItemContactsGroup(HashSet<CraftingItem> items)
        {
            this.items = items;
        }

        public void Add(CraftingItem item)
        {
            items.Add(item);
        }

        public void Remove(CraftingItem item)
        {
            items.Remove(item);
        }

        public bool Contains(CraftingItem item)
        {
            return items.Contains(item);
        }

        public override bool Equals(object obj)
        {
            var other = (ItemContactsGroup)obj;
            if(other != null)
            {
                return items.SetEquals(other.items);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            //hmm
            int hash = 19;
            foreach(var item in items)
            {
                hash += 31 * item.GetHashCode();
            }

            return hash;
        }
    }

    public enum CraftingResultState
    {
        None,
        PartialIngredientMatch,
        SuccessfulCraft
    }

    [SerializeField] private CraftingItemDatabase itemDatabase;
    [SerializeField] private CraftingItem thumbnailPrefab;
    [SerializeField] private CraftingItemWindow windowPrefab;
    [Space]
    [SerializeField] private float multiItemSpawnDelay = 0.1f;
    [Header("Crafting zones")]
    [SerializeField] private List<CrafterPlacementZone> placementPoints;
    [SerializeField] private Transform craftingResultSpawnPos;
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

    private HashSet<CraftingItem> placedItems = new HashSet<CraftingItem>();

    //Dictionary of {ITEM : CONTACTS} where ITEM is the item which initially reported the contact.
    //Contains duplicates so all contacts can be found using any contacting item's key, e.g.
    //{WATER : [WATER, GROUND, BEACH]}
    //{GROUND : [WATER, GROUND, BEACH]}
    //{BEACH : [WATER, GROUND, BEACH]}
    private Dictionary<CraftingItem, ItemContactsGroup> itemContactsDict = new Dictionary<CraftingItem, ItemContactsGroup>();
    
    //HashSet containing only each unique contact group
    private HashSet<ItemContactsGroup> itemContactGroups = new HashSet<ItemContactsGroup>();

    private bool canCraft;

    private Coroutine spawnHelperItemCoroutine;

    private void OnEnable()
    {
        foreach(var placementPoint in placementPoints)
        {
            placementPoint.ItemPlaced += OnItemPlaced;
        }
    }

    private void Start()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }

        itemLayerMask = LayerMask.GetMask("Item");
    }

    private void OnDisable()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }

        foreach (var placementPoint in placementPoints)
        {
            placementPoint.ItemPlaced -= OnItemPlaced;
        }
    }

    private void LateUpdate()
    {
        if (debugDisplay)
        {
            debugDisplay.SetCraftingDebugInfo(itemContactGroups);
        }

        UpdateTrimmedItemGroups();

        if(CheckForPossibleCrafts())
        {
            if (spawnHelperItemCoroutine != null)
            {
                StopCoroutine(spawnHelperItemCoroutine);
            } 
        }
        else //no crafts possible - start spawning helper items
        {
            if (spawnHelperItemCoroutine == null)
            {
                spawnHelperItemCoroutine = StartCoroutine(SpawnHelperItem());
            }
        }
    }

    private void OnItemPlaced(CraftingItem item)
    {
        placedItems.Add(item);
        if(placedItems.Count > 1)
        {
            TryCraft(placedItems);
        }
    }

    private void UpdateTrimmedItemGroups()
    {
        itemContactGroups.Clear();
        foreach(var keyItem in itemContactsDict.Keys)
        {
            var group = new HashSet<CraftingItem>() { keyItem };
            ExpandItemGroup(group, itemContactsDict[keyItem].Items);
            itemContactGroups.Add(new ItemContactsGroup(group));
        }
    
        void ExpandItemGroup(HashSet<CraftingItem> existingGroup, HashSet<CraftingItem> newGroup)
        {
            if(newGroup != existingGroup)
            {
                var newItems = new HashSet<CraftingItem>(newGroup);
                newItems.ExceptWith(existingGroup);
                existingGroup.UnionWith(newGroup);
                foreach (var item in newItems)
                {
                    if(itemContactsDict.TryGetValue(item, out var newItemContacts))
                    {
                        ExpandItemGroup(existingGroup, newItemContacts.Items);
                    }
                }
            }
        }
    }

    public void AddItemContact(CraftingItem item, CraftingItem contactingItem)
    {
        if(!item || !contactingItem)
        {
            return;
        }

        if(itemContactsDict.TryGetValue(item, out var touchingItems))
        {
            if(!touchingItems.Contains(contactingItem))
            {
                touchingItems.Add(contactingItem);
            }
        }
        else
        {
            itemContactsDict.Add(item, new ItemContactsGroup(new HashSet<CraftingItem> { item, contactingItem })); //item is always touching itself
        }

        //UpdateTrimmedItemGroups();
    }

    public void RemoveItemContact(CraftingItem item, CraftingItem contactingItem)
    {
        if(!item || !contactingItem)
        {
            return;
        }

        //remove other item from this item's contacts
        if(itemContactsDict.TryGetValue(item, out var touchingThisItem) && touchingThisItem.Contains(contactingItem))
        {
            touchingThisItem.Remove(contactingItem);
         
            if(touchingThisItem.Count < 2) //delete if <2 since we always add the item itself to the touching items (TODO: this is jank)
            {
                itemContactsDict.Remove(item);
            }
        }

        //remove this item from other item's contacts
        if(itemContactsDict.TryGetValue(contactingItem, out var touchingOtherItem) && touchingOtherItem.Contains(item))
        {
            touchingOtherItem.Remove(contactingItem);

            if (touchingOtherItem.Count < 2) 
            {
                itemContactsDict.Remove(contactingItem);
            }
        }

        //UpdateTrimmedItemGroups();
    }

    public void RemoveAllItemContactsForItem(CraftingItem item)
    {
        if(!item)
        {
            return;
        }

        if(itemContactsDict.TryGetValue(item, out var itemContacts))
        {
            itemContactsDict.Remove(item);
            foreach(var contactingItem in itemContacts.Items)
            {
                if(itemContactsDict.TryGetValue(contactingItem, out var otherItemContacts))
                {
                    otherItemContacts.Remove(item);
                }
            }
        }

        //UpdateTrimmedItemGroups();
    }

    private int TryCraft(HashSet<CraftingItem> ingredients)
    {
        var numResults = GetCraftResultsAllItems(ingredients, craftResults, craftResultStates);

        if(numResults == 0)
        {
            return numResults;
        }

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

    private int GetCraftResultsAllItems(HashSet<CraftingItem> ingredients, CraftingItemData[] results, CraftingResultState[] resultStates)
    {
        if (ingredients.Count < 2)
        {
            Debug.LogError($"Crafting attempted with <2 ingredients! This shouldn't happen");
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

    private int GetNumMatchingIngredientsToItemPrereqs(CraftingItemData item, HashSet<CraftingItem> ingredients)
    {
        unusedIngredients.Clear();
        unusedIngredients.AddRange(ingredients);

        int numMatching = 0;
        foreach (var prereqData in item.Prerequisites)
        {
            for (int i = unusedIngredients.Count - 1; i >= 0; i--)
            {
                //TODO: probably ingredients which are flagged as not to be used in crafts shouldn't even make it to this stage; refactor?
                if (!unusedIngredients[i].CanCraft) 
                {
                    continue;
                }
                
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
    private IEnumerator DoSuccessfulCrafts(HashSet<CraftingItem> ingredients, List<CraftingItemData> successfulCrafts)
    {
        if(ingredients.Count == 0 || successfulCrafts.Count == 0)
        {
            yield break;
        }

        //turn ingredient physics/collision/input off
        foreach (var ingredient in ingredients)
        {
            ingredient.SetState(CraftingItem.State.Animatable);
        }

        var ingredientsList = new List<CraftingItem>(ingredients);
        var numIngredients = ingredients.Count;

        var centrePos = Vector3.zero;
        if(craftingResultSpawnPos)
        {
            centrePos = craftingResultSpawnPos.position;
        }
        else
        {
            Debug.LogError($"No crafting result spawn position Transform set! Spawning at (0,0,0).");
        }

        var angleInc = 360f / ingredients.Count;
        var upInc = Vector3.up * 0.1f; //add a small vertical increment to each card to avoid z-fighting (and to look nice)

        //lerp ingredients to outer positions
        const float radius = 1f;
        var startPositions = new Vector3[numIngredients];
        var targetPositions = new Vector3[numIngredients];
        for (int i = 0; i < numIngredients; i++)
        {
            startPositions[i] = ingredientsList[i].transform.position + (upInc * i); 
            targetPositions[i] = centrePos + (upInc * i) + (Quaternion.Euler(0f, angleInc * i, 0f) * Vector3.left * radius);
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
                ingredientsList[i].transform.position = Vector3.Slerp(targetPositions[i], centrePos + (upInc * i), t);
            }

            yield return null;
        }
        while (t < 1f);

        //Spawn new items - angle stuff is jank lmao
        var newItemStartAngle = Random.Range(0f, 360f);
        var newItemOffset = new Vector3(0f, 0.5f, 1f); //forward and up
        var numToSpawn = successfulCrafts.Count;
        foreach(var result in successfulCrafts)
        {
            numToSpawn += result.ExtraProducts.Count;
        }
        var craftInstantiateAngleInc = 360f / numToSpawn;
        int spawnCount = 0;

        for(int i = 0; i < successfulCrafts.Count; i++)
        {
            Debug.Log($"Successfully crafted {successfulCrafts[i].ItemName} from ingredients " + string.Join(", ", ingredients) + "!");

            //Instantiate new item
            var result = successfulCrafts[i];
            var posOffset = Quaternion.Euler(0f, (angleInc * spawnCount) + newItemStartAngle, 0f) * newItemOffset;
            SpawnItem(result, centrePos + posOffset);
            spawnCount++;

            //TODO: "new WaitForSeconds" allocates - make a helper class to get WaitForSecondses without allocation
            yield return new WaitForSeconds(multiItemSpawnDelay); 

            //Instantiate any extra products
            if (result.ExtraProducts.Count > 0)
            {
                foreach (var product in result.ExtraProducts)
                {
                    //spawn item
                    posOffset = Quaternion.Euler(0f, (angleInc * spawnCount) + newItemStartAngle, 0f) * newItemOffset;
                    var item = InstantiateItem(product, centrePos + posOffset);
                    //CraftingItemDeck.Inst?.AddItemToTopDeck(item);
                    spawnCount++;
                    Debug.Log($"Spawned extra product {product.ItemName} from craft result {result.ItemName}!");

                    yield return new WaitForSeconds(multiItemSpawnDelay);
                }
            }
        }

        foreach (var ingredient in ingredientsList)
        {
            ingredient.OnCraftAttempt(CraftingResultState.SuccessfulCraft);
        }
    }

    private void SpawnItem(CraftingItemData itemData, Vector3 targetSpawnPos)
    {
        InstantiateItem(itemData, targetSpawnPos);
    }

    private Vector3 GetFreeSpawnPosition(Vector3 targetPos)
    {
        var aabbExtents = new Vector3(1f, 0.1f, 1.5f); //TODO: get extents from item collider? 
        var aabbExtentsMag = aabbExtents.magnitude;

        Debug.DrawRay(targetPos + RaycastStartUpOffset, Vector3.down * MaxRaycastDist, Color.yellow, 10f);

        const int maxTries = 16;
        var tries = 0;
        var spawnPos = targetPos;
        var isSpawnObstructed = false;
        do
        {
            var startPos = spawnPos + RaycastStartUpOffset;
            isSpawnObstructed = Physics.BoxCast(startPos, aabbExtents / 2f, Vector3.down, Quaternion.identity, MaxRaycastDist, itemLayerMask);

            if(tries > 0)
            {
                Debug.DrawRay(startPos, Vector3.down * MaxRaycastDist, isSpawnObstructed ? Color.red : Color.green, 10f);
            }

            if (!isSpawnObstructed)
            {
                break;
            }

            tries++;
            const float maxSearchRadiusMult = 4f;
            var tryPct = tries / (float)maxTries;
            var dist = aabbExtentsMag * maxSearchRadiusMult * tryPct;
            var offset = Quaternion.Euler(0f, 720f * tryPct, 0f) * Vector3.forward * dist;
            spawnPos = targetPos + offset;
        }
        while(isSpawnObstructed && tries < maxTries);

        return spawnPos;
    }

    private void TryCraftAllItemContacts()
    {
        if(!canCraft)
        {
            return;
        }

        var anySuccessfulCraft = false;
        foreach (var contactGroup in itemContactGroups)
        {
            if(contactGroup.Count < 2)
            {
                continue;
            }

            var numResults = TryCraft(contactGroup.Items);
            for(int i = 0; i < numResults; i++)
            {
                if (craftResultStates[i] == CraftingResultState.SuccessfulCraft)
                {
                    anySuccessfulCraft = true;
                    break;
                }
            }
        }

        if(anySuccessfulCraft)
        {
            canCraft = false;
        }
    }

    /// <summary>Instantiate a crafting item directly</summary>
    /// <param name="wasCrafted">if true, call the item's on-crafted callback</param>
    public CraftingItem InstantiateItem(CraftingItemData itemData, Vector3 position, bool wasCrafted = true)
    {
        return InstantiateItem(itemData, position, Quaternion.identity);
    }

    /// <summary>Instantiate a crafting item directly</summary>
    /// <param name="wasCrafted">if true, call the item's on-crafted callback</param>
    public CraftingItem InstantiateItem(CraftingItemData itemData, Vector3 position, Quaternion rotation, bool wasCrafted = true)
    {
        var item = Instantiate<CraftingItem>(thumbnailPrefab, position, rotation);
        item.Data = itemData;

        if(wasCrafted)
        {
            item.OnCrafted();
        }

        return item;
    }

    public void OnItemDisabledOrDestroyed(CraftingItem item)
    {
        foreach(var contacts in itemContactsDict.Values)
        {
            if (contacts.Contains(item))
            {
                contacts.Remove(item);
            }
        }

        if(itemContactsDict.ContainsKey(item))
        {
            itemContactsDict.Remove(item);
        }

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

            InstantiateItem(randomProducts[Random.Range(0, randomProducts.Count)],
                CrafterBoard.Inst.GetRandomPointOnBoard(padding: 1f) + Vector3.up * 10f);
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

    public void AddPlacementPoint(CrafterPlacementZone placementPoint)
    {
        placementPoints.Add(placementPoint);
        placementPoint.ItemPlaced += OnItemPlaced;
    }

    public void RemovePlacementPoint(CrafterPlacementZone placementPoint)
    {
        placementPoints.Remove(placementPoint);
        placementPoint.ItemPlaced -= OnItemPlaced;
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        //TODO: prototype hack
        if(e == Cursor.CursorEvent.LeftClickUp)
        {
            canCraft = true;
        }
    }

    private void OnDrawGizmos()
    {
        var contactPositions = new List<Vector3>();
        if(itemContactsDict.Count > 0)
        {
            Gizmos.matrix = Matrix4x4.identity;
            var hue = 0f;
            var inc = 1f / itemContactsDict.Count;
            foreach (var contactGroup in itemContactGroups)
            {
                contactPositions.Clear();
                Gizmos.color = Color.HSVToRGB(hue, 1f, 1f);
                foreach (var contact in contactGroup.Items)
                {
                    Gizmos.DrawSphere(contact.transform.position, 0.1f);
                    contactPositions.Add(contact.transform.position);
                }

                for(int i = 0; i < contactPositions.Count; i++)
                {
                    for(int j = 0; j < contactPositions.Count; j++)
                    {
                        if(i != j)
                        {
                            Gizmos.DrawLine(contactPositions[i], contactPositions[j]);
                        }
                    }
                }

                hue += inc;
            }
        }
    }
}
