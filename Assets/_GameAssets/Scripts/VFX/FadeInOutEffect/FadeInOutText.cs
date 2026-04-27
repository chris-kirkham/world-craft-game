using System.Collections;
using TMPro;
using UnityEngine;

public class FadeInOutText : FadeInOut
{
    [SerializeField] private TextMeshProUGUI text;

    /// <summary>
    /// Amount should be in range [0, 1]
    /// </summary>
    /// <param name="amount"></param>
    protected override void SetFadeAmount(float amount)
    {
        var col = text.color;
        text.color = new Color(col.r, col.g, col.b, amount);
    }
}
