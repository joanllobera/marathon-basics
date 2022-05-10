using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Mujoco
{
    [CustomEditor(typeof(MjRagdollTweaker))]
    public class MjRagdollTweakerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();



            base.OnInspectorGUI();

            if (GUILayout.Button("Scale Stiffness"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var hj in t.joints)
                {
                    hj.Settings.Spring.Stiffness *= t.stiffnessScale;
                }
            }

            if (GUILayout.Button("Scale Damping"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var hj in t.joints)
                {
                    hj.Settings.Spring.Damping *= t.dampingScale;
                }
            }

            if (GUILayout.Button("Scale Gearing"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var act in t.actuatorRoot.GetComponentsInChildren<MjActuator>())
                {
                    act.CommonParams.Gear[0] *= t.gearScale;
                }
            }
            if (GUILayout.Button("Scale Control Limits"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var act in t.actuatorRoot.GetComponentsInChildren<MjActuator>())
                {
                    act.CommonParams.CtrlRange *= t.controlLimitScale;
                }
            }
             if (GUILayout.Button("Scale Armature"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var hj in t.joints)
                {
                    hj.Settings.Armature *= t.armatureScale;
                }
            }

            if (GUILayout.Button("Set ConType"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var g in t.tweakedRoot.GetComponentsInChildren<MjGeom>())
                {
                    g.Settings.Filtering.Contype = t.conType;
                }
            }

            if (GUILayout.Button("Set ConAffinity"))
            {
                MjRagdollTweaker t = target as MjRagdollTweaker;
                foreach (var g in t.tweakedRoot.GetComponentsInChildren<MjGeom>())
                {
                    g.Settings.Filtering.Conaffinity = t.conAffinity;
                }
            }



            serializedObject.ApplyModifiedProperties();

        }


    }


}