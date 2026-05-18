using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    public class CraftingItemDeck : DraggablePlacementPoint, ICursorEventListener
    {
        //ADDING/REMOVING ITEMS: should the item be told when it's added/removed and handle toggling physics/input itself?
        //That way would make it easier to do animations/coroutine stuff since it would stop being the deck's responsibility
        //as soon as the item is added or removed, but that may not be necessary

        [SerializeField] private CraftingItemDatabase startingDeck;
        [SerializeField] private float itemHeight = 0.1f;
        [SerializeField] private float itemZOffset = 0.05f;
        [SerializeField] private bool populateOnEnable;
        [SerializeField] private bool singleItemType = true;
        [SerializeField] private bool infinite = true;
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

        private void CheckForTopItemRemoved()
        {
            if (IsEmpty())
            {
                return;
            }

            if (!deck.Last.Value) //top deck item is null
            {
                RemoveTopItem();
                return;
            }

            var cursor = Cursor.Inst;
            if (!cursor || !cursor.CurrentDragTarget)
            {
                return;
            }

            if (cursor.CurrentDragTarget == deck.Last.Value)
            {
                var itemData = deck.Last.Value.Data;
                RemoveTopItem();

                //TODO: prototype - infinite deck - spawn new item to replace grabbed one
                if (infinite && deck.Count < 1)
                {
                    var newItem = CraftingManager.Inst.SpawnItem(
                        itemData, Vector3.zero, Quaternion.identity, doItemOnCraftedCallback: false);
                    AddItemToTopDeck(newItem, animateToDeck: false);
                }
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

        private void AddItemToTopDeck(CraftingItem item, bool animateToDeck = true)
        {
            if(singleItemType && !IsEmpty() && item.Data != deck.Last.Value.Data)
            {
                Debug.LogError("Added a different item type to a single-item deck! This should be dealt with earlier in code.");
            }

            OnItemAdded(item);
            deck.AddLast(item);

            if(animateToDeck)
            {
                TweenItemToDeck(item, GetTopDeckPos());
            }
            else
            {
                item.transform.position = GetTopDeckPos();
            }
        }

        private Vector3 GetTopDeckPos()
        {
            return transform.position
                + (deck.Count * itemHeight * Vector3.up)
                + (deck.Count * itemZOffset * Vector3.forward);
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
            TweenItemToDeck(item, transform.position);
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

        private void TweenItemToDeck(CraftingItem item, Vector3 targetPos)
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
                item.SetState(CraftingItem.State.Draggable);
                item.TryStartDrag();
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

        protected override bool CanPlace(DraggableObject obj)
        {
            if(!(obj is CraftingItem))
            {
                return false;
            }

            if(IsEmpty() || !singleItemType)
            {
                return true;
            }
            else
            {
                var itemData = ((CraftingItem)obj).Data;
                return itemData == PeekTopItem().Data;
            }
        }

        protected override void PlaceObject(DraggableObject obj)
        {
            if(obj is CraftingItem)
            {
                AddItemToTopDeck((CraftingItem)obj);
            }
            else
            {
                Debug.Log($"Tried to place a non-crafting item object on this deck! This should have been caught earlier.");
            }
        }

        protected override void OnDraggableEnterPlacementArea(DraggableObject obj)
        {
            base.OnDraggableEnterPlacementArea(obj);

            if (cursor && onHoverPreviewVFX)
            {
                onHoverPreviewVFX.gameObject.SetActive(true);
            }
        }

        protected override void OnDraggableExitPlacementArea(DraggableObject obj)
        {
            base.OnDraggableExitPlacementArea(obj);

            if (cursor && onHoverPreviewVFX)
            {
                onHoverPreviewVFX.gameObject.SetActive(false);
            }
        }

        //ICursorEventListener
        public override void OnCursorEvent(Cursor.EventID e)
        {
            base.OnCursorEvent(e);

            if (e == Cursor.EventID.LeftClickDown)
            {
                if (Cursor.Inst.IsHovered(this))
                {
                    GrabTopDeckItem();
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