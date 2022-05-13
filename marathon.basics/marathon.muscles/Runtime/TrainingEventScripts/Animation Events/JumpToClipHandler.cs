using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpToClipHandler : DelayableEventHandler
{
    [SerializeField]
    Animator animator;
    [SerializeField]
    string clipName;
    public override EventHandler Handler => JumpToClip;

    [SerializeField]
    float normalizedTimeRange;

    void JumpToClip(object sender, EventArgs args)
    {
        
        if (IsWaiting) return;
        if(framesToWait!=0)
        {
            StartCoroutine(DelayedExecution(sender, args));
            return;
        }

        Debug.Log("Resetting clip undelayed!");
        animator.Play(stateName:clipName, layer: 0, normalizedTime: UnityEngine.Random.Range(0f, normalizedTimeRange));
    }

    protected override IEnumerator DelayedExecution(object sender, EventArgs args)
    {
        IsWaiting = true;
        yield return WaitFrames();
        Debug.Log("Resetting clip delayed!");
        animator.Play(stateName: clipName, layer: 0, normalizedTime: UnityEngine.Random.Range(0f, normalizedTimeRange));
        IsWaiting = false;
    }
}
