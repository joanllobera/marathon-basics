using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Kinematic;

public class StrafeObservationSource : ObservationSource
{
    [SerializeField]
    Transform strafeDirection;

    [SerializeField]
    Transform frameOfReference;

    IKinematic trackedBody;

    public override int Size => 2;

    private void Awake()
    {
        trackedBody = frameOfReference.GetComponent<ArticulationBody>() ? (IKinematic) new ArticulationBodyAdapter(frameOfReference.GetComponent<ArticulationBody>()) : new RigidbodyAdapter(frameOfReference.GetComponent<Rigidbody>());
    }

    public override void FeedObservationsToSensor(VectorSensor sensor)
    {
        Vector2 diff = frameOfReference.InverseTransformDirection(strafeDirection.forward).Horizontal().normalized - frameOfReference.InverseTransformDirection(trackedBody.Velocity).Horizontal().normalized;
        sensor.AddObservation(diff);
    }

    public override void OnAgentInitialize()
    {

    }
}
