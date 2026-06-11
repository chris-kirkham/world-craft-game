using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.GraphToolkit;
using Unity.GraphToolkit.Editor;
using UnityEditor.Graphs;

public class CraftingItemCreationWindow : EditorWindow
{
    private ObjectField itemDatabaseField;
    private CraftingItemDatabase itemDatabase;
    private ScrollView graphRoot;
    private ScrollView tierListScrollView;

    //TODO: allow user to specify paths
    private const string newDatabasePath = "Assets/_GameAssets/Data/"; 
    private const string newItemPath = "Assets/_GameAssets/Data/CraftingItems/"; 

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

        graphRoot = new ScrollView();
        rootVisualElement.Add(graphRoot);

        rootVisualElement.Add(GetCreateItemUI());

        var newItemDBName = new TextField("New item database name: Assets/_GameAssets/Data/");
        rootVisualElement.Add(newItemDBName);

        var newItemDBButton = new Button();
        newItemDBButton.text = "Create new item database";
        newItemDBButton.clicked += () => CreateNewGraph(newDatabasePath, newItemDBName.text);
        //newItemDBButton.clicked += () => SaveScriptableObjectAsset(CreateInstance<CraftingItemDatabase>(), newDatabasePath, newItemDBName.text);
        rootVisualElement.Add(newItemDBButton);

        var checkForLoopsButton = new Button();
        checkForLoopsButton.text = "Check for prerequisite loops";
        checkForLoopsButton.clicked += () => CheckAllForPrerequisiteLoops((CraftingItemDatabase)itemDatabaseField.value);
        rootVisualElement.Add(checkForLoopsButton);

        tierListScrollView = new ScrollView();
        rootVisualElement.Add(tierListScrollView);
    }

    private void OnItemDatabaseChanged(CraftingItemDatabase database)
    {
        if(!database)
        {
            return;
        }

        itemDatabase = database;

        UpdateTierGraph(itemDatabase);

        var itemsSortedByTier = new List<CraftingItemData>(database.ItemList);
        itemsSortedByTier.Sort((a, b) => a.Tier.CompareTo(b.Tier));
        foreach(var itemData in itemsSortedByTier)
        {
            var node = CreateGraphNode(itemData);
            tierListScrollView.Add(node);
        }
    }

    private VisualElement UpdateTierGraph(CraftingItemDatabase itemDatabase)
    {
        itemDatabase.UpdateItemTiers();

        var rows = new List<VisualElement>();
        foreach(var itemData in itemDatabase.ItemList)
        {
            if(!itemData)
            {
                continue;
            }

            //add extra rows to match item tier if necessary
            while(rows.Count <= itemData.Tier)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 10;
                rows.Add(row);
            }

            var itemField = new ItemTierGraphNode(itemData);
            rows[itemData.Tier].Add(itemField);
        }

        graphRoot.Clear();
        foreach (var row in rows)
        {
            graphRoot.Add(row);
        }

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

    private VisualElement GetCreateItemUI()
    {
        var itemData = CraftingItemData.CreateInstance<CraftingItemData>();
        var idSO = new SerializedObject(itemData);

        var rootElement = new Box();

        var header = new Label("Create new item");
        rootElement.Add(header);

        var itemNameField = new PropertyField();
        rootElement.Add(itemNameField);
        itemNameField.bindingPath = "itemName";
        itemNameField.Bind(idSO);

        var imageField = new PropertyField();
        rootElement.Add(imageField);
        imageField.bindingPath = "thumbnailTex";
        imageField.Bind(idSO);

        var prereqField = new PropertyField();
        rootElement.Add(prereqField);
        prereqField.bindingPath = "prerequisites";
        prereqField.Bind(idSO);

        var extraProductField = new PropertyField();
        rootElement.Add(extraProductField);
        extraProductField.bindingPath = "products";
        extraProductField.Bind(idSO);
        extraProductField.label = "Extra products on craft";

        var createItemButton = new Button();
        rootElement.Add(createItemButton);
        createItemButton.text = "Create";
        createItemButton.clicked += () =>
        {
            idSO.ApplyModifiedProperties();
            SaveScriptableObjectAsset(itemData, newItemPath, "CraftData_" + idSO.FindProperty("itemName").stringValue);
        };

        return rootElement;
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

    private void CreateNewGraph(string path, string assetName)
    {
        var fileExtensionIdx = assetName.IndexOf('.');
        if(fileExtensionIdx > 0)
        {
            assetName = assetName.Substring(0, fileExtensionIdx);
        }

        assetName += "." + CraftingItemDatabaseGraph.AssetExtension;

        Unity.GraphToolkit.Editor.GraphDatabase.CreateGraph<CraftingItemDatabaseGraph>(newDatabasePath + assetName);
    }

    private void SaveScriptableObjectAsset<T>(T obj, string path, string assetName) where T : ScriptableObject
    {
        if (!assetName.EndsWith(".asset"))
        {
            assetName += ".asset";
        }

        AssetDatabase.CreateAsset(obj, path + assetName);
        AssetDatabase.SaveAssets();
    }
}
