using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class Utils 
{



    // Find angular velocity. The delta rotation is converted to radians within [-pi, +pi].
    // Vector3 OldGetAngularVelocity(Quaternion from, Quaternion to, float timeDelta)
    // {
    //     var rotationVelocity = FromToRotation(from, to);
    //     var angularVelocityInDeg = NormalizedEulerAngles(rotationVelocity.eulerAngles) / timeDelta;
    //     var angularVelocity = angularVelocityInDeg * Mathf.Deg2Rad;
    //     return angularVelocity;
    // }


    public static Vector3 GetAngularVelocity(Quaternion from, Quaternion to, float timeDelta = 1f)
    {
        Vector3 fromInDeg = Utils.GetSwingTwist(from);
        Vector3 toInDeg = Utils.GetSwingTwist(to);

        return AngularVelocityInReducedCoordinates(fromInDeg, toInDeg, timeDelta);
    }


    //you can also use this to calculate acceleration, right?
    public static Vector3 AngularVelocityInReducedCoordinates(Vector3 fromIn, Vector3 toIn, float timeDelta = 1f)
    {

        if (timeDelta == 0)
        {
            Debug.LogWarning("a velocity with a time increment of 0 does NOT make any sense");
            return Vector3.zero;
        }
        Vector3 diff = (fromIn - toIn)*Mathf.Deg2Rad;
        Vector3 angularVelocity = diff / timeDelta;
        return angularVelocity;
    }



    public static Vector3 GetSwingTwist(Quaternion localRotation) 
    {


        Quaternion a = new Quaternion();
        Quaternion b = new Quaternion();


        return GetSwingTwist(localRotation, out a, out b);

    }






    public static Vector3 GetSwingTwist(Quaternion localRotation, out Quaternion swing, out Quaternion twist)
    {

        //the decomposition in swing-twist, typically works like this:

        swing = new Quaternion(0.0f, localRotation.y, localRotation.z, localRotation.w);
        swing = swing.normalized;

        //Twist: assuming   q_localRotation = q_swing * q_twist 

        twist = Quaternion.Inverse(swing) * localRotation;


        //double check:
        Quaternion temp = swing * twist;

        bool isTheSame = (Mathf.Abs(Quaternion.Angle(temp, localRotation)) < 0.001f);


        if (!isTheSame)
            Debug.LogError("I have: " + temp + "which does not match: " + localRotation + "because their angle is: " + Quaternion.Angle(temp, localRotation));


        Vector3 InReducedCoord = new Vector3(twist.eulerAngles.x, swing.eulerAngles.y, swing.eulerAngles.z);            //this is consistent with how the values are stored in ArticulationBody:


        //we make sure we keep the values nearest to 0 (with a modulus)
        if (Mathf.Abs(InReducedCoord.x - 360) < Mathf.Abs(InReducedCoord.x))
            InReducedCoord.x = (InReducedCoord.x - 360);
        if (Mathf.Abs(InReducedCoord.y - 360) < Mathf.Abs(InReducedCoord.y))
            InReducedCoord.y = (InReducedCoord.y - 360);
        if (Mathf.Abs(InReducedCoord.z - 360) < Mathf.Abs(InReducedCoord.z))
            InReducedCoord.z = (InReducedCoord.z - 360);

        return InReducedCoord;



    }


    public static ArticulationReducedSpace GetReducedSpaceFromTargetVector3(Vector3 target) {

        ArticulationReducedSpace ars = new ArticulationReducedSpace();
        ars.dofCount = 3;
        ars[0] = target.x;
        ars[1] = target.y;
        ars[2] = target.z;

        return ars;

    }


    public static Vector3 GetArticulationReducedSpaceInVector3(ArticulationReducedSpace ars)
    {
        Vector3 result = Vector3.zero;// new Vector3();

        if (ars.dofCount > 0)
            result.x = ars[0];
        if (ars.dofCount > 1)
            result.y = ars[1];
        if (ars.dofCount > 2)
            result.z = ars[2];

        return result;
    }
    // Return rotation from one rotation to another
    public static Quaternion FromToRotation(Quaternion from, Quaternion to)
    {
        if (to == from) return Quaternion.identity;

        return to * Quaternion.Inverse(from);
    }

    public static Vector2 Horizontal(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 Horizontal3D(this Vector3 vector3)
    {
        return new Vector3(vector3.x, 0f, vector3.z);
    }

    public static float VolumeFromSize(this Vector3 vector3)
    {
        return vector3.x* vector3.y*vector3.z;
    }

    public static Vector3 Sum(this IEnumerable<Vector3> vectors)
    {
        Vector3 sum = Vector3.zero;
        foreach(Vector3 vector in vectors)
        {
            sum += vector;
        }
        return sum;
    }

    public static IReadOnlyList<ArticulationDrive> GetDrives(this ArticulationBody ab)
    {
        return new List<ArticulationDrive> { ab.xDrive, ab.yDrive, ab.zDrive }.AsReadOnly();
    }

    /// <summary>
    /// IEnumerable of twist, swingY and swingZ.
    /// </summary>
    public static IReadOnlyList<ArticulationDofLock> GetLocks(this ArticulationBody ab)
    {
        return new List<ArticulationDofLock> { ab.twistLock, ab.swingYLock, ab.swingZLock}.AsReadOnly();
    }

    public static IReadOnlyList<float> GetComponents(this Vector3 vector3)
    {
        return new List<float> { vector3.x, vector3.y, vector3.z }.AsReadOnly();
    }

    public static void SetDriveAtIndex(this ArticulationBody ab, int i, ArticulationDrive drive)
    {
        switch(i)
        {
            case 0:
                ab.xDrive = drive;
                break;
            case 1:
                ab.yDrive = drive;
                break;
            case 2:
                ab.zDrive = drive;
                break;
            default:
                throw new IndexOutOfRangeException("Only x, y and z drives are supported with indices 0, 1, 2");
        }
    }

    public static string SegmentName(string query)
    {
        return query.Split(':').Last();
    }

}
