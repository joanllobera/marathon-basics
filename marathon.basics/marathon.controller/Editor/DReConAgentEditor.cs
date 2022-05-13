using DReCon;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DReConAgent))]
public class DReConAgentEditor : Editor
{

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GUILayout.Label("");




        base.OnInspectorGUI();


        if (GUILayout.Button("Calculate observation space and action space size"))
        {
            DReConAgent t = target as DReConAgent;
            Debug.Log($"Action space size: {t.ActionSpaceSize}");
            Debug.Log($"Observation space size: {t.ObservationSpaceSize}");
        }

        if (GUILayout.Button("Update observation space and action space size"))
        {
            DReConAgent t = target as DReConAgent;

            BehaviorParameters bp = t.GetComponent<BehaviorParameters>();

            Unity.MLAgents.Actuators.ActionSpec myActionSpec = bp.BrainParameters.ActionSpec;

            myActionSpec.NumContinuousActions = t.ActionSpaceSize;
            bp.BrainParameters.ActionSpec = myActionSpec;

            bp.BrainParameters.VectorObservationSize = t.ObservationSpaceSize;
        }



        serializedObject.ApplyModifiedProperties();

    }


}
