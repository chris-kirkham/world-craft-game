using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    private string craftingDebugInfo;
    private string cursorDebugInfo;

    private void LateUpdate()
    {
        if (debugText)
        {
            debugText.text = craftingDebugInfo + "\n" + cursorDebugInfo;
        }
        else
        {
            Debug.LogError($"{nameof(debugText)} not set!");
            return;
        }

    }

    public void SetCraftingDebugInfo(HashSet<CraftingManager.ItemContactsGroup> itemContacts)
    {
        craftingDebugInfo = "Touching items:";
        foreach(var contactGroup in itemContacts)
        {
            craftingDebugInfo += "\n[";
            foreach(var contact in contactGroup.items)
            {
                craftingDebugInfo += contact.Data.ItemName + ", ";
            }
            craftingDebugInfo = craftingDebugInfo.Remove(craftingDebugInfo.Length - 2, 2); //remove trailing comma
            craftingDebugInfo += "] HashCode: " + contactGroup.GetHashCode();
        }
    }

    public void SetCursorDebugInfo(Cursor cursor, HashSet<DraggableElement> dragRequests)
    {
        cursorDebugInfo = "Cursor: ";
        cursorDebugInfo += $"screen-space pos = {cursor.ClampedPosition_SS}, world pos = {cursor.ClampedPosition_WS}\n";
        var currDragTargetName = cursor.CurrentDragTarget ? cursor.CurrentDragTarget.name : "none";
        var dragRequestsDebug = dragRequests.Count > 0 ? $", drag requests = {string.Join(", ", dragRequests)}" : "";
        cursorDebugInfo += $"drag target = {currDragTargetName}{dragRequestsDebug}";
    }
}
