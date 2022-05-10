using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
using ManyWorlds;

public class Rewards2Learn : MonoBehaviour
{
    [Header("Reward")]
    public float SumOfSubRewards;
    public float Reward;

    [Header("Position Reward")]
    public float SumOfDistances;
    public float SumOfSqrDistances;
    public float PositionReward;

    [Header("Velocity Reward")]
    public float PointsVelocityDifferenceSquared;
    public float PointsVelocityReward;

    [Header("Local Pose Reward")]
    public List<float> RotationDifferences;
    public float SumOfRotationDifferences;
    public float SumOfRotationSqrDifferences;
    public float LocalPoseReward;



    [Header("Center of Mass Velocity Reward")]
    public Vector3 MocapCOMVelocity;
    public Vector3 RagDollCOMVelocity;

    public float COMVelocityDifference;
    public float ComVelocityReward;
    public float ComReward;

    [Header("Center of Mass Direction Reward")]
    public float ComDirectionDistance;
    public float ComDirectionReward;

    [Header("Center of Mass Position Reward")]
    public float ComDistance;
    public float DistanceFactor;
    public float ComPositionReward;

    // [Header("Direction Factor")]
    // public float DirectionDistance;
    // public float DirectionFactor;
    [Header("Velocity Difference Reward")]
    public float VelDifferenceError;
    public float VelAvDiffTwoStepsError; 
    public float VelAvDiffFourStepsError;
    public float VelDifferenceReward;
    public float VelDiffTwoStepsReward; 
    public float VelDiffFourStepsReward;
    float [][] _velHistoryTwoSteps;
    float [][] _velHistoryFourSteps;


    [Header("Minimize Energy Reward")]
    public float KineticEnergyMetric;
    public float EnergyMinimReward;



    [Header("Misc")]
    public float HeadHeightDistance;

    [Header("Gizmos")]
    public int ObjectForPointDistancesGizmo;

    SpawnableEnv _spawnableEnv;
    MapAnim2Ragdoll _mocap;
    GameObject _ragDoll;
    InputController _inputController;

    internal RewardStats _mocapBodyStats;
    internal RewardStats _ragDollBodyStats;

    // List<ArticulationBody> _mocapBodyParts;
    // List<ArticulationBody> _ragDollBodyParts;
    Transform _mocapHead;
    Transform _ragDollHead;

    bool _hasLazyInitialized;
    bool _reproduceDReCon;

    [Header("Things to check for rewards")]
    public string headname = "head";

    public string targetedRootName = "articulation:Hips";


