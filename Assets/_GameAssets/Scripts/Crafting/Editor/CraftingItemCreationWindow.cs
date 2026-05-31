using Crafting;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftingItemCreationWindow : EditorWindow
{
    private ObjectField itemDatabaseField;
    private CraftingItemDatabase itemDatabase;
    private VisualElement graphRoot;

    [MenuItem("Window/Crafting/Crafting Item Editor")]
    public static void CreateWindow()
    {
        var window = GetWindow<CraftingItemCreationWindow>();
        window.titleContent = new GUIContent("Crafting Item Editor");

        window.minSize = new Vector2(300, 200);
    }

    private void CreateGUI()
    {
        itemDatabaseField = new ObjectField("Item database");
        itemDatabaseField.objectType = typeof(CraftingItemDatabase);
        itemDatabaseField.RegisterValueChangedCallback(
            evt => OnItemDatabaseChanged((CraftingItemDatabase)evt.newValue));
        rootVisualElement.Add(itemDatabaseField);

        graphRoot = new VisualElement();
        rootVisualElement.Add(graphRoot);
    }

    private void OnItemDatabaseChanged(CraftingItemDatabase database)
    {
        if(!database)
        {
            return;
        }

        itemDatabase = database;

        CreateGraph(itemDatabase);
        foreach(var itemData in itemDatabase.ItemList)
        {
            var node = CreateGraphNode(itemData);
            rootVisualElement.Add(node);
        }
    }

    private VisualElement CreateGraph(CraftingItemDatabase itemDatabase)
    {
        UpdateItemTiers(itemDatabase);
        return null;
    }

    private void UpdateItemTiers(CraftingItemDatabase itemDatabase)
    {
        var uncheckedItems = new List<CraftingItemData>(itemDatabase.ItemList);
        var checkedItems = new HashSet<CraftingItemData>();

        int tier = 0;
        //while(uncheckedItems.Count > 0)
        {
            for(int i = uncheckedItems.Count - 1; i >= 0; i--)
            {
                var itemData = uncheckedItems[i];
                var allPrereqsInPrevTier = true;
                if(itemData.Prerequisites.Count > 0)
                {
                    if(itemData.HasPrerequisiteLoop)
                    {
                        continue;
                    }

                    foreach(var prereq in itemData.Prerequisites)
                    {
                        if(!checkedItems.Contains(prereq))
                        {
                            allPrereqsInPrevTier = false;
                            break;
                        }
                    }
                }

                if(allPrereqsInPrevTier)
                {
                    itemData.Tier = tier;
                    checkedItems.Add(itemData);
                    uncheckedItems.RemoveAt(i);
                    Debug.Log($"{itemData.name} is tier {tier}");
                }
            }

            tier++;
        }
    }

    private bool HasPrerequisiteLoop(List<CraftingItemData> itemList)
    {
        if(itemList == null || itemList.Count == 0)
        {
            return false;
        }

        var visited = new HashSet<CraftingItemData>(itemList.Count);
        var recursion = new HashSet<CraftingItemData>(itemList.Count);

        foreach(var itemData in itemList)
        {
            if(!visited.Contains(itemData) && NodeHasLoop(itemData))
            {
                return true;
            }
        }

        return false;

        bool NodeHasLoop(CraftingItemData itemData)
        {
            //node already visited - loop detected!
            if (recursion.Contains(itemData))
            {
                return true;
            }

            //node already visited and no loop found
            if (visited.Contains(itemData))
            {
                return false;
            }

            //add this item to visited and recursion sets
            visited.Add(itemData);
            recursion.Add(itemData);

            foreach(var prereq in itemData.Prerequisites)
            {
                if(NodeHasLoop(prereq))
                {
                    return true;
                }
            }

            //remove this item from recursion stack if no loop found
            recursion.Remove(itemData);
            return false;
        }
    }

    private VisualElement CreateGraphNode(CraftingItemData itemData)
    {
        var root = new VisualElement();
        var label = new Label(itemData.name + ", " + itemData.Tier);
        root.Add(label);

        return root;
    }
}
