using UnityEngine;

public class FadeInOutMaterialOpacity : FadeInOut
{
    [SerializeField] private Renderer r;
    [SerializeField] private string colourPropName;

    private Material mat;
    private int propID;

    private void Awake()
    {
        if(r)
        {
            mat = r.material;
        }
        else
        {
            Debug.LogError($"Renderer not set!");
        }

        propID = Shader.PropertyToID(colourPropName);
    }

    protected override void SetFadeAmount(float amount)
    {
        if(mat)
        {
            var col = mat.GetColor(propID);
            col = new Color(col.r, col.g, col.b, amount);
            mat.SetColor(propID, col);
        }
    }
}
