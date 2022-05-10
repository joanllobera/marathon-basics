using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using ManyWorlds;
using UnityEngine.Assertions;

using System;

public class RagDollAgent : Agent 
{
    [Header("Settings")]
	public float FixedDeltaTime = 1f/60f;
    public float SmoothBeta = 0.2f;

    [Header("Camera")]

    public bool RequestCamera;
	public bool CameraFollowMe;
	public Transform CameraTarget;    

    [Header("... debug")]
    public bool SkipRewardSmoothing;
    public bool debugCopyMocap;
    public bool ignorActions;
    public bool dontResetOnZeroReward;
    public bool dontSnapMocapToRagdoll;
    public bool DebugPauseOnReset;
    public bool UsePDControl = true;

    List<Rigidbody> _mocapBodyParts;
    List<ArticulationBody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    DReConObservations _dReConObservations;
    DReConRewards _dReConRewards;
    RagDoll004 _ragDollSettings;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;
    List<ArticulationBody> _motors;
    MarathonTestBedController _debugController;  
    InputController _inputController;
    SensorObservations _sensorObservations;
    DecisionRequester _decisionRequester;
    MocapAnimatorController _mocapAnimatorController;


    bool _hasLazyInitialized;
    float[] _smoothedActions;
    float[] _mocapTargets;

    [Space(16)]
    [SerializeField]
    bool _hasAwake = false;
    MocapControllerArtanim _mocapControllerArtanim;

