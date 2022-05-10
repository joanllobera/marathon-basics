using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RagDoll004 : MonoBehaviour {

    [System.Serializable]
    public class MusclePower
    {
        public string Muscle;
        public Vector3 PowerVector;
    }

    public List<MusclePower> MusclePowers;

    public float MotorScale = 1f;
    public float Stiffness = 100f;
    public float Damping = 100f;
    public float ForceLimit = float.MaxValue;


    [Header("Debug Collisions")]
    [SerializeField]
    bool skipCollisionSetup;


	// Use this for initialization
	void Start () {
		Setup();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void Setup () {

        if (!skipCollisionSetup) { 

		// handle collision overlaps
        IgnoreCollision("articulation:Spine2", new []{ "LeftArm", "RightArm"});
        IgnoreCollision("articulation:Hips", new []{ "RightUpLeg", "LeftUpLeg" });

        IgnoreCollision("LeftForeArm", new []{ "LeftArm" });
        IgnoreCollision("RightForeArm", new []{ "RightArm" });
        IgnoreCollision("RightLeg", new []{ "RightUpLeg" });
        IgnoreCollision("LeftLeg", new[] { "LeftUpLeg" });

        IgnoreCollision("RightLeg", new []{"RightFoot"});
        IgnoreCollision("LeftLeg", new []{"LeftFoot"});

        }

        //
        var joints = GetComponentsInChildren<Joint>().ToList();
        foreach (var joint in joints)
            joint.enablePreprocessing = false;
	}
	void IgnoreCollision(string first, string[] seconds)
    {
        foreach (var second in seconds)
        {
            IgnoreCollision(first, second);
        }
    }
    void IgnoreCollision(string first, string second)
    {
        var rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
        var colliderOnes = rigidbodies.FirstOrDefault(x=>x.name.Contains(first))?.GetComponents<Collider>();
        var colliderTwos = rigidbodies.FirstOrDefault(x=>x.name.Contains(second))?.GetComponents<Collider>();
        if (colliderOnes == null || colliderTwos == null)
            return;
        foreach (var c1 in colliderOnes)
            foreach (var c2 in colliderTwos)
                Physics.IgnoreCollision(c1, c2);
    }
}
