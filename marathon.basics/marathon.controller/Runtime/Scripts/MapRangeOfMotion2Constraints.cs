using System.Collections;
using System.Collections.Generic;

using System;
using System.Linq;
using UnityEngine;

using Unity.MLAgents.Policies;
using Unity.MLAgents;

public class MapRangeOfMotion2Constraints : MonoBehaviour
{

    /*
    [SerializeField]
    bool applyROMInGamePlay;

    public bool ApplyROMInGamePlay {  set => applyROMInGamePlay = value; }
    */


    public RangeOfMotionValues info2store;

    [Range(0, 359)]
    int MinROMNeededForJoint = 5;




    [SerializeField]
    bool debugWithLargestROM = false;

    [Tooltip("extra range for upper and lower angles of moving articulations ")]
    [SerializeField]
    float extraoffset = 10;


    public void ConfigureTrainingForRagdoll(int minROM)
    {

        MinROMNeededForJoint = minROM;

        int dof = ApplyRangeOfMotionToRagDoll();
        if (dof == -1)
        {
            Debug.LogError("Problems applying the range of motion to the ragdoll");
        }
        else
        {

            ApplyDoFOnBehaviorParameters(dof);
        }

    }


    public int ApplyRangeOfMotionToRagDoll()
    {
        if (info2store == null || info2store.Values.Length == 0)
            return -1;

        ArticulationBody[] articulationBodies = 
            GetComponentsInChildren<ArticulationBody>(true)
            .Where(x=>x.name.StartsWith("articulation:"))
            .ToArray();

        int DegreesOfFreedom = 0;


  
        foreach (ArticulationBody body in articulationBodies)
        {
            if (body.isRoot)
                continue;

            string keyword1 = "articulation:";
            string keyword2 = "collider:";
            string valuename = body.name.TrimStart(keyword1.ToArray<char>());
            valuename = valuename.TrimStart(keyword2.ToArray<char>());

            RangeOfMotionValue rom = info2store.Values.FirstOrDefault(x => x.name == valuename);

            if (rom == null)
            {
                Debug.LogError("Could not find a rangoe of motionvalue for articulation: " + body.name);
                continue;
                //return -1;
            }

            bool isLocked = true;
            body.twistLock = ArticulationDofLock.LockedMotion;
            body.swingYLock = ArticulationDofLock.LockedMotion;
            body.swingZLock = ArticulationDofLock.LockedMotion;
            body.jointType = ArticulationJointType.FixedJoint;

            body.anchorRotation = Quaternion.identity; //we make sure the anchor has no Rotation, otherwise the constraints do not make any sense

            if (rom.rangeOfMotion.x > (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.twistLock = ArticulationDofLock.LimitedMotion;
                var drive = body.xDrive;
                drive.lowerLimit = rom.lower.x - extraoffset;
                drive.upperLimit = rom.upper.x + extraoffset;
                body.xDrive = drive;
                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170;
                    drive.upperLimit = +170;
                }

            }
            if (rom.rangeOfMotion.y >= (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.swingYLock = ArticulationDofLock.LimitedMotion;
                var drive = body.yDrive;
                drive.lowerLimit = rom.lower.y - extraoffset;
                drive.upperLimit = rom.upper.y + extraoffset;
                body.yDrive = drive;

                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170 - extraoffset;
                    drive.upperLimit = +170 + extraoffset;
                }


            }
            if (rom.rangeOfMotion.z >= (float)MinROMNeededForJoint)
            {
                DegreesOfFreedom++;
                isLocked = false;
                body.swingZLock = ArticulationDofLock.LimitedMotion;
                var drive = body.zDrive;
                drive.lowerLimit = rom.lower.z - extraoffset;
                drive.upperLimit = rom.upper.z + extraoffset;
                body.zDrive = drive;

                if (debugWithLargestROM)
                {
                    drive.lowerLimit = -170;
                    drive.upperLimit = +170;
                }

            }

            if (!isLocked)
            {
                body.jointType = ArticulationJointType.SphericalJoint;
            }

        }

        return DegreesOfFreedom;

    }





    void ApplyDoFOnBehaviorParameters(int DegreesOfFreedom)
    {
       
        BehaviorParameters bp = GetComponent<BehaviorParameters>();

        Unity.MLAgents.Actuators.ActionSpec myActionSpec = bp.BrainParameters.ActionSpec;



        myActionSpec.NumContinuousActions = DegreesOfFreedom;
        myActionSpec.BranchSizes = new List<int>().ToArray();
        bp.BrainParameters.ActionSpec = myActionSpec;
        Debug.Log("Space of actions calculated at:" + myActionSpec.NumContinuousActions + " continuous dimensions");


        /*
         * To calculate the space of observations:
        
        */

     
        int numsensors = GetComponentsInChildren<SensorBehavior>().Length;
        int num_miscelaneous = GetComponent<ProcRagdollAgent>().calculateDreConObservationsize();

        //apparently the number of sensors is already acocunted for in the degrees of freedom, so:
        //   int ObservationDimensions = DegreesOfFreedom + numsensors + num_miscelaneous;
        int ObservationDimensions = DegreesOfFreedom +  num_miscelaneous;
        bp.BrainParameters.VectorObservationSize = ObservationDimensions;
        Debug.Log("Space of perceptions calculated at:" + bp.BrainParameters.VectorObservationSize + " continuous dimensions, with: " + " sensors: " + numsensors + "and DreCon miscelaneous: " + num_miscelaneous);


    }



}
