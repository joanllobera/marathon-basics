using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotorUpdate;
using Unity.MLAgents;


using Unity.Mathematics;

public abstract class ModularMuscles : Muscles
{

    [SerializeField]
    protected MotorUpdateRule updateRule;

    protected List<IArticulation> _motors;

    public abstract List<IArticulation> GetMotors();


    void Awake()
    {
        //Setup();

        _motors = GetMotors();


        if (updateRule != null)
            updateRule.Initialize(this);
        else
            Debug.LogError("there is no motor update rule");


    }


    public void ApplyRuleAsRelativeTorques(float3[] targetRotation)
    {



        float3[] torques = updateRule.GetJointForces(targetRotation);
        for (int i = 0; i < _motors.Count; i++)
        {

            _motors[i].AddRelativeTorque(torques[i]);

        }

    }

}