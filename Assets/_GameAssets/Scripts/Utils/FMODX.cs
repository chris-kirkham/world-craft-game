using UnityEngine;
using FMODUnity;

public static class FMODX 
{
    public static void PlayOneShotAtPosition(EventReference eventRef, Vector3 position)
    {
        RuntimeManager.PlayOneShot(eventRef, position);
    }

    public static void PlayOneShotAttached(EventReference eventRef, GameObject attachedObj)
    {
        RuntimeManager.PlayOneShotAttached(eventRef, attachedObj);
    }
}
