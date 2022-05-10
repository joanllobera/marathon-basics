using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TeleportMJ))]
public class TeleportMJEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();


        if (GUILayout.Button("Copy State"))
        {
            TeleportMJ t = target as TeleportMJ;
            t.CopyState();
        }

        if (GUILayout.Button("Teleport"))
        {
            TeleportMJ t = target as TeleportMJ;
            t.TeleportRoot();
        }

        if (GUILayout.Button("Rotate"))
        {
            TeleportMJ t = target as TeleportMJ;
            t.RotateRoot();
        }

        serializedObject.ApplyModifiedProperties();

    }
}
