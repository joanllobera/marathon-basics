using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mujoco;
using System;
using Kinematic;

public class MjKinematicRig : MonoBehaviour, IKinematicReference
{
    [SerializeField]
    private Transform weldRoot;

    [SerializeField]
    private Transform trackedTransformRoot;

    [SerializeField]
    private Vector3 offset;

    [SerializeField]
    private string prefix;

    [SerializeField]
    private Transform ragdollRoot;

    [SerializeField]
    private int targetId;

    // Private, since other scripts should reference rigidbodies from the hierarchy, and not depend on KinematicRig implementation if possible
    private IReadOnlyList<Transform> riggedTransforms;
    private IReadOnlyList<Transform> trackedTransforms;

    public IReadOnlyList<Transform> RagdollTransforms => bodies.Select(bd => bd.transform).ToList();

    private IReadOnlyList<MjMocapBody> mjMocapBodies;

    private IReadOnlyList<MjBody> bodies;

    public IReadOnlyList<MjBody> Bodies { get => bodies; }

    public IReadOnlyList<Vector3> RagdollLinVelocities => throw new NotImplementedException();

    public IReadOnlyList<Vector3> RagdollAngularVelocities => throw new NotImplementedException();

    public IReadOnlyList<IKinematic> Kinematics => ragdollRoot.GetComponentsInChildren<MjBody>().Select(mjb => (IKinematic) new MjBodyAdapter(mjb)).ToList();

    public Transform RagdollRoot { get => ragdollRoot; }

    public void Awake()
    {
        OnAgentInitialize();
    }

    void OnAnimatorIK(int k)
    {
        TrackKinematics();
    }

    public void OnAgentInitialize()
    {
        Func<string, string> MocapName = animatedName => $"{prefix}{Utils.SegmentName(animatedName)}";
        riggedTransforms = weldRoot.GetComponentsInChildren<MjWeld>().Select(krt => krt.Body1.transform).Where(t => t.name.Contains("Mocap") && t.gameObject.activeSelf).ToList().AsReadOnly();

        trackedTransforms = riggedTransforms.Select(rt => trackedTransformRoot.GetComponentsInChildren<Transform>().First(tt => MocapName(tt.name).Equals(rt.name))).ToList().AsReadOnly();

        mjMocapBodies = riggedTransforms.Select(t => t.GetComponent<MjMocapBody>()).ToList();
        bodies = ragdollRoot.GetComponentsInChildren<MjBody>().ToList();

    }

    public unsafe void TrackKinematics()
    {
        foreach ((var mjb, var tr) in riggedTransforms.Zip(trackedTransforms, Tuple.Create))
        {
            mjb.position = tr.position;
            mjb.rotation = tr.rotation;
        }

        foreach (var mcbd in mjMocapBodies)
        {
            mcbd.OnSyncState(MjScene.Instance.Data);
        }

    }



    public void TeleportRoot(Vector3 position, Quaternion rotation)
    {
        

        Vector3 posLag = bodies[0].transform.position - riggedTransforms[0].position;
        Quaternion rotLag = bodies[0].transform.rotation * Quaternion.Inverse(riggedTransforms[0].rotation);

        TrackKinematics();

        if (targetId > -1)
        {
            //Debug.Log(ragdollRoot.GetComponentInChildren<MjFreeJoint>().MujocoId);
            MjState.TeleportMjRoot(targetId, posLag + position, rotLag * rotation);
            return;
        }



        MjState.TeleportMjRoot(ragdollRoot.GetComponentInChildren<MjFreeJoint>(), posLag + position, rotLag * rotation);
    }

    public void TeleportRoot(Vector3 position)
    {
        

        Vector3 posLag = bodies[0].transform.position - riggedTransforms[0].position;
        Quaternion rotLag = bodies[0].transform.rotation * Quaternion.Inverse(riggedTransforms[0].rotation);

        TrackKinematics();

        if (targetId > -1)
        {
            Debug.Log(ragdollRoot.GetComponentInChildren<MjFreeJoint>().MujocoId);
            MjState.TeleportMjRoot(targetId, posLag + position, rotLag * riggedTransforms[0].rotation);
            return;
        }


        MjState.TeleportMjRoot(ragdollRoot.GetComponentInChildren<MjFreeJoint>(), posLag + position, rotLag * riggedTransforms[0].rotation);
    }

    public void ReplaceMocapBodies()
    {
        foreach(var weld in weldRoot.GetComponentsInChildren<MjWeld>())
        {
            var mocapGO = weld.Body1.gameObject;
            DestroyImmediate(weld.Body1);
            var newBody = mocapGO.AddComponent<MjMocapBody>();
            weld.Body1 = newBody;
        }

        foreach (var remainingBody in weldRoot.parent.GetComponentsInDirectChildren<MjBody>().Where(bd => bd.name.Contains(prefix)))
        {
            var bodyGO = remainingBody.gameObject;
            DestroyImmediate(remainingBody);
            bodyGO.AddComponent<MjMocapBody>();
        }
    }

    public unsafe void TrackKinematicsOffline()
    {


        Func<string, string> MocapName = animatedName => $"{prefix}{Utils.SegmentName(animatedName)}";
        Func<string, string> RemovePrefix = mocapName => mocapName.Replace(prefix, "");

        var riggedTransforms = weldRoot.GetComponentsInChildren<MjWeld>().Select(krt => krt.Body1.transform).Where(t => t.name.Contains(prefix) && t.gameObject.activeSelf).ToList().AsReadOnly();

        Debug.Log(string.Join(", ", weldRoot.GetComponentsInChildren<MjWeld>().Select(krt => krt.Body1.transform).Select(t => t.name.Contains(prefix))));

        var trackedTransforms = riggedTransforms.Select(rt => trackedTransformRoot.GetComponentsInChildren<Transform>().First(tt => tt.name.Contains(RemovePrefix(rt.name)))).ToList().AsReadOnly();

        foreach ((var mjb, var tr) in riggedTransforms.Zip(trackedTransforms, Tuple.Create))
        {
            mjb.position = tr.position;
            mjb.rotation = tr.rotation;
        }

        /*        foreach (var mcbd in mjMocapBodies)
                {
                    mcbd.OnSyncState(MjScene.Instance.Data);
                }*/

    }

}

