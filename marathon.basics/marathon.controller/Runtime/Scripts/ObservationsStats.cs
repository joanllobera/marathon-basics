using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using ManyWorlds;
using UnityEngine.Assertions;

public class ObservationStats : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        [HideInInspector]
        public Vector3 LastLocalPosition;
        [HideInInspector]
        public Quaternion LastLocalRotation;
        [HideInInspector]
        public bool LastIsSet;
    }

    public MonoBehaviour ObjectToTrack;

    [Header("Anchor stats")]
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    // public Vector3 CenterOfMassInWorldSpace; 
    public Vector3 AngularVelocity;

    [Header("Stats, relative to HorizontalDirection & Center Of Mass")]
    public Vector3 CenterOfMassVelocity;
    public Vector3 CenterOfMassHorizontalVelocity;
    public float CenterOfMassVelocityMagnitude;
    public Vector3 CenterOfMassVelocityInRootSpace;
    public float CenterOfMassVelocityMagnitudeInRootSpace;
    public float CenterOfMassHorizontalVelocityMagnitude;
    public Vector3 DesiredCenterOfMassVelocity;
    public Vector3 CenterOfMassVelocityDifference;
    public List<Stat> Stats;

    // [Header("... for debugging")]
    [Header("Gizmos")]
    public bool VelocityInWorldSpace = true;
    public bool HorizontalVelocity = true;

    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public Quaternion LastRotation;
    [HideInInspector]
    public bool LastIsSet;


    SpawnableEnv _spawnableEnv;
    List<Collider> _bodyParts;
    internal List<Rigidbody> _rigidbodyParts;
    internal List<ArticulationBody> _articulationBodyParts;
    GameObject _root;
    IAnimationController _animationController;
    bool _hasLazyInitialized;
    MapAnim2Ragdoll _mapAnim2Ragdoll;

    string rootName = "articulation:Hips";

    public void setRootName(string s)
    {
        rootName = s;

    }

    public void OnAgentInitialize(Transform defaultTransform)
    {
        Assert.IsFalse(_hasLazyInitialized);
        _hasLazyInitialized = true;

        _mapAnim2Ragdoll = defaultTransform.GetComponent<MapAnim2Ragdoll>();
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _animationController = _spawnableEnv.GetComponentInChildren<IAnimationController>();
        _rigidbodyParts = ObjectToTrack.GetComponentsInChildren<Rigidbody>().ToList();
        _articulationBodyParts = ObjectToTrack.GetComponentsInChildren<ArticulationBody>().ToList();

        if (_rigidbodyParts?.Count > 0)
            _bodyParts = _rigidbodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Collider>())
                .Distinct()
                .ToList();
        else
            _bodyParts = _articulationBodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Collider>())
                .Distinct()
                .ToList();


        _bodyParts  =   _bodyParts
            .Where(x => x.enabled)
            .Where(x => !x.isTrigger)
            .Where(x=> {
                var ignoreCollider = x.GetComponent<IgnoreColliderForObservation>();
                if (ignoreCollider == null)
                    return true;
                return !ignoreCollider.enabled;})
            .Distinct()
            .ToList();


      
        Stats = _bodyParts
            .Select(x => new Stat { Name = x.name })
            .ToList();

        //TODO: this is quite sketchy, we should have a better way to deal with this
        if (_root == null)
        {
           // Debug.Log("in game object: " + name + " my rootname is: " + rootName);
            if (_rigidbodyParts?.Count > 0)
                _root = _rigidbodyParts.First(x => x.name == rootName).gameObject;
            else
                _root = _articulationBodyParts.First(x => x.name == rootName).gameObject;
        }
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }

    public void OnReset()
    {
        Assert.IsTrue(_hasLazyInitialized);
        ResetStatus();
        LastIsSet = false;
    }
    void ResetStatus()
    {
        LastIsSet = false;
        var timeDelta = float.MinValue;
        SetStatusForStep(timeDelta);
    }


    // Return rotation from one rotation to another
    public static Quaternion FromToRotation(Quaternion from, Quaternion to)
    {
        if (to == from) return Quaternion.identity;

        return to * Quaternion.Inverse(from);
    }

    // Adjust the value of an angle to lie within [-pi, +pi].
    public static float NormalizedAngle(float angle)
    {
        if (angle < 180)
        {
            return angle * Mathf.Deg2Rad;
        }
        return (angle - 360) * Mathf.Deg2Rad;
    }

    // Calculate rotation between two rotations in radians. Adjusts the value to lie within [-pi, +pi].
    public static Vector3 NormalizedEulerAngles(Vector3 eulerAngles)
    {
        var x = NormalizedAngle(eulerAngles.x);
        var y = NormalizedAngle(eulerAngles.y);
        var z = NormalizedAngle(eulerAngles.z);
        return new Vector3(x, y, z);
    }

    // Find angular velocity. The delta rotation is converted to radians within [-pi, +pi].
    public static Vector3 GetAngularVelocity(Quaternion from, Quaternion to, float timeDelta)
    {
        var rotationVelocity = FromToRotation(from, to);
        var angularVelocity = NormalizedEulerAngles(rotationVelocity.eulerAngles) / timeDelta;
        return angularVelocity;
    }

    public void SetStatusForStep(float timeDelta)
    {
        // get Center Of Mass velocity in f space
        Vector3 newCOM;
        // if Moocap, then get from anim2Ragdoll
        if (_mapAnim2Ragdoll != null)
        {
            newCOM = _mapAnim2Ragdoll.LastCenterOfMassInWorldSpace;
            var newHorizontalDirection = _mapAnim2Ragdoll.HorizontalDirection;
            HorizontalDirection = newHorizontalDirection / 180f;
            if (!LastIsSet)
            {
                LastCenterOfMassInWorldSpace = newCOM;
            }
            transform.position = newCOM;
            transform.rotation = Quaternion.Euler(newHorizontalDirection);
            CenterOfMassVelocity = _mapAnim2Ragdoll.CenterOfMassVelocity;
            CenterOfMassVelocityMagnitude = _mapAnim2Ragdoll.CenterOfMassVelocityMagnitude;
            CenterOfMassVelocityInRootSpace = transform.InverseTransformVector(CenterOfMassVelocity);
            CenterOfMassVelocityMagnitudeInRootSpace = CenterOfMassVelocityInRootSpace.magnitude;
        }
        else
        {
            newCOM = GetCenterOfMass();
            var newHorizontalDirection = new Vector3(0f, _root.transform.eulerAngles.y, 0f);
            HorizontalDirection = newHorizontalDirection / 180f;
            if (!LastIsSet)
            {
                LastCenterOfMassInWorldSpace = newCOM;
            }
            transform.position = newCOM;
            transform.rotation = Quaternion.Euler(newHorizontalDirection);
            var velocity = newCOM - LastCenterOfMassInWorldSpace;
            velocity /= timeDelta;
            CenterOfMassVelocity = velocity;

            // Mucked about
           // CenterOfMassVelocity = GetCenterOfMassVelocity();
            //Debug.Log(CenterOfMassVelocity);
            CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;
            CenterOfMassVelocityInRootSpace = transform.InverseTransformVector(CenterOfMassVelocity);
            CenterOfMassVelocityMagnitudeInRootSpace = CenterOfMassVelocityInRootSpace.magnitude;
        }
        LastCenterOfMassInWorldSpace = newCOM;

        // get Center Of Mass horizontal velocity in f space
        var comHorizontalDirection = new Vector3(CenterOfMassVelocity.x, 0f, CenterOfMassVelocity.z);
        CenterOfMassHorizontalVelocity = transform.InverseTransformVector(comHorizontalDirection);
        CenterOfMassHorizontalVelocityMagnitude = CenterOfMassHorizontalVelocity.magnitude;

        // get Desired Center Of Mass horizontal velocity in f space
        Vector3 desiredCom = _animationController.GetDesiredVelocity();
        DesiredCenterOfMassVelocity = transform.InverseTransformVector(desiredCom);

        // get Desired Center Of Mass horizontal velocity in f space
        CenterOfMassVelocityDifference = DesiredCenterOfMassVelocity - CenterOfMassHorizontalVelocity;

        if (!LastIsSet)
        {
            LastRotation = transform.rotation;
        }
        AngularVelocity = GetAngularVelocity(LastRotation, transform.rotation, timeDelta);
        LastRotation = transform.rotation;

        // get bodyParts stats in local space
        foreach (var bodyPart in _bodyParts)
        {
            Stat bodyPartStat = Stats.First(x => x.Name == bodyPart.name);

            Vector3 c = Vector3.zero;
            CapsuleCollider capsule = bodyPart as CapsuleCollider;
            BoxCollider box = bodyPart as BoxCollider;
            SphereCollider sphere = bodyPart as SphereCollider;
            if (capsule != null)
                c = capsule.center;
            else if (box != null)
                c = box.center;
            else if (sphere != null)
                c = sphere.center;
            Vector3 worldPosition = bodyPart.transform.TransformPoint(c);

            Quaternion worldRotation = bodyPart.transform.rotation;
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Quaternion localRotation = FromToRotation(transform.rotation, worldRotation);
            if (!bodyPartStat.LastIsSet || !LastIsSet)
            {
                bodyPartStat.LastLocalPosition = localPosition;
                bodyPartStat.LastLocalRotation = localRotation;
            }

            bodyPartStat.Position = localPosition;
            bodyPartStat.Rotation = localRotation;
            bodyPartStat.Velocity = (localPosition - bodyPartStat.LastLocalPosition) / timeDelta;
            bodyPartStat.AngularVelocity = GetAngularVelocity(bodyPartStat.LastLocalRotation, localRotation, timeDelta);
            bodyPartStat.LastLocalPosition = localPosition;
            bodyPartStat.LastLocalRotation = localRotation;
            bodyPartStat.LastIsSet = true;
        }
        LastIsSet = true;
    }

    Vector3 GetCenterOfMass()
    {
        var centerOfMass = Vector3.zero;
        float totalMass = 0f;
        foreach (ArticulationBody ab in _articulationBodyParts)
        {
            centerOfMass += ab.worldCenterOfMass * ab.mass;
            totalMass += ab.mass;
        }
        centerOfMass /= totalMass;
        // centerOfMass -= _spawnableEnv.transform.position;
        return centerOfMass;
    }

    //Mucked about
    public Vector3 GetCenterOfMassVelocity()
    {
        return _articulationBodyParts.Select(rb => rb.velocity * rb.mass).Sum() / _articulationBodyParts.Select(rb => rb.mass).Sum();
    }

    void OnDrawGizmosSelected()
    {
        if (_bodyParts == null || _bodyParts.Count ==0)
            return;
        // draw arrow for desired input velocity
        // Vector3 pos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 pos = new Vector3(transform.position.x, .3f, transform.position.z);
        Vector3 vector = DesiredCenterOfMassVelocity;
        if (VelocityInWorldSpace)
            vector = transform.TransformVector(vector);
        DrawArrow(pos, vector, Color.green);
        Vector3 desiredInputPos = pos + vector;

        if (HorizontalVelocity)
        {
            // arrow for actual velocity
            vector = CenterOfMassHorizontalVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.blue);
            Vector3 actualPos = pos + vector;

            // arrow for actual velocity difference
            vector = CenterOfMassVelocityDifference;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(actualPos, vector, Color.red);
        }
        else
        {
            vector = CenterOfMassVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(pos, vector, Color.blue);
            Vector3 actualPos = pos + vector;

            // arrow for actual velocity difference
            vector = DesiredCenterOfMassVelocity - CenterOfMassVelocity;
            if (VelocityInWorldSpace)
                vector = transform.TransformVector(vector);
            DrawArrow(actualPos, vector, Color.red);

        }
    }
    void DrawArrow(Vector3 start, Vector3 vector, Color color)
    {
        float headSize = 0.25f;
        float headAngle = 20.0f;
        Gizmos.color = color;
        Gizmos.DrawRay(start, vector);

        if (vector.magnitude > 0f)
        {
            Vector3 right = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 + headAngle, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(vector) * Quaternion.Euler(0, 180 - headAngle, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(start + vector, right * headSize);
            Gizmos.DrawRay(start + vector, left * headSize);
        }
    }
}
