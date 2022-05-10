using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


//This script assumes information was stored as SwingTwist
//This means the script goes together with ROMparserSwingTwist

//we assume the articulationBodies have a name structure of hte form ANYNAME:something-in-the-targeted-joint

[CustomEditor(typeof(ROMparserSwingTwist))]
public class ROMparserSwingTwistEditor : Editor
{

    [SerializeField]
    SerializedProperty ROMparserSwingTwist;

    //NOT USED ANYMORE
    //[SerializeField]
    //string keyword4prefabs = "Constrained-procedurally";
    //[SerializeField]
    //RangeOfMotion004 theRagdollPrefab;



    void OnEnable()
    {
        ROMparserSwingTwist = serializedObject.FindProperty("ROMparserSwingTwist");

    }




    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        GUILayout.Label("");




        base.OnInspectorGUI();

        /*
        if (GUILayout.Button("0.Debug: Set Joints To Max ROM"))
        {
            ROMparserSwingTwist t = target as ROMparserSwingTwist;
            t.SetJointsToMaxROM();
        }



        if (GUILayout.Button("1.Store ROM info "))
        {
            ROMparserSwingTwist t = target as ROMparserSwingTwist;
            EditorUtility.SetDirty(t.info2store);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        GUILayout.Label("Prefab Ragdoll:");



        GUILayout.Label("How to use:");

        GUILayout.TextArea(
            "Step 1: execute in play mode until the values in the Range Of Motion Preview  do not change any more" +
            // " \n Step 2: click on button 1 to apply the constraints to check if the ragdoll looks reasonable" +
            // " \n Step 3: in edit mode, click on button 1, and then on button 2, to generate a new constrained ragdoll. If a template for a SpawnableEnv is provided, also a new environment for training");
            " \n Step 2: open the Ragdoll on which you want to apply the range of motion, and use the script ApplyRangeOfMotion004");
        */

        serializedObject.ApplyModifiedProperties();

    }



    void storeNewPrefabWithROM(ProcRagdollAgent rda, ManyWorlds.SpawnableEnv envPrefab = null)
    {
        ROMparserSwingTwist t = target as ROMparserSwingTwist;

        //ROMinfoCollector infoStored = t.info2store;

        Transform targetRoot = t.targetRagdollRoot.transform;


        //string add2prefabs = keyword4prefabs;



        // Set the path,
        // and name it as the GameObject's name with the .Prefab format
        string localPath = "Assets/MarathonEnvs/Agents/Characters/MarathonMan004/" + targetRoot.name + ".prefab";

        // Make sure the file name is unique, in case an existing Prefab has the same name.
        string uniqueLocalPath = AssetDatabase.GenerateUniqueAssetPath(localPath);


        if (PrefabUtility.IsAnyPrefabInstanceRoot(targetRoot.gameObject))
            //We want to store it independently from the current prefab. Therefore:
            PrefabUtility.UnpackPrefabInstance(targetRoot.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);


        // Create the new Prefab.
        PrefabUtility.SaveAsPrefabAsset(targetRoot.gameObject, uniqueLocalPath);

        Debug.Log("Saved new CharacterPrefab at: " + uniqueLocalPath);


        if (envPrefab != null)
        {
            Transform targetEnv = envPrefab.transform;

            targetEnv.name = "ControllerMarathonManEnv";

            string localEnvPath = "Assets/MarathonController/Environments/" + targetEnv.name + ".prefab";

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            string uniqueLocalEnvPath = AssetDatabase.GenerateUniqueAssetPath(localEnvPath);

            if (PrefabUtility.IsAnyPrefabInstanceRoot(targetEnv.gameObject))
                //We want to store it independently from the current prefab. Therefore:
                PrefabUtility.UnpackPrefabInstance(targetEnv.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);


            // Create the new Prefab.
            PrefabUtility.SaveAsPrefabAsset(targetEnv.gameObject, uniqueLocalEnvPath);

            Debug.Log("Saved new Environment Prefab at: " + uniqueLocalEnvPath);

        }



    }


}
