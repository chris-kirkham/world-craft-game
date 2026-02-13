using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CraftingItem : MonoBehaviour, ICursorEventListener
{
    /// <summary>
    /// Animatable = crafting OFF, input OFF, kinematic rb, collision OFF
    /// Inert = crafting OFF, input OFF, non-kinematic rb, collision ON
    /// Draggable = crafting OFF, input ON, kinematic rb, collision OFF
    /// Active = crafting ON, input ON, non-kinematic rb, collision ON
    /// </summary>
    public enum State
    {
        Animatable,
        Inert, 
        Draggable,
        Active
    }

    [SerializeField] private CraftingItemData itemData;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider coll;
    [SerializeField] private DraggableElement dragHandler;

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
    private bool isDragging;
    private HashSet<CraftingItem> touchingItems;

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

    private void OnEnable()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.RegisterCraftingItem(this);
        }

        SetAcceptInput(true);
        SetCanBeUsedInCraft(true);
        UpdateData();

        touchingItems = new HashSet<CraftingItem>();
        touchingItems.Add(this); //an item is always touching itself

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

    private void Start()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
        else
        {
            Debug.LogError($"Instance of {nameof(Cursor)} not found!");
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!canBeUsedInCraft)
        {
            return;
        }

        var otherItem = other.GetComponentInParent<CraftingItem>();
        if(otherItem && !touchingItems.Contains(otherItem) && otherItem.canBeUsedInCraft)
        {
            OnNewItemContact(otherItem);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var otherItem = other.gameObject.GetComponentInParent<CraftingItem>();
        if (otherItem && touchingItems.Contains(otherItem))
        {
            OnLostItemContact(otherItem);
        }
    }

    private void OnNewItemContact(CraftingItem item)
    {
        CraftingManager.Inst.AddItemContact(this, item);

        touchingItems.Add(item);
    }

    private void OnLostItemContact(CraftingItem item)
    {
        CraftingManager.Inst.RemoveItemContact(this, item);

        touchingItems.Remove(item);
        if(touchingItems.Count < 2)
        {
            SetPartialCraftVFX(false);
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

    private void OpenItem()
    {
        if(!WindowManager.InstExists())
        {
            Debug.LogError($"Instance of {nameof(WindowManager)} not found! Cannot open item window.");
            return;
        }

        WindowManager.Inst.CreateWindow(itemData.WindowContent);
        Destroy(this.gameObject);
    }

    public void TryForceDragStart()
    {
        dragHandler.TryStartDrag();
    }

    private void OnDragStart()
    {
        SetCollisionEnabled(false);
        SetPartialCraftVFX(false);
        isDragging = true;
    }

    private void OnDragEnd()
    {
        SetCollisionEnabled(true);
        isDragging = false;
    }

    //called when this item is first crafted
    public void OnCrafted()
    {
        OnCraftedVFX();

        //SetCollisionEnabled(false);
        //StartCoroutine(SetCollisionEnabledWithDelay(true, onCraftedCollisionEnableDelay));
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
        //Destroy(gameObject);
    }

    public void SetState(State state)
    {
        SetPhysAndCollision(state == State.Inert || state == State.Active);
        SetCanBeUsedInCraft(state == State.Active);
        SetAcceptInput(state == State.Draggable || state == State.Active);
    }

    private void SetCollisionEnabled(bool enabled)
    {
        if(coll)
        {
            coll.enabled = enabled;
        }
        else
        {
            Debug.LogError($"No Collider set to enable/disable collision on!");
        }
    }

    private void SetPhysAndCollision(bool enabled)
    {
        rb.isKinematic = !enabled;
        coll.enabled = enabled;
    }

    private void SetAcceptInput(bool acceptInput)
    {
        this.acceptInput = acceptInput;
        dragHandler.enabled = acceptInput;
    }

    private void SetCanBeUsedInCraft(bool enabled)
    {
        canBeUsedInCraft = enabled;
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if (e == Cursor.CursorEvent.EnterElement)
        {
            isHovered = true;
        }
        else if (e == Cursor.CursorEvent.ExitElement)
        {
            isHovered = false;
        }
        else if (e == Cursor.CursorEvent.RightClickDown)
        {
            //inspect item while holding right-click?
        }
    }
}
