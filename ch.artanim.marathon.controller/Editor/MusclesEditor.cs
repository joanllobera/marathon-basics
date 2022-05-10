using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(Muscles))]
public class MusclesEditor : Editor
{

    [SerializeField]
    SerializedProperty Muscles;

    void OnEnable()
    {
        Muscles = serializedObject.FindProperty("Muscles");

    }




    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GUILayout.Label("");




        base.OnInspectorGUI();

        /*
        if (GUILayout.Button("Recalculate Center of Masses"))
        {
            Muscles t = target as Muscles;
            t.CenterABMasses();
        }*/



        serializedObject.ApplyModifiedProperties();

    }





}
