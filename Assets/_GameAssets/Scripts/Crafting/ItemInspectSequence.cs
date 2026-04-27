using DG.Tweening;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class ItemInspectSequence
{
    [SerializeField] private float lerpToInspectPosTime = 1f;
    [SerializeField] private float inspectHoldTime = 1f;

    public IEnumerator InspectItem(CraftingItem item)
    {
        item.SetState(CraftingItem.State.Animatable);
        
        var cam = Cursor.Inst.Cam;
        item.transform.localScale = Vector3.zero; //make item scale up as it animates in
        yield return item.transform.DOScale(1f, 0.5f).WaitForCompletion();
        yield return new WaitForSeconds(0.5f);
        
        item.SetState(CraftingItem.State.Animatable);
        yield return Tweening.DoTransform(
            item.transform,
            cam.transform.position + (cam.transform.forward * 2f),
            Quaternion.identity,
            Vector3.one,
            lerpToInspectPosTime).WaitForCompletion();
        item.SetOnInspectVFX(true);
        yield return new WaitForSeconds(inspectHoldTime);
        item.SetOnInspectVFX(false);
    }
}
