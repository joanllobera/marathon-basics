using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(MapAnim2Ragdoll))]
public class MapAnim2RagdollEditor : Editor
{

    [SerializeField]
    SerializedProperty Ragdoll;

    void OnEnable()
    {
        Ragdoll = serializedObject.FindProperty("MapAnim2Ragdoll");

    }




    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GUILayout.Label("");




        base.OnInspectorGUI();

        
        if (GUILayout.Button("Add runtime ragdoll to hierarchy"))
        {
            MapAnim2Ragdoll t = target as MapAnim2Ragdoll;
            t.DynamicallyCreateRagdollForMocap();
        }



        serializedObject.ApplyModifiedProperties();

    }





}
