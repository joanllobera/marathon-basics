using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flexible base class that can supply a handling function when paired up with a TrainingEvent
/// </summary>
public abstract class TrainingEventHandler : MonoBehaviour
{
    public abstract EventHandler Handler { get; }
}

public abstract class DelayableEventHandler: TrainingEventHandler
{
    [SerializeField]
    protected int framesToWait;

    [SerializeField]
    private bool isWaiting;

    public bool IsWaiting { get => isWaiting; set => isWaiting = value; }

    protected abstract IEnumerator DelayedExecution(object sender, EventArgs args);

    protected IEnumerator WaitFrames()
    {
        for (int i = 0; i < framesToWait+1; i++) yield return new WaitForFixedUpdate();
    }
}