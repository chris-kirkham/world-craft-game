using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class CraftingItem : MonoBehaviour, ICursorEventListener
{
    /// <summary>
    /// Animatable = crafting OFF, input OFF, kinematic rb, collision OFF
    /// Draggable = crafting OFF, input ON, kinematic rb, collision OFF
    /// Active = crafting ON, input ON, non-kinematic rb, collision ON
    /// </summary>
    public enum State
    {
        Animatable,
        Draggable,
        Active
    }

    [Header("Item data")]
    [SerializeField] private CraftingItemData itemData;

    [Header("Physics/movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider coll;
    [SerializeField] private DraggablePhysicsObject dragHandler;

    [Header("UI")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Renderer image;
    [SerializeField] private RectTransform imageArea;
    [SerializeField] private TextMeshProUGUI debugImageText; //debug text for when image is missing

    [Header("VFX")]
    [SerializeField] private GameObject onCraftedVFX;
    [SerializeField] private GameObject craftingPotentialVFX;

    private Material imageMat;
    private bool acceptInput = true;
    private bool canBeUsedInCraft = true;
    private bool isHovered;
    private bool isTouchingOtherItems;

    public CraftingItemData Data 
    {
        get => itemData;
        set
        {
            itemData = value;
            UpdateData();
        } 
    }

    public bool CanCraft => canBeUsedInCraft;

    public event Action OnUsedInSuccessfulCraft;

    private void OnEnable()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.RegisterCraftingItem(this);
        }

        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
        else
        {
            Debug.LogError($"Instance of {nameof(Cursor)} not found!");
        }

        SetState(State.Active);
        UpdateData();

        if(craftingPotentialVFX)
        {
            craftingPotentialVFX.SetActive(false);
        }

        if (dragHandler)
        {
            dragHandler.DragStarted.AddListener(OnDragStart);
            dragHandler.DragEnded.AddListener(OnDragEnd);
        }
    }

    private void OnDisable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }

        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.OnItemDisabledOrDestroyed(this);
        }

        if (dragHandler)
        {
            dragHandler.DragStarted.RemoveListener(OnDragStart);
            dragHandler.DragEnded.RemoveListener(OnDragEnd);
        }

        RemoveAllItemContacts();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!canBeUsedInCraft)
        {
            return;
        }

        var otherItem = other.GetComponentInParent<CraftingItem>();
        if(otherItem && otherItem.canBeUsedInCraft)
        {
            AddItemContact(otherItem);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var otherItem = other.gameObject.GetComponentInParent<CraftingItem>();
        if (otherItem)
        {
            RemoveItemContact(otherItem);
        }
    }

    private void AddItemContact(CraftingItem item)
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.AddItemContact(this, item);
        }
    }

    private void RemoveItemContact(CraftingItem item)
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.RemoveItemContact(this, item);
        }

        if(!isTouchingOtherItems) //TODO: add this functionality back in
        {
            SetPartialCraftVFX(false);
        }
    }

    private void RemoveAllItemContacts()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.RemoveAllItemContactsForItem(this);
        }
    }

    private void UpdateData()
    {
        if (!itemData)
        {
            Debug.Log($"No item data set for crafting item object {name}!");
            if(Application.isPlaying)
            {
                gameObject.name = "Item_MissingItemData";
            }

            if(debugImageText)
            {
                debugImageText.text = "NO ITEM DATA";
                debugImageText.gameObject.SetActive(true);
            }
            
            return;
        }
    
        gameObject.name = "Item_" + itemData.ItemName;

        if(CraftingManager.InstExists())
        {
            var itemsInPlay = CraftingManager.Inst.ActiveItems;
            int sameItemCount = 0;
            foreach(var item in itemsInPlay)
            {
                if (item.itemData == itemData)
                {
                    sameItemCount++;
                }
            }

            if(sameItemCount > 0)
            {
                gameObject.name += $" ({sameItemCount})";
            }
        }

        if(image && itemData.ThumbnailTex && Application.isPlaying) //don't get material instance + default to text if not playing
        {
            imageMat = image.material;
            if (imageMat)
            {
                imageMat.mainTexture = itemData.ThumbnailTex;
                imageMat.SetTexture("_EmissionMap", itemData.ThumbnailTex);
                image.gameObject.SetActive(true);
            }

            if(debugImageText)
            {
                debugImageText.gameObject.SetActive(false);
            }
        }
        else if(debugImageText) //if no image set, use debug text
        {
            debugImageText.text = itemData.ItemName;
            debugImageText.gameObject.SetActive(true);
            image.gameObject.SetActive(false);
        }
    }

    public void TryStartDrag()
    {
        dragHandler.TryStartDrag();
    }

    private void OnDragStart()
    {
        SetCollisionEnabled(false);
        SetPartialCraftVFX(false);
    }

    private void OnDragEnd()
    {
        SetCollisionEnabled(true);
    }

    //called when this item is first crafted
    public void OnCrafted()
    {
        OnCraftedVFX();
    }

    private void OnCraftedVFX()
    {
        if (onCraftedVFX)
        {
            onCraftedVFX.SetActive(true);
        }
    }

    public void OnCraftAttempt(CraftingManager.CraftingResultState resultState)
    {
        if(resultState == CraftingManager.CraftingResultState.SuccessfulCraft)
        {
            OnSuccessfulCraft();
        }
        else if(resultState == CraftingManager.CraftingResultState.PartialIngredientMatch)
        {
            SetPartialCraftVFX(true);
        }
        else
        {
            SetPartialCraftVFX(false);
        }
    }

    public void SetPartialCraftVFX(bool on)
    {
        if(craftingPotentialVFX)
        {
            craftingPotentialVFX.SetActive(on);
        }
    }

    private void OnSuccessfulCraft()
    {
        SetPartialCraftVFX(false);
        SetCollisionEnabled(false);
        if(CraftingItemDeck.InstExists())
        {
            CraftingItemDeck.Inst.AddItemToBottomDeck(this);
        }
        OnUsedInSuccessfulCraft?.Invoke();
        //Destroy(gameObject);
    }

    public void SetState(State state)
    {
        SetPhysAndCollision(state == State.Active);
        SetCanBeUsedInCraft(state == State.Active);
        SetAcceptInput(state == State.Draggable || state == State.Active);
    }

    private void SetCollisionEnabled(bool enabled)
    {
        if(coll)
        {
            coll.enabled = enabled;

            if(!enabled)
            {
                RemoveAllItemContacts();
            }
        }
        else
        {
            Debug.LogError($"No Collider set to enable/disable collision on!");
        }
    }

    private void SetPhysAndCollision(bool enabled)
    {
        rb.isKinematic = !enabled;
        SetCollisionEnabled(enabled);
        dragHandler.ReEnablePhysicsOnEndDrag = enabled; //hack
    }

    private void SetAcceptInput(bool acceptInput)
    {
        this.acceptInput = acceptInput;
        dragHandler.SetDragEnabled(acceptInput);

        if(!acceptInput)
        {
            RemoveAllItemContacts();
        }
    }

    private void SetCanBeUsedInCraft(bool enabled)
    {
        canBeUsedInCraft = enabled;

        if(!canBeUsedInCraft)
        {
            RemoveAllItemContacts();
        }
    }

    public void OnCursorEvent(Cursor.EventID e)
    {
        if (e == Cursor.EventID.EnterElement)
        {
            isHovered = true;
        }
        else if (e == Cursor.EventID.ExitElement)
        {
            isHovered = false;
        }
        else if (e == Cursor.EventID.RightClickDown)
        {
            //inspect item while holding right-click?
        }
    }
}
