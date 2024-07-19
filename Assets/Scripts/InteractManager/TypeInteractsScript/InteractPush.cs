using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractPush : InteractCalled
{
    public override void Awake()
    {
        Type = TypeInteract.Push;
    }

    [SerializeField] private UnityEvent _unityEvent;


    public override void Interact()
    {
        _unityEvent.Invoke();
        InteractManager.Main.InteractEnd();
    }
}
