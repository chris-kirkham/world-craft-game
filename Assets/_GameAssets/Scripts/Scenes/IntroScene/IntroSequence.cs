using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using Crafting;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private float preChooseItemPauseTime;
    [SerializeField] private CameraMovement playerCam;
    [SerializeField] private FadeInOutText topText;
    [SerializeField] private List<IntroSeqCraftingItem> startingItemChoices;
    [SerializeField] private List<Transform> itemSpawnTransforms;
    [SerializeField] private CrafterBoard crafterBoard;
    [SerializeField] private GameObject postIntroLighting;

    private const float spawnAnimFlipTime = 0.1f;
    private const float spawnAnimWaitTime = 0.25f;
    private const float moveToBoardAnimWaitTime = 0.2f;

    private Cursor cursor;

    private void OnEnable()
    {
        if(!playerCam)
        {
            playerCam = FindFirstObjectByType<CameraMovement>();
        }

        if(playerCam)
        {
            playerCam.SetMovementEnabled(false, false);
        }
        else
        {
            Debug.LogError($"No {nameof(CameraMovement)} set and none found in scene!");
        }

        if (!crafterBoard)
        {
            crafterBoard = FindFirstObjectByType<CrafterBoard>();
        }

        if(postIntroLighting)
        {
            postIntroLighting.SetActive(false);
        }
        
        StartCoroutine(PreChooseItemPauseRoutine());
    }

    private void Start()
    {
        cursor = Cursor.Inst;
        if(cursor)
        {
            cursor.SetAllowInput(false);
        }
    }

    private void OnDisable()
    {
        if(cursor)
        {
            cursor.SetAllowInput(true);
        }
    }

    //TODO: kind of jank - split pre- and post- item selection sequences into two timelines (annoying to do)?
    //Or use signal emitters (seems overkill for this case)
    private IEnumerator PreChooseItemPauseRoutine()
    {
        yield return new WaitForSeconds(preChooseItemPauseTime);
        playableDirector.Pause();
        cursor.SetAllowInput(true);
    }

    public void OnChooseItem(CraftingItem item)
    {
        StartCoroutine(SpawnChosenItemsRoutine(item.Data));
    }

    private IEnumerator SpawnChosenItemsRoutine(CraftingItemData chosenItemData)
    {
        cursor.SetAllowInput(false);

        topText.FadeOut();
        yield return new WaitForSeconds(spawnAnimWaitTime);
        Destroy(topText.gameObject);

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

        if(postIntroLighting)
        {
            postIntroLighting.SetActive(true);
        }

        if(playerCam)
        {
            playerCam.SetMovementEnabled(true, true);
        }

        playableDirector.Resume();

        yield return new WaitForSeconds(spawnAnimWaitTime);

        cursor.SetAllowInput(true);
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
