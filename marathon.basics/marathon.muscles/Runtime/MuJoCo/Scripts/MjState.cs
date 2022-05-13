using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Mujoco
{
    public static class MjState
    {
        static MjScene mjScene { get => MjScene.Instance; }
        public static unsafe (IEnumerable<double[]>, IEnumerable<double[]>) GetMjKinematics( MjBody rootBody)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            var joints = rootBody.GetComponentsInChildren<MjBaseJoint>();
            var positions = new List<double[]>();
            var velocities = new List<double[]>();
            foreach (var joint in joints)
            {
                switch (Model->jnt_type[joint.MujocoId])
                {
                    default:
                    case (int)MujocoLib.mjtJoint.mjJNT_HINGE:
                    case (int)MujocoLib.mjtJoint.mjJNT_SLIDE:
                        positions.Add(new double[] { Data->qpos[joint.QposAddress] });
                        velocities.Add(new double[] { Data->qvel[joint.DofAddress] });
                        break;
                    case (int)MujocoLib.mjtJoint.mjJNT_BALL:

                        positions.Add(new double[] { Data->qpos[joint.QposAddress],
                                                     Data->qpos[joint.QposAddress+1],
                                                     Data->qpos[joint.QposAddress+2],
                                                     Data->qpos[joint.QposAddress+3]});

                        velocities.Add(new double[] { Data->qvel[joint.DofAddress],
                                                      Data->qvel[joint.DofAddress+1],
                                                      Data->qvel[joint.DofAddress+2]});
                        break;
                    case (int)MujocoLib.mjtJoint.mjJNT_FREE:
                        positions.Add(new double[] {
                                                        Data->qpos[joint.QposAddress],
                                                        Data->qpos[joint.QposAddress+1],
                                                        Data->qpos[joint.QposAddress+2],
                                                        Data->qpos[joint.QposAddress+3],
                                                        Data->qpos[joint.QposAddress+4],
                                                        Data->qpos[joint.QposAddress+5],
                                                        Data->qpos[joint.QposAddress+6]});
                        velocities.Add( new double[] {
                                                        Data->qvel[joint.DofAddress],
                                                        Data->qvel[joint.DofAddress+1],
                                                        Data->qvel[joint.DofAddress+2],
                                                        Data->qvel[joint.DofAddress+3],
                                                        Data->qvel[joint.DofAddress+4],
                                                        Data->qvel[joint.DofAddress+5]});
                        break;
                    }
                }
            return (positions, velocities);
        }

        public static unsafe void SetMjKinematics(this MjScene mjScene, MjBody rootBody, IEnumerable<double[]> positions, IEnumerable<double[]> velocities)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            var joints = rootBody.GetComponentsInChildren<MjBaseJoint>();
            foreach ((var joint, (var position, var velocity)) in joints.Zip( positions.Zip(velocities, Tuple.Create), Tuple.Create))
            {
                switch (Model->jnt_type[joint.MujocoId])
                {
                    default:
                    case (int)MujocoLib.mjtJoint.mjJNT_HINGE:
                    case (int)MujocoLib.mjtJoint.mjJNT_SLIDE:
                        Data->qpos[joint.QposAddress] = position[0];
                        Data->qvel[joint.DofAddress] = velocity[0];
                        break;
                    case (int)MujocoLib.mjtJoint.mjJNT_BALL:
                        Data->qpos[joint.QposAddress] = position[0];
                        Data->qpos[joint.QposAddress + 1] = position[1];
                        Data->qpos[joint.QposAddress + 2] = position[2];
                        Data->qpos[joint.QposAddress + 3] = position[3];
                        Data->qvel[joint.DofAddress] = velocity[0];
                        Data->qvel[joint.DofAddress + 1] = velocity[1];
                        Data->qvel[joint.DofAddress + 2] = velocity[2];
                        break;
                    case (int)MujocoLib.mjtJoint.mjJNT_FREE:
                        Data->qpos[joint.QposAddress] = position[0];
                        Data->qpos[joint.QposAddress + 1] = position[1];
                        Data->qpos[joint.QposAddress + 2] = position[2];
                        Data->qpos[joint.QposAddress + 3] = position[3];
                        Data->qpos[joint.QposAddress + 4] = position[4];
                        Data->qpos[joint.QposAddress + 5] = position[5];
                        Data->qpos[joint.QposAddress + 6] = position[6];
                        Data->qvel[joint.DofAddress] = velocity[0];
                        Data->qvel[joint.DofAddress + 1] = velocity[1];
                        Data->qvel[joint.DofAddress + 2] = velocity[2];
                        Data->qvel[joint.DofAddress + 3] = velocity[3];
                        Data->qvel[joint.DofAddress + 4] = velocity[4];
                        Data->qvel[joint.DofAddress + 5] = velocity[5];
                        break;
                }

            }
            // update mj transforms:
            MujocoLib.mj_kinematics(Model, Data);
            mjScene.SyncUnityToMjState();
        }

        public static unsafe void TeleportMjRoot(MjFreeJoint root, Vector3 unityPos, Quaternion unityRot)
        {
            MujocoLib.mjData_* Data = mjScene.Data;
            MujocoLib.mjModel_* Model = mjScene.Model;

            Quaternion oldUnityRotation = MjEngineTool.UnityQuaternion(Data->xquat, root.MujocoId);
            var startOffset = root.QposAddress;
            Quaternion oldMjQuat = new Quaternion(w: (float)Data->qpos[startOffset + 3],
                                                  x: (float)Data->qpos[startOffset + 4],
                                                  y: (float)Data->qpos[startOffset + 5],
                                                  z: (float)Data->qpos[startOffset + 6]);
            Quaternion manualUnityQuat = MjEngineTool.UnityQuaternion(oldMjQuat);

            MjEngineTool.SetMjTransform(Data->qpos, unityPos, unityRot, root.MujocoId);

            



            /*Quaternion rotationOffset = unityRot * Quaternion.Inverse(manualUnityQuat);
            Debug.Log($"rotationOffset: {rotationOffset}");*/

            Quaternion rotationOffset = unityRot * Quaternion.Inverse(manualUnityQuat);
            Vector3 fromUnityLinVel = MjEngineTool.UnityVector3(Data->qvel, root.MujocoId * 2);

            startOffset = root.DofAddress;
            
            Vector3 toMjLinVel = MjEngineTool.MjVector3(rotationOffset * fromUnityLinVel);
            Data->qvel[startOffset] = toMjLinVel[0];
            Data->qvel[startOffset+1] = toMjLinVel[1];
            Data->qvel[startOffset + 2] = toMjLinVel[2];


            Vector3 fromUnityAngVel = MjEngineTool.UnityVector3(Data->qvel, (root.MujocoId *2 )+1);
            Vector3 toMjAngVel = MjEngineTool.MjVector3(rotationOffset * fromUnityAngVel);

            Data->qvel[startOffset + 3] = toMjAngVel[0];
            Data->qvel[startOffset + 4] = toMjAngVel[1];
            Data->qvel[startOffset + 5] = toMjAngVel[2];

            MujocoLib.mj_kinematics(Model, Data);
            mjScene.SyncUnityToMjState();

        }

        public static unsafe void TeleportMjRoot(int id, Vector3 unityPos, Quaternion unityRot)
        {
            MujocoLib.mjData_* Data = mjScene.Data;
            MujocoLib.mjModel_* Model = mjScene.Model;

            Quaternion oldUnityRotation = MjEngineTool.UnityQuaternion(Data->xquat, id);
            var startOffset = mjScene.Model->jnt_qposadr[id];
            Quaternion oldMjQuat = new Quaternion(w: (float)Data->qpos[startOffset + 3],
                                                  x: (float)Data->qpos[startOffset + 4],
                                                  y: (float)Data->qpos[startOffset + 5],
                                                  z: (float)Data->qpos[startOffset + 6]);
            Quaternion manualUnityQuat = MjEngineTool.UnityQuaternion(oldMjQuat);

            MjEngineTool.SetMjTransform(Data->qpos, unityPos, unityRot, id);





            /*Quaternion rotationOffset = unityRot * Quaternion.Inverse(manualUnityQuat);
            Debug.Log($"rotationOffset: {rotationOffset}");*/

            Quaternion rotationOffset = unityRot * Quaternion.Inverse(manualUnityQuat);
            Vector3 fromUnityLinVel = MjEngineTool.UnityVector3(Data->qvel, id * 2);

            startOffset = mjScene.Model->jnt_dofadr[id];

            Vector3 toMjLinVel = MjEngineTool.MjVector3(rotationOffset * fromUnityLinVel);
            Data->qvel[startOffset] = toMjLinVel[0];
            Data->qvel[startOffset + 1] = toMjLinVel[1];
            Data->qvel[startOffset + 2] = toMjLinVel[2];


            Vector3 fromUnityAngVel = MjEngineTool.UnityVector3(Data->qvel, (id * 2) + 1);
            Vector3 toMjAngVel = MjEngineTool.MjVector3(rotationOffset * fromUnityAngVel);

            Data->qvel[startOffset + 3] = toMjAngVel[0];
            Data->qvel[startOffset + 4] = toMjAngVel[1];
            Data->qvel[startOffset + 5] = toMjAngVel[2];

            MujocoLib.mj_kinematics(Model, Data);
            mjScene.SyncUnityToMjState();

        }

        public static Vector3 GetBoxSize(this MjInertial inertial)
        {
            return new Vector3(Mathf.Sqrt((inertial.DiagInertia[1] + inertial.DiagInertia[2] - inertial.DiagInertia[0]) / inertial.Mass * 6.0f),
                               Mathf.Sqrt((inertial.DiagInertia[0] + inertial.DiagInertia[2] - inertial.DiagInertia[1]) / inertial.Mass * 6.0f),
                               Mathf.Sqrt((inertial.DiagInertia[0] + inertial.DiagInertia[1] - inertial.DiagInertia[2]) / inertial.Mass * 6.0f));
        }


        
        public static unsafe Vector3 GlobalVelocity(this MjBody body)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            Vector3 bodyVel = Vector3.one;
            double[] mjBodyVel = new double[6];
            fixed (double* res = mjBodyVel)
            {
                MujocoLib.mj_objectVelocity(
                    Model, Data, (int)MujocoLib.mjtObj.mjOBJ_BODY, body.MujocoId, res, 0);
                // linear velocity is in the last 3 entries
                bodyVel = MjEngineTool.UnityVector3(res, 1);
            }
            return bodyVel;
        }

        public static unsafe Vector3 GlobalAngularVelocity(this MjBody body)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            Vector3 bodyAngVel = Vector3.one;
            double[] mjBodyAngVel = new double[6];
            fixed (double* res = mjBodyAngVel)
            {
                MujocoLib.mj_objectVelocity(
                    Model, mjScene.Data, (int)MujocoLib.mjtObj.mjOBJ_BODY, body.MujocoId, res, 0);
                bodyAngVel = MjEngineTool.UnityVector3(res, 0);
            }
            return bodyAngVel;
        }

        public static unsafe Vector3 LocalVelocity(this MjBody body)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            Vector3 bodyAngVel = Vector3.one;
            double[] mjBodyAngVel = new double[6];
            fixed (double* res = mjBodyAngVel)
            {
                MujocoLib.mj_objectVelocity(
                    Model, Data, (int)MujocoLib.mjtObj.mjOBJ_BODY, body.MujocoId, res, 1);
                bodyAngVel = MjEngineTool.UnityVector3(res, 0);
            }
            return bodyAngVel;
        }

        public static unsafe Vector3 LocalAngularVelocity(this MjBody body)
        {
            MujocoLib.mjModel_* Model = mjScene.Model;
            MujocoLib.mjData_* Data = mjScene.Data;
            Vector3 bodyAngVel = Vector3.one;
            double[] mjBodyAngVel = new double[6];
            fixed (double* res = mjBodyAngVel)
            {
                MujocoLib.mj_objectVelocity(
                    mjScene.Model, Data, (int)MujocoLib.mjtObj.mjOBJ_BODY, body.MujocoId, res, 1);
                bodyAngVel = MjEngineTool.UnityVector3(res, 0);
            }
            return bodyAngVel;
        }

        public static unsafe float GetAcceleration(this MjActuator act)
        {
            MujocoLib.mjData_* Data = mjScene.Data;
            
            return act.Rad2Length((float)(Data -> qacc[act.Joint.DofAddress]));
        }

        public static unsafe float GetAccelerationRad(this MjHingeJoint j)
        {
            MujocoLib.mjData_* Data = mjScene.Data;

            return (float)(Data->qacc[j.DofAddress]);
        }

        public static unsafe float GetPositionRad(this MjHingeJoint j)
        {
            MujocoLib.mjData_* Data = mjScene.Data;

            return (float)(Data->qpos[j.QposAddress]);
        }

        public static unsafe float GetVelocityRad(this MjHingeJoint j)
        {
            MujocoLib.mjData_* Data = mjScene.Data;

            return (float)(Data->qvel[j.DofAddress]);
        }

        public static unsafe float[] GetStateVector(this MjHingeJoint j)
        {
            return new float[] { j.GetPositionRad(), j.GetVelocityRad(), j.GetAccelerationRad() };
        }

        public static float Deg2Length(this MjActuator act, float deg)
        {
            return Mathf.Deg2Rad * deg * act.CommonParams.Gear[0];
        }

        public static float Rad2Length(this MjActuator act, float rad)
        {
            return rad * act.CommonParams.Gear[0];
        }

        public static float Length2Rad(this MjActuator act, float length, int g = 0)
        {
            return length / act.CommonParams.Gear[g];
        }

        public static float LengthInRad(this MjActuator act, int g = 0)
        {
            return act.Length / act.CommonParams.Gear[g];
        }

        public static float Length2Deg(this MjActuator act, float length, int g=0)
        {
            return length / act.CommonParams.Gear[g] * Mathf.Rad2Deg;
        }

        public static float LengthInDeg(this MjActuator act, int g = 0)
        {
            return act.Length / act.CommonParams.Gear[g] * Mathf.Rad2Deg;
        }

        public static unsafe int GetDoFAddress(this MjBaseJoint j)
        {
            return mjScene.Model->jnt_dofadr[j.MujocoId];
        }

        public static unsafe float GetMass(this MjBody bd)
        {
            return (float) mjScene.Model -> body_mass[bd.MujocoId];
        }

        public static unsafe Vector3 GetLocalCenterOfMass(this MjBody bd)
        {
            return MjEngineTool.UnityVector3(mjScene.Model->body_ipos, bd.MujocoId);
        }

        public static unsafe Quaternion GetLocalCenterOfMassRotation(this MjBody bd)
        {
            return MjEngineTool.UnityQuaternion(mjScene.Model->body_iquat, bd.MujocoId);
        }

        public static unsafe Vector3 GetInertia(this MjBody bd)
        {
            return MjEngineTool.UnityVector3(mjScene.Model->body_inertia, bd.MujocoId);
        }

        public static unsafe Vector3 GetPosition(this MjBody bd)
        {
            return MjEngineTool.UnityVector3(mjScene.Data->xpos, bd.MujocoId);
        }

        public static unsafe Matrix4x4 GetLocalCenterOfMassMatrix(this MjBody bd)
        {
            return Matrix4x4.TRS(bd.GetLocalCenterOfMass(), bd.GetLocalCenterOfMassRotation(), Vector3.one);
        }

        public static unsafe Matrix4x4 GetTransformMatrix(this MjBody bd)
        {
            var position = MjEngineTool.UnityVector3(mjScene.Data->xpos, bd.MujocoId);
            var rotation = MjEngineTool.UnityQuaternion(mjScene.Data->xquat, bd.MujocoId);
            return Matrix4x4.TRS(position, rotation, Vector3.one);
        }


        public static unsafe Quaternion GetQuaternion(this MjBallJoint bj)
        {
            var coords = mjScene.Data->qpos;
            var startOffset = bj.QposAddress;
            return new Quaternion(
                x: (float)coords[startOffset + 1], y: (float)coords[startOffset + 3],
                z: (float)coords[startOffset + 2], w: (float)-coords[startOffset]);
        }

    }
}