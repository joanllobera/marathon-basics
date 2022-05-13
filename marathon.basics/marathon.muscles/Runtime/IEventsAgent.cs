using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IEventsAgent
{
    public event EventHandler<AgentEventArgs> onActionHandler;
    public event EventHandler<AgentEventArgs> onBeginHandler;
}



public class AgentEventArgs : EventArgs
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


