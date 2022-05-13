using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


using Unity.Mathematics;
using Unity.MLAgents;//for the DecisionRequester

using MotorUpdate;

public class ArticulationMusclesSimplified : ModularMuscles
{
    //This class takes care of connecting the MotorUpdateRule with Muscles in the form of ArticulationBody components


    [System.Serializable]
    public class MusclePower
    {
        public string Muscle;
        public Vector3 PowerVector;
    }



    [Header("Parameters for Legacy and PD:")]
    public List<MusclePower> MusclePowers;

    //  public float MotorScale = 1f;
    public float Stiffness = 50f;
    public float Damping = 100f;
    public float ForceLimit = float.MaxValue;
    public float DampingRatio = 1.0f;


    [Header("Extra Parameters for PD:")]

    public float NaturalFrequency = 40f;
    public float ForceScale = .3f;




    [Header("Parameters for StablePD:")]
    public float KP_Stiffness = 50;
    public float ForceScaleSPD = .3f;






    [Header("Debug Collisions")]
    [SerializeField]
    bool skipCollisionSetup;



    List<ArticulationBody> _motors;

    


    public override int ActionSpaceSize
    {
        get => GetActionsFromState().Length;
    }



#if USE_LSPD
    private LSPDHierarchy _lpd;
#endif




    //for the PDopenloop case:
    public List<Transform> _referenceTransforms;



    // Use this for initialization
    void Awake()
    {
         Setup();

        _motors = GetArticulationMotors();

      


        if (updateRule != null)
            updateRule.Initialize(this);
        else
            Debug.LogError("there is no update rule that corresponds to classicPD update");

         
    }

    // Update is called once per frame
    void Update()
    {

      


    }
    
    void Setup()
    {

        if (!skipCollisionSetup)
        {

            // handle collision overlaps
            IgnoreCollision("articulation:Spine2", new[] { "LeftArm", "RightArm" });
            IgnoreCollision("articulation:Hips", new[] { "RightUpLeg", "LeftUpLeg" });

            IgnoreCollision("LeftForeArm", new[] { "LeftArm" });
            IgnoreCollision("RightForeArm", new[] { "RightArm" });
            IgnoreCollision("RightLeg", new[] { "RightUpLeg" });
            IgnoreCollision("LeftLeg", new[] { "LeftUpLeg" });

            IgnoreCollision("RightLeg", new[] { "RightFoot" });
            IgnoreCollision("LeftLeg", new[] { "LeftFoot" });

        }

        //
        var joints = GetComponentsInChildren<Joint>().ToList();
        foreach (var joint in joints)
            joint.enablePreprocessing = false;
    }
    void IgnoreCollision(string first, string[] seconds)
    {
        foreach (var second in seconds)
        {
            IgnoreCollision(first, second);
        }
    }
    void IgnoreCollision(string first, string second)
    {
        var rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
        var colliderOnes = rigidbodies.FirstOrDefault(x => x.name.Contains(first))?.GetComponents<Collider>();
        var colliderTwos = rigidbodies.FirstOrDefault(x => x.name.Contains(second))?.GetComponents<Collider>();
        if (colliderOnes == null || colliderTwos == null)
            return;
        foreach (var c1 in colliderOnes)
            foreach (var c2 in colliderTwos)
                Physics.IgnoreCollision(c1, c2);
    }
    

    void ApplyRuleAsRelativeTorques(float3[] targetRotation) {



        float3[] torques = updateRule.GetJointForces( targetRotation);
        for (int i = 0; i < _motors.Count; i++) {

             _motors[i].AddRelativeTorque(torques[i]);
        
        }

    }

    public override float[] GetActionsFromState()
    {
        var vectorActions = new List<float>();
        foreach (ArticulationBody m in _motors)
        {

            float3 targetRotation = Mathf.Deg2Rad * Utils.GetSwingTwist(m.transform.localRotation);

            if (m.isRoot)
                continue;
            int i = 0;
            if (m.jointType != ArticulationJointType.SphericalJoint)
                continue;
            if (m.twistLock != ArticulationDofLock.LockedMotion)
            {
                var target = targetRotation.x;
                vectorActions.Add(target);
            }
            if (m.swingYLock != ArticulationDofLock.LockedMotion)
            {
                var target = targetRotation.y;
                vectorActions.Add(target);
            }
            if (m.swingZLock != ArticulationDofLock.LockedMotion)
            {
                var target = targetRotation.z;
                vectorActions.Add(target);
            }
        }
        return vectorActions.ToArray();
    }



