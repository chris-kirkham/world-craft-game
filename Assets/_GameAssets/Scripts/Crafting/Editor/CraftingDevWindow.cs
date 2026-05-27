using Crafting;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

public class CraftingDevWindow : EditorWindow
{
    private List<CraftingItemDatabase> itemDatabases = new List<CraftingItemDatabase>();

    private ScrollView searchResultsRoot;
    private VisualElement possibleCraftsRoot;

    [MenuItem("Window/Crafting/Crafting Helper")]
    public static void CreateWindow()
    {
        var window = GetWindow<CraftingDevWindow>();
        window.titleContent = new GUIContent("Crafting Helper");

        window.minSize = new Vector2(300, 200);
    }

    public void CreateGUI()
    {
        FetchItemDatabases();

        var searchBox = new ToolbarSearchField();
        searchBox.placeholderText = "search for an item!";
        searchBox.RegisterValueChangedCallback(evt => DoItemSearch(evt.newValue));
        rootVisualElement.Add(searchBox);

        var showPossibleCraftsButton = new Button();
        showPossibleCraftsButton.clicked += ShowPossibleCrafts;
        showPossibleCraftsButton.Add(new Label("Show possible crafts"));
        rootVisualElement.Add(showPossibleCraftsButton);

        searchResultsRoot = new ScrollView();
        rootVisualElement.Add(searchResultsRoot);

        possibleCraftsRoot = new ScrollView();
        rootVisualElement.Add(possibleCraftsRoot);

    }

    private void DoItemSearch(string searchString)
    {
        searchResultsRoot.Clear();

        if(string.IsNullOrEmpty(searchString))
        {
            return;
        }

        foreach(var database in itemDatabases)
        {
            var databaseNameLabel = new Label(database.name + ":");
            searchResultsRoot.Add(databaseNameLabel);

            foreach (var item in database.ItemList)
            {
                var additionalInfoText = "";

                var isMatch = ContainsSearchString(item.ItemName); 
                foreach (var prereq in item.Prerequisites)
                {
                    var prereqMatch = ContainsSearchString(prereq.ItemName);
                    if (prereqMatch)
                    {
                        additionalInfoText += $"({prereq.ItemName} is prerequisite";
                        isMatch |= prereqMatch;
                    }
                }   

                foreach(var product in item.ExtraProducts)
                {
                    var additionalProductMatch = ContainsSearchString(product.ItemName);
                    if (additionalProductMatch)
                    {
                        additionalInfoText += additionalInfoText.Length > 0 ? ", " : "(";
                        additionalInfoText += $"{product.ItemName} is extra product";
                        isMatch |= additionalProductMatch;
                    }
                }

                if(isMatch)
                {
                    var div = new VisualElement();
                    div.style.flexDirection = FlexDirection.Row;
                    div.style.flexWrap = Wrap.NoWrap;
                    
                    var assetLink = new ObjectField();
                    assetLink.SetValueWithoutNotify(item);
                    div.Add(assetLink);

                    if(additionalInfoText.Length > 0)
                    {
                        additionalInfoText += ")";
                        var additionalInfoLabel = new Label(additionalInfoText);
                        div.Add(additionalInfoLabel);
                    }

                    searchResultsRoot.Add(div);
                }
            }
        }

        bool ContainsSearchString(string str)
        {
            return !string.IsNullOrEmpty(searchString) && str.Contains(searchString, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ShowPossibleCrafts()
    {
        possibleCraftsRoot.Clear();

        if (CraftingManager.InstExists())
        {
            var possibleCrafts = CraftingManager.Inst.FindPossibleCrafts(includeAlreadyCraftedItems: !GameplaySettings.InfiniteDecks); 
            foreach(var itemData in possibleCrafts)
            {
                var assetLink = new ObjectField();
                assetLink.SetValueWithoutNotify(itemData);
                possibleCraftsRoot.Add(assetLink);
            }
        }
        else
        {
            possibleCraftsRoot.Add(new Label("Game must be in progress to display possible crafts!"));
        }
    }
    
    private void FetchItemDatabases()
    {
        var itemDatabaseGUIDs = AssetDatabase.FindAssets($"t:{nameof(CraftingItemDatabase)}");
        itemDatabases.Clear();
        foreach (var guid in itemDatabaseGUIDs)
        {
            itemDatabases.Add(AssetDatabase.LoadAssetAtPath<CraftingItemDatabase>(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }
}
