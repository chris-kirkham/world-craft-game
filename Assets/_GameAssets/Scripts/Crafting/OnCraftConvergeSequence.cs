using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Crafting
{
    [System.Serializable]
    public class OnCraftConvergeSequence
    {
        [SerializeField] private Transform convergePoint;
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private TimelineAsset postMoveToOuterSequence;
        [SerializeField, Tooltip("Amount each item should be raised when converging to avoid z-fighting (and to look good)")]
        private float itemUpIncrement = 0.1f;
        [SerializeField] private float outerRadius = 1f;
        [SerializeField] private float lerpToOuterTime = 0.75f;
        [SerializeField] private float convergeInCentreTime = 0.5f;

        public ICollection<CraftingItem> Ingredients { private get; set; }

        public IEnumerator ConvergeIngredients()
        {
            if (Ingredients.Count == 0)
            {
                yield break;
            }

            var convergePos = Vector3.zero;
            if (convergePoint)
            {
                convergePos = convergePoint.position;
            }
            else
            {
                Debug.LogWarning($"{nameof(convergePoint)} not set - converge position will be (0,0,0)!");
            }

            Cursor.Inst.SetAllowInput(false);

            var ingredientsList = new List<CraftingItem>(Ingredients);
            var numIngredients = Ingredients.Count;
            var angleInc = 360f / Ingredients.Count;
            var upInc = Vector3.up * 0.1f; //add a small vertical increment to each card to avoid z-fighting (and to look nice)

            //lerp ingredients to outer positions
            var startPositions = new Vector3[numIngredients];
            var targetPositions = new Vector3[numIngredients];
            for (int i = 0; i < numIngredients; i++)
            {
                startPositions[i] = ingredientsList[i].transform.position + (upInc * i);
                targetPositions[i] = convergePos + (upInc * i) + (Quaternion.Euler(0f, angleInc * i, 0f) * Vector3.left * outerRadius);
            }

            for (int i = 0; i < numIngredients; i++)
            {
                ingredientsList[i].transform.DOMove(targetPositions[i], lerpToOuterTime);
            }
            yield return new WaitForSeconds(lerpToOuterTime);

            //N.B. this should be simultaneous with converging 
            if(playableDirector && postMoveToOuterSequence)
            {
                playableDirector.Play(postMoveToOuterSequence);
            }

            //make ingredients converge in the centre (and shrink)!
            for (int i = 0; i < numIngredients; i++)
            {
                var ingredientTform = ingredientsList[i].transform;
                Tweening.DoTransform(ingredientTform, convergePos + (upInc * i), ingredientTform.rotation, Vector3.zero, convergeInCentreTime);
            }
            yield return new WaitForSeconds(convergeInCentreTime);
        }
    }
}