    void Awake()
    {
		if (RequestCamera && CameraTarget != null)
		{
            // Will follow the last object to be spawned
            var camera = FindObjectOfType<Camera>();
            if(camera != null) { 
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
        if (!_spawnableEnv.IsPointWithinBoundsInWorldSpace(_mocapControllerArtanim.transform.position))
        {
            _mocapControllerArtanim.transform.position = _spawnableEnv.transform.position;
            _trackBodyStatesInWorldSpace.LinkStatsToRigidBodies();
            EndEpisode();
        }
    }
	override public void CollectObservations(VectorSensor sensor)
    {
        Assert.IsTrue(_hasLazyInitialized);

        float timeDelta = Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
        _dReConObservations.OnStep(timeDelta);

        sensor.AddObservation(_dReConObservations.MocapCOMVelocity);
        sensor.AddObservation(_dReConObservations.RagDollCOMVelocity);
        sensor.AddObservation(_dReConObservations.RagDollCOMVelocity-_dReConObservations.MocapCOMVelocity);
        sensor.AddObservation(_dReConObservations.InputDesiredHorizontalVelocity);
        sensor.AddObservation(_dReConObservations.InputJump);
        sensor.AddObservation(_dReConObservations.InputBackflip);
        sensor.AddObservation(_dReConObservations.HorizontalVelocityDifference);
        // foreach (var stat in _dReConObservations.MocapBodyStats)
        // {
        //     sensor.AddObservation(stat.Position);
        //     sensor.AddObservation(stat.Velocity);
        // }
        foreach (var stat in _dReConObservations.RagDollBodyStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }                
        foreach (var stat in _dReConObservations.BodyPartDifferenceStats)
        {
            sensor.AddObservation(stat.Position);
            sensor.AddObservation(stat.Velocity);
        }
        sensor.AddObservation(_dReConObservations.PreviousActions);
        
        // add sensors (feet etc)
        sensor.AddObservation(_sensorObservations.SensorIsInTouch);
    }
	public override void OnActionReceived(ActionBuffers actions)
    {
        float[] vectorAction = actions.ContinuousActions.Select(x=>x).ToArray();
        
        Assert.IsTrue(_hasLazyInitialized);

        float timeDelta = Time.fixedDeltaTime;
        if (!_decisionRequester.TakeActionsBetweenDecisions)
            timeDelta = timeDelta*_decisionRequester.DecisionPeriod;
        _dReConRewards.OnStep(timeDelta);        

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
        if (UsePDControl)
        {
            var targets = GetMocapTargets();
            vectorAction = vectorAction
                .Zip(targets, (action, target)=> Mathf.Clamp(target + action *2f, -1f, 1f))
                .ToArray();
        }
        if (!SkipRewardSmoothing)
            vectorAction = SmoothActions(vectorAction);
        if (ignorActions)
            vectorAction = vectorAction.Select(x=>0f).ToArray();
		int i = 0;
		foreach (var m in _motors)
		{
            if (m.isRoot)
                continue;
            if (dontUpdateMotor)
                continue;
            Vector3 targetNormalizedRotation = Vector3.zero;

			if (m.twistLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.x = vectorAction[i++];
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.y = vectorAction[i++];
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.z = vectorAction[i++];
            UpdateMotor(m, targetNormalizedRotation);
        }
        _dReConObservations.PreviousActions = vectorAction;

        AddReward(_dReConRewards.Reward);

        // if (_dReConRewards.HeadHeightDistance > 0.5f || _dReConRewards.Reward < 1f)
        if (_dReConRewards.HeadHeightDistance > 0.5f || _dReConRewards.Reward <= 0f)

        {
            if (!dontResetOnZeroReward)
                EndEpisode();
        }
        // else if (_dReConRewards.HeadDistance > 1.5f)
        else if (_dReConRewards.Reward <= 0.1f && !dontSnapMocapToRagdoll)
        {
            Transform ragDollCom = _dReConObservations.GetRagDollCOM();
            Vector3 snapPosition = ragDollCom.position;
            snapPosition.y = 0f;
            _mocapControllerArtanim.SnapTo(snapPosition);
            AddReward(-.5f);
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
            debugMotor.Actions = new Vector3 (
                Mathf.Clamp(debugMotor.Actions.x, -1f, 1f),
                Mathf.Clamp(debugMotor.Actions.y, -1f, 1f),
                Mathf.Clamp(debugMotor.Actions.z, -1f, 1f)
            );
            Vector3 targetNormalizedRotation = debugMotor.Actions;

            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.x);
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.y);
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                debugActions.Add(targetNormalizedRotation.z);
        }
        
        debugActions = debugActions.Select(x=>Mathf.Clamp(x,-1f,1f)).ToList();
        _debugController.Actions = debugActions.ToArray();
        return debugActions.ToArray();
    }

    float[] SmoothActions(float[] vectorAction)
    {
        // yt =β at +(1−β)yt−1
        if (_smoothedActions == null)
            _smoothedActions = vectorAction.Select(x=>0f).ToArray();
        _smoothedActions = vectorAction
            .Zip(_smoothedActions, (a, y)=> SmoothBeta * a + (1f-SmoothBeta) * y)
            .ToArray();
        return _smoothedActions;
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
            UsePDControl = false;
        }

        _mocapControllerArtanim = _spawnableEnv.GetComponentInChildren<MocapControllerArtanim>();
        _mocapBodyParts = _mocapControllerArtanim.GetRigidBodies();

        _bodyParts = GetComponentsInChildren<ArticulationBody>().ToList();
        _dReConObservations = GetComponent<DReConObservations>();
        _dReConRewards = GetComponent<DReConRewards>();

        _trackBodyStatesInWorldSpace = _mocapControllerArtanim.GetComponent<TrackBodyStatesInWorldSpace>();

        _ragDollSettings = GetComponent<RagDoll004>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        _sensorObservations = GetComponent<SensorObservations>();

        foreach (var body in GetComponentsInChildren<ArticulationBody>())
        {
            body.solverIterations = 255;
            body.solverVelocityIterations = 255;
        }

        _motors = GetComponentsInChildren<ArticulationBody>()
            .Where(x=>x.jointType == ArticulationJointType.SphericalJoint)
            .Where(x=>!x.isRoot)
            .Distinct()
            .ToList();
        var individualMotors = new List<float>();
        foreach (var m in _motors)
        {
            if (m.twistLock == ArticulationDofLock.LimitedMotion)
                individualMotors.Add(0f);
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                individualMotors.Add(0f);
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                individualMotors.Add(0f);
        }
        _dReConObservations.PreviousActions = individualMotors.ToArray();

        //_mocapAnimatorController = _mocapControllerArtanim.GetComponentInChildren<MocapAnimatorController>();
        _mocapAnimatorController = _mocapControllerArtanim.GetComponent<MocapAnimatorController>();



        _mocapControllerArtanim.OnAgentInitialize();
        _dReConObservations.OnAgentInitialize();
        _dReConRewards.OnAgentInitialize();
        _trackBodyStatesInWorldSpace.OnAgentInitialize();
        _mocapAnimatorController.OnAgentInitialize();
        _inputController.OnReset();

        _hasLazyInitialized = true;
    }
	public override void OnEpisodeBegin()
	{
        Assert.IsTrue(_hasAwake);
        _smoothedActions = null;
        debugCopyMocap = false;

        _mocapAnimatorController.OnReset();
        var angle = Vector3.SignedAngle(Vector3.forward, _inputController.HorizontalDirection, Vector3.up);
        var rotation = Quaternion.Euler(0f, angle, 0f);
        _mocapControllerArtanim.OnReset(rotation);
        _mocapControllerArtanim.CopyStatesTo(this.gameObject);

        // _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
        float timeDelta = float.MinValue;
        _dReConObservations.OnReset();
        _dReConRewards.OnReset();
        _dReConObservations.OnStep(timeDelta);
        _dReConRewards.OnStep(timeDelta);
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
    }   

