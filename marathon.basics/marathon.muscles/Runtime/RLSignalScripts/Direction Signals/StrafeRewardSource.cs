using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinematic;

public class StrafeRewardSource : RewardSource
{
    [SerializeField]
    Transform strafeDirection;

    [SerializeField]
    Transform frameOfReference;
    public override float Reward => CalculateReward();

    IKinematic trackedBody;

    private float CalculateReward()
    {
        Vector2 desiredDirection = frameOfReference.InverseTransformDirection(strafeDirection.forward).Horizontal().normalized;
        Vector2 actualCOMVdirection = frameOfReference.InverseTransformDirection(trackedBody.Velocity).Horizontal().normalized;

        float angle = Vector2.Angle(desiredDirection, actualCOMVdirection);
        float normed = angle / 90f;

        return Mathf.Clamp(1 - normed, 0.5f, 1f);

    }

    public override void OnAgentInitialize()
    {
        trackedBody = frameOfReference.GetComponent<ArticulationBody>() ? (IKinematic) new ArticulationBodyAdapter(frameOfReference.GetComponent<ArticulationBody>()) : (IKinematic)new RigidbodyAdapter(frameOfReference.GetComponent<Rigidbody>());
    }
}
