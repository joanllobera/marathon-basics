using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using ManyWorlds;
using UnityEngine.Assertions;
using Unity.Collections;

public class MarConObservations2Learn : MonoBehaviour
{
    public string[] JointNames;
    [Header("Observations")]

    [Tooltip("Kinematic character center of mass velocity, Vector3")]
    public Vector3 MocapCOMVelocity;

    [Tooltip("RagDoll character center of mass velocity, Vector3")]
    public Vector3 RagDollCOMVelocity;

    [Tooltip("User-input desired horizontal CM velocity. Vector2")]
    public Vector2 InputDesiredHorizontalVelocity;

    [Tooltip("User-input requests jump, bool")]
    public bool InputJump;

    [Tooltip("User-input requests backflip, bool")]
    public bool InputBackflip;

    [Tooltip("User-input requests stand, bool")]
    public bool InputStand;

    [Tooltip("User-input requests walk or run, bool")]
    public bool InputWalkOrRun;
    [Tooltip("Difference between RagDoll character horizontal CM velocity and user-input desired horizontal CM velocity. Vector2")]
    public Vector2 HorizontalVelocityDifference;

    [Tooltip("Positions and velocities for subset of bodies")]
    public Vector3[] DifferenceInPositions;
    public Vector3[] DifferenceInVelocities;
    public float[] DifferenceInDofRotationWithinRangeOfMotion;
    public float[] DifferenceInDofAngularVelocity;
    public float[] DifferenceInDofRotationInRad;


    [Tooltip("Smoothed actions produced in the previous step of the policy are collected in t −1")]
    public float[] PreviousActions;
    

    MarConObservationsStats _mocapStats;
    MarConObservationsStats _ragdollStats;
    ArticulationBody[] _joints;
    InputController _inputController;
    SpawnableEnv _spawnableEnv;
    public void OnAgentInitialize(GameObject ragdoll, MapAnim2Ragdoll mocap, ArticulationBody[] joints)
    {
        joints = joints
            .Where(x => x.jointType == ArticulationJointType.SphericalJoint)
            .Where(x => !x.isRoot)
            .Distinct()
            .ToArray();
        JointNames = joints
            .Select(x=>x.name)
            .Select(x=>x.Replace("articulation:", ""))
            .Select(x=>x.Replace("mixamorig:", ""))
            .ToArray();
        var articulationRoot = GetComponentsInChildren<ArticulationBody>()
            .First(x=>x.isRoot);

        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();

        _joints = joints;
        _mocapStats = new GameObject("MocapObservationStats").AddComponent<MarConObservationsStats>();
        _ragdollStats = new GameObject("RagdollObservationStats").AddComponent<MarConObservationsStats>();
        _mocapStats.transform.SetParent(_spawnableEnv.transform);
        _ragdollStats.transform.SetParent(_spawnableEnv.transform);
        _mocapStats.OnAgentInitialize(mocap.transform, _joints, articulationRoot);
        _ragdollStats.OnAgentInitialize(ragdoll.transform, _joints, articulationRoot);


        int dof = 0;
        foreach (var m in joints)
        {
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                dof++;
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                dof++;
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                dof++;
        }
        PreviousActions = Enumerable.Range(0,dof).Select(x=>0f).ToArray();
        DifferenceInPositions = Enumerable.Range(0,_mocapStats.Positions.Length).Select(x=>Vector3.zero).ToArray();
        DifferenceInVelocities = Enumerable.Range(0,_mocapStats.Positions.Length).Select(x=>Vector3.zero).ToArray();
        DifferenceInDofRotationWithinRangeOfMotion = Enumerable.Range(0,dof).Select(x=>0f).ToArray();
        DifferenceInDofAngularVelocity = Enumerable.Range(0,dof).Select(x=>0f).ToArray();
        DifferenceInDofRotationInRad = Enumerable.Range(0,dof).Select(x=>0f).ToArray();
    }
    public void OnStep(float timeDelta)
    {
        _mocapStats.OnStep(timeDelta);
        _ragdollStats.OnStep(timeDelta);
        MocapCOMVelocity = _mocapStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragdollStats.CenterOfMassVelocity;
        InputDesiredHorizontalVelocity = new Vector2(
            _ragdollStats.DesiredCenterOfMassVelocity.x,
            _ragdollStats.DesiredCenterOfMassVelocity.z);
        if (_inputController != null)
        {
            InputJump = _inputController.Jump;
            InputBackflip = _inputController.Backflip;
            InputStand = _inputController.Stand;
            InputWalkOrRun = _inputController.WalkOrRun;
        }
        HorizontalVelocityDifference = new Vector2(
            _ragdollStats.CenterOfMassVelocityDifference.x,
            _ragdollStats.CenterOfMassVelocityDifference.z);
        for (int i = 0; i < _mocapStats.Positions.Length; i++)
        {
            DifferenceInPositions[i] = _mocapStats.Positions[i] - _ragdollStats.Positions[i];
            DifferenceInVelocities[i] = _mocapStats.Velocities[i] - _ragdollStats.Velocities[i];
        }
        for (int i = 0; i < _mocapStats.DofRotationWithinRangeOfMotion.Length; i++)
        {
            DifferenceInDofRotationWithinRangeOfMotion[i] = 
                _mocapStats.DofRotationWithinRangeOfMotion[i] - _ragdollStats.DofRotationWithinRangeOfMotion[i];
            DifferenceInDofAngularVelocity[i] = 
                _mocapStats.DofAngularVelocity[i] - _ragdollStats.DofAngularVelocity[i];
            DifferenceInDofRotationInRad[i] = 
                _mocapStats.DofRotationInRad[i] - _ragdollStats.DofRotationInRad[i];
        }
    }    
    public void OnReset()
    {
        // _mocapStats.OnReset();
        // _RagdollStats.OnReset();
        _ragdollStats.transform.position = _mocapStats.transform.position;
        _ragdollStats.transform.rotation = _mocapStats.transform.rotation;
        var timeDelta = float.MinValue;
        OnStep(timeDelta);
    }
    public MarConObservationsStats GetRagdollStats() => _ragdollStats;
    public Transform GetRagDollCOM()
    {
        return _ragdollStats.transform;
    }

}
