using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CraftingDebugDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    public void SetDebugInfo(HashSet<Crafter.ItemContactsGroup> itemContacts)
    {
        if(!debugText)
        {
            Debug.LogError($"{nameof(debugText)} not set!");
            return;
        }

        var debugStr = "Touching items:";
        foreach(var contactGroup in itemContacts)
        {
            debugStr += "\n[";
            foreach(var contact in contactGroup.items)
            {
                debugStr += contact.Data.ItemName + ", ";
            }
            debugStr = debugStr.Remove(debugStr.Length - 2, 2); //remove trailing comma
            debugStr += "] HashCode: " + contactGroup.GetHashCode();
        }

        debugText.text = debugStr;
    }
}
