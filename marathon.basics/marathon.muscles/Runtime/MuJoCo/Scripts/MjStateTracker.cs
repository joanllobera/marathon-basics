using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotorUpdate;
using Mujoco;

public class MjStateTracker : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    GameObject mjObjectToTrack;

    IState trackedState;

    [DebugGUIGraph(min: -8f, max: 8f, autoScale: false, r: 1, g: 0, b: 0)]
    public float position;

    [DebugGUIGraph(min: -50f, max: 50, autoScale: false, r: 0, g: 1, b: 0)]
    public float velocity;

    [DebugGUIGraph(min: -150f, max: 150f, autoScale: false, r: 0, g: 0, b: 1)]
    public float acceleration;

    private void Awake()
    {
        trackedState = mjObjectToTrack.GetComponent<MjActuator>() ? new MjActuatorState(mjObjectToTrack.GetComponent<MjActuator>()) : new MjHingeJointState(mjObjectToTrack.GetComponent<MjHingeJoint>());
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    unsafe void FixedUpdate()
    {
        if (MjScene.Instance.Data == null) return;
        position = trackedState.Position;
        velocity = trackedState.Velocity;
        acceleration = trackedState.Velocity;
    }
}
