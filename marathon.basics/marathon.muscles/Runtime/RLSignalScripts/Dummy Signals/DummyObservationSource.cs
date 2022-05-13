using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DummyObservationSource : ObservationSource
{
    [SerializeField]
    Transform agentLocation;

    [SerializeField]
    Transform targetLocation;

    public override int Size => 6;

    public override void FeedObservationsToSensor(VectorSensor sensor)
    {
        sensor.AddObservation(targetLocation.position-agentLocation.position);
        sensor.AddObservation(agentLocation.position);
    }

    public override void OnAgentInitialize()
    {
       
    }
}
