using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyRewardSource : RewardSource
{
    [SerializeField]
    Transform agentLocation;

    [SerializeField]
    Transform targetLocation;

    public override float Reward => Mathf.Clamp01(1.5f-(agentLocation.position-targetLocation.position).magnitude);

    public override void OnAgentInitialize()
    {

    }
}
