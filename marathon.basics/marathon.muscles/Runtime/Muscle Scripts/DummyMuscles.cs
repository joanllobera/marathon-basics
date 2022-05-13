using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DummyMuscles : Muscles
{
    public override int ActionSpaceSize => 3;

    [SerializeField]
    Transform movedTransform;

    public override void ApplyActions(float[] actions, float actionTimeDelta)
    {
        movedTransform.position = 0.99f * movedTransform.position + 0.01f * new Vector3(actions[0], actions[1], actions[2]);
    }

    public override float[] GetActionsFromState()
    {
        return movedTransform.position.GetComponents().ToArray();
    }
}
