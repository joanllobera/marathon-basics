using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using ManyWorlds;
using UnityEngine.Assertions;

public class Observations2Learn : MonoBehaviour
{
    [Header("Observations")]

    [Tooltip("Kinematic character center of mass velocity, Vector3")]
    public Vector3 MocapCOMVelocity;

    [DebugGUIGraph(min: -1, max: 1, r: 0, g: 1, b: 1, group:0, autoScale: true)]
    public float debugVelocityKin =>MocapCOMVelocity.magnitude;

    [DebugGUIGraph(min: -1, max: 1, r: 1, g: 1, b: 0, group:1, autoScale: true)]
    public float debugVelocitySim => RagDollCOMVelocity.magnitude;

    [Tooltip("RagDoll character center of mass velocity, Vector3")]
    public Vector3 RagDollCOMVelocity;

    [Tooltip("User-input desired horizontal CM velocity. Vector2")]
    public Vector2 InputDesiredHorizontalVelocity;

    [Tooltip("User-input requests jump, bool")]
    public bool InputJump;

    [Tooltip("User-input requests backflip, bool")]
    public bool InputBackflip;

    [Tooltip("Difference between RagDoll character horizontal CM velocity and user-input desired horizontal CM velocity. Vector2")]
    public Vector2 HorizontalVelocityDifference;

    [Tooltip("Positions and velocities for subset of bodies")]
    public List<BodyPartDifferenceStats> BodyPartDifferenceStats;
    public List<ObservationStats.Stat> MocapBodyStats;
    public List<ObservationStats.Stat> RagDollBodyStats;

    [Tooltip("Smoothed actions produced in the previous step of the policy are collected in t −1")]
    public float[] PreviousActions;

    //[Tooltip("RagDoll ArticulationBody joint positions in reduced space")]
    //public float[] RagDollJointPositions;
//    [Tooltip("RagDoll ArticulationBody joint velocity in reduced space")]
//    public float[] RagDollJointVelocities;


  //  [Tooltip("RagDoll ArticulationBody joint accelerations in reduced space")]
  //  public float[] RagDollJointAccelerations;
    [Tooltip("RagDoll ArticulationBody joint forces in reduced space")]
    public float[] RagDollJointForces;


    [Tooltip("Macap: ave of joint angular velocity")]
    public float EnergyAngularMocap;
    [Tooltip("RagDoll: ave of joint angular velocity")]
    public float EnergyAngularRagDoll;
    [Tooltip("RagDoll-Macap: ave of joint angular velocity")]
    public float EnergyDifferenceAngular;

    [Tooltip("Macap: ave of joint velocity in local space")]
    public float EnergyPositionalMocap;
    [Tooltip("RagDoll: ave of joint velocity in local space")]
    public float EnergyPositionalRagDoll;
    [Tooltip("RagDoll-Macap: ave of joint velocity in local space")]
    public float EnergyDifferencePositional;


    [Header("Gizmos")]
    public bool VelocityInWorldSpace = true;
    public bool PositionInWorldSpace = true;


    public string targetedRootName = "articulation:Hips";



    InputController _inputController;
    SpawnableEnv _spawnableEnv;
    ObservationStats _mocapBodyStats;
    ObservationStats _ragDollBodyStats;
    bool _hasLazyInitialized;
    List<ArticulationBody> _motors;