    public void OnAgentInitialize(bool reproduceDReCon)
    {
        Assert.IsFalse(_hasLazyInitialized);

        _hasLazyInitialized = true;
        _reproduceDReCon = reproduceDReCon;

        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        Assert.IsNotNull(_spawnableEnv);

        _mocap = _spawnableEnv.GetComponentInChildren<MapAnim2Ragdoll>();

        _ragDoll = _spawnableEnv.GetComponentInChildren<ProcRagdollAgent>().gameObject;
        Assert.IsNotNull(_mocap);
        Assert.IsNotNull(_ragDoll);
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        // _mocapBodyParts = _mocap.GetComponentsInChildren<ArticulationBody>().ToList();
        // _ragDollBodyParts = _ragDoll.GetComponentsInChildren<ArticulationBody>().ToList();
        // Assert.AreEqual(_mocapBodyParts.Count, _ragDollBodyParts.Count);
        _mocapHead = _mocap
            .GetComponentsInChildren<Transform>()
            .First(x => x.name == headname);
        _ragDollHead = _ragDoll
            .GetComponentsInChildren<Transform>()
            .First(x => x.name == headname);
        _mocapBodyStats = new GameObject("MocapDReConRewardStats").AddComponent<RewardStats>();
        _mocapBodyStats.setRootName(targetedRootName);
        _mocapBodyStats.setHeadName(headname);

        _mocapBodyStats.ObjectToTrack = _mocap;

        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAgentInitialize(_mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats = new GameObject("RagDollDReConRewardStats").AddComponent<RewardStats>();
        _ragDollBodyStats.setRootName(targetedRootName);
        _ragDollBodyStats.setHeadName(headname);


        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAgentInitialize(transform, _mocapBodyStats);

        _velHistoryTwoSteps = new float[][]{
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray(),
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray()
            };
        _velHistoryFourSteps = new float[][]{
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray(),
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray(),
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray(),
            _ragDollBodyStats.PointVelocity
                .Zip(_mocapBodyStats.PointVelocity, (x,y) => x.magnitude-y.magnitude)
                .ToArray()
            };

        _mocapBodyStats.AssertIsCompatible(_ragDollBodyStats);
    }

    // Update is called once per frame
    public void OnStep(float timeDelta)
    {
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);

        if (_reproduceDReCon)
        {
            DReConRewards(timeDelta);
            return;
        }

        // deep sort scales
        float num_points = _ragDollBodyStats.Points.Length;
        float num_joints = num_points / 6f;
        float pose_scale = 2.0f / 15f * num_joints;
        float vel_scale = 0.1f / 15f * num_joints;
        // note is 10 in code, but 40 in paper. 
        const float position_scale = 10f; // was end_eff_scale
        // const float root_scale = 5f;
        const float com_scale = 5f;

        // DeepMimic
        // const float pose_w = 0.5f;
        // const float vel_w = 0.05f;
        // const float position_w = 0.15f; // was end_eff_w
        // // const float root_w = 0.2f;
        // const float com_w = 0.2f; // * 2
        // const float energy_w = 0.2f;

        // // UniCon
        // const float pose_w = 0.4f;
        // const float vel_w = 0.1f;
        // const float position_w = 0.1f;
        // // const float com_direction_w = 0.1f; 
        // const float com_direction_w = 0.2f; 
        // const float com_velocity_w = 0.1f; 
        // const float energy_w = 0.2f;

        // MarCon
        const float pose_w = 0.2f;
        const float position_w = 0.2f;
        const float com_direction_w = 0.1f; 
        const float com_velocity_w = 0.3f; 
        const float energy_w = 0.2f;

        // position reward
        List<float> distances = _mocapBodyStats.GetPointDistancesFrom(_ragDollBodyStats);
        List<float> sqrDistances = distances.Select(x=> x*x).ToList();
        SumOfDistances = distances.Sum();
        SumOfSqrDistances = sqrDistances.Sum();
        var deepMimicScaledDistance = SumOfSqrDistances / (num_points / 4);
        PositionReward = Mathf.Exp(-position_scale * (deepMimicScaledDistance));
        if (PositionReward == 0f)
        {
            PositionReward = 0f;
        }

        // center of mass velocity reward
        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        COMVelocityDifference = (MocapCOMVelocity-RagDollCOMVelocity).magnitude;
        ComVelocityReward = Mathf.Pow(COMVelocityDifference,2);
        ComVelocityReward = Mathf.Exp(-com_scale*ComVelocityReward);

        // points velocity
        List<float> velocityDistances = _mocapBodyStats.GetPointVelocityDistancesFrom(_ragDollBodyStats);
        List<float> sqrVelocityDistances = velocityDistances.Select(x=> x*x).ToList();
        PointsVelocityDifferenceSquared = sqrVelocityDistances.Sum();
        var deepMimicScaledVelocityDistanceSquared = PointsVelocityDifferenceSquared / (num_points / 4);
        PointsVelocityReward = Mathf.Exp(-vel_scale * deepMimicScaledVelocityDistanceSquared);

        // local pose reward
        if (RotationDifferences == null || RotationDifferences.Count < _mocapBodyStats.Rotations.Count)
            RotationDifferences = Enumerable.Range(0,_mocapBodyStats.Rotations.Count)
            .Select(x=>0f)
            .ToList();
        SumOfRotationDifferences = 0f;
        SumOfRotationSqrDifferences = 0f;
        for (int i = 0; i < _mocapBodyStats.Rotations.Count; i++)
        { 
            var angle = Quaternion.Angle(_mocapBodyStats.Rotations[i], _ragDollBodyStats.Rotations[i]);
            Assert.IsTrue(angle <= 180f);
            angle = DReConObservationStats.NormalizedAngle(angle);
            angle = Mathf.Abs(angle);
            angle = angle / Mathf.PI;
            var sqrAngle = angle * angle;
            RotationDifferences[i] = angle;
            SumOfRotationDifferences += angle;
            SumOfRotationSqrDifferences += sqrAngle;
        }
        LocalPoseReward = Mathf.Exp(-pose_scale * SumOfRotationSqrDifferences);

        // center of mass direction reward (from 0f to 1f)
        ComDirectionDistance = Vector3.Dot( 
            _mocapBodyStats.transform.forward, 
            _ragDollBodyStats.transform.forward);
        ComDirectionDistance = 1f-((ComDirectionDistance + 1f)/2f);
        ComDirectionReward = ComDirectionDistance;
        ComDirectionReward = Mathf.Exp(-com_scale*ComDirectionReward);

        // // COM velocity factor
        // var comVelocityFactor = COMVelocityDifference;
        // comVelocityFactor = comVelocityFactor / 2f;
        // comVelocityFactor = 1.01f - comVelocityFactor;
        // comVelocityFactor = Mathf.Clamp(comVelocityFactor, 0f, 1f);

        // Calc Velocity difference Error
        VelDifferenceError = _ragDollBodyStats.PointVelocity
            .Zip(_mocapBodyStats.PointVelocity, (x,y) => (x-y).magnitude)
            .Average();
        VelDifferenceError = Mathf.Abs(VelDifferenceError);
        VelDifferenceReward = Mathf.Exp(-10f * VelDifferenceError);
        VelDifferenceReward = Mathf.Clamp(VelDifferenceReward, 0f, 1f);
        // roll history
        var oldestTwoSteps = _velHistoryTwoSteps[1];
        _velHistoryTwoSteps[1] = _velHistoryTwoSteps[0];
        _velHistoryTwoSteps[0] = oldestTwoSteps;
        var oldestFourSteps = _velHistoryFourSteps[3];
        _velHistoryFourSteps[3] = _velHistoryFourSteps[2];
        _velHistoryFourSteps[2] = _velHistoryFourSteps[1];
        _velHistoryFourSteps[1] = _velHistoryFourSteps[0];
        _velHistoryFourSteps[0] = oldestFourSteps;
        int velRows = _ragDollBodyStats.PointVelocity.Length;     
        for (int i = 0; i < velRows; i++)
        {
            var diff = _ragDollBodyStats.PointVelocity[i]-_mocapBodyStats.PointVelocity[i];
            var sqDiff = diff.magnitude*diff.magnitude;
            var absDiff = Mathf.Abs(diff.magnitude);
            oldestTwoSteps[i] = absDiff;
            oldestFourSteps[i] = absDiff;
            // oldestTwoSteps[i] = sqDiff;
            // oldestFourSteps[i] = sqDiff;
        }
        VelAvDiffTwoStepsError = 
            _velHistoryTwoSteps[0].Sum() +
            _velHistoryTwoSteps[1].Sum();
        VelAvDiffTwoStepsError /= velRows*2f;
        VelAvDiffFourStepsError = 
            _velHistoryFourSteps[0].Sum() +
            _velHistoryFourSteps[1].Sum() +
            _velHistoryFourSteps[2].Sum() +
            _velHistoryFourSteps[3].Sum();
        VelAvDiffFourStepsError /= velRows*4f;
        VelDiffTwoStepsReward = Mathf.Exp(-10 * VelAvDiffTwoStepsError);
        VelDiffFourStepsReward = Mathf.Exp(-10 * VelAvDiffFourStepsError);

        // calculate energy:
        //we obviate the masses, we want this to be important all across
        List<float> es = _ragDollBodyStats.PointVelocity.Select(x => x.magnitude * x.magnitude).ToList<float>();
        KineticEnergyMetric = es.Sum() / es.Count;
        //a quick run  suggests the input values range between 0 and 10, with most values near 0, so a simple way to get a reward value between 0 and 1 seems:
        EnergyMinimReward = Mathf.Exp(-KineticEnergyMetric);


        // misc
        HeadHeightDistance = (_mocapHead.position.y - _ragDollHead.position.y);
        HeadHeightDistance = Mathf.Abs(HeadHeightDistance);

        // reward
        // SumOfSubRewards = ComPositionReward+ComVelocityReward+ComDirectionReward+PositionReward+LocalPoseReward+PointsVelocityReward+VelDifferenceReward;
        SumOfSubRewards = ComVelocityReward+ComDirectionReward+PositionReward+LocalPoseReward+VelDifferenceReward;
        Reward = 0f +
                    (ComVelocityReward * com_velocity_w) + // com_w) +
                    (ComDirectionReward * com_direction_w) + // com_w) +
                    (PositionReward * position_w) +
                    (LocalPoseReward * pose_w) +
                    // (PointsVelocityReward * vel_w) +
                    (VelDifferenceReward * energy_w);


        // Reward = Reward + EnergyMinimReward;

        // var sqrtComVelocityReward = Mathf.Sqrt(ComVelocityReward);
        // var sqrtComDirectionReward = Mathf.Sqrt(ComDirectionReward);
        // Reward *= (sqrtComVelocityReward*sqrtComDirectionReward);      
    }

