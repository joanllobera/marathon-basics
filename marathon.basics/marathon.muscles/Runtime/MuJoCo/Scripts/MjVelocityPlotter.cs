using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Linq;

public class MjVelocityPlotter : MonoBehaviour
{
    [SerializeField]
    MjBody bodyToPlot;

    [DebugGUIGraph(min: 0, max: 5, r: 1, g: 0, b: 0, autoScale: true)]
    float velX;
    [DebugGUIGraph(min: 0, max: 5, r: 0, g: 1, b: 0, autoScale: true)]
    float velY;
    [DebugGUIGraph(min: 0, max: 5, r: 0, g: 0, b: 1, autoScale: true)]
    float velZ;

    private Vector3 curVel;


    private void Update()
    {
        curVel = bodyToPlot.GlobalVelocity();
        velX = curVel.x;
        velY = curVel.y;
        velZ = curVel.z;
    }
}
