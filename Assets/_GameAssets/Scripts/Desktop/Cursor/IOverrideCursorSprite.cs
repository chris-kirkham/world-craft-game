using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public interface IOverrideCursorSprite
{
    public class CursorOverrideEvent
    {

    }

    public Cursor.EventID OverrideOnInputEvent { get; }

    public Sprite CursorSpriteOverride { get; }

    //public CursorOverrideEvent GetCursorOverrideEvent();
}