    float[] GetMocapTargets()
    {
        if (_mocapTargets == null)
        {
            _mocapTargets = _motors
                .Where(x=>!x.isRoot)
                .SelectMany(x => {
                    List<float> list = new List<float>();
                    if (x.twistLock == ArticulationDofLock.LimitedMotion)
                        list.Add(0f);
                    if (x.swingYLock == ArticulationDofLock.LimitedMotion)
                        list.Add(0f);
                    if (x.swingZLock == ArticulationDofLock.LimitedMotion)
                        list.Add(0f);
                    return list.ToArray();
                })
                .ToArray();
        }
        int i=0;
        foreach (var joint in _motors)
		{
            if (joint.isRoot)
                continue;
            Rigidbody mocapBody = _mocapBodyParts.First(x=>x.name == joint.name);
            Vector3 targetRotationInJointSpace = -(Quaternion.Inverse(joint.anchorRotation) * Quaternion.Inverse(mocapBody.transform.localRotation) * joint.parentAnchorRotation).eulerAngles;
            targetRotationInJointSpace = new Vector3(
                Mathf.DeltaAngle(0, targetRotationInJointSpace.x),
                Mathf.DeltaAngle(0, targetRotationInJointSpace.y),
                Mathf.DeltaAngle(0, targetRotationInJointSpace.z));
            if (joint.twistLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = joint.xDrive;
                var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var target = (targetRotationInJointSpace.x -midpoint) / scale;
                _mocapTargets[i] = target;
                i++;
            }
            if (joint.swingYLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = joint.yDrive;
                var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var target = (targetRotationInJointSpace.y -midpoint) / scale;
                _mocapTargets[i] = target;
                i++;
            }
            if (joint.swingZLock == ArticulationDofLock.LimitedMotion)
            {
                var drive = joint.zDrive;
                var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
                var midpoint = drive.lowerLimit + scale;
                var target = (targetRotationInJointSpace.z -midpoint) / scale;
                _mocapTargets[i] = target;
                i++;
            }
        }
        return _mocapTargets;
    }

    void UpdateMotor(ArticulationBody joint, Vector3 targetNormalizedRotation)
    {
        //Vector3 power = _ragDollSettings.MusclePowers.First(x=>x.Muscle == joint.name).PowerVector;

        Vector3 power = Vector3.zero;
        try
        {
            power = _ragDollSettings.MusclePowers.First(x => x.Muscle == joint.name).PowerVector;

        }
        catch (Exception e)
        {
            Debug.Log("there is no muscle for joint " + joint.name);

        }


        power *= _ragDollSettings.Stiffness;
        float damping = _ragDollSettings.Damping;
        float forceLimit = _ragDollSettings.ForceLimit;

        if (joint.twistLock == ArticulationDofLock.LimitedMotion)
        {
            var drive = joint.xDrive;
            var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
            var midpoint = drive.lowerLimit + scale;
            var target = midpoint + (targetNormalizedRotation.x *scale);
            drive.target = target;
            drive.stiffness = power.x;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;
        }

        if (joint.swingYLock == ArticulationDofLock.LimitedMotion)
        {
            var drive = joint.yDrive;
            var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
            var midpoint = drive.lowerLimit + scale;
            var target = midpoint + (targetNormalizedRotation.y *scale);
            drive.target = target;
            drive.stiffness = power.y;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.yDrive = drive;
        }

        if (joint.swingZLock == ArticulationDofLock.LimitedMotion)
        {
            var drive = joint.zDrive;
            var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
            var midpoint = drive.lowerLimit + scale;
            var target = midpoint + (targetNormalizedRotation.z *scale);
            drive.target = target;
            drive.stiffness = power.z;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.zDrive = drive;
        }
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
        if (_dReConRewards == null)
            return;
        var comTransform = _dReConRewards._ragDollBodyStats.transform;
        var vector = new Vector3( _inputController.MovementVector.x, 0f, _inputController.MovementVector.y);
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
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180+headAngle,0) * new Vector3(0,0,1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0,180-headAngle,0) * new Vector3(0,0,1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}
