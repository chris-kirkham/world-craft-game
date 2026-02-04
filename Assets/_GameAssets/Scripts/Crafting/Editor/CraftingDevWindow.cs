using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class CraftingDevWindow : EditorWindow
{
    private List<CraftingItemDatabase> itemDatabases = new List<CraftingItemDatabase>();

    private VisualElement searchResultsRoot;

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

        searchResultsRoot = new VisualElement();
        rootVisualElement.Add(searchResultsRoot);
    }

    private void DoItemSearch(string searchString)
    {
        searchResultsRoot.Clear();

        foreach(var database in itemDatabases)
        {
            var databaseNameLabel = new Label(database.name + ":");
            searchResultsRoot.Add(databaseNameLabel);

            foreach (var item in database.ItemList)
            {
                var additionalInfoText = "";

                var isMatch = IsEqualToSearchString(item.ItemName); 
                foreach (var prereq in item.Prerequisites)
                {
                    if(isMatch)
                    {
                        break;
                    }

                    isMatch |= IsEqualToSearchString(prereq.ItemName);

                    if(isMatch)
                    {
                        additionalInfoText += "(prerequisite";
                    }
                }   

                foreach(var product in item.ExtraProducts)
                {
                    if(isMatch)
                    {
                        break;
                    }

                    isMatch |= IsEqualToSearchString(product.ItemName);

                    if (isMatch)
                    {
                        additionalInfoText += additionalInfoText.Length > 0 ? ", " : "(";
                        additionalInfoText += "extra product";
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

        bool IsEqualToSearchString(string str)
        {
            return str.Equals(searchString, System.StringComparison.OrdinalIgnoreCase);
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
