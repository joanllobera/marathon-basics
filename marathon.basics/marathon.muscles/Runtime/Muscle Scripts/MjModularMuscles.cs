using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

using Unity.MLAgents;

using MotorUpdate;

namespace Mujoco
{
    public class MjModularMuscles : ModularMuscles
    {
        [SerializeField]
        protected Transform actuatorRoot;

        [SerializeField]
        protected List<MjActuator> actuatorSubset;


        protected IReadOnlyList<Tuple<MjActuator, MjHingeJoint>> activeActRefPairs;
        protected IReadOnlyList<Tuple<MjActuator, MjHingeJoint>> passiveActRefPairs;

        private IReadOnlyList<MjActuator> actuators;
        public virtual IReadOnlyList<MjActuator> Actuators { get => actuatorRoot.GetComponentsInChildren<MjActuator>().ToList(); }

        public override List<IArticulation> GetMotors() {

            List<IArticulation> result = new List<IArticulation>();
            /*(    foreach(MjActuator in Actuators)
                    result.Add(new )
        
            */
            Debug.LogError("we need to return a list of IArticulation from Actuators, and need to implement the right adapter from mujoco");
            return result;
        }

        [SerializeField]
        Transform kinematicRef;

        [SerializeField]
        bool trackPosition;
        [SerializeField]
        bool trackVelocity;

        private bool IsSubsetDefined { get => (actuatorSubset != null && actuatorSubset.Count > 0); }

        public override int ActionSpaceSize => IsSubsetDefined && kinematicRef && trackPosition ? actuatorSubset.Count : actuatorRoot.GetComponentsInChildren<MjActuator>().ToList().Count;

        float[] nextActions;

        unsafe private void UpdateTorque(object sender, MjStepArgs e)
        {
            foreach ((var action, (var actuator, var reference)) in nextActions.Zip(activeActRefPairs, Tuple.Create))
            {

                var curState = new float[] { (float)e.data->qpos[actuator.Joint.QposAddress],
                                             (float)e.data->qvel[actuator.Joint.DofAddress],
                                             (float)e.data->qacc[actuator.Joint.DofAddress]};
                var targetState = trackPosition ? new float[] { (float)e.data->qpos[reference.QposAddress]+action,
                                                                    trackVelocity? (float)e.data->qvel[reference.DofAddress] : 0f} : new float[] { action, 0f };
                float torque = updateRule.GetTorque(curState, targetState);
                e.data->ctrl[actuator.MujocoId] = torque;
                actuator.Control = torque;
            }

            foreach ((var actuator, var reference) in passiveActRefPairs)
            {

                var curState = new float[] { (float)e.data->qpos[actuator.Joint.QposAddress],
                                             (float)e.data->qvel[actuator.Joint.DofAddress],
                                             (float)e.data->qacc[actuator.Joint.DofAddress]};
                var targetState = new float[] { (float)e.data->qpos[reference.QposAddress],
                                                 trackVelocity? (float)e.data->qvel[reference.DofAddress] : 0f};
                float torque = updateRule.GetTorque(curState, targetState);
                e.data->ctrl[actuator.MujocoId] = torque;
                actuator.Control = torque;
            }
        }

        public override void ApplyActions(float[] actions, float actionTimeDelta)
        {
            nextActions = actions;
            
        }

        public override float[] GetActionsFromState()
        {
            if (trackPosition) return Enumerable.Repeat(0f, ActionSpaceSize).ToArray();
            if (kinematicRef) return activeActRefPairs.Select(a => Mathf.Deg2Rad * a.Item2.Configuration).ToArray();
            return activeActRefPairs.Select(a => a.Item1.Control).ToArray();
        }

        public override void OnAgentInitialize() 
        {
          
            MjScene.Instance.ctrlCallback += UpdateTorque;
            actuators = Actuators;

            if (IsSubsetDefined && kinematicRef && trackPosition)
            {
                var passiveActs = actuators.Where(a => !actuatorSubset.Contains(a));
                activeActRefPairs = actuatorSubset.Select(a => Tuple.Create(a, FindReference(a))).ToList();
                passiveActRefPairs = passiveActs.Select(a => Tuple.Create(a, FindReference(a))).ToList();
                return;
            }

            activeActRefPairs = actuators.Select(a => Tuple.Create(a, FindReference(a))).ToList();
            passiveActRefPairs = new List<Tuple<MjActuator, MjHingeJoint>>();
        }

        private void OnDisable()
        {

            if (MjScene.InstanceExists) MjScene.Instance.ctrlCallback -= UpdateTorque;
        }

        private MjHingeJoint FindReference(MjActuator act)
        {
            return kinematicRef? kinematicRef.GetComponentsInChildren<MjHingeJoint>().First(rj => rj.name.Contains(act.Joint.name)) : null;
        }
    }
}
