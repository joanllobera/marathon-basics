using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrafeTarget : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    Transform FakeParent;

    [SerializeField]
    float timeScaler;

    [SerializeField]
    float amplitudeScaler;

    [SerializeField]
    float inertia;

   /* [DebugGUIGraph(min: -1, max: 1, r: 0, g: 1, b: 1, autoScale: true)]*/
    float angleDeviation;

    Quaternion rootRot;

    Quaternion RootRot
    {
        get => rootRot;

        set
        {
            rootRot = Quaternion.Slerp(rootRot, value, 1f-inertia);
        }
    }

    private void Start()
    {
        rootRot = Quaternion.LookRotation(FakeParent.forward.Horizontal3D(), Vector3.up);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        gameObject.transform.position = FakeParent.position;
        angleDeviation = (Mathf.PerlinNoise(0f, Time.time * timeScaler) - 0.5f) * amplitudeScaler;
        RootRot = Quaternion.LookRotation(FakeParent.forward.Horizontal3D(), Vector3.up);
        gameObject.transform.rotation = Quaternion.AngleAxis(angleDeviation, Vector3.up) * RootRot;
    }
}
