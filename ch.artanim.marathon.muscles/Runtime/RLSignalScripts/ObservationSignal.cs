using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ObservationSignal : MonoBehaviour
{
    [SerializeField]
    private List<ObservationSource> observationSources;

    public int Size => observationSources.Select(s => s.Size).Sum();

    /// <summary>Each StateSource of the StateSignal adds its relevant observations to the provided sensor.</summary>
    public void PopulateObservations(VectorSensor sensor)
    {
        foreach (ObservationSource observationSource in observationSources.Where(obs => obs !=null))
        {
            observationSource.FeedObservationsToSensor(sensor);
        }
    }

    public void OnAgentInitialize()
    {
        foreach (ObservationSource observationSource in observationSources.Where(obs => obs != null))
        {
            observationSource.OnAgentInitialize();
        }
    }


}

