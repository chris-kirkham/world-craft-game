using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Crafting
{
    public class OnCraftSequence : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private TimelineAsset startSequence;
        [SerializeField] private OnCraftConvergeSequence convergeSequence;
        [SerializeField] private TimelineAsset postConvergeSequence;
        [SerializeField] private OnCraftSpawnItemSequence spawnItemSequence;
        [SerializeField] private TimelineAsset finishSequence;

        public void DoCraftSequence(ICollection<CraftingItem> ingredients, List<CraftingItemData> itemsToSpawn)
        {
            convergeSequence.Ingredients = ingredients;
            spawnItemSequence.Ingredients = ingredients;
            spawnItemSequence.ItemsToSpawn = itemsToSpawn;
            StartCoroutine(CraftSequenceRoutine());
        }

        private IEnumerator CraftSequenceRoutine()
        {
            Debug.Assert(playableDirector);

            yield return TimelineX.PlayTimelineAndWaitForFinish(playableDirector, startSequence);
            yield return convergeSequence.ConvergeIngredients();
            yield return TimelineX.PlayTimelineAndWaitForFinish(playableDirector, postConvergeSequence);
            yield return spawnItemSequence.SpawnItems();
            yield return TimelineX.PlayTimelineAndWaitForFinish(playableDirector, finishSequence);

            gameObject.SetActive(false);
        }
    }
}
