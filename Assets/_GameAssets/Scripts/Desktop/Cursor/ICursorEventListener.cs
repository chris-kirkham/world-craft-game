using System;
using UnityEngine;

public interface ICursorEventListener
{ 
    /*
    //TODO: ???
    [Flags]
    public enum EventsToListenFor
    {
        OnlyOnThisObject = 1,
        InParents = 1 << 1,
        InChildren = 1 << 2,
        MAX = 1 << 3
    }
    */

    public void RegisterListener()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
        else
        {
            Debug.LogError($"No instance of {nameof(Cursor)} found! Cannot register listener.");
        }
    }

    public void DeregisterListener()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    public void OnCursorEvent(Cursor.CursorEvent e);
}
