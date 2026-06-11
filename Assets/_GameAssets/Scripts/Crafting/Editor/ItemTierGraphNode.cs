using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

public class ItemTierGraphNode : VisualElement
{
    private CraftingItemData item;

    private VisualElement root;
    private Label itemName;
    private Label itemTier;
    private Image itemSprite;
    private Foldout prereqsFoldout;
    private Foldout extraProductsFoldout;
    private PropertyField prereqsProp;
    private PropertyField extraProductsProp;

    public ItemTierGraphNode(CraftingItemData itemData)
    {
        this.item = itemData;

        if(!item)
        {
            root.Add(new Label("ITEM IS NULL!"));
            return;
        }

        var itemSO = new SerializedObject(item);

        root = new VisualElement();
        root.style.minWidth = 128;
        root.style.minHeight = 128;
        root.style.backgroundColor = new StyleColor(Color.cornflowerBlue);
        root.style.borderLeftWidth = 10;
        root.style.borderRightWidth = 10;
        root.style.borderTopWidth = 10;
        root.style.borderBottomWidth = 10;
        Add(root);

        itemName = new Label(item.ItemName);
        root.Add(itemName);

        itemTier = new Label(item.Tier.ToString());
        root.Add(itemTier);

        itemSprite = new Image();
        itemSprite.image = item.ThumbnailTex ? item.ThumbnailTex : Resources.Load<Texture2D>("TX_Error_Sprite");
        const float imgSize = 100;
        itemSprite.style.minWidth = imgSize;
        itemSprite.style.minHeight = imgSize;
        itemSprite.style.maxWidth = imgSize;
        itemSprite.style.maxHeight = imgSize;
        root.Add(itemSprite);
        
        prereqsFoldout = new Foldout();
        prereqsProp = new PropertyField(itemSO.FindProperty("prerequisites"));
        prereqsFoldout.Add(prereqsProp);
        root.Add(prereqsFoldout);

        extraProductsFoldout = new Foldout();
        extraProductsProp = new PropertyField(itemSO.FindProperty("extraProducts"));
        extraProductsFoldout.Add(extraProductsProp);
        root.Add(extraProductsFoldout);
    }
}
