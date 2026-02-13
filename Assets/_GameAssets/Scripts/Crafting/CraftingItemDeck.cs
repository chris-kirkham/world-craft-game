using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingItemDeck : SingletonMonoBehaviour<CraftingItemDeck>, ICursorEventListener
{
    //ADDING/REMOVING ITEMS: should the item be told when it's added/removed and handle toggling physics/input itself?
    //That way would make it easier to do animations/coroutine stuff since it would stop being the deck's responsibility
    //as soon as the item is added or removed, but that may not be necessary

    [SerializeField] private CraftingItemDatabase startingDeck;
    [SerializeField] private float itemHeight = 0.1f;
    [SerializeField] private bool populateOnEnable;

    private LinkedList<CraftingItem> deck = new LinkedList<CraftingItem>();

    private bool isHovered;

    private void Start()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
        else
        {
            Debug.LogError($"No instance of {nameof(Cursor)} found! Cannot register listener.");
        }

        if (populateOnEnable)
        {
            PopulateDeck(startingDeck);
        }
    }

    private void OnDisable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    public CraftingItem RemoveTopItem()
    {
        if(deck.Count == 0)
        {
            return null;
        }

        var item = deck.Last.Value;
        OnItemRemoved(item);
        deck.RemoveLast();
        return item;
    }

    public void AddItemToTopDeck(CraftingItem item)
    {
        OnItemAdded(item);
        deck.AddLast(item);
        MoveItemToDeck(item.transform, transform.position + (deck.Count * itemHeight * Vector3.up));
    }

    public void AddItemToBottomDeck(CraftingItem item)
    {
        var itemUpInc = Vector3.up * itemHeight;
        foreach(var deckItem in deck)
        {
            deckItem.transform.position += itemUpInc;
        }

        OnItemAdded(item);
        deck.AddFirst(item);
        MoveItemToDeck(item.transform, transform.position);
    }

    private void OnItemAdded(CraftingItem item)
    {
        item.transform.parent = transform;
        item.SetState(CraftingItem.State.Animatable);
    }

    private void OnItemRemoved(CraftingItem item)
    {
        item.transform.parent = null;
        item.SetState(CraftingItem.State.Active);
    }

    private void MoveItemToDeck(Transform item, Vector3 targetPos)
    {
        item.transform.position = targetPos;
        item.transform.rotation = transform.rotation;
        //StartCoroutine(AnimateItemToDeckRoutine(item, targetPos, transform.rotation));
    }

    private void PopulateDeck(CraftingItemDatabase deckItems)
    {
        ClearDeck();

        if(deckItems == null)
        {
            return;
        }

        for(int i = 0; i < deckItems.ItemList.Count; i++)
        {
            var itemData = deckItems.ItemList[i];
            var item = CraftingManager.Inst.InstantiateItem(itemData, transform.position);
            item.transform.parent = transform;
            AddItemToTopDeck(item);
        }
    }

    private void ClearDeck()
    {
        foreach(var item in deck)
        {
            GameObject.Destroy(item.gameObject);
        }

        deck.Clear();
    }

    private void GrabTopDeckItem()
    {
        var item = RemoveTopItem();
        if(item)
        {
           item.TryForceDragStart();
        }
    }

    private IEnumerator AnimateItemToDeckRoutine(Transform item, Vector3 targetPos, Quaternion targetRotation)
    {
        //move y position first (so card doesn't phase through others in deck) and rotate to correct orientation
        var targetYPos = new Vector3(item.position.x, targetPos.y, item.position.z);
        var startPos = item.position;
        var startRotation = item.rotation;
        float t;
        const float yMoveTime = 0.25f;
        for(t = 0f; t <= yMoveTime; t = Mathf.Min(yMoveTime, t + Time.deltaTime))
        {
            item.position = Vector3.Lerp(startPos, targetYPos, t);
            item.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        startPos = item.position;
        const float xzMoveTime = 1f;
        for(t = 0f; t <= xzMoveTime; t = Mathf.Min(xzMoveTime, t + Time.deltaTime))
        {
            item.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(e == Cursor.CursorEvent.EnterElement)
        {
            isHovered = true;
        }
        else if(e == Cursor.CursorEvent.ExitElement)
        {
            isHovered = false;
        }
        else if(e == Cursor.CursorEvent.LeftClickDown)
        {
            if(isHovered)
            {
                GrabTopDeckItem();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.1f, 1f));
    }
}
