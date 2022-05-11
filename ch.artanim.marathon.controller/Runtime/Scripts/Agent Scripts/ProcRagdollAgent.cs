using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using ManyWorlds;
using UnityEngine.Assertions;

using Unity.Mathematics;

using System;
using DReCon;

public class ProcRagdollAgent : Agent, IRememberPreviousActions
{
    [Header("Settings")]
    public float FixedDeltaTime = 1f / 60f;
    public float ActionSmoothingBeta = 0.2f;
    public bool ReproduceDReCon = true;
   

    [Header("Camera")]

    public bool RequestCamera;
    public bool CameraFollowMe;
    public Transform CameraTarget;

    [Header("... debug")]
    public bool SkipActionSmoothing;
    public bool debugCopyMocap;
    public bool ignorActions;
    public bool dontResetOnZeroReward;
    public bool dontSnapMocapToRagdoll = true;
    public bool DebugPauseOnReset;
    public bool dontResetWhenOutOfBounds;


    [SerializeField]
    KinematicRig rig;




    //List<Rigidbody> _mocapBodyParts;
    SpawnableEnv _spawnableEnv;
    Observations2Learn _observations2Learn;
    Rewards2Learn _rewards2Learn;
    ArticulationMusclesSimplified _ragDollMuscles;
    List<ArticulationBody> _motors;

 

    MarathonTestBedController _debugController;
    InputController _inputController;
    SensorObservations _sensorObservations;
    DecisionRequester _decisionRequester;
    IAnimationController _controllerToMimic;

    float[] previousActions;
    public float[] PreviousActions { get => previousActions; set => previousActions = value; }


    bool _hasLazyInitialized;
    //float[] _smoothedActions;
    float[] _mocapTargets;

    [Space(16)]
    [SerializeField]
    bool _hasAwake = false;
    MapAnim2Ragdoll _mapAnim2Ragdoll;


    public float ObservationTimeDelta
    {
        get => Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
    }

    public float ActionTimeDelta
    {
        get => _decisionRequester.TakeActionsBetweenDecisions? Time.fixedDeltaTime :  Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
    }
    

