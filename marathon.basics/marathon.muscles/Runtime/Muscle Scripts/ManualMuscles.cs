using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotorUpdate;
using Mujoco;
using System.Linq;
using System;

public class ManualMuscles : Muscles
{
    [SerializeField]
    private List<ActuatorPositionTarget> actuatorGameObjects;

    [SerializeField]
    MotorUpdateRule updateRule;


    public override int ActionSpaceSize => actuatorGameObjects.Count;


    unsafe public override void ApplyActions(float[] actions, float actionTimeDelta)
    {
        foreach((var motor, var action) in actuatorGameObjects.Zip(actions, Tuple.Create))
        {
            motor.actuator.Control = action;
        }
    }

    public override float[] GetActionsFromState()
    {
        return actuatorGameObjects.Select(a => a.actuator.Control).ToArray();
    }

    private void Awake()
    {

        foreach (var a in actuatorGameObjects)
        {
            a.state = new MjHingeJointState(a.actuator.Joint as MjHingeJoint);
        }

        MjScene.Instance.preUpdateEvent += UpdateTorques;

    }

    private void Start()
    {

    }

    private unsafe void Sync()
    {
        foreach(var a in actuatorGameObjects)
        {
            a.actuator.OnSyncState(MjScene.Instance.Data);
            a.actuator.Joint.OnSyncState(MjScene.Instance.Data);
        }
    }

    private unsafe void UpdateTorques(object Sender, EventArgs e)
    {
        Sync();
        //MujocoLib.mj_kinematics(MjScene.Instance.Model, MjScene.Instance.Data);
        float[] curActions = actuatorGameObjects.Select(a => updateRule.GetTorque(a.state, new StaticState(Mathf.Deg2Rad * a.target, 0f, 0f))).ToArray();
        ApplyActions(curActions, Time.fixedDeltaTime);
        Sync();
    }

    [Serializable]
    class ActuatorPositionTarget
    {
        [SerializeField]
        public MjActuator actuator;

        [SerializeField]
        public float target;

        public IState state;
    }

}
