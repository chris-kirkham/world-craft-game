using System.Collections.Generic;
using UnityEngine;
using static ItemContactsTracker;

public class ItemContactsTracker
{
    public class ItemContactsGroup
    {
        private HashSet<CraftingItem> items;

        public HashSet<CraftingItem> Items => items;
        public int Count => items.Count;

        public ItemContactsGroup(HashSet<CraftingItem> items)
        {
            this.items = items;
        }

        public void Add(CraftingItem item)
        {
            items.Add(item);
        }

        public void Remove(CraftingItem item)
        {
            items.Remove(item);
        }

        public bool Contains(CraftingItem item)
        {
            return items.Contains(item);
        }

        public override bool Equals(object obj)
        {
            var other = (ItemContactsGroup)obj;
            if (other != null)
            {
                return items.SetEquals(other.items);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            //hmm
            int hash = 19;
            foreach (var item in items)
            {
                hash += 31 * item.GetHashCode();
            }

            return hash;
        }
    }

    //Dictionary of {ITEM : CONTACTS} where ITEM is the item which initially reported the contact.
    //Contains duplicates so all contacts can be found using any contacting item's key, e.g.
    //{WATER : [WATER, GROUND, BEACH]}
    //{GROUND : [WATER, GROUND, BEACH]}
    //{BEACH : [WATER, GROUND, BEACH]}
    private Dictionary<CraftingItem, ItemContactsGroup> itemContactsDict = new Dictionary<CraftingItem, ItemContactsGroup>();

    //HashSet containing only each unique contact group
    public HashSet<ItemContactsGroup> ContactGroups { get; } = new HashSet<ItemContactsGroup>();

    public void LateUpdate()
    {
        UpdateTrimmedItemGroups();
    }

    private void UpdateTrimmedItemGroups()
    {
        ContactGroups.Clear();
        foreach (var keyItem in itemContactsDict.Keys)
        {
            var group = new HashSet<CraftingItem>() { keyItem };
            ExpandItemGroup(group, itemContactsDict[keyItem].Items);
            ContactGroups.Add(new ItemContactsGroup(group));
        }

        void ExpandItemGroup(HashSet<CraftingItem> existingGroup, HashSet<CraftingItem> newGroup)
        {
            if (newGroup != existingGroup)
            {
                var newItems = new HashSet<CraftingItem>(newGroup);
                newItems.ExceptWith(existingGroup);
                existingGroup.UnionWith(newGroup);
                foreach (var item in newItems)
                {
                    if (itemContactsDict.TryGetValue(item, out var newItemContacts))
                    {
                        ExpandItemGroup(existingGroup, newItemContacts.Items);
                    }
                }
            }
        }
    }

    public void AddItemContact(CraftingItem item, CraftingItem contactingItem)
    {
        if (!item || !contactingItem)
        {
            return;
        }

        if (itemContactsDict.TryGetValue(item, out var touchingItems))
        {
            if (!touchingItems.Contains(contactingItem))
            {
                touchingItems.Add(contactingItem);
            }
        }
        else
        {
            itemContactsDict.Add(item, new ItemContactsGroup(new HashSet<CraftingItem> { item, contactingItem })); //item is always touching itself
        }

        //UpdateTrimmedItemGroups();
    }

    public void RemoveItemContact(CraftingItem item, CraftingItem contactingItem)
    {
        if (!item || !contactingItem)
        {
            return;
        }

        //remove other item from this item's contacts
        if (itemContactsDict.TryGetValue(item, out var touchingThisItem) && touchingThisItem.Contains(contactingItem))
        {
            touchingThisItem.Remove(contactingItem);

            if (touchingThisItem.Count < 2) //delete if <2 since we always add the item itself to the touching items (TODO: this is jank)
            {
                itemContactsDict.Remove(item);
            }
        }

        //remove this item from other item's contacts
        if (itemContactsDict.TryGetValue(contactingItem, out var touchingOtherItem) && touchingOtherItem.Contains(item))
        {
            touchingOtherItem.Remove(contactingItem);

            if (touchingOtherItem.Count < 2)
            {
                itemContactsDict.Remove(contactingItem);
            }
        }

        //UpdateTrimmedItemGroups();
    }

    public void RemoveAllItemContactsForItem(CraftingItem item)
    {
        if (!item)
        {
            return;
        }

        if (itemContactsDict.TryGetValue(item, out var itemContacts))
        {
            itemContactsDict.Remove(item);
            foreach (var contactingItem in itemContacts.Items)
            {
                if (itemContactsDict.TryGetValue(contactingItem, out var otherItemContacts))
                {
                    otherItemContacts.Remove(item);
                }
            }
        }

        //UpdateTrimmedItemGroups();
    }

    public void OnItemDisabledOrDestroyed(CraftingItem item)
    {
        foreach (var contacts in itemContactsDict.Values)
        {
            if (contacts.Contains(item))
            {
                contacts.Remove(item);
            }
        }

        if (itemContactsDict.ContainsKey(item))
        {
            itemContactsDict.Remove(item);
        }
    }

    public void OnDrawGizmos()
    {
        var contactPositions = new List<Vector3>();
        if (itemContactsDict.Count > 0)
        {
            Gizmos.matrix = Matrix4x4.identity;
            var hue = 0f;
            var inc = 1f / itemContactsDict.Count;
            foreach (var contactGroup in ContactGroups)
            {
                contactPositions.Clear();
                Gizmos.color = Color.HSVToRGB(hue, 1f, 1f);
                foreach (var contact in contactGroup.Items)
                {
                    Gizmos.DrawSphere(contact.transform.position, 0.1f);
                    contactPositions.Add(contact.transform.position);
                }

                for (int i = 0; i < contactPositions.Count; i++)
                {
                    for (int j = 0; j < contactPositions.Count; j++)
                    {
                        if (i != j)
                        {
                            Gizmos.DrawLine(contactPositions[i], contactPositions[j]);
                        }
                    }
                }

                hue += inc;
            }
        }
    }
}
