using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DReCon
{
    public class DReConFallDetection : TrainingEvent
    {
        [SerializeField]
        private Transform KinematicHead;

        [SerializeField]
        private Transform SimulationHead;

        private void Update()
        {
            if ((KinematicHead.position - SimulationHead.position).magnitude > 1f) OnTrainingEvent(EventArgs.Empty);
        }
    }
}