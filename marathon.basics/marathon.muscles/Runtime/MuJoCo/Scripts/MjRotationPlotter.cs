using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System;

public class MjRotationPlotter : MonoBehaviour
{
    [SerializeField]
    List<VisualizedHj> hjs;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    unsafe void FixedUpdate()
    {
        if (MjScene.Instance.Data == null) return;
        MjScene.Instance.SyncUnityToMjState();
        foreach (var vhj in hjs)
        {
            vhj.projectedAngle = vhj.GetCurProjectedAngle();
        }
        Debug.Log($"TransformAngle: {hjs[0].hj.transform.parent.localEulerAngles.magnitude}\nProjected angle: {hjs[0].projectedAngle}\nAngle from joint: {hjs[0].hj.Configuration}\nAngle from actuator: {hjs[0].actuator.LengthInDeg()}");
    }

    unsafe private void LateUpdate()
    {

    }

    private void OnDrawGizmos()
    {
        foreach (var vhj in hjs)
        {
            vhj.hj.transform.parent.localRotation.ToAngleAxis(out float angle, out Vector3 axis);
            Gizmos.DrawLine(vhj.hj.transform.parent.position, vhj.hj.transform.parent.position + vhj.hj.transform.parent.parent.TransformDirection(axis));

        }
    }

    [Serializable]
    class VisualizedHj
    {
        [SerializeField]
        public MjHingeJoint hj;
        [SerializeField]
        public float projectedAngle;
        [SerializeField]
        public MjActuator actuator;

        public float GetCurProjectedAngle()
        {
            return ProjectRotation(hj.transform.parent.localRotation);
        }

        public float ProjectRotation(Quaternion rot)
        {

            rot.ToAngleAxis(out float angle, out Vector3 axis);
            if (float.IsNaN(axis.x)) return 0f;

            angle = ((angle + 180) % 360) - 180;
            return angle * Vector3.Dot(axis, hj.transform.parent.parent.InverseTransformDirection(hj.RotationAxis));
        }
    }
}