    public void OnAgentInitialize()
    {
        Assert.IsFalse(_hasLazyInitialized);
        _hasLazyInitialized = true;

        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();

        _mocapBodyStats = new GameObject("MocapDReConObservationStats").AddComponent<ObservationStats>();
        _mocapBodyStats.setRootName(targetedRootName);



        _mocapBodyStats.ObjectToTrack = _spawnableEnv.GetComponentInChildren<MapAnim2Ragdoll>();

        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAgentInitialize(_mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats = new GameObject("RagDollDReConObservationStats").AddComponent<ObservationStats>();
        _ragDollBodyStats.setRootName(targetedRootName);

        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAgentInitialize(transform);

        BodyPartDifferenceStats = _mocapBodyStats.Stats
            .Select(x => new BodyPartDifferenceStats { Name = x.Name })
            .ToList();

        int numJoints = 0;
        _motors = GetComponentsInChildren<ArticulationBody>()
            .Where(x => x.jointType == ArticulationJointType.SphericalJoint)
            .Where(x => !x.isRoot)
            .Distinct()
            .ToList();
        foreach (var m in _motors)
        {
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                numJoints++;
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                numJoints++;
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                numJoints++;
        }
        PreviousActions = Enumerable.Range(0,numJoints).Select(x=>0f).ToArray();
        //RagDollJointPositions = Enumerable.Range(0,numJoints).Select(x=>0f).ToArray();
       // RagDollJointVelocities = Enumerable.Range(0,numJoints).Select(x=>0f).ToArray();
       // RagDollJointAccelerations = Enumerable.Range(0,numJoints).Select(x=>0f).ToArray();
        RagDollJointForces = Enumerable.Range(0,numJoints).Select(x=>0f).ToArray();
    }

    public List<Collider> EstimateBodyPartsForObservation()
    {
        var colliders = GetComponentsInChildren<Collider>()
            .Where(x => x.enabled)
            .Where(x => !x.isTrigger)
            .Where(x=> {
                var ignoreCollider = x.GetComponent<IgnoreColliderForObservation>();
                if (ignoreCollider == null)
                    return true;
                return !ignoreCollider.enabled;})
            .Distinct()
            .ToList();
        return colliders;
    }
    public List<Collider> EstimateBodyPartsForReward()
    {
        var colliders = GetComponentsInChildren<Collider>()
            .Where(x => x.enabled)
            .Where(x => !x.isTrigger)
            .Where(x=> {
                var ignoreCollider = x.GetComponent<IgnoreColliderForReward>();
                if (ignoreCollider == null)
                    return true;
                return !ignoreCollider.enabled;})
            .Distinct()
            .ToList();
        return colliders;
    }


    public void OnStep(float timeDelta)
    {
        Assert.IsTrue(_hasLazyInitialized);
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);
        UpdateObservations(timeDelta);
    }
    public void OnReset()
    {
        Assert.IsTrue(_hasLazyInitialized);
        _mocapBodyStats.OnReset();
        _ragDollBodyStats.OnReset();
        _ragDollBodyStats.transform.position = _mocapBodyStats.transform.position;
        _ragDollBodyStats.transform.rotation = _mocapBodyStats.transform.rotation;
        var timeDelta = float.MinValue;
        UpdateObservations(timeDelta);
    }