    //This function is to debug the motor update modes, mimicking a reference animation.
    /// To be used only with the root frozen, "hand of god" mode, it will not work on a 
    /// proper physics character
    /*
    public void MimicRigidBodies(List<Rigidbody> targets, float deltaTime)
    {
        Vector3[] targetRotations = new Vector3[_motors.Count];

        int im = 0;
        foreach (var a in targets)
        {
            targetRotations[im] = Mathf.Deg2Rad * Utils.GetSwingTwist(a.transform.localRotation);
            im++;
        }

   
#if USE_LSPD
                _lpd.LaunchMimicry(targetRotations);
#else
                Debug.LogError("To use this functionality you need to import the Artanim LSPD package");
#endif
              


         //default:

                int j = 0;
                foreach (var imotor in _motors)
                {

                     ArticulationBody m = (ArticulationBody)imotor;

                    if (m.isRoot)
                    {
                        continue; //never happens because excluded from list
                    }
                    else
                    {
                        //we find the normalized rotation that corresponds to the target rotation (the inverse of what happens inside UpdateMotorWithPD
                        ArticulationBody joint = m;

                        Vector3 normalizedRotation = new Vector3();




                        if (joint.twistLock == ArticulationDofLock.LimitedMotion)
                        {
                            var drive = joint.xDrive;
                            var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                            var midpoint = drive.lowerLimit + scale;
                            normalizedRotation.x = (Mathf.Rad2Deg * targetRotations[j].x - midpoint) / scale;
                            //target.x = midpoint + (targetNormalizedRotation.x * scale);

                        }

                        if (joint.swingYLock == ArticulationDofLock.LimitedMotion)
                        {
                            var drive = joint.yDrive;
                            var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                            var midpoint = drive.lowerLimit + scale;
                            normalizedRotation.y = (Mathf.Rad2Deg * targetRotations[j].y - midpoint) / scale;
                            //target.y = midpoint + (targetNormalizedRotation.y * scale);


                        }

                        if (joint.swingZLock == ArticulationDofLock.LimitedMotion)
                        {
                            var drive = joint.zDrive;
                            var scale = (drive.upperLimit - drive.lowerLimit) / 2f;
                            var midpoint = drive.lowerLimit + scale;
                            normalizedRotation.z = (Mathf.Rad2Deg * targetRotations[j].z - midpoint) / scale;
                            //target.z = midpoint + (targetNormalizedRotation.z * scale);

                        }



                       // UpdateMotor(m, normalizedRotation, Time.fixedDeltaTime);
                    }

                    j++;

                }
               
    



    }

    */


    public override void ApplyActions(float[] actions, float actionTimeDelta)
    {

        int i = 0;//keeps track of hte number of actions

        int j = 0;//keeps track of the number of motors

        float3[] targetRots = new float3[_motors.Count];

        foreach (var imotor in _motors)
        {
            ArticulationBody m = (ArticulationBody)imotor;
            if (m.isRoot)
                continue;
         
            Vector3 targetRot = Vector3.zero;
            if (m.jointType != ArticulationJointType.SphericalJoint)
                continue;
            if (m.twistLock != ArticulationDofLock.LockedMotion)
                targetRot.x = actions[i++];
            if (m.swingYLock != ArticulationDofLock.LockedMotion)
                targetRot.y = actions[i++];
            if (m.swingZLock != ArticulationDofLock.LockedMotion)
                targetRot.z = actions[i++];

            j++;

        }

        ApplyRuleAsRelativeTorques(targetRots);

    }



    #region ClassicPDandFriends
    List<ArticulationBody> GetArticulationMotors()
    {
       

        return GetComponentsInChildren<ArticulationBody>()
                .Where(x => x.jointType == ArticulationJointType.SphericalJoint)
                .Where(x => !x.isRoot)
                .Distinct()
                .ToList();


    }

    public override List<IArticulation> GetMotors()
    {
        List<IArticulation> result = new List<IArticulation>();
        List<ArticulationBody> abl = GetArticulationMotors();

      
            foreach (ArticulationBody a in abl)
            {
                result.Add(new ArticulationBodyAdapter(a));
            }

        return result;



    }


    protected float GetActionTimeDelta(GameObject agentObject)
    {
        DecisionRequester _decisionRequester = agentObject.GetComponent<DecisionRequester>();
        return _decisionRequester.TakeActionsBetweenDecisions ? Time.fixedDeltaTime : Time.fixedDeltaTime * _decisionRequester.DecisionPeriod;
    }





    #endregion


    public void FixedUpdate()
    {
        /*
#if USE_LSPD
        switch (MotorUpdateMode)
        {

            case (MotorMode.LSPD):

                _lpd.CompleteMimicry();

                break;

        }
#endif
        */
    }

}
