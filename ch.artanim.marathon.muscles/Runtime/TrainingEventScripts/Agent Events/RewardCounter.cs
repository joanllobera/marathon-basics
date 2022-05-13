using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class RewardCounter : TrainingEventHandler
{
    [SerializeField]
    Agent agent;

    IEventsAgent eventsAgent;
    public override EventHandler Handler => PrintEvents;

    private float rewardSum;
    private float averageReward;
    private int numberOfSteps;

    private void Awake()
    {
        eventsAgent = agent as IEventsAgent;
        rewardSum = 0;
        averageReward = 0;
        numberOfSteps = 0;

        eventsAgent.onActionHandler += AccumulateRewards;

    }

    private void AccumulateRewards(object sender, AgentEventArgs args)
    {
        rewardSum += args.reward;
        numberOfSteps++;

        averageReward = rewardSum / numberOfSteps;
    }

    public void PrintEvents(object sender, EventArgs args)
    {
        
        Debug.Log($"Total reward: {rewardSum}\nAverage reward per action: {averageReward}");

        rewardSum = 0;
        numberOfSteps = 0;
        averageReward = 0;
    }
}
