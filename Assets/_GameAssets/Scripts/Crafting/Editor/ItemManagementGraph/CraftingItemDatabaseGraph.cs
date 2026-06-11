using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

[Graph(AssetExtension)]
[Serializable]
public class CraftingItemDatabaseGraph : Graph
{
    public const string AssetExtension = "itemgraph";

    [MenuItem("Assets/Create/Crafting/CraftingDatabaseGraph", false)]
    static void CreateAssetFile()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<CraftingItemDatabaseGraph>();
    }

}
