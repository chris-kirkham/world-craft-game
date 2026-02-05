using UnityEngine;

public class IntroSeqCraftingItem : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private CraftingItem item;
    [SerializeField] private IntroSequence introSequence;
    [SerializeField] private FadeInOutText onHoverText;

    private bool isHovered;

    public CraftingItemData ItemData => item.Data;

    private void OnEnable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }

        if (onHoverText)
        {
            onHoverText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
    }

    private void OnDisable()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }

        onHoverText.PlayClipThenDisable();
    }

    private void OnSelected()
    {
        introSequence.OnChooseItem(item);
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(!enabled)
        {
            return;
        }

        if(e == Cursor.CursorEvent.EnterElement)
        {
            isHovered = true;
            onHoverText.EnableAndPlayInClip();
        }

        if (e == Cursor.CursorEvent.ExitElement)
        {
            isHovered = false;
            onHoverText.PlayClipThenDisable();
        }

        if(e == Cursor.CursorEvent.LeftClickDown && isHovered)
        {
            OnSelected();
        }
    }
}
