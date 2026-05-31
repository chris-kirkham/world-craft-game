using Crafting;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftingItemCreationWindow : EditorWindow
{
    private ObjectField itemDatabaseField;
    private CraftingItemDatabase itemDatabase;
    private VisualElement graphRoot;
    private Button checkForLoopsButton;
    private ScrollView tierListScrollView;

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

        checkForLoopsButton = new Button();
        checkForLoopsButton.text = "Check for prerequisite loops";
        checkForLoopsButton.clicked += () => CheckAllForPrerequisiteLoops((CraftingItemDatabase)itemDatabaseField.value);
        rootVisualElement.Add(checkForLoopsButton);

        tierListScrollView = new ScrollView();
        rootVisualElement.Add(tierListScrollView);

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
        var itemsSortedByTier = new List<CraftingItemData>(database.ItemList);
        itemsSortedByTier.Sort((a, b) => a.Tier.CompareTo(b.Tier));
        foreach(var itemData in itemsSortedByTier)
        {
            var node = CreateGraphNode(itemData);
            tierListScrollView.Add(node);
        }
    }

    private VisualElement CreateGraph(CraftingItemDatabase itemDatabase)
    {
        itemDatabase.UpdateItemTiers();
        return null;
    }

    private void CheckAllForPrerequisiteLoops(CraftingItemDatabase itemDatabase)
    {
        if(!itemDatabase)
        {
            return;
        }

        foreach(var itemData in itemDatabase.ItemList)
        {
            itemData.CheckForPrerequisiteLoops();
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