    public void UpdateObservations(float timeDelta)
    {

        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        InputDesiredHorizontalVelocity = new Vector2(
            _ragDollBodyStats.DesiredCenterOfMassVelocity.x,
            _ragDollBodyStats.DesiredCenterOfMassVelocity.z);
        if (_inputController != null)
        {
            InputJump = _inputController.Jump;
            InputBackflip = _inputController.Backflip;
        }
        HorizontalVelocityDifference = new Vector2(
            _ragDollBodyStats.CenterOfMassVelocityDifference.x,
            _ragDollBodyStats.CenterOfMassVelocityDifference.z);

        MocapBodyStats = _mocapBodyStats.Stats.ToList();
        RagDollBodyStats = MocapBodyStats
            .Select(x => _ragDollBodyStats.Stats.First(y => y.Name == x.Name))
            .ToList();
        // BodyPartStats = 
        foreach (var differenceStats in BodyPartDifferenceStats)
        {
            var mocapStats = _mocapBodyStats.Stats.First(x => x.Name == differenceStats.Name);
            var ragDollStats = _ragDollBodyStats.Stats.First(x => x.Name == differenceStats.Name);

            differenceStats.Position = mocapStats.Position - ragDollStats.Position;
            differenceStats.Velocity = mocapStats.Velocity - ragDollStats.Velocity;
            differenceStats.AngualrVelocity = mocapStats.AngularVelocity - ragDollStats.AngularVelocity;
            differenceStats.Rotation = ObservationStats.GetAngularVelocity(mocapStats.Rotation, ragDollStats.Rotation, timeDelta);
        }
        int i = 0;
        foreach (var m in _motors)
        {
            int j = 0;
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
            {
                //RagDollJointPositions[i] = m.jointPosition[j];
              //  RagDollJointVelocities[i] = m.jointVelocity[j];
              //  RagDollJointAccelerations[i] = m.jointAcceleration[j];
                RagDollJointForces[i++] = m.jointForce[j++];
            }
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
            {
              //  RagDollJointPositions[i] = m.jointPosition[j];
             //   RagDollJointVelocities[i] = m.jointVelocity[j];
             //   RagDollJointAccelerations[i] = m.jointAcceleration[j];
                RagDollJointForces[i++] = m.jointForce[j++];
            }
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
            {
              //  RagDollJointPositions[i] = m.jointPosition[j];
              //  RagDollJointVelocities[i] = m.jointVelocity[j];
              //  RagDollJointAccelerations[i] = m.jointAcceleration[j];
                RagDollJointForces[i++] = m.jointForce[j++];
            }
        }
        EnergyAngularMocap = MocapBodyStats
            .Select(x=>x.AngularVelocity.magnitude)
            .Average();
        EnergyAngularRagDoll = RagDollBodyStats
            .Select(x=>x.AngularVelocity.magnitude)
            .Average();
        EnergyDifferenceAngular = RagDollBodyStats
            .Zip(MocapBodyStats, (x,y) => x.AngularVelocity.magnitude-y.AngularVelocity.magnitude)
            .Average();
        EnergyPositionalMocap = MocapBodyStats
            .Select(x=>x.Velocity.magnitude)
            .Average();
        EnergyPositionalRagDoll = RagDollBodyStats
            .Select(x=>x.Velocity.magnitude)
            .Average();
        EnergyDifferencePositional = RagDollBodyStats
            .Zip(MocapBodyStats, (x,y) => x.Velocity.magnitude-y.Velocity.magnitude)
            .Average();
        
    }
    public Transform GetRagDollCOM()
    {
        return _ragDollBodyStats.transform;
    }
    public Vector3 GetMocapCOMVelocityInWorldSpace()
    {
        var velocity = _mocapBodyStats.CenterOfMassVelocity;
        var velocityInWorldSpace = _mocapBodyStats.transform.TransformVector(velocity);
        return velocityInWorldSpace;
    }
    void OnDrawGizmos()
    {
        if (_mocapBodyStats == null)
            return;
        // MocapCOMVelocity
        Vector3 pos = new Vector3(transform.position.x, .3f, transform.position.z);
        Vector3 vector = MocapCOMVelocity;
        if (VelocityInWorldSpace)
            vector = _mocapBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.grey);

        // RagDollCOMVelocity;
        vector = RagDollCOMVelocity;
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.blue);
        Vector3 actualPos = pos + vector;

        // InputDesiredHorizontalVelocity;
        vector = new Vector3(InputDesiredHorizontalVelocity.x, 0f, InputDesiredHorizontalVelocity.y);
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.green);

        // HorizontalVelocityDifference;
        vector = new Vector3(HorizontalVelocityDifference.x, 0f, HorizontalVelocityDifference.y);
        if (VelocityInWorldSpace)
            vector = _ragDollBodyStats.transform.TransformVector(vector);
        DrawArrow(actualPos, vector, Color.red);

        for (int i = 0; i < RagDollBodyStats.Count; i++)
        {
            var stat = RagDollBodyStats[i];
            var differenceStat = BodyPartDifferenceStats[i];
            pos = stat.Position;
            vector = stat.Velocity;
            if (PositionInWorldSpace)
                pos = _ragDollBodyStats.transform.TransformPoint(pos);
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.cyan);
            Vector3 velocityPos = pos + vector;

            pos = stat.Position;
            vector = differenceStat.Position;
            if (PositionInWorldSpace)
                pos = _ragDollBodyStats.transform.TransformPoint(pos);
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(pos, vector);
            Vector3 differencePos = pos + vector;

            vector = differenceStat.Velocity;
            if (VelocityInWorldSpace)
                vector = _ragDollBodyStats.transform.TransformVector(vector);
            DrawArrow(velocityPos, vector, Color.red);
        }

    }
    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        float headSize = 0.25f;
        float headAngle = 20.0f;
        Gizmos.color = color;
        Gizmos.DrawRay(start, vector);

        if (vector.magnitude > 0f)
        {
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 + headAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 - headAngle, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}