    void DReConRewards(float timeDelta)
    {
        // position reward
        List<float> distances = _mocapBodyStats.GetPointDistancesFrom(_ragDollBodyStats);
        PositionReward = -7.37f / (distances.Count / 6f);
        List<float> sqrDistances = distances.Select(x => x * x).ToList();
        SumOfDistances = distances.Sum();
        SumOfSqrDistances = sqrDistances.Sum();
        PositionReward *= SumOfSqrDistances;
        PositionReward = Mathf.Exp(PositionReward);

        // center of mass velocity reward
        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        COMVelocityDifference = (MocapCOMVelocity - RagDollCOMVelocity).magnitude;
        ComReward = -Mathf.Pow(COMVelocityDifference, 2);
        ComReward = Mathf.Exp(ComReward);

        // points velocity
        List<float> velocityDistances = _mocapBodyStats.GetPointVelocityDistancesFrom(_ragDollBodyStats);
        List<float> sqrVelocityDistances = velocityDistances.Select(x => x * x).ToList();
        PointsVelocityDifferenceSquared = sqrVelocityDistances.Sum();
        PointsVelocityReward = (-1f / _mocapBodyStats.PointVelocity.Length) * PointsVelocityDifferenceSquared;
        PointsVelocityReward = Mathf.Exp(PointsVelocityReward);

        // local pose reward
        if (RotationDifferences == null || RotationDifferences.Count < _mocapBodyStats.Rotations.Count)
            RotationDifferences = Enumerable.Range(0, _mocapBodyStats.Rotations.Count)
            .Select(x => 0f)
            .ToList();
        SumOfRotationDifferences = 0f;
        SumOfRotationSqrDifferences = 0f;
        for (int i = 0; i < _mocapBodyStats.Rotations.Count; i++)
        {
            var angle = Quaternion.Angle(_mocapBodyStats.Rotations[i], _ragDollBodyStats.Rotations[i]);
            Assert.IsTrue(angle <= 180f);
            angle = ObservationStats.NormalizedAngle(angle);
            var sqrAngle = angle * angle;
            RotationDifferences[i] = angle;
            SumOfRotationDifferences += angle;
            SumOfRotationSqrDifferences += sqrAngle;
        }
        LocalPoseReward = -6.5f / RotationDifferences.Count;
        LocalPoseReward *= SumOfRotationSqrDifferences;
        LocalPoseReward = Mathf.Exp(LocalPoseReward);

        // distance factor
        ComDistance = (_mocapBodyStats.LastCenterOfMassInWorldSpace - _ragDollBodyStats.LastCenterOfMassInWorldSpace).magnitude;
        HeadHeightDistance = (_mocapHead.position.y - _ragDollHead.position.y);
        HeadHeightDistance = Mathf.Abs(HeadHeightDistance);
        var headDistance = (_mocapBodyStats.HeadPositionInWorldSpace - _ragDollBodyStats.HeadPositionInWorldSpace).magnitude;
        DistanceFactor = Mathf.Pow(headDistance, 2);
        DistanceFactor = 1.4f * DistanceFactor;
        DistanceFactor = 1.3f - DistanceFactor;
        DistanceFactor = Mathf.Clamp(DistanceFactor, 0f, 1f);

        // // direction factor
        // Vector3 desiredDirection = _inputController.HorizontalDirection;
        // var curDirection = _ragDollBodyStats.transform.forward;
        // // cosAngle
        // var directionDifference = Vector3.Dot(desiredDirection, curDirection);
        // DirectionDistance = (1f + directionDifference) /2f; // normalize the error 
        // DirectionFactor = Mathf.Pow(DirectionDistance,2);
        // DirectionFactor = Mathf.Clamp(DirectionFactor, 0f, 1f);

        // misc

        // reward
        SumOfSubRewards = PositionReward + ComReward + PointsVelocityReward + LocalPoseReward;
        Reward = DistanceFactor * SumOfSubRewards;
        // Reward = (DirectionFactor*SumOfSubRewards) * DistanceFactor;
    }
    public void OnReset()
    {
        Assert.IsTrue(_hasLazyInitialized);

        _mocapBodyStats.OnReset();
        _ragDollBodyStats.OnReset();
    }
    void OnDrawGizmos()
    {
        if (_ragDollBodyStats == null)
            return;
        var max = (_ragDollBodyStats.Points.Length / 6) - 1;
        ObjectForPointDistancesGizmo = Mathf.Clamp(ObjectForPointDistancesGizmo, -1, max);
        // _mocapBodyStats.DrawPointDistancesFrom(_ragDollBodyStats, ObjectForPointDistancesGizmo);
        _ragDollBodyStats.DrawPointDistancesFrom(_mocapBodyStats, ObjectForPointDistancesGizmo);
    }
}
