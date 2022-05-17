using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//using ManyWorlds;
using MotorUpdate;
using Unity.Mathematics;

public class AnimationAsTargetPose : MonoBehaviour
{
 
    ModularMuscles _ragDollMuscles;

   
    MapAnim2Ragdoll _mapAnim2Ragdoll;
    float3[] targetRotations;

    List<MotorUpdate.IArticulation> lrb;

    List<IReducedState> targets;



    List<IReducedState> getTargets() {

      

        List<IReducedState> rbs = new List<IReducedState>();

        List<Rigidbody> rblistraw = transform.parent.GetComponentInChildren<MapAnim2Ragdoll>().GetRigidBodies();

        foreach (var m in lrb)
        {

            Rigidbody a = rblistraw.Find(x => x.name == m.gameObject.name);

            if (a != null)
                rbs.Add(new RigidbodyAdapter(a)) ;

        }
        return rbs;


    }

    // Start is called before the first frame update
    void OnEnable()
    {

      

        _mapAnim2Ragdoll = transform.parent.GetComponentInChildren<MapAnim2Ragdoll>();

        _ragDollMuscles = GetComponent<ModularMuscles>();
        lrb = _ragDollMuscles.GetMotors();

        targets = getTargets();
        targetRotations = new float3[targets.Count];

    }





    // Update is called once per frame
    void FixedUpdate()
    {
        float[] actions = _ragDollMuscles.GetActionsFromState();

        int im = 0;
        foreach (var a in targets)
        {
            targetRotations[im] = a.JointPosition;

            im++;
        }

        _ragDollMuscles.ApplyRuleAsRelativeTorques(targetRotations);
    }

}