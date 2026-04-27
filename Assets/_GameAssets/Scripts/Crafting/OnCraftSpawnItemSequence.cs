using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace Crafting
{
    [System.Serializable]
    public class OnCraftSpawnItemSequence
    {
        [SerializeField] private CrafterBoard craftingBoard;
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private TimelineAsset onItemSpawnSequence;
        [SerializeField] private ItemInspectSequence inspectSequence;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float multiItemSpawnDelay = 0.1f;
        [SerializeField] private float onSpawnInspectTime = 1f;
        [SerializeField] private float onSpawnMoveToEndPosTime = 1f;
        [SerializeField] private bool inspectItemOnFirstCraft = true;

        public ICollection<CraftingItem> Ingredients { private get; set; }

        public List<CraftingItemData> ItemsToSpawn { private get; set; }

        public IEnumerator SpawnItems()
        {
            foreach (var ingredient in Ingredients)
            {
                ingredient.OnCraftAttempt(CraftingManager.CraftingResultState.SuccessfulCraft);
            }

            for (int i = 0; i < ItemsToSpawn.Count; i++)
            {
                yield return SpawnItem(ItemsToSpawn[i]);
                yield return new WaitForSeconds(multiItemSpawnDelay);
            }

            Cursor.Inst.SetAllowInput(true);
        }

        private IEnumerator SpawnItem(CraftingItemData item)
        {
            Debug.Log($"Successfully crafted {item.ItemName} from ingredients " + string.Join(", ", Ingredients) + "!");
            
            var craftingManager = CraftingManager.Inst;

            var SpawnPos = Vector3.zero;
            if (spawnPoint)
            {
                SpawnPos = spawnPoint.position;
            }
            else
            {
                Debug.LogWarning($"{nameof(spawnPoint)} not set - items will spawn at (0,0,0)!");
            }

            //spawn item
            yield return SpawnItemToGridRoutine(item, SpawnPos, Quaternion.identity);

            //TODO: "new WaitForSeconds" allocates - make a helper class to get WaitForSecondses without allocation
            yield return new WaitForSeconds(multiItemSpawnDelay);

            //spawn any extra products that are created when this item spawns
            if (item.ExtraProducts.Count > 0)
            {
                foreach (var extraProduct in item.ExtraProducts)
                {
                    yield return SpawnItemToGridRoutine(extraProduct, SpawnPos, Quaternion.identity);
                    Debug.Log($"Spawned extra product {extraProduct.ItemName} from craft result {item.ItemName}!");
                    yield return new WaitForSeconds(multiItemSpawnDelay);
                }
            }
        }

        public IEnumerator SpawnItemToGridRoutine(CraftingItemData itemData, Vector3 startPos_WS, Quaternion startRotation_WS)
        {
            var craftingManager = CraftingManager.Inst;

            if(!craftingManager)
            {
                Debug.Assert(craftingManager);
                yield break;
            }

            if (playableDirector && onItemSpawnSequence)
            {
                //N.B. we don't wait for this to complete; it should play simultaneously to the inspect/move to grid stuff
                playableDirector.Play(onItemSpawnSequence);
            }

            var wasCraftedPreviously = craftingManager.WasItemCraftedPreviously(itemData);
            var item = craftingManager.SpawnItem(itemData, startPos_WS, startRotation_WS, doItemOnCraftedCallback: true);
            
            if(!wasCraftedPreviously && inspectItemOnFirstCraft)
            {
                yield return inspectSequence.InspectItem(item);
            }

            craftingManager.MoveItemToGrid(item);
        }
    }
}
