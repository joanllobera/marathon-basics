using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapRangeOfMotion2Constraints))]
public class MapRangeOfMotion2ConstraintsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GUILayout.Label("");




        base.OnInspectorGUI();


        if (GUILayout.Button("Apply Constraints"))
        {
            MapRangeOfMotion2Constraints t = target as MapRangeOfMotion2Constraints;
            t.ApplyRangeOfMotionToRagDoll();

        }



        serializedObject.ApplyModifiedProperties();

    }


}
