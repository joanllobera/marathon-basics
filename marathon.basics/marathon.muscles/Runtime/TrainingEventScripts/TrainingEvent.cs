using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flexible base class that can be inherited for interfacing with different handlers
/// </summary>
public abstract class TrainingEvent : MonoBehaviour
{

    protected event EventHandler eventHandler;
    public virtual void SubscribeHandler(EventHandler subscriber)
    {
        eventHandler += subscriber;
    }

    public virtual void UnsubscribeHandler(EventHandler subscribed)
    {
        eventHandler -= subscribed;
    }

    protected virtual void OnTrainingEvent(EventArgs e)
    {
        eventHandler?.Invoke(this, e);
    }


    public void ManuallyTrigger(EventArgs e)
    {
        OnTrainingEvent(e);
    }

}
