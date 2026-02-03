using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private CameraMovement playerCam;
    [SerializeField] private FadeInOutText topText;
    [SerializeField] private List<IntroSeqCraftingItem> startingItemChoices;
    [SerializeField] private List<Transform> itemSpawnTransforms;

    private void OnEnable()
    {
        if(playerCam)
        {
            playerCam.SetMovementEnabled(false);
        }
        else
        {
            playerCam = FindFirstObjectByType<CameraMovement>();
            if(playerCam)
            {
                playerCam.SetMovementEnabled(false);
            }
            else
            {
                Debug.LogError($"No {nameof(CameraMovement)} set and none found in scene!");
            }
        }
    }

    private void OnDisable()
    {
        if(playerCam)
        {
            playerCam.SetMovementEnabled(true);
        }
    }

    public void OnChooseItem(CraftingItem item)
    {
        playableDirector.Stop();

        //spawn copies of chosen item at spawn positions
        foreach(var t in itemSpawnTransforms)
        {
            CraftingManager.Inst.InstantiateItem(item.Data, t.position, t.rotation);
        }

        //delete starting items
        foreach(var startingItem in startingItemChoices)
        {
            Destroy(startingItem.gameObject);
        }

        topText.PlayClipThenDisable();
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
        foreach(var t in itemSpawnTransforms)
        {
            Gizmos.DrawSphere(t.position, 0.1f);
        }
    }
}
