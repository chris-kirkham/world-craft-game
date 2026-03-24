using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class CraftingItem : MonoBehaviour, ICursorEventListener, IStackable
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
    [SerializeField] private Vector3 stackingOffset;

    [Header("Art")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Renderer image;
    [SerializeField] private RectTransform imageArea;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private FadeInOutText nameTextFade;
    [SerializeField] private TextMeshProUGUI debugImageText; //debug text for when image is missing

    [Header("VFX")]
    [SerializeField] private GameObject onCraftedVFX;
    [SerializeField] private GameObject craftingPotentialVFX;

    private Material imageMat;
    private bool acceptInput = true;
    private bool canBeUsedInCraft = true;
    private bool isTouchingOtherItems;
    private Cursor cursor;
    private State state;

    private Sequence tweenSequence;

    private bool isInspecting;

    public CraftingItemData Data 
    {
        get => itemData;
        set
        {
            itemData = value;
            UpdateData();
        } 
    }

    //IStackable
    public bool CanStack => false;
    public Vector3 StackingOffset => stackingOffset;
    public Stacker CurrentStacker { get; set; }
    
    public event Action OnUsedInSuccessfulCraft;

    private void OnEnable()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.RegisterCraftingItem(this);
        }

        if (Cursor.InstExists())
        {
            cursor = Cursor.Inst;
            cursor.AddCursorEventListener(this);
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
        if (cursor)
        {
            cursor.RemoveCursorEventListener(this);
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

        if(tweenSequence != null)
        {
            //tweenSequence.Kill();
        }
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

        if (nameText)
        {
            nameText.text = itemData.ItemName;
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
        
        //check for stacking!!
        if(CanStack && cursor)
        {
            //TODO: PROTOTYPE
            var hits = Physics.RaycastAll(transform.position, Vector3.down);
            var minDist = Mathf.Infinity;
            IStackable bestHit = null;
            foreach(var hit in hits)
            {
                var stackable = hit.transform.GetComponentInParent<IStackable>();
                if(stackable == null || stackable == (IStackable)this)
                {
                    continue;
                }

                if(hit.distance < minDist)
                {
                    minDist = hit.distance;
                    bestHit = stackable;
                }
            }

            if(bestHit != null)
            {
                Stacker.Stack(bestHit, this);
            }
        }
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
        OnUsedInSuccessfulCraft?.Invoke();
        Destroy(gameObject);
    }

    public void SetState(State state)
    {
        this.state = state;
        SetPhysAndCollision(state == State.Active);
        SetCanBeUsedInCraft(state == State.Active);
        SetAcceptInput(state == State.Draggable || state == State.Active);
    }

    private void SetCanBeUsedInCraft(bool enabled)
    {
        canBeUsedInCraft = enabled;

        if (!canBeUsedInCraft)
        {
            RemoveAllItemContacts();
        }
    }
    private void SetCollisionEnabled(bool enabled)
    {
        if (coll)
        {
            coll.enabled = enabled;

            if (!enabled)
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

        if (!acceptInput)
        {
            RemoveAllItemContacts();
        }
    }

    public void AnimateTo(Vector3 position_WS, Quaternion rotation_WS, float time, bool returnToPrevStateOnEndAnim = true)
    {
       StartCoroutine(AnimateToRoutine(position_WS, rotation_WS, time, returnToPrevStateOnEndAnim));
    }

    public IEnumerator AnimateToRoutine(Vector3 position_WS, Quaternion rotation_WS, float time, bool returnToPrevStateOnEndAnim = true)
    {
        var prevState = state;
        SetState(State.Animatable);

        if(time <= 0f)
        {
            transform.position = position_WS;
            transform.rotation = rotation_WS;
        }
        else
        {
            if (tweenSequence != null && tweenSequence.active)
            {
                yield return tweenSequence.WaitForCompletion();
            }

            tweenSequence = DOTween.Sequence(transform);
            tweenSequence.Append(transform.DOMove(position_WS, time));
            tweenSequence.Join(transform.DORotate(rotation_WS.eulerAngles, time));

            yield return tweenSequence.WaitForCompletion();

            //necessary?
            transform.position = position_WS;
            transform.rotation = rotation_WS;
        }

        if (returnToPrevStateOnEndAnim)
        {
            SetState(prevState);
        }
    }

    public void SetOnInspectVFX(bool inspecting)
    {
        isInspecting = inspecting;
        if (inspecting)
        {
            if (nameTextFade)
            {
                nameTextFade.EnableAndPlayInClip();
            }
        }
        else
        {
            if (nameTextFade)
            {
                nameTextFade.PlayClipThenDisable();
            }
        }
    }

    private IEnumerator DoInspectRoutine(Vector3 endPos, Quaternion endRotation)
    {
        //TODO: placeholder position and rotation!
        var cam = cursor.Cam;
        var targetPos = cam.transform.position + (cam.transform.forward * 2f);
        var targetRotation = Quaternion.identity;

        var prevPos = transform.position;
        var prevRotation = transform.rotation;
        var prevState = state;

        yield return AnimateToRoutine(targetPos, targetRotation, 0.5f, returnToPrevStateOnEndAnim: false);

        while(isInspecting)
        {
            yield return null;
        }

        //TODO: fix grab ending on this (due to going into animation state) and/or think about desired behaviour
        yield return AnimateToRoutine(prevPos, prevRotation, 0.5f, returnToPrevStateOnEndAnim: false);
        SetState(prevState);
    }

    public void OnCursorEvent(Cursor.EventID e)
    {
        if (e == Cursor.EventID.RightClickDown)
        {
            //inspect item while holding right-click
            if (cursor.CurrentDragTarget == dragHandler)
            {
                SetOnInspectVFX(true);
            }
        }
        else if(e == Cursor.EventID.RightClickUp)
        {
            SetOnInspectVFX(false);
        }
    }

    public void OnAddedToStack()
    {
        throw new NotImplementedException();
    }

    public void OnRemovedFromStack()
    {
        throw new NotImplementedException();
    }
}