    float observationTimeDelta;
    float actionTimeDelta;
    void Awake()
    {
        if (RequestCamera && CameraTarget != null)
        {
            // Will follow the last object to be spawned
            var camera = FindObjectOfType<Camera>();
            if (camera != null)
            {
                var follow = camera.GetComponent<SmoothFollow>();
                if (follow != null)
                    follow.target = CameraTarget;
            }
        }


        _hasAwake = true;
    }
    void Update()
    {
        if (debugCopyMocap)
        {
            EndEpisode();
        }

        Assert.IsTrue(_hasLazyInitialized);

        // hadle mocap going out of bounds
        bool isOutOfBounds = !_spawnableEnv.IsPointWithinBoundsInWorldSpace(_mapAnim2Ragdoll.transform.position+new Vector3(0f, .1f, 0f));
        bool reset = isOutOfBounds && dontResetWhenOutOfBounds == false;
        if (reset)
        {
            _mapAnim2Ragdoll.transform.position = _spawnableEnv.transform.position;
            EndEpisode();
        }
    }
    override public void CollectObservations(VectorSensor sensor)
    {
        Assert.IsTrue(_hasLazyInitialized);

        observationTimeDelta = Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
        _mapAnim2Ragdoll.OnStep(observationTimeDelta);
        _observations2Learn.OnStep(observationTimeDelta);
      

        if (ReproduceDReCon)
        {
            AddDReConObservations(sensor);
            return;
        }

        sensor.AddObservation(_observations2Learn.MocapCOMVelocity);
        sensor.AddObservation(_observations2Learn.RagDollCOMVelocity);
        sensor.AddObservation(_observations2Learn.RagDollCOMVelocity - _observations2Learn.MocapCOMVelocity);
        sensor.AddObservation(_observations2Learn.InputDesiredHorizontalVelocity);
        sensor.AddObservation(_observations2Learn.InputJump);
        sensor.AddObservation(_observations2Learn.InputBackflip);
        sensor.AddObservation(_observations2Learn.HorizontalVelocityDifference);
        // foreach (var stat in _dReConObservations.MocapBodyStats)
        // {
        //     sensor.AddObservation(stat.Position);
        //     sensor.AddObservation(stat.Velocity);
        // }
        foreach (var stat in _observations2Learn.RagDollBodyStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }
        foreach (var stat in _observations2Learn.BodyPartDifferenceStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }
        sensor.AddObservation(_observations2Learn.PreviousActions);

        // add sensors (feet etc)
        sensor.AddObservation(_sensorObservations.SensorIsInTouch);
    }
    void AddDReConObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_observations2Learn.MocapCOMVelocity);
        sensor.AddObservation(_observations2Learn.RagDollCOMVelocity);
        sensor.AddObservation(_observations2Learn.RagDollCOMVelocity - _observations2Learn.MocapCOMVelocity);
        sensor.AddObservation(_observations2Learn.InputDesiredHorizontalVelocity);
        sensor.AddObservation(_observations2Learn.InputJump);
        sensor.AddObservation(_observations2Learn.InputBackflip);
        sensor.AddObservation(_observations2Learn.HorizontalVelocityDifference);
        // foreach (var stat in _dReConObservations.MocapBodyStats)
        // {
        //     sensor.AddObservation(stat.Position);
        //     sensor.AddObservation(stat.Velocity);
        // }
        foreach (var stat in _observations2Learn.RagDollBodyStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }
        foreach (var stat in _observations2Learn.BodyPartDifferenceStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }
        sensor.AddObservation(_observations2Learn.PreviousActions);
    }

    //adapted from previous function (Collect Observations)
    public int calculateDreConObservationsize()
    {
        int size = 0;

        size +=
        3  //sensor.AddObservation(_dReConObservations.MocapCOMVelocity);
        + 3 //sensor.AddObservation(_dReConObservations.RagDollCOMVelocity);
        + 3 //sensor.AddObservation(_dReConObservations.RagDollCOMVelocity - _dReConObservations.MocapCOMVelocity);
        + 2 //sensor.AddObservation(_dReConObservations.InputDesiredHorizontalVelocity);
        + 1 //sensor.AddObservation(_dReConObservations.InputJump);
        + 1 //sensor.AddObservation(_dReConObservations.InputBackflip);
        + 2;//sensor.AddObservation(_dReConObservations.HorizontalVelocityDifference);


        Observations2Learn _checkDrecon = GetComponent<Observations2Learn>();


        //foreach (var stat in _dReConObservations.RagDollBodyStats)

        foreach (var collider in _checkDrecon.EstimateBodyPartsForObservation())

        {
            size +=
             3 //sensor.AddObservation(stat.Position);
             + 3; //sensor.AddObservation(stat.Velocity);
        }
        //foreach (var stat in _dReConObservations.BodyPartDifferenceStats)
        foreach (var collider in _checkDrecon.EstimateBodyPartsForObservation())

        {
            size +=
            +3 // sensor.AddObservation(stat.Position);
            + 3; // sensor.AddObservation(stat.Velocity);
        }

        //action size and sensor size are calculated separately, we do not use:
        //sensor.AddObservation(_dReConObservations.PreviousActions);
        //sensor.AddObservation(_sensorObservations.SensorIsInTouch);

        return size;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Assert.IsTrue(_hasLazyInitialized);
        float[] vectorAction = actionBuffers.ContinuousActions.Select(x=>x).ToArray();

        actionTimeDelta = Time.fixedDeltaTime;
        if (!_decisionRequester.TakeActionsBetweenDecisions)
            actionTimeDelta = actionTimeDelta*_decisionRequester.DecisionPeriod;
        //_mapAnim2Ragdoll.OnStep(actionTimeDelta);
        //rig.TrackKinematics();
        _rewards2Learn.OnStep(actionTimeDelta);

        bool shouldDebug = _debugController != null;
        bool dontUpdateMotor = false;
        if (_debugController != null)
        {
            dontUpdateMotor = _debugController.DontUpdateMotor;
            dontUpdateMotor &= _debugController.isActiveAndEnabled;
            dontUpdateMotor &= _debugController.gameObject.activeInHierarchy;
            shouldDebug &= _debugController.isActiveAndEnabled;
            shouldDebug &= _debugController.gameObject.activeInHierarchy;
        }
        if (shouldDebug)
        {
            vectorAction = GetDebugActions(vectorAction);
        }

        if (!SkipActionSmoothing)
            vectorAction = SmoothActions(vectorAction);

        _ragDollMuscles.ApplyActions(vectorAction, ActionTimeDelta);

      
        previousActions = vectorAction;
        _observations2Learn.PreviousActions = vectorAction;

        AddReward(_rewards2Learn.Reward);

        if (ReproduceDReCon)
        {
            // DReCon Logic
            if (_rewards2Learn.HeadHeightDistance > 1f || _rewards2Learn.Reward <= 0f)
            {
                if (!dontResetOnZeroReward)
                    EndEpisode();
            }
            else if (_rewards2Learn.Reward <= 0.1f && !dontSnapMocapToRagdoll)
            {
                Transform ragDollCom = _observations2Learn.GetRagDollCOM();
                Vector3 snapPosition = ragDollCom.position;
                // snapPosition.y = 0f;
                var snapDistance = _mapAnim2Ragdoll.SnapTo(snapPosition);
                // AddReward(-.5f);
            }
        }
        else
        {
            // Our Logic
            bool terminate = false;
            terminate = terminate || _rewards2Learn.PositionReward < 1E-5f;
            if (StepCount > 4)  // HACK
                terminate = terminate || _rewards2Learn.ComVelocityReward < 1E-50f;
            // terminate = terminate || _dReConRewards.ComDirectionReward < .01f;
            if (_rewards2Learn.VelDifferenceReward > 0f && StepCount > 4) // HACK
                terminate = terminate || _rewards2Learn.VelDifferenceReward < 1E-10f;
            terminate = terminate || _rewards2Learn.LocalPoseReward < 1E-5f;
            // terminate = terminate || _dReConRewards.PositionReward < .01f;
            // // terminate = terminate || _dReConRewards.ComVelocityReward < .01f;
            // terminate = terminate || _dReConRewards.ComDirectionReward < .01f;
            // if (_dReConRewards.VelDifferenceReward > 0f) // HACK
            //     terminate = terminate || _dReConRewards.VelDifferenceReward < .01f;
            // terminate = terminate || _dReConRewards.LocalPoseReward < .01f;
            if (dontResetOnZeroReward)
                terminate = false;
            if (terminate)
            {
                EndEpisode();
            }
            else if (!dontSnapMocapToRagdoll)
            {
                Transform ragDollCom = _observations2Learn.GetRagDollCOM();
                Vector3 snapPosition = ragDollCom.position;
                // snapPosition.y = 0f;
                var snapDistance = _mapAnim2Ragdoll.SnapTo(snapPosition);
                // AddReward(-.5f);
            }            
        }
    }
    float[] GetDebugActions(float[] vectorAction)
    {
        var debugActions = new List<float>();
        foreach (var m in _motors)
        {
            if (m.isRoot)
                continue;
            DebugMotor debugMotor = m.GetComponent<DebugMotor>();
            if (debugMotor == null)
            {
                debugMotor = m.gameObject.AddComponent<DebugMotor>();
            }
            // clip to -1/+1
            debugMotor.Actions = new Vector3(
                Mathf.Clamp(debugMotor.Actions.x, -1f, 1f),
                Mathf.Clamp(debugMotor.Actions.y, -1f, 1f),
                Mathf.Clamp(debugMotor.Actions.z, -1f, 1f)
            );
            Vector3 targetNormalizedRotation = debugMotor.Actions;

			if (m.jointType != ArticulationJointType.SphericalJoint)
                continue;
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.x);
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.y);
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.z);
        }

        debugActions = debugActions.Select(x => Mathf.Clamp(x, -1f, 1f)).ToList();
        if (_debugController.ApplyRandomActions)
        {
            debugActions = debugActions
                .Select(x => UnityEngine.Random.Range(-_debugController.RandomRange, _debugController.RandomRange))
                .ToList();
        }

        _debugController.Actions = debugActions.ToArray();
        return debugActions.ToArray();
    }

    float[] SmoothActions(float[] vectorAction)
    {
        // yt =β at +(1−β)yt−1
        var smoothedActions = vectorAction
            .Zip(_observations2Learn.PreviousActions, (a, y) => ActionSmoothingBeta * a + (1f - ActionSmoothingBeta) * y)
            .ToArray();
        return smoothedActions;
    }
    float[] GetActionsFromRagdollState()
    {
        var vectorActions = new List<float>();
        foreach (var m in _motors)
        {
            if (m.isRoot)
                continue;
            int i = 0;
			if (m.jointType != ArticulationJointType.SphericalJoint)
                continue;
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = m.xDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = m.jointPosition[i++] * Mathf.Rad2Deg;
                var target = (deg - midpoint) / scale;
                vectorActions.Add(target);
            }
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = m.yDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = m.jointPosition[i++] * Mathf.Rad2Deg;
                var target = (deg - midpoint) / scale;
                vectorActions.Add(target);
            }
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = m.zDrive;
                var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var deg = m.jointPosition[i++] * Mathf.Rad2Deg;
                var target = (deg - midpoint) / scale;
                vectorActions.Add(target);
            }
        }
        return vectorActions.ToArray();
    }
    public override void Initialize()
    {
        Assert.IsTrue(_hasAwake);
        Assert.IsFalse(_hasLazyInitialized);
        _hasLazyInitialized = true;

        _decisionRequester = GetComponent<DecisionRequester>();
        _debugController = FindObjectOfType<MarathonTestBedController>();
        Time.fixedDeltaTime = FixedDeltaTime;
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();

        if (_debugController != null)
        {
            dontResetOnZeroReward = true;
            dontSnapMocapToRagdoll = true;
        }

        _mapAnim2Ragdoll = _spawnableEnv.GetComponentInChildren<MapAnim2Ragdoll>();
        //_mocapBodyParts = _mapAnim2Ragdoll.GetRigidBodies();

        _observations2Learn = GetComponent<Observations2Learn>();
        _rewards2Learn = GetComponent<Rewards2Learn>();

        _ragDollMuscles = GetComponent<ArticulationMusclesSimplified>();
      



        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        _sensorObservations = GetComponent<SensorObservations>();


        _motors = GetComponentsInChildren<ArticulationBody>()
            .Where(x => x.jointType == ArticulationJointType.SphericalJoint)
            .Where(x => !x.isRoot)
            .Distinct()
            .ToList();
        //var individualMotors = new List<float>();

        




        _observations2Learn.PreviousActions = GetActionsFromRagdollState();
        previousActions = GetActionsFromRagdollState();

        _controllerToMimic = _mapAnim2Ragdoll.GetComponent<IAnimationController>();



        _mapAnim2Ragdoll.OnAgentInitialize();
        //it can only be used AFTER _mapAnim2Ragdoll is initialzed.
        //_ragDollMuscles.SetKinematicReference(_mapAnim2Ragdoll);//only used in mode PDopenloop

        _observations2Learn.OnAgentInitialize();
        _rewards2Learn.OnAgentInitialize(ReproduceDReCon);
        _controllerToMimic.OnAgentInitialize();

     
        _hasLazyInitialized = true;
    }
    public override void OnEpisodeBegin()
    {
        Assert.IsTrue(_hasAwake);
        //_smoothedActions = null;
        debugCopyMocap = false;

        Vector3 resetVelocity = Vector3.zero;
        if (_inputController != null)
        {
            // _inputController.OnReset();
            _controllerToMimic.OnReset();
            // resets to source anim
            // var angle = Vector3.SignedAngle(Vector3.forward, _inputController.HorizontalDirection, Vector3.up);
            // var rotation = Quaternion.Euler(0f, angle, 0f);
            var rotation = _mapAnim2Ragdoll.transform.rotation;
            _mapAnim2Ragdoll.OnReset(rotation);
            _mapAnim2Ragdoll.CopyStatesTo(this.gameObject);
            resetVelocity = _controllerToMimic.GetDesiredVelocity();
            _mapAnim2Ragdoll.CopyVelocityTo(this.gameObject, resetVelocity);
        }
        else
        {
            _controllerToMimic.OnReset();
            // source anim is continious
            var rotation = _mapAnim2Ragdoll.transform.rotation;
            _mapAnim2Ragdoll.OnReset(rotation);
            resetVelocity = _controllerToMimic.GetDesiredVelocity();
            _mapAnim2Ragdoll.CopyStatesTo(this.gameObject);
            _mapAnim2Ragdoll.CopyVelocityTo(this.gameObject, resetVelocity);
        }

        _observations2Learn.OnReset();
        _rewards2Learn.OnReset();
        // float timeDelta = float.Epsilon;
        // _dReConObservations.OnStep(timeDelta);
        // _dReConRewards.OnStep(timeDelta);
#if UNITY_EDITOR		
		if (DebugPauseOnReset)
		{
	        UnityEditor.EditorApplication.isPaused = true;
		}
#endif	        
        if (_debugController != null && _debugController.isActiveAndEnabled)
        {
            _debugController.OnAgentEpisodeBegin();
        }
        _observations2Learn.PreviousActions = GetActionsFromRagdollState();
    }


    void FixedUpdate()
    {
        if (debugCopyMocap)
        {
            EndEpisode();
        }
    }
    void OnDrawGizmos()
    {
        if (_rewards2Learn == null || _inputController == null)
            return;
        var comTransform = _rewards2Learn._ragDollBodyStats.transform;
        var vector = new Vector3(_inputController.MovementVector.x, 0f, _inputController.MovementVector.y);
        var pos = new Vector3(comTransform.position.x, 0.001f, comTransform.position.z);
        DrawArrow(pos, vector, Color.black);
    }
    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        float headSize = 0.25f;
        float headAngle = 20.0f;
        Gizmos.color = color;
        Gizmos.DrawRay(start, vector);
        if (vector != Vector3.zero)
        {
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 + headAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 - headAngle, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}

