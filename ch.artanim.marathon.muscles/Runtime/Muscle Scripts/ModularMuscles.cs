using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotorUpdate;
using Unity.MLAgents;

public abstract class ModularMuscles : Muscles
{

    [SerializeField]
    protected MotorUpdateRule updateRule;


    public abstract List<IArticulation> GetMotors();




}
