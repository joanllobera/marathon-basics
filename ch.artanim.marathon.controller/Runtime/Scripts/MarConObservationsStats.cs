using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


using ManyWorlds;
using Unity.Collections;
using UnityEngine;

public class MarConObservationsStats : MonoBehaviour
{
    [Header("Anchor stats")]
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    public Vector3 AngualrVelocity;

    [Header("Stats, relative to HorizontalDirection & Center Of Mass")]
    public Vector3 CenterOfMassVelocity;
    public Vector3 CenterOfMassHorizontalVelocity;
    public float CenterOfMassVelocityMagnitude;
    public Vector3 CenterOfMassVelocityInRootSpace;
    public float CenterOfMassVelocityMagnitudeInRootSpace;
    public float CenterOfMassHorizontalVelocityMagnitude;
    public Vector3 DesiredCenterOfMassVelocity;
    public Vector3 CenterOfMassVelocityDifference;
    [HideInInspector] public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector] public Quaternion LastRotation;
    [Header("Stats, in local joint space")]
    public float[] DofRotationWithinRangeOfMotion;
    public float[] DofAngularVelocity;
    public float[] DofRotationInRad;
    [HideInInspector] public float[] DofLastRotationInRad;    

    ArticulationBody[] _articulationBodyJoints;
    Rigidbody[] _rigidBodyJoints;
    GameObject[] _jointsToTrack;
    Collider[] _collidersToTrack;
    GameObject[] _jointForTrackedColliders;
    public Vector3[] Positions;
    public Quaternion[] Rotations;
    public Vector3[] Velocities;
    public Vector3[] AngualrVelocities;
    [HideInInspector]
    public Vector3[] LastLocalPositions;
    [HideInInspector]
    public Quaternion[] LastLocalRotations;
    bool LastIsSet;
    MapAnim2Ragdoll _mapAnim2Ragdoll;
    GameObject _root;
    IAnimationController _animationController;
    SpawnableEnv _spawnableEnv;

    public void OnAgentInitialize(
        Transform defaultTransform, 
        ArticulationBody[] articulationBodyJoints,
        ArticulationBody articulationBodyRoot)
    {
        _mapAnim2Ragdoll = defaultTransform.GetComponent<MapAnim2Ragdoll>();
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _animationController = _spawnableEnv.GetComponentInChildren<IAnimationController>();

        var jointNames = articulationBodyJoints
            .Select(x=>x.name)
            .Select(x=>x.Replace("articulation:", ""))
            .Select(x=>x.Replace("mixamorig:", ""))
            .ToArray();
        _articulationBodyJoints = articulationBodyJoints;
        _rigidBodyJoints = GetComponentsInChildren<Rigidbody>();
        if (_rigidBodyJoints.Length > 0)
        {
            _jointsToTrack = jointNames
                .Select(x=>_rigidBodyJoints.First(y => y.name.EndsWith(x)))
                .Select(x=>x.gameObject)
                .ToArray();
        }
        else
        {
            _jointsToTrack = jointNames
                .Select(x=>articulationBodyJoints.First(y => y.name.EndsWith(x)))
                .Select(x=>x.gameObject)
                .ToArray();
        }
        _collidersToTrack = _jointsToTrack
            .SelectMany(x=>x.GetComponentsInChildren<Collider>())
            .Where(x => x.enabled)
            .Where(x => !x.isTrigger)
            .Where(x=> {
                var ignoreCollider = x.GetComponent<IgnoreColliderForObservation>();
                if (ignoreCollider == null)
                    return true;
                return !ignoreCollider.enabled;})
            .Distinct()
            .ToArray();

        if (_rigidBodyJoints.Length > 0)
        {
            _jointForTrackedColliders = _collidersToTrack
                .Select(x=>x.GetComponentsInParent<Rigidbody>().First())
                .Select(x=>x.gameObject)
                .ToArray();
        }
        else
        {
            _jointForTrackedColliders = _collidersToTrack
                .Select(x=>x.GetComponentsInParent<ArticulationBody>().First())
                .Select(x=>x.gameObject)
                .ToArray();
        }
        Positions = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Vector3.zero).ToArray();
        Rotations = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Quaternion.identity).ToArray();
        Velocities = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Vector3.zero).ToArray();
        AngualrVelocities = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Vector3.zero).ToArray();
        LastLocalPositions = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Vector3.zero).ToArray();
        LastLocalRotations = Enumerable.Range(0, _collidersToTrack.Length).Select(x=>Quaternion.identity).ToArray();

        int dof = 0;
        foreach (var m in _articulationBodyJoints)
        {
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                dof++;
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                dof++;
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                dof++;
        }
        DofRotationWithinRangeOfMotion = Enumerable.Range(0, dof).Select(x=>0f).ToArray();
        DofAngularVelocity = Enumerable.Range(0, dof).Select(x=>0f).ToArray();
        DofRotationInRad = Enumerable.Range(0, dof).Select(x=>0f).ToArray();
        DofLastRotationInRad = Enumerable.Range(0, dof).Select(x=>0f).ToArray();

        if (_root == null)
        {
            Debug.Log("in game object: " + name + "my rootname is: " + articulationBodyRoot.name);
            if (_rigidBodyJoints.Length > 0)
                _root = _rigidBodyJoints.First(x => x.name == articulationBodyRoot.name).gameObject;
            else
                _root = articulationBodyRoot.gameObject;
        }
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
        LastIsSet = false;
    }    
    public void OnStep(float timeDelta)
    {
        // get Center Of Mass velocity in f space
        Vector3 newCOM;
        // if Moocap, then get from anim2Ragdoll
        if (_mapAnim2Ragdoll != null)
        {
            newCOM = _mapAnim2Ragdoll.LastCenterOfMassInWorldSpace;
            var newHorizontalDirection = _mapAnim2Ragdoll.HorizontalDirection;
            HorizontalDirection = newHorizontalDirection / 180f;
            if (!LastIsSet)
            {
                LastCenterOfMassInWorldSpace = newCOM;
            }
            transform.position = newCOM;
            transform.rotation = Quaternion.Euler(newHorizontalDirection);
            CenterOfMassVelocity = _mapAnim2Ragdoll.CenterOfMassVelocity;
            CenterOfMassVelocityMagnitude = _mapAnim2Ragdoll.CenterOfMassVelocityMagnitude;
            CenterOfMassVelocityInRootSpace = transform.InverseTransformVector(CenterOfMassVelocity);
            CenterOfMassVelocityMagnitudeInRootSpace = CenterOfMassVelocityInRootSpace.magnitude;
        }
        else
        {
            newCOM = GetCenterOfMass();
            var newHorizontalDirection = new Vector3(0f, _root.transform.eulerAngles.y, 0f);
            HorizontalDirection = newHorizontalDirection / 180f;
            if (!LastIsSet)
            {
                LastCenterOfMassInWorldSpace = newCOM;
            }
            transform.position = newCOM;
            transform.rotation = Quaternion.Euler(newHorizontalDirection);
            var velocity = newCOM - LastCenterOfMassInWorldSpace;
            velocity /= timeDelta;
            CenterOfMassVelocity = velocity;
            CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;
            CenterOfMassVelocityInRootSpace = transform.InverseTransformVector(CenterOfMassVelocity);
            CenterOfMassVelocityMagnitudeInRootSpace = CenterOfMassVelocityInRootSpace.magnitude;
        }
        LastCenterOfMassInWorldSpace = newCOM;

        // get Center Of Mass horizontal velocity in f space
        var comHorizontalDirection = new Vector3(CenterOfMassVelocity.x, 0f, CenterOfMassVelocity.z);
        CenterOfMassHorizontalVelocity = transform.InverseTransformVector(comHorizontalDirection);
        CenterOfMassHorizontalVelocityMagnitude = CenterOfMassHorizontalVelocity.magnitude;

        // get Desired Center Of Mass horizontal velocity in f space
        Vector3 desiredCom = _animationController.GetDesiredVelocity();
        DesiredCenterOfMassVelocity = transform.InverseTransformVector(desiredCom);

        // get Desired Center Of Mass horizontal velocity in f space
        CenterOfMassVelocityDifference = DesiredCenterOfMassVelocity - CenterOfMassHorizontalVelocity;

        if (!LastIsSet)
        {
            LastRotation = transform.rotation;
        }
        AngualrVelocity = Utils.GetAngularVelocity(LastRotation, transform.rotation, timeDelta);
        LastRotation = transform.rotation;

        TrackUsingColliders(timeDelta);
        // TrackUsingJointsForTrackedColliders(timeDelta);
        TrackUsingDof(timeDelta);
        
        LastIsSet = true;        
    }

    void TrackUsingJointsForTrackedColliders(float timeDelta)
    {
        // track in local space
        for (int i = 0; i < _jointForTrackedColliders.Length; i++)
        {
            Transform joint = _jointForTrackedColliders[i].transform;
            Vector3 worldPosition = joint.position;
            Quaternion worldRotation = transform.rotation;
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Quaternion localRotation = Utils.FromToRotation(transform.rotation, worldRotation);
            if (!LastIsSet)
            {
                LastLocalPositions[i] = localPosition;
                LastLocalRotations[i] = localRotation;
            }

            Positions[i] = localPosition;
            Rotations[i] = localRotation;
            Velocities[i] = (localPosition - LastLocalPositions[i]) / timeDelta;
            AngualrVelocities[i] = Utils.GetAngularVelocity(LastLocalRotations[i], localRotation, timeDelta);
            LastLocalPositions[i] = localPosition;
            LastLocalRotations[i] = localRotation;
        }
    }
    void TrackUsingColliders(float timeDelta)
    {
        // track in local space
        for (int i = 0; i < _collidersToTrack.Length; i++)
        {
            Vector3 c = Vector3.zero;
            CapsuleCollider capsule = _collidersToTrack[i] as CapsuleCollider;
            BoxCollider box = _collidersToTrack[i] as BoxCollider;
            SphereCollider sphere = _collidersToTrack[i] as SphereCollider;
            Bounds b = new Bounds(c, c);
            if (capsule != null)
            {
                c = capsule.center;
                var r = capsule.radius * 2;
                var h = capsule.height;
                h = Mathf.Max(r, h); // capsules height is clipped at r
                if (capsule.direction == 0)
                    b = new Bounds(c, new Vector3(h, r, r));
                else if (capsule.direction == 1)
                    b = new Bounds(c, new Vector3(r, h, r));
                else if (capsule.direction == 2)
                    b = new Bounds(c, new Vector3(r, r, h));
                else throw new NotImplementedException();
            }
            else if (box != null)
            {
                c = box.center;
                b = new Bounds(c, box.size);
            }
            else if (sphere != null)
            {
                c = sphere.center;
                var r = sphere.radius * 2;
                b = new Bounds(c, new Vector3(r, r, r));
            }
            else
                throw new NotImplementedException();

            Vector3 worldPosition = _collidersToTrack[i].transform.TransformPoint(c);

            Quaternion worldRotation = _collidersToTrack[i].transform.rotation;
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Quaternion localRotation = Utils.FromToRotation(transform.rotation, worldRotation);
            if (!LastIsSet)
            {
                LastLocalPositions[i] = localPosition;
                LastLocalRotations[i] = localRotation;
            }

            Positions[i] = localPosition;
            Rotations[i] = localRotation;
            Velocities[i] = (localPosition - LastLocalPositions[i]) / timeDelta;
            AngualrVelocities[i] = Utils.GetAngularVelocity(LastLocalRotations[i], localRotation, timeDelta);
            LastLocalPositions[i] = localPosition;
            LastLocalRotations[i] = localRotation;
        }
    }
    
    void TrackUsingDof(float timeDelta)
    {
        // track in local space
        int i = 0;
        for (int j = 0; j < _jointForTrackedColliders.Length; j++)
        {
            var joint = _jointsToTrack[j];
            var reference = _articulationBodyJoints[j];
            Vector3 decomposedRotation = Utils.GetSwingTwist(joint.transform.localRotation);
            if (reference.twistLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = reference.xDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = decomposedRotation.x;
                var pos = (deg - midpoint) / scale;
                DofRotationInRad[i] = deg * Mathf.Deg2Rad;
                DofRotationWithinRangeOfMotion[i++] = pos;
            }
            if (reference.swingYLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = reference.yDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = decomposedRotation.y;
                var pos = (deg - midpoint) / scale;
                DofRotationInRad[i] = deg * Mathf.Deg2Rad;
                DofRotationWithinRangeOfMotion[i++] = pos;
            }
            if (reference.swingZLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = reference.zDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = decomposedRotation.z;
                var pos = (deg - midpoint) / scale;
                DofRotationInRad[i] = deg * Mathf.Deg2Rad;
                DofRotationWithinRangeOfMotion[i++] = pos;
            }
        }

        for (i = 0; i < DofRotationInRad.Length; i++)
        {
            if (!LastIsSet)
            {
                DofLastRotationInRad[i] = DofRotationInRad[i];
            }
            DofAngularVelocity[i] = DofRotationInRad[i] - DofLastRotationInRad[i];
            DofAngularVelocity[i] /= timeDelta;
            DofLastRotationInRad[i] = DofRotationInRad[i];
        }
    }    

    public void OnReset()
    {
        OnStep(float.Epsilon);
        LastIsSet = false;
    }
    Vector3 GetCenterOfMass()
    {
        var centerOfMass = Vector3.zero;
        float totalMass = 0f;
        foreach (ArticulationBody ab in _articulationBodyJoints)
        {
            centerOfMass += ab.worldCenterOfMass * ab.mass;
            totalMass += ab.mass;
        }
        centerOfMass /= totalMass;
        // centerOfMass -= _spawnableEnv.transform.position;
        return centerOfMass;
    }    
}