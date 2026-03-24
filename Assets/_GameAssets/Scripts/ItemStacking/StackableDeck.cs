using System.Collections.Generic;
using UnityEngine;

public class StackableDeck<T> : MonoBehaviour, ICursorEventListener, IDraggable where T : IStackable
{
    [SerializeField] private Vector3 stackPosOffset;

    private LinkedList<T> deck;

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
    }

    private void OnDisable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    private void Update()
    {
        //if player has started dragging the top item, remove it from the deck
        //TODO: this is jank
        CheckForTopItemGrabbed();
    }

    private void CheckForTopItemGrabbed()
    {
        if (deck.Count == 0)
        {
            return;
        }

        var cursor = Cursor.Inst;
        if (!cursor || !cursor.CurrentDragTarget)
        {
            return;
        }

        if (cursor.CurrentDragTarget.gameObject == deck.Last.Value.gameObject)
        {
            RemoveFromTop();
        }
    }

    public void AddToTopDeck(T obj)
    {
        OnItemAdded(obj);
        deck.AddLast(obj);
        var targetPos = transform.position + (stackPosOffset * deck.Count);
        MoveItemToDeck(obj.transform, targetPos);
    }

    public void AddItemToBottomDeck(T obj)
    {
        foreach (var deckItem in deck)
        {
            deckItem.transform.position += stackPosOffset;
        }

        OnItemAdded(obj);
        deck.AddFirst(obj);
        MoveItemToDeck(obj.transform, transform.position);
    }

    private void OnItemAdded(T obj)
    {
        obj.transform.parent = transform;
        obj.OnAddedToStack();
    }

    private void OnItemRemoved(T obj)
    {
        obj.transform.parent = null;
        obj.OnRemovedFromStack();
    }

    private void MoveItemToDeck(Transform item, Vector3 targetPos)
    {
        item.transform.position = targetPos;
        item.transform.rotation = transform.rotation;
        //StartCoroutine(AnimateItemToDeckRoutine(item, targetPos, transform.rotation));
    }

    private void ClearDeck()
    {
        foreach (var item in deck)
        {
            GameObject.Destroy(item.gameObject);
        }

        deck.Clear();
    }

    private void GrabTopDeckItem()
    {
        var item = deck.Last.Value;
        if (item.gameObject)
        {
            throw new System.NotImplementedException();
            //item.TryStartDrag();
        }
    }

    public void Remove(T obj)
    {
        if (deck.Count == 0)
        {
            return;
        }

        var node = deck.Find(obj);
        if (node != null)
        {
            deck.Remove(node);
            OnItemRemoved(obj);
        }
    }

    private void RemoveFromTop()
    {
        if (deck.Count == 0)
        {
            return;
        }

        var item = deck.Last.Value;
        OnItemRemoved(item);
        deck.RemoveLast();
    }

    public void TryStartDrag()
    {
        throw new System.NotImplementedException();
    }

    public void OnCursorEvent(Cursor.EventID e)
    {
        if (e == Cursor.EventID.LeftClickDown)
        {
            if (Cursor.Inst.IsHovered(this))
            {
                GrabTopDeckItem();
            }
        }

        if (e == Cursor.EventID.LeftClickUp)
        {
            //drop hovered item onto deck
            var cursor = Cursor.Inst;
            if (cursor.IsHovered(this))
            {
                var dragTarget = cursor.CurrentDragTarget;
                if(dragTarget && dragTarget is IStackable)
                {
                    cursor.CurrentDragTarget.CancelDrag();
                    //AddToTopDeck(dragTarget);
                }
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
