using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;

public class TeleportMJ : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    MjBody targetModel;

    [SerializeField]
    MjBody sourceModel;

    [SerializeField]
    Transform teleportTransform;

    [SerializeField]
    Transform animationRoot;

    Transform animationTransform;

    [SerializeField]
    MjKinematicRig kinematicRig;


    private void Start()
    {
        if (animationRoot != null)
        {
            animationTransform = animationRoot.GetComponentInParent<Animator>().transform;
        }
    }

    private void  Update()
    {
        //Debug.Log(targetModel.transform.position);
        //PrintPosFromSim();
        
    }

    public unsafe void  PrintPosFromSim()
    {
        MjScene scene = MjScene.Instance;
        int start = targetModel.MujocoId * 7;
        //Debug.Log(new Vector3((float)scene.Data->qpos[start], (float)scene.Data->qpos[start + 2], (float)scene.Data->qpos[start + 1]));
    }

    public void CopyState()
    {
        MjScene mjScene = MjScene.Instance;
        (var positions, var velocities) = MjState.GetMjKinematics(sourceModel);
        mjScene.SetMjKinematics(targetModel, positions, velocities);
    }

    public void TeleportRoot()
    {
        //mjScene.TeleportMjRoot(targetModel.GetComponentInChildren<MjFreeJoint>(), teleportTransform.position, teleportTransform.rotation);

        Vector3 posOffset = animationTransform.position - animationRoot.position;


        Vector3 posLag = targetModel.transform.position - animationRoot.position;
        Quaternion rotLag =  targetModel.transform.rotation * Quaternion.Inverse(animationRoot.rotation);

        if (animationTransform != null)
        {
            animationTransform.position = teleportTransform.position + posOffset;
            animationTransform.rotation = teleportTransform.rotation;
        }

        

        if (kinematicRig != null)
        {
            kinematicRig.TrackKinematics();
        }

        MjState.TeleportMjRoot(targetModel.GetComponentInChildren<MjFreeJoint>(), posLag + animationRoot.position, rotLag * animationRoot.rotation);

/*        if (animationTransform != null)
        {
            animationTransform.position = teleportTransform.position + posOffset;
            animationTransform.rotation = teleportTransform.rotation;
        }

        if (kinematicRig != null)
        {
            kinematicRig.TrackKinematics();
        }*/
    }

    public void RotateRoot()
    {
        var root = targetModel.GetComponentInChildren<MjFreeJoint>();
        Debug.Log(root.name);
        MjState.TeleportMjRoot(root, targetModel.transform.position, Quaternion.Euler(0f, 10f, 0f) * targetModel.transform.localRotation);
    }

}
