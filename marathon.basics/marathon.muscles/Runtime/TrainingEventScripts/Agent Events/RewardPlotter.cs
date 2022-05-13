using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class RewardPlotter : TrainingEventHandler
{
    [SerializeField]
    Agent agent;

    IEventsAgent eventsAgent;
    public override EventHandler Handler => PrintReward;

    [DebugGUIGraph(min: 0, max: 2, r: 0, g: 1, b: 0, autoScale: true)]
    private float curReward;

    private void Awake()
    {
        eventsAgent = agent as IEventsAgent;
        curReward = 0;
        eventsAgent.onActionHandler += UpdateReward;

    }

    private void UpdateReward(object sender, AgentEventArgs args)
    {
        curReward = args.reward;
    }

    public void PrintReward(object sender, EventArgs args)
    {

        Debug.Log($"Current reward: {curReward}");

    }
}
