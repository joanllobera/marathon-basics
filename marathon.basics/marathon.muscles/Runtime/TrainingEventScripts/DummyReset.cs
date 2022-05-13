using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyReset : TrainingEventHandler
{
    [SerializeField]
    Transform agentLocation;

    [SerializeField]
    Transform targetLocation;

    public override EventHandler Handler => HandleReset;

    private void HandleReset(object sender, System.EventArgs e)
    {
        agentLocation.position = new Vector3(0, 0.2f, -0.35f);
        targetLocation.position = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.2f, UnityEngine.Random.Range(-0.5f, 0.5f)  );
    }
}
