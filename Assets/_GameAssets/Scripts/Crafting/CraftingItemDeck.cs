using DG.Tweening;
using System;
using System.Collections;
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
        [SerializeField] private float animateItemToDeckTime = 0.4f;
        [Header("VFX")]
        [SerializeField] private FadeInOut onHoverPreviewVFX;

        private LinkedList<CraftingItem> deck = new LinkedList<CraftingItem>();

        private Cursor cursor;

        public event Action<CraftingItem> OnItemPlaced;
        public event Action<CraftingItem> OnItemRemoved;

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

        public CraftingItem PeekTopItem()
        {
            return deck.Count > 0 ? deck.Last.Value : null;
        }

        public bool IsEmpty()
        {
            return deck.Count == 0;
        }

        private Vector3 GetTopDeckPos()
        {
            return transform.position
                + (deck.Count * itemHeight * Vector3.up)
                + (deck.Count * itemZOffset * Vector3.forward);
        }

        private Vector3 GetBottomDeckPos()
        {
            return transform.position;
        }

        private IEnumerator TweenItemToDeck(CraftingItem item)
        {
            //for infinite decks, items tweening to the deck should look like they're merging/being absorbed into the deck,
            //as infinite decks should only ever contain one item which represents that item type
            //TODO: WE NEED TO ACTUALLY NOT ADD MORE ITEMS TO INFINITE DECKS, THIS IS JUST A VFX HACK
            if(GameplaySettings.InfiniteDecks && deck.Count > 1)
            {
                //TODO: scaling cards to 0 and not scaling them back when dragging makes them invisible LOL
                //Fix infinite decks properly!
                yield return Tweening.DoTransform(
                    item.transform, GetBottomDeckPos(), transform.rotation, Vector3.zero, animateItemToDeckTime).WaitForCompletion();

                var topItem = deck.Last.Value;
                if (TryRemovePlacedObj(topItem))
                {
                    Destroy(topItem.gameObject);
                }
            }
            else //non-infinite decks should visually stack items
            {
                yield return Tweening.DoTransform(
                    item.transform, GetTopDeckPos(), transform.rotation, animateItemToDeckTime).WaitForCompletion();
            }
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
                if(!TryPlaceObject(item))
                {
                    Debug.LogError($"Unable to place item when populating deck for some reason!");
                }
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
                item.RequestDrag();
            }
        }

        private void RemoveTopItem()
        {
            if (deck.Count == 0)
            {
                return;
            }

            var item = deck.Last.Value;
            deck.RemoveLast();

            if (!item)
            {
                Debug.LogError($"Removed a null item from the deck! Why did the deck contain a null item?");
                return;
            }

            var itemData = item.Data;
            item.transform.parent = null;
            item.SetState(CraftingItem.State.Active);

            //TODO: prototype - infinite deck - spawn new item to replace removed one
            if (GameplaySettings.InfiniteDecks && deck.Count < 1)
            {
                var newItem = CraftingManager.Inst.SpawnItem(
                    itemData, GetTopDeckPos(), Quaternion.identity, doItemOnCraftedCallback: false);
                if(!TryPlaceObject(newItem))
                {
                    Debug.LogError("Unable to replace item in deck for some reason!");
                }
            }
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

        private void AddItemToTopDeck(CraftingItem item, bool animateToDeck = true)
        {
            if(!item)
            {
                Debug.LogError($"Tried to add null item to deck!");
                return;
            }

            if (singleItemType && !IsEmpty() && item.Data != deck.Last.Value.Data)
            {
                Debug.LogError("Added a different item type to a single-item deck! This should be dealt with earlier in code.");
            }

            if(GameplaySettings.InfiniteDecks && !IsEmpty())
            {
                //TODO: for infinite decks, still tween item to the deck but make it look like it's merging with the first item or something
                //infinite decks should basically look like one card which represents that item type - cards should be able to be placed/returned
                //to the deck but it shouldn't stack as if there are multiple (but finite) cards
                //maybe make cards added to infinite decks scale down to zero as they move to the deck?
            }

            item.transform.parent = transform;
            item.SetState(CraftingItem.State.Animatable);

            deck.AddLast(item);

            if (animateToDeck)
            {
                StartCoroutine(TweenItemToDeck(item));
            }
            else
            {
                item.transform.position = GetTopDeckPos();
            }
        }

        protected override bool CanRemovePlacedObj(DraggableObject obj)
        {
            return !IsEmpty() && PeekTopItem() == obj;
        }

        protected override void OnPlacedObjRemoved(DraggableObject obj)
        {
            if(PeekTopItem() == obj)
            {
                RemoveTopItem();
            }
            else
            {
                Debug.LogError($"Object {obj} removed from deck, but it isn't the top item in this deck! This shouldn't happen.");
            }
        }

        protected override void OnDraggableEnterPlacementArea(DraggableObject obj)
        {
            base.OnDraggableEnterPlacementArea(obj);

            SetPlacementPreviewVFXEnabled(true);
        }

        private void SetPlacementPreviewVFXEnabled(bool enabled)
        {
            if (cursor && onHoverPreviewVFX)
            {
                onHoverPreviewVFX.gameObject.SetActive(enabled);
            }
        }

        protected override void OnDraggableExitPlacementArea(DraggableObject obj)
        {
            base.OnDraggableExitPlacementArea(obj);

            SetPlacementPreviewVFXEnabled(false);
        }

        //ICursorEventListener
        public override void OnCursorEvent(Cursor.EventID e)
        {
            base.OnCursorEvent(e);

            //slightly jank but w/e
            if(!Cursor.Inst.CurrentDragTarget)
            {
                SetPlacementPreviewVFXEnabled(false);
            }

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