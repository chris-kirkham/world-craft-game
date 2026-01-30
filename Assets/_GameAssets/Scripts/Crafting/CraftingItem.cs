using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CraftingItem : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private CraftingItemData itemData;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider coll;
    [SerializeField] private float onCraftedCollisionEnableDelay = 0.5f;
    [SerializeField] private DraggableElement dragHandler;

    [Header("UI")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RawImage thumbnailImage;
    [SerializeField] private Vector2Int itemSize = new Vector2Int(100, 100);
    [SerializeField] private RectTransform imageArea;
    [SerializeField] private TextMeshProUGUI debugImageText; //debug text for when image is missing

    [Header("VFX")]
    [SerializeField] private GameObject onCraftedVFX;
    [SerializeField] private GameObject craftingPotentialVFX;


    private bool acceptInput = true;
    private bool isHovered;
    
    public HashSet<CraftingItem> TouchingItems => touchingItems;
    private HashSet<CraftingItem> touchingItems;

    public bool CanBeUsedInCraft => canBeUsedInCraft;
    private bool canBeUsedInCraft = true;


    public CraftingItemData Data 
    {
        get => itemData;
        set
        {
            itemData = value;
            UpdateData();
        } 
    } 

    private void OnValidate()
    {
        UpdateData();
    }

    private void OnEnable()
    {
        if(Crafter.InstExists())
        {
            Crafter.Inst.RegisterCraftingItem(this);
        }

        SetAcceptInput(true);
        UpdateData();

        touchingItems = new HashSet<CraftingItem>();
        touchingItems.Add(this); //an item is always touching itself

        if(craftingPotentialVFX)
        {
            //TODO: check for crafting potential on enable?
            craftingPotentialVFX.SetActive(false);
        }

        if (dragHandler)
        {
            dragHandler.DragStarted += OnDragStart;
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
        else
        {
            Debug.LogError($"Instance of {nameof(Cursor)} not found!");
        }

        if(Crafter.InstExists())
        {
            Crafter.Inst.OnItemDisabledOrDestroyed(this);
        }

        if (dragHandler)
        {
            dragHandler.DragStarted -= OnDragStart;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var otherItem = other.GetComponentInParent<CraftingItem>();
        if(otherItem && !touchingItems.Contains(otherItem))
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
        Crafter.Inst.AddItemContact(this, item);

        touchingItems.Add(item);
    }

    private void OnLostItemContact(CraftingItem item)
    {
        Crafter.Inst.RemoveItemContact(this, item);

        touchingItems.Remove(item);
        if(touchingItems.Count == 0)
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

            thumbnailImage.texture = Resources.Load<Texture2D>("TX_Error_Sprite");

            return;
        }
    
        gameObject.name = "Item_" + itemData.ItemName;

        if(itemData.ThumbnailTex)
        {
            thumbnailImage.gameObject.SetActive(true);
            thumbnailImage.texture = itemData.ThumbnailTex;
            if(debugImageText)
            {
                debugImageText.gameObject.SetActive(false);
            }
        }
        else if(debugImageText) //if no image set, use debug text
        {
            debugImageText.text = itemData.ItemName;
            debugImageText.gameObject.SetActive(true);
            thumbnailImage.gameObject.SetActive(false);
        }

        if(canvasRect)
        {
            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemSize.x);
            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemSize.y);
        }

        if (thumbnailImage.texture)
        {
            //scale thumbnail image so its longest side matches canvas size, maintaining aspect ratio
            var imageAreaWidth = imageArea.rect.width;
            var imageAreaHeight = imageArea.rect.height;
            var imageWidth = thumbnailImage.texture.width;
            var imageHeight = thumbnailImage.texture.height;
            var imageRect = thumbnailImage.rectTransform;
            //if (imageWidth > imageHeight) //fit entire image in
            if (imageHeight > imageWidth) //scale image up so smallest side fills image area (needs mask)
            {
                var scale = imageAreaWidth / (float)imageWidth;
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageAreaWidth);
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, imageHeight * scale);
            }
            else
            {
                var scale = imageAreaHeight / (float)imageHeight;
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageWidth * scale);
                imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, imageAreaHeight);
            }
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

    //accept or block user input (e.g. for when animating item)
    private void SetAcceptInput(bool acceptInput)
    {
        this.acceptInput = acceptInput;
        if(!acceptInput) //necessary?
        {
            isHovered = false;
        }
    }

    public void SetCanBeUsedInCraft(bool enabled)
    {
        canBeUsedInCraft = enabled;
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(!acceptInput)
        {
            return;
        }
        
        if(e == Cursor.CursorEvent.EnterElement)
        {
            isHovered = true;
        }
        else if(e == Cursor.CursorEvent.ExitElement)
        {
            isHovered = false;
        }
    }

    private void OnDragStart()
    {
        //TODO: other stuff to do here?
        SetPartialCraftVFX(false);
    }

    //called when this item is first crafted
    public void OnCrafted()
    {
        //TODO: other VFX
        OnCraftedVFX();
        //StartCoroutine(OnCraftedAnim(transform, Cursor.Inst.ClampedPosition_WS));

        SetCollisionEnabled(false);
        StartCoroutine(SetCollisionEnabledWithDelay(true, onCraftedCollisionEnableDelay));
    }

    private void OnCraftedVFX()
    {
        if (onCraftedVFX)
        {
            onCraftedVFX.SetActive(true);
        }
    }

    //TODO: prototype/placeholder
    private void OnCraftedPush()
    {
        var pushForce = Random.insideUnitCircle;
        rb.AddForce(new Vector3(pushForce.x, 1f, pushForce.y), ForceMode.Acceleration);
        var pushTorque = new Vector3(0f, Random.Range(-15f, 15f), 0f);
        rb.AddTorque(pushTorque, ForceMode.Acceleration);
    }

    public void OnCraftAttempt(Crafter.CraftingResultState resultState)
    {
        if(resultState == Crafter.CraftingResultState.SuccessfulCraft)
        {
            OnSuccessfulCraft();
        }
        else if(resultState == Crafter.CraftingResultState.PartialIngredientMatch)
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
        craftingPotentialVFX.SetActive(on);
    }

    private void OnSuccessfulCraft()
    {
        SetCollisionEnabled(false);
        Destroy(gameObject);
        //StartCoroutine(OnSuccessfulCraftAnim());
    }

    private IEnumerator OnSuccessfulCraftAnim()
    {
        //TODO: other VFX
        SetAcceptInput(false);
        OnCraftedPush();
        yield return new WaitForSeconds(1f);
        SetAcceptInput(true); //ONLY IF NOT DESTROYING ON CRAFT
        Destroy(gameObject);
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

    private IEnumerator SetCollisionEnabledWithDelay(bool enabled, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetCollisionEnabled(enabled);
    }

    public void TogglePhysics(bool enabled)
    {
        rb.isKinematic = enabled;
        coll.enabled = !enabled;
    }
}
