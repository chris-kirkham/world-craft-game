using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingManager : SingletonMonoBehaviour<CraftingManager>, ICursorEventListener
{
    public class ItemContactsGroup
    {
        public HashSet<CraftingItem> items;

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
        NoIngredientMatch,
        PartialIngredientMatch,
        SuccessfulCraft
    }

    [SerializeField] private CraftingItemDatabase itemDatabase;
    [SerializeField] private CraftingItem thumbnailPrefab;
    [SerializeField] private CraftingItemWindow windowPrefab;
    [SerializeField] private CraftingDebugDisplay debugDisplay;
    [SerializeField] private List<CrafterPlacementZone> placementZones;
    [Space]
    [SerializeField] private List<CraftingItemData> randomProducts;
    [Space]
    [SerializeField] private bool SpawnHelperIngredientsIfNoCraftPossible = true;
    [SerializeField] private float helperSpawnMinTime = 1f;
    [SerializeField] private float helperSpawnMaxTime = 10f;

    private List<CraftingItemData> currentIngredients = new List<CraftingItemData>();

    private HashSet<CraftingItem> activeItems = new HashSet<CraftingItem>(); //list of all active crafting items currently on the board

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
        foreach(var placementZone in placementZones)
        {
            placementZone.ItemPlaced += OnItemPlaced;
        }
    }

    private void Start()
    {
        Cursor.Inst.AddCursorEventListener(this);
    }

    private void OnDisable()
    {
        foreach (var placementZone in placementZones)
        {
            placementZone.ItemPlaced -= OnItemPlaced;
        }

        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    private void LateUpdate()
    {
        //update trimmed item contacts
        //TODO: optimise
        itemContactGroups.Clear();
        foreach(var contacts in itemContactsDict.Values)
        {
            if(!itemContactGroups.Contains(contacts))
            {
                itemContactGroups.Add(contacts);
            }
        }

        TryCraftAllItemContacts();

        if(debugDisplay)
        {
            debugDisplay.SetDebugInfo(itemContactGroups);
        }

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

    private void OnItemPlaced(CraftingItemData itemData)
    {
        Debug.Log($"Item {itemData.name} placed in crafter!");
        currentIngredients.Add(itemData);
    }

    public void AddItemContact(CraftingItem item, CraftingItem contactingItem)
    {
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
    }

    public void RemoveItemContact(CraftingItem item, CraftingItem contactingItem)
    {
        if(!item)
        {
            return;
        }

        if(itemContactsDict.TryGetValue(item, out var touchingItems))
        {
            if(touchingItems.Contains(contactingItem))
            {
                touchingItems.Remove(contactingItem);
         
                //if(touchingItems.Count == 0)
                if(touchingItems.items.Count < 2) //delete if <2 since we always add the item itself to the touching items (TODO: this is jank)
                {
                    itemContactsDict.Remove(item);
                }
            }
            else
            {
                Debug.LogError($"No item {contactingItem.name} found in contacts list for item {item.name}!");
            }
        }
        else
        {
            Debug.LogError($"No key for item {item.name} found in contacts dictionary!");
        }
    }

    private CraftingResultState TryCraft(HashSet<CraftingItem> ingredients)
    {
        var resultState = GetCraftResultAllItems(ingredients, out var craftResult);
        if(resultState == CraftingResultState.SuccessfulCraft)
        {
            Debug.Log($"Successfully crafted {craftResult.ItemName} from ingredients " + string.Join(", ", ingredients) + "!");

            StartCoroutine(DoSuccessfulCraft(ingredients, craftResult));

            /* TODO: figure out if using placement zones 
            foreach (var placementZone in placementZones)
            {
                placementZone.RemoveItem();
            }
            */
        }
        else
        {
            foreach (var ingredient in ingredients)
            {
                //TODO FOR PARTIAL CRAFT RESULT: TELL CRAFTING ITEM IF IT IS PART OF THE CRAFT OR NOT
                ingredient.OnCraftAttempt(resultState);
            }
        }

        return resultState;
    }

    //TODO: PROTOTYPE, optimise!!!!
    public CraftingResultState GetCraftResultAllItems(HashSet<CraftingItem> ingredients, out CraftingItemData result)
    {
        result = null;
        var resultState = CraftingResultState.NoIngredientMatch;

        if (ingredients.Count < 2)
        {
            Debug.LogError($"Crafting attempted with <2 ingredients! This shouldn't happen");
            return CraftingResultState.NoIngredientMatch;
        }

        foreach (var item in itemDatabase.ItemList)
        {
            //exit if item isn't craftable by two or more other items
            if (item.Prerequisites.Count < 2)
            {
                continue;
            }

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
                    resultState = CraftingResultState.PartialIngredientMatch;
                }
                else //successful craft
                {
                    result = item;
                    return CraftingResultState.SuccessfulCraft;
                }
            }

            if (numMatching > 1)
            {
                resultState = CraftingResultState.PartialIngredientMatch;
            }
        }

        return resultState;
    }

    private int GetNumMatchingIngredientsToItemPrereqs(CraftingItemData item, HashSet<CraftingItem> ingredients)
    {
        int numMatching = 0;
        var unusedIngredients = new List<CraftingItem>(ingredients); //TODO: ALLOCATION
        foreach (var prereqData in item.Prerequisites)
        {
            for (int i = unusedIngredients.Count - 1; i >= 0; i--)
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

        return numMatching;
    }

    //TODO: placeholder animation
    private IEnumerator DoSuccessfulCraft(HashSet<CraftingItem> ingredients, CraftingItemData result)
    {
        var ingredientsList = new List<CraftingItem>(ingredients);

        var numIngredients = ingredients.Count;

        /*
        var centrePos = Vector3.zero;
        foreach (var ingredient in ingredients)
        {
            centrePos += ingredient.transform.position;
        }
        centrePos /= ingredients.Count;
        centrePos += Vector3.up * 0.1f;
        */

        var centrePos = Cursor.Inst.Cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 5f));
        const float startAngle = -90f; //starting angle offset so the first card is on the left rather than top
        var angleInc = 360f / ingredients.Count;
        var upInc = Vector3.up * 0.1f; //add a small vertical increment to each card to avoid z-fighting (and to look nice)

        //disable collision + make kinematic
        foreach (var ingredient in ingredients)
        {
            ingredient.TogglePhysics(true);
        }

        //lerp ingredients to spin positions
        const float radius = 1f;
        var startPositions = new Vector3[numIngredients];
        var targetPositions = new Vector3[numIngredients];
        for (int i = 0; i < numIngredients; i++)
        {
            startPositions[i] = ingredientsList[i].transform.position + (upInc * i); 
            targetPositions[i] = centrePos + (upInc * i) + (Quaternion.Euler(0f, (angleInc * i) + startAngle, 0f) * Vector3.forward * radius);
        }

        const float lerpToOuterTime = 1f;
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

        //spin ingredients in a circle!
        /*
        const float spinTime = 1f;
        const float numSpins = 2f;
        const float totalRotation = 360f * numSpins;
        var angle = 0f;
        t = 0f;
        do
        {
            t = Mathf.Clamp01(t + (Time.deltaTime / spinTime));
            for(int i = 0; i < numIngredients; i++)
            {
                angle = (angleInc * i) + (t * totalRotation);
                ingredientsList[i].transform.position = centrePos + (Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius);
            }

            yield return null;
        }
        while (t < 1f);
        */

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

        //Instantiate new item
        var newItemUpOffset = Vector3.up * 2f;
        var newItem = InstantiateItem(result, centrePos);

        //Instantiate any extra crafting products
        if(result.ExtraProducts.Count > 0)
        {
            foreach (var product in result.ExtraProducts)
            {
                var randomOffset = Random.onUnitSphere * 4f;
                InstantiateItem(product, centrePos + randomOffset + newItemUpOffset);
            }
        }
        else //TEST/PROTOTYPE: if no extra products defined, instantiate a random one from the list
        {
            var product = randomProducts[Random.Range(0, randomProducts.Count)];
            InstantiateItem(product, centrePos + (Random.onUnitSphere * 4f) + newItemUpOffset);
        }

        //animate new item
        const float animNewItemUpTime = 0.1f;
        const float moveSpeed = 0.1f;
        t = 0f;
        newItem.TogglePhysics(true);
        
        do
        {
            t = Mathf.Clamp01(t + (Time.deltaTime / animNewItemUpTime));
            newItem.transform.position += Vector3.up * moveSpeed * t;
            yield return null;
        }
        while (t < 1f);

        newItem.TogglePhysics(false);

        foreach (var ingredient in ingredientsList)
        {
            ingredient.OnCraftAttempt(CraftingResultState.SuccessfulCraft);
        }
    }

    //TODO: need to sort crafting flow out - really think about it!
    private void TryCraftAllItemContacts()
    {
        if(!canCraft)
        {
            return;
        }

        var anySuccessfulCraft = false;
        foreach (var ingredients in itemContactGroups)
        {
            if(ingredients.items.Count < 2)
            {
                continue;
            }

            var resultState = TryCraft(ingredients.items);
            if (resultState == CraftingResultState.SuccessfulCraft)
            {
                anySuccessfulCraft = true;
            }
        }

        if(anySuccessfulCraft)
        {
            canCraft = false;
        }
    }

    private CraftingItem InstantiateItem(CraftingItemData itemData, Vector3 position)
    {
        var item = Instantiate<CraftingItem>(thumbnailPrefab, position, Quaternion.identity);
        item.Data = itemData;
        item.OnCrafted();

        /*
        var window = Instantiate<CraftingItemWindow>(windowPrefab);
        window.SetItem(itemData);
        */

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
                foreach (var contact in contactGroup.items)
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
