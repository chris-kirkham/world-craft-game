using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TMPTypewriterText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private float typewriterTimePerChar = 0.1f;

    private Coroutine typewriterRoutine;

    public void OnEnable()
    {
        DoTypewriterText(typewriterTimePerChar);  
    }

    public void DoTypewriterText(float timePerCharacter)
    {
        if(typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        if(timePerCharacter > 0f)
        {
            StartCoroutine(TypewriterRoutine(timePerCharacter));
        }
    }

    private IEnumerator TypewriterRoutine(float timePerCharacter)
    {
        var strLength = tmpText.text.Length;
        tmpText.maxVisibleCharacters = 0;
        var waitForSeconds = new WaitForSeconds(timePerCharacter);
        for(int i = 1; i <= strLength; i++)
        {
            yield return waitForSeconds;
            tmpText.maxVisibleCharacters = i;
        }

        tmpText.maxVisibleCharacters = 99999; //this is the default value TMPro uses
    }
}
