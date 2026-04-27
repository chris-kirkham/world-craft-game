using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    public class CraftingItemDeck : MonoBehaviour, ICursorEventListener
    {
        //ADDING/REMOVING ITEMS: should the item be told when it's added/removed and handle toggling physics/input itself?
        //That way would make it easier to do animations/coroutine stuff since it would stop being the deck's responsibility
        //as soon as the item is added or removed, but that may not be necessary

        [SerializeField] private CraftingItemDatabase startingDeck;
        [SerializeField] private float itemHeight = 0.1f;
        [SerializeField] private float itemZOffset = 0.05f;
        [SerializeField] private bool populateOnEnable;
        [SerializeField] private float animateItemToDeckTime = 0.4f;
        [Header("VFX")]
        [SerializeField] private FadeInOut onHoverPreviewVFX;

        private LinkedList<CraftingItem> deck = new LinkedList<CraftingItem>();

        private Cursor cursor;

        private void Start()
        {
            if (Cursor.InstExists())
            {
                cursor = Cursor.Inst;
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

        private void Update()
        {
            //if player has started dragging the top item, remove it from the deck
            //TODO: this is jank
            CheckForTopItemRemoved();
        }

        private void LateUpdate()
        {
            if (cursor && onHoverPreviewVFX)
            {
                onHoverPreviewVFX.gameObject.SetActive(cursor.IsHovered(this) && cursor.CurrentDragTarget);
            }
        }

        private void CheckForTopItemRemoved()
        {
            if (deck.Count == 0)
            {
                return;
            }

            if (!deck.Last.Value)
            {
                RemoveTopItem();
                return;
            }

            var cursor = Cursor.Inst;
            if (!cursor || !cursor.CurrentDragTarget)
            {
                return;
            }

            if (cursor.CurrentDragTarget.gameObject == deck.Last.Value.gameObject)
            {
                RemoveTopItem();
            }
        }

        public CraftingItem PeekTopItem()
        {
            return deck.Count > 0 ? deck.Last.Value : null;
        }

        public bool IsEmpty()
        {
            return deck.Count == 0;
        }

        public void AddItemToTopDeck(CraftingItem item)
        {
            OnItemAdded(item);
            deck.AddLast(item);
            MoveItemToDeck(item, transform.position
                + (deck.Count * itemHeight * Vector3.up)
                + (deck.Count * itemZOffset * Vector3.forward));
        }

        public void AddItemToBottomDeck(CraftingItem item)
        {
            Debug.Log($"Adding item {item.gameObject.name} to bottom of deck!");
            var itemUpInc = Vector3.up * itemHeight;
            foreach (var deckItem in deck)
            {
                deckItem.transform.position += itemUpInc;
                deckItem.transform.position += Vector3.forward * itemZOffset;
            }

            OnItemAdded(item);
            deck.AddFirst(item);
            MoveItemToDeck(item, transform.position);
        }

        private void OnItemAdded(CraftingItem item)
        {
            if (item)
            {
                item.transform.parent = transform;
                item.SetState(CraftingItem.State.Animatable);
            }
        }

        private void OnItemRemoved(CraftingItem item)
        {
            if (item)
            {
                item.transform.parent = null;
                item.SetState(CraftingItem.State.Active);
            }
        }

        private void MoveItemToDeck(CraftingItem item, Vector3 targetPos)
        {
            Tweening.DoTransform(item.transform, targetPos, transform.rotation, animateItemToDeckTime);
        }

        private void PopulateDeck(CraftingItemDatabase deckItems)
        {
            ClearDeck();

            if (deckItems == null)
            {
                return;
            }

            for (int i = 0; i < deckItems.ItemList.Count; i++)
            {
                var itemData = deckItems.ItemList[i];
                var item = CraftingManager.Inst.SpawnItem(itemData, transform.position, Quaternion.identity);
                item.transform.parent = transform;
                AddItemToTopDeck(item);
            }
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
            if (IsEmpty())
            {
                return;
            }

            var item = deck.Last.Value;
            if (item)
            {
                item.TryStartDrag();
            }
        }

        public void RemoveItem(CraftingItem item)
        {
            if (deck.Count == 0)
            {
                return;
            }

            var node = deck.Find(item);
            if (node != null)
            {
                deck.Remove(node);
                OnItemRemoved(item);
            }
        }

        private void RemoveTopItem()
        {
            if (deck.Count == 0)
            {
                return;
            }

            var item = deck.Last.Value;
            OnItemRemoved(item);
            deck.RemoveLast();
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
            else if (e == Cursor.EventID.LeftClickUp)
            {
                //drop hovered item onto deck
                var cursor = Cursor.Inst;
                if (cursor.IsHovered(this)
                    && cursor.CurrentDragTarget
                    && cursor.CurrentDragTarget.TryGetComponent<CraftingItem>(out var item))
                {
                    cursor.CurrentDragTarget.CancelDrag();
                    AddItemToTopDeck(item);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            if (Cursor.InstExists() && Cursor.Inst.IsHovered(this))
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.white;
            }
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0f, 1f));
        }
    }
}