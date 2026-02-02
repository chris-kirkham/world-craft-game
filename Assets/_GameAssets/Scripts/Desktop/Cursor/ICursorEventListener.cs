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

    public void OnCursorEvent(Cursor.CursorEvent e);
}
