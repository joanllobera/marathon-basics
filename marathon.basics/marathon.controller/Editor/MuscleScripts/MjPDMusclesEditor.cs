/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mujoco
{

    [CustomEditor(typeof(MjPDMuscles))]
    public class MjPDMusclesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            GUILayout.Label("");




            base.OnInspectorGUI();


            if (GUILayout.Button("Set Control Limits"))
            {
                MjPositionMuscles t = target as MjPDMuscles;
                foreach (var a in t.Actuators)
                {
                    if (a.CustomParams.BiasPrm[2] != 0) continue;
                    var h = a.Joint as MjHingeJoint;
                    a.CommonParams.CtrlLimited = true;
                    a.CommonParams.CtrlRange = new Vector2(h.RangeLower, h.RangeUpper);
                }
            }

            if (GUILayout.Button("Remove Control Limits"))
            {
                MjPositionMuscles t = target as MjPDMuscles;
                foreach (var a in t.Actuators)
                {
                    var h = a.Joint as MjHingeJoint;
                    a.CommonParams.CtrlLimited = false;
                    a.CommonParams.CtrlRange = Vector2.zero;
                }
            }

            if (GUILayout.Button("Update Gains"))
            {
                MjPositionMuscles t = target as MjPDMuscles;
                t.UpdateGains();
            }



            serializedObject.ApplyModifiedProperties();

        }
    }
}*/