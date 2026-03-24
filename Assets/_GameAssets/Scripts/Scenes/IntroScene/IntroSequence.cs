using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEditor.Progress;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private CameraMovement playerCam;
    [SerializeField] private FadeInOutText topText;
    [SerializeField] private List<IntroSeqCraftingItem> startingItemChoices;
    [SerializeField] private List<Transform> itemSpawnTransforms;
    [SerializeField] private CrafterBoard crafterBoard;
    [SerializeField] private GameObject postIntroLighting;

    private const float spawnAnimFlipTime = 0.1f;
    private const float spawnAnimWaitTime = 0.25f;
    private const float moveToBoardAnimWaitTime = 0.2f;

    private void OnEnable()
    {
        if(playerCam)
        {
            //playerCam.SetMovementEnabled(false, false);
        }
        else
        {
            playerCam = FindFirstObjectByType<CameraMovement>();
            if(playerCam)
            {
                //playerCam.SetMovementEnabled(false, false);
            }
            else
            {
                Debug.LogError($"No {nameof(CameraMovement)} set and none found in scene!");
            }
        }

        if(!crafterBoard)
        {
            crafterBoard = FindFirstObjectByType<CrafterBoard>();
        }

        if(postIntroLighting)
        {
            postIntroLighting.SetActive(false);
        }
    }

    public void OnChooseItem(CraftingItem item)
    {
        StartCoroutine(SpawnChosenItemsRoutine(item.Data));
    }

    private IEnumerator SpawnChosenItemsRoutine(CraftingItemData chosenItemData)
    {
        playableDirector.Stop();
        topText.PlayClipThenDisable();
        yield return new WaitForSeconds(spawnAnimWaitTime);

        //spawn copies of chosen item at spawn positions
        var spawnedItems = new List<CraftingItem>(startingItemChoices.Count);
        foreach(var startingItem in startingItemChoices)
        {
            var startingItemTform = startingItem.transform;

            if (startingItem.ItemData == chosenItemData) 
            {
                //if chosen item is this one, replace it with the actual item without doing the flip anim
                var spawnedItem = CraftingManager.Inst.SpawnItem(chosenItemData, startingItemTform.position, startingItemTform.rotation);
                spawnedItem.SetState(CraftingItem.State.Animatable);
                spawnedItems.Add(spawnedItem);
                Destroy(startingItem.gameObject);
                yield return new WaitForSeconds((spawnAnimFlipTime * 2f) + spawnAnimWaitTime);
            }
            else //do spawn animation
            {
                //roll starting item card so its item type isn't visible by the camera
                var cam = Cursor.Inst.Cam;
                var itemToCam = cam.transform.position - startingItem.transform.position;
                var itemToCamYFlat = Vector3.ProjectOnPlane(itemToCam, Vector3.forward);
                var angle = Vector3.Angle(Vector3.right, itemToCamYFlat.normalized);
                yield return startingItemTform.DORotate(new Vector3(0f, 0f, angle), spawnAnimFlipTime)
                    .SetLink(startingItem.gameObject, LinkBehaviour.KillOnDestroy)
                    .WaitForCompletion();

                //spawn chosen item at same position and rotation so it looks seamless (hopefully!)
                var spawnedItem = CraftingManager.Inst.SpawnItem(chosenItemData, startingItemTform.position, startingItemTform.rotation);
                spawnedItem.SetState(CraftingItem.State.Animatable);
                spawnedItems.Add(spawnedItem);    

                Destroy(startingItem.gameObject);

                //roll chosen item back to flat
                yield return spawnedItem.transform.DORotate(new Vector3(0f, 0f, 180f), spawnAnimFlipTime).WaitForCompletion();
                yield return new WaitForSeconds(spawnAnimFlipTime);
            }
        }

        //move spawned items to grid
        if(crafterBoard)
        {
            foreach(var item in spawnedItems)
            {
                crafterBoard.MoveItemToGrid(item);
                yield return new WaitForSeconds(moveToBoardAnimWaitTime);
            }
        }

        postIntroLighting.SetActive(true);
        //playerCam.SetMovementEnabled(true, true);
        enabled = false;
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
