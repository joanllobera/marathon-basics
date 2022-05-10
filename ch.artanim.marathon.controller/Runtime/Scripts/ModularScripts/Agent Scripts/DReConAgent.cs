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
using Unity.MLAgents.Policies;


public class DReConAgent : Agent, IRememberPreviousActions, IEventsAgent
{
    [Header("Settings")]

    [SerializeField]
    private float fixedDeltaTime = 1f / 60f;
    [SerializeField]
    private float actionSmoothingBeta = 0.2f;
    [SerializeField]
    private int maxStep = 0;

    [SerializeField]
    GameObject kinematicRigObject;

    IKinematicReference kinematicRig;

    [SerializeField]
    ObservationSignal observationSignal;

    [SerializeField]
    RewardSignal rewardSignal;

    [SerializeField]
    Muscles ragDollMuscles;


    DecisionRequester decisionRequester;
    BehaviorParameters behaviorParameters;

    float[] previousActions;
    public float[] PreviousActions { get => previousActions;}



    bool hasLazyInitialized;

    public event EventHandler<AgentEventArgs> onActionHandler;
    public event EventHandler<AgentEventArgs> onBeginHandler;


    public float ObservationTimeDelta => fixedDeltaTime * decisionRequester.DecisionPeriod;

    public float ActionTimeDelta => decisionRequester.TakeActionsBetweenDecisions ? fixedDeltaTime : fixedDeltaTime * decisionRequester.DecisionPeriod;
    public float FixedDeltaTime { get => fixedDeltaTime; }

    public int ActionSpaceSize => ragDollMuscles.ActionSpaceSize;

    public int ObservationSpaceSize => observationSignal.Size;

    public override void Initialize()
    {
        this.MaxStep = maxStep;
        Assert.IsFalse(hasLazyInitialized);
        hasLazyInitialized = true;
        Time.fixedDeltaTime = fixedDeltaTime;
        
        decisionRequester = GetComponent<DecisionRequester>();
    }

    private void Start()
    {
        rewardSignal.OnAgentInitialize();
        observationSignal.OnAgentInitialize();

        if (kinematicRigObject != null)
        {
            kinematicRig = kinematicRigObject.GetComponent<IKinematicReference>();
            kinematicRig.OnAgentInitialize();
        }

        if (ragDollMuscles == null) ragDollMuscles = GetComponent<Muscles>();
        ragDollMuscles.OnAgentInitialize();
        previousActions = ragDollMuscles.GetActionsFromState();
    }

    override public void CollectObservations(VectorSensor sensor)
    {
        Assert.IsTrue(hasLazyInitialized);

        observationSignal.PopulateObservations(sensor);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Assert.IsTrue(hasLazyInitialized);
        float[] vectorAction = actionBuffers.ContinuousActions.ToArray();
        vectorAction = SmoothActions(vectorAction);

        ragDollMuscles.ApplyActions(vectorAction, ActionTimeDelta);

        previousActions = vectorAction;

        float currentReward = rewardSignal.Reward;
        AddReward(currentReward);

        onActionHandler?.Invoke(this, new AgentEventArgs(vectorAction, currentReward));
    }
    public override void OnEpisodeBegin()
    {
        previousActions = ragDollMuscles.GetActionsFromState();

        onBeginHandler?.Invoke(this, AgentEventArgs.Empty);
    }



    float[] SmoothActions(float[] vectorAction)
    {
        var smoothedActions = vectorAction
            .Zip(PreviousActions, (a, y) => actionSmoothingBeta * a + (1f - actionSmoothingBeta) * y)
            .ToArray();
        return smoothedActions;
    }
}

public interface IRememberPreviousActions
{
    public float[] PreviousActions { get; }
}

public interface IEventsAgent
{
    public event EventHandler<AgentEventArgs> onActionHandler;
    public event EventHandler<AgentEventArgs> onBeginHandler;
}

public class AgentEventArgs: EventArgs
{
    public float[] actions;
    public float reward;

    public AgentEventArgs(float[] actions, float reward)
    {
        this.actions = actions;
        this.reward = reward;
    }

    new public static AgentEventArgs Empty => new AgentEventArgs(new float[0], 0f);
        
}

