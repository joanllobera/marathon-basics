using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Kinematic;

/// <summary>
/// The kinematic rig maps the source avatar's movement to the standard MarathonController hierarchy, so properties of the kinematic controller's segments can be queried.
/// A new class inheriting from KinematicRig should be implemented for new animation -> ragdoll mapping.
/// </summary>
public class KinematicRig : MonoBehaviour, IKinematicReference
{
    [SerializeField]
    private Transform kinematicRagdollRoot;

    [SerializeField]
    private Transform trackedTransformRoot;

    [SerializeField]
    private Vector3 offset;


    // Private, since other scripts should reference rigidbodies from the hierarchy, and not depend on KinematicRig implementation if possible
    private IReadOnlyList<Rigidbody> riggedRigidbodies;
    private IReadOnlyList<Transform> trackedTransforms;

    public IReadOnlyList<Transform> RagdollTransforms => riggedRigidbodies.Select(rb => rb.transform).ToList();

    public IEnumerable<Rigidbody> Rigidbodies => riggedRigidbodies;


    public IReadOnlyList<Vector3> RagdollLinVelocities => riggedRigidbodies.Select(rb => rb.velocity).ToList();

    public IReadOnlyList<Vector3> RagdollAngularVelocities => riggedRigidbodies.Select(rb => rb.angularVelocity).ToList();

    public IReadOnlyList<IKinematic> Kinematics => riggedRigidbodies.Select(rb => (IKinematic) new RigidbodyAdapter(rb)).ToList();

    // Update is called once per frame
    void FixedUpdate()
    {
        TrackKinematics();
    }

    public void OnAgentInitialize()
    {
        riggedRigidbodies = kinematicRagdollRoot.GetComponentsInChildren<Rigidbody>();
        trackedTransforms = trackedTransformRoot.GetComponentsInChildren<Transform>();
        (riggedRigidbodies, trackedTransforms) = MarathonControllerMapping(riggedRigidbodies, trackedTransforms); //Only transfroms that have corresponding RB are tracked
    }

    private void TrackKinematics()
    {
        foreach((var rb, var tr) in riggedRigidbodies.Zip(trackedTransforms, Tuple.Create))
        {
            rb.MoveRotation(tr.rotation);
            rb.MovePosition(tr.position + offset);
        }
    }

    public void TeleportRoot(Vector3 position, Quaternion rotation)
    {
        Vector3 positionalOffset = position - riggedRigidbodies[0].position;
        Quaternion rotationalOffset = Quaternion.Inverse(riggedRigidbodies[0].rotation) * rotation;

        IEnumerable<KinematicState> kinematicStates = riggedRigidbodies.Select(rb => new KinematicState(rb.angularVelocity, rb.velocity, rb.position, rb.rotation));
        foreach((Rigidbody rb, KinematicState kinState) in riggedRigidbodies.Zip(kinematicStates, Tuple.Create))
        {
            rb.position = kinState.position + positionalOffset;
            rb.rotation = kinState.rotation * rotationalOffset;
            rb.velocity = rotationalOffset * kinState.linearVelocity;
            rb.angularVelocity = rotationalOffset * kinState.angularVelocity;
        }
    }

    public void TeleportRoot(Vector3 position)
    {
        Vector3 positionalOffset = position - riggedRigidbodies[0].position;

        IEnumerable<KinematicState> kinematicStates = riggedRigidbodies.Select(rb => new KinematicState(rb.angularVelocity, rb.velocity, rb.position, rb.rotation));
        foreach ((Rigidbody rb, KinematicState kinState) in riggedRigidbodies.Zip(kinematicStates, Tuple.Create))
        {
            rb.position = kinState.position + positionalOffset;
            rb.velocity = kinState.linearVelocity;
            rb.angularVelocity = kinState.angularVelocity;
        }
    }

    private (IReadOnlyList<Rigidbody>, IReadOnlyList<Transform>) MarathonControllerMapping(IReadOnlyList<Rigidbody> rigidbodies, IReadOnlyList<Transform> transforms)
    {
        List<string> rigidbodyNames = riggedRigidbodies.Select(rb => Utils.SegmentName(rb.name)).ToList();
        transforms = transforms.Where(t => rigidbodyNames.Contains(Utils.SegmentName(t.name))).ToList();

        return (rigidbodies, transforms);
    }

    private struct KinematicState
    {
        public readonly Vector3 angularVelocity;
        public readonly Vector3 linearVelocity;
        public readonly Vector3 position;
        public readonly Quaternion rotation;

        public KinematicState(Vector3 angularVelocity, Vector3 linearVelocity, Vector3 position, Quaternion rotation)
        {
            this.angularVelocity = angularVelocity;
            this.linearVelocity = linearVelocity;
            this.position = position;
            this.rotation = rotation;
        }
    }
}

/// <summary>
/// Temporary interface so both KinematicRig and MapAnimation2Ragdoll works
/// </summary>
public interface IKinematicReference
{
    public IReadOnlyList<Transform> RagdollTransforms { get; }

    public void OnAgentInitialize();

    public void TeleportRoot(Vector3 targetPosition);
    public void TeleportRoot(Vector3 targetPosition, Quaternion targetRotation);

    public IReadOnlyList<Vector3> RagdollLinVelocities { get; }

    public IReadOnlyList<Vector3> RagdollAngularVelocities { get; }

    public IReadOnlyList<IKinematic> Kinematics { get;  }
}