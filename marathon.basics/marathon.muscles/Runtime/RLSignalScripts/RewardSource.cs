using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public abstract class RewardSource: MonoBehaviour
{
    public abstract float Reward { get;}

    public abstract void OnAgentInitialize();
}

[Serializable]
public class WeightedRewardSource
{
    [SerializeField]
    private RewardSource source;
    [SerializeField]
    private float weight;

    public float Weight { get => weight; }
    public float Reward { get => source.Reward; }

    public void OnAgentInitialize()
    {
        source.OnAgentInitialize();
    }


    public bool IsEmpty()
    {
        return source == null;
    }
}




