using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using Unity.MLAgents;

namespace MotorUpdate
{
    [CreateAssetMenu(fileName = "ClassicPD", menuName = "ScriptableObjects/ClassicPD", order = 1)]
    public class ClassicPDUpdateRule : MotorUpdateRule
    {
              

       
        protected float dT = 1 / 60;

        List<IArticulation> _motors;
      
        public float DampingRatio = 1.0f;
        public float NaturalFrequency = 40f;

      


        public override void Initialize(ModularMuscles muscles = null, float dT = 1 / 60)
        {


            this.dT = GetActionTimeDelta(muscles.gameObject);

            _motors = muscles.GetMotors();

        }

        public override float3 GetRelativeTorque(IArticulation joint, float3 targetRotation)
        {
            //for AddRelativeTorque
            var m = joint.Mass;
          
            var n = NaturalFrequency; // n should be in the range 1..20
            var stiffness = Mathf.Pow(n, 2) * m;
            var damping = DampingRatio * (2 * Mathf.Sqrt(stiffness * m));
            

            float3 targetVel = Utils.AngularVelocityInReducedCoordinates(joint.JointPosition, targetRotation, dT);

            //we calculate the force:
            float3 torque = stiffness * (joint.JointPosition - targetRotation)*Mathf.Deg2Rad - damping * (joint.JointVelocity - targetVel) * Mathf.Deg2Rad;
            return torque;



        }


        public override float3[] GetJointForces(float3[] targetRotation)
        {
            float3[] result = new float3[_motors.Count];

            for (int i = 0; i < _motors.Count; i++)
                result[i] = GetRelativeTorque(_motors[i], targetRotation[i]);

            return result;

        }




    }
}