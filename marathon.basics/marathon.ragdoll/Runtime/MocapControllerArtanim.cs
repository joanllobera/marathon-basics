using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Assertions;

using System.Linq.Expressions;

public class MocapControllerArtanim : MonoBehaviour, IOnSensorCollision 
{
	public List<float> SensorIsInTouch;
	List<GameObject> _sensors;

	internal Animator anim;

	[Range(0f,1f)]
	public float NormalizedTime;    
	public float Lenght;
	public bool IsLoopingAnimation;

	[SerializeField]
	Rigidbody _rigidbodyRoot;

	private List<Rigidbody> _rigidbodies;
	private List<Transform> _transforms;

	public bool RequestCamera;
	public bool CameraFollowMe;
	public Transform CameraTarget;

	Vector3 _resetPosition;
	Quaternion _resetRotation;


	[SerializeField]
	bool _isGeneratedProcedurally = false;

	public bool IsGeneratedProcedurally { set => _isGeneratedProcedurally = value; }



	[Space(20)]
	//---------------------- piece added to deal with mixamo characters and mapping between skinned and physical characters
	//[SerializeField]
	//bool _usesMotionMatching = false;
    private bool _usingMocapAnimatorController = false;
	MocapAnimatorController _mocapAnimController;

	[SerializeField]
	float _debugDistance = 0.0f;


	//I try to configure here, directly, the offsets.


	// [SerializeField]
	// string rigBaseName = "mixamorig";

	//  private List<Transform> _targetPoseTransforms = null;
	//[SerializeField]
	//Transform _targetMocapCharacter;





	private List<MappingOffset> _offsetsSource2RB = null;

    //for debugging, we disable this when setTpose in MarathonTestBedController is on
    [HideInInspector]
    public bool doFixedUpdate = true;

	bool _hasLazyInitialized;



	void SetOffsetSourcePose2RBInProceduralWorld() {

		_transforms = GetComponentsInChildren<Transform>().ToList();

		_offsetsSource2RB = new List<MappingOffset>();


		if (_rigidbodies == null)
		{
			_rigidbodies = _rigidbodyRoot.GetComponentsInChildren<Rigidbody>().ToList();
			// _transforms = GetComponentsInChildren<Transform>().ToList();
		}



		foreach (Rigidbody rb in _rigidbodies)
		{

			//ArticulationBody ab = _articulationbodies.First(x => x.name == abname);

			string[] temp = rb.name.Split(':');

			//string tname = temp[1];
			string tname = rb.name.TrimStart(temp[0].ToArray<char>());

			tname = tname.TrimStart(':');


			//if structure is "articulation:" + t.name, it comes from a joint:

			if (temp[0].Equals("articulation"))
			{

				Transform t = _transforms.First(x => x.name == tname);


				//TODO: check these days if those values are different from 0, sometimes
				Quaternion qoffset = rb.transform.rotation * Quaternion.Inverse(t.rotation);

				MappingOffset r = new MappingOffset(t, rb, qoffset);

				_offsetsSource2RB.Add(r);
				r.UpdateRigidBodies = true;//TODO: check if really needed, probably the constructor already does it



			}
		}



	}




	MappingOffset SetOffsetSourcePose2RB(string rbname, string tname)
	{
		//here we set up:
		// a. the transform of the rigged character input
		// NO b. the rigidbody of the physical character
		// c. the offset calculated between the rigged character INPUT, and the rigidbody


		if (_transforms == null)
		{
			_transforms = GetComponentsInChildren<Transform>().ToList();
			//Debug.Log("the number of transforms  in source pose is: " + _transforms.Count);

		}


		if (_offsetsSource2RB == null)
		{
			_offsetsSource2RB = new List<MappingOffset>();

		}

		if (_rigidbodies == null )
		{
			_rigidbodies = _rigidbodyRoot.GetComponentsInChildren<Rigidbody>().ToList();
			// _transforms = GetComponentsInChildren<Transform>().ToList();
		}




		Rigidbody rb = null;


		try
		{
			rb = _rigidbodies.First(x => x.name == rbname);

		}
		catch (Exception e)
		{

			Debug.LogError("no rigidbody with name " + rbname);

		}


	
		Transform tref = null;
		try
		{

			tref = _transforms.First(x => x.name == tname);

		}
		catch (Exception e)
		{
			Debug.LogError("no bone transform with name in input pose " + tname);

		}

		//from refPose to Physical body:
		//q_{physical_body} = q_{offset} * q_{refPose}
		//q_{offset} = q_{physical_body} * Quaternion.Inverse(q_{refPose})

		//Quaternion qoffset = rb.transform.localRotation * Quaternion.Inverse(tref.localRotation);


		//using the global rotation instead of the local one prevents from dependencies on bones that are not mapped to the rigid body (like the shoulder)
		Quaternion qoffset = rb.transform.rotation * Quaternion.Inverse(tref.rotation);


		MappingOffset r = new MappingOffset(tref, rb, qoffset);
		r.UpdateRigidBodies = true;//not really needed, the constructor already does it

		_offsetsSource2RB.Add(r);
		return r;
	}



	//void SetSon(MappingOffset o, string tsonname) {

	//	Transform tref = null;
	//	try
	//	{

	//		tref = _transforms.First(x => x.name == tsonname);

	//	}
	//	catch (Exception e)
	//	{
	//		Debug.LogError("no bone transform with name in input pose " + tsonname);

	//	}

	//	o.SetSon(tref);

	//}


	//public bool UsingMocapAnimatorController { get => _usingMocapAnimatorController; set => _usingMocapAnimatorController = value; }

	//public bool UsingMocapAnimatorController { get => _usingMocapAnimatorController;  }

	public void OnAgentInitialize()
	{
		LazyInitialize();
	}
	void LazyInitialize()
    {
		if (_hasLazyInitialized)
			return;
		try
		{
			_mocapAnimController = GetComponent<MocapAnimatorController>();
			string s = _mocapAnimController.name;//this should launch an exception if there is no animator
			_usingMocapAnimatorController = true;
		}
		catch(Exception e) {
			_usingMocapAnimatorController = false;
			Debug.LogWarning("Mocap Controller is working WITHOUT MocapAnimatorController");
		}


		//we already created this in the procedural case:
		if(! _isGeneratedProcedurally)
			DynamicallyCreateRagdollForMocap();

        SetupSensors();

		anim = GetComponent<Animator>();
		if (_usingMocapAnimatorController && !_isGeneratedProcedurally)
		{

			// anim.Play("Record",0, NormalizedTime);
			anim.Play("Idle", 0, NormalizedTime);
			anim.Update(0f);
		}

			if (RequestCamera && CameraTarget != null)
		{
			var instances = FindObjectsOfType<MocapControllerArtanim>().ToList();
			if (instances.Count(x=>x.CameraFollowMe) < 1)
				CameraFollowMe = true;
		}
        if (CameraFollowMe){
            var camera = FindObjectOfType<Camera>();
            var follow = camera.GetComponent<SmoothFollow>();
            follow.target = CameraTarget;
        }
		_resetPosition = transform.position;
		_resetRotation = transform.rotation;

		_hasLazyInitialized = true;
    }


	public void DynamicallyCreateRagdollForMocap()
	{
		// Find Ragdoll in parent
		Transform parent = this.transform.parent;
		RagDollAgent[] ragdolls = parent.GetComponentsInChildren<RagDollAgent>(true);
		Assert.AreEqual(ragdolls.Length, 1, "code only supports one RagDollAgent");
		RagDollAgent ragDoll = ragdolls[0];
		var ragdollForMocap = new GameObject("RagdollForMocap");
		ragdollForMocap.transform.SetParent(this.transform, false);
		Assert.AreEqual(ragDoll.transform.childCount, 1, "code only supports 1 child");
		var ragdollRoot = ragDoll.transform.GetChild(0);
		// clone the ragdoll root
		var clone = Instantiate(ragdollRoot);
		// remove '(clone)' from names
		foreach (var t in clone.GetComponentsInChildren<Transform>())
		{
			t.name = t.name.Replace("(Clone)", "");
		}
		clone.transform.SetParent(ragdollForMocap.transform, false);
		// swap ArticulatedBody for RidgedBody
		foreach (var abody in clone.GetComponentsInChildren<ArticulationBody>())
		{
			var bodyGameobject = abody.gameObject;
			var rb = bodyGameobject.AddComponent<Rigidbody>();
			rb.mass = abody.mass;
			rb.useGravity = abody.useGravity;
			DestroyImmediate(abody);
		}
		// make Kinematic
		foreach (var rb in clone.GetComponentsInChildren<Rigidbody>())
		{
			rb.isKinematic = true;
		}
		// set the root
		this._rigidbodyRoot = clone.GetComponent<Rigidbody>();
		// set the layers
		ragdollForMocap.layer = this.gameObject.layer;
		foreach (Transform child in ragdollForMocap.transform)
		{
			child.gameObject.layer  = this.gameObject.layer;
		}
		// setup HandleOverlap
		foreach (var rb in clone.GetComponentsInChildren<Rigidbody>())
		{
			// remove cloned HandledOverlap
			var oldHandleOverlap = rb.GetComponent<HandleOverlap>();
			DestroyImmediate(oldHandleOverlap);
			var handleOverlap = rb.gameObject.AddComponent<HandleOverlap>();
			handleOverlap.Parent = clone.gameObject;
		}

	}
	void SetupSensors()
	{
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
	}

    void FixedUpdate()
    {
		LazyInitialize();
        if (doFixedUpdate)
            OnFixedUpdate();
	
    }


    void OnFixedUpdate() {
		LazyInitialize();

        //if (!_usesMotionMatching)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfo = anim.GetCurrentAnimatorClipInfo(0);
            Lenght = stateInfo.length;
            NormalizedTime = stateInfo.normalizedTime;
            IsLoopingAnimation = stateInfo.loop;
            var timeStep = stateInfo.length * stateInfo.normalizedTime;
            //var endTime = 1f;
            //if (IsLoopingAnimation)
            //    endTime = 3f;
            // if (NormalizedTime <= endTime) {
            // }       
        }

		if (_isGeneratedProcedurally)
			MimicAnimationArtanimInProceduralWorld();
		else
			MimicAnimationArtanim();


    }


	void MimicAnimationArtanimInProceduralWorld() {
		if (!anim.enabled)
			return;
		else
			SetOffsetSourcePose2RBInProceduralWorld();


	}


	void MimicAnimationArtanim() {
		if (!anim.enabled)
			return;

		if (_offsetsSource2RB == null)
		{
			MappingOffset o = SetOffsetSourcePose2RB("articulation:Hips", "mixamorig:Hips");
			

			o.SetAsRoot(true, _debugDistance);
			SetOffsetSourcePose2RB("articulation:Spine", "mixamorig:Spine");
			SetOffsetSourcePose2RB("articulation:Spine1", "mixamorig:Spine1");
			SetOffsetSourcePose2RB("articulation:Spine2", "mixamorig:Spine2");
            SetOffsetSourcePose2RB("articulation:Neck", "mixamorig:Neck");

            SetOffsetSourcePose2RB("head", "mixamorig:Head");


			SetOffsetSourcePose2RB("articulation:LeftShoulder", "mixamorig:LeftShoulder");


			SetOffsetSourcePose2RB("articulation:LeftArm", "mixamorig:LeftArm");
			
			SetOffsetSourcePose2RB("articulation:LeftForeArm", "mixamorig:LeftForeArm");

			//	SetOffsetSourcePose2RB("left_hand", "mixamorig:LeftHand");
			// no rigidbodies in hands, so far

			SetOffsetSourcePose2RB("articulation:RightShoulder", "mixamorig:RightShoulder");


			SetOffsetSourcePose2RB("articulation:RightArm", "mixamorig:RightArm");
			SetOffsetSourcePose2RB("articulation:RightForeArm", "mixamorig:RightForeArm");
			//	SetOffsetSourcePose2RB("right_hand", "mixamorig:RightHand");

			SetOffsetSourcePose2RB("articulation:LeftUpLeg", "mixamorig:LeftUpLeg");
			SetOffsetSourcePose2RB("articulation:LeftLeg", "mixamorig:LeftLeg");


			SetOffsetSourcePose2RB("articulation:LeftToeBase", "mixamorig:LeftToeBase");
	

			SetOffsetSourcePose2RB("articulation:RightUpLeg", "mixamorig:RightUpLeg");
			SetOffsetSourcePose2RB("articulation:RightLeg", "mixamorig:RightLeg");
			SetOffsetSourcePose2RB("articulation:RightToeBase", "mixamorig:RightToeBase");
		



		}
		else {
			MimicCynematicChar();
		}


	}

	


	void MimicCynematicChar()
	{

		try
		{
			foreach (MappingOffset o in _offsetsSource2RB)
			{
				o.UpdateRotation();

			}
		}
		catch (Exception e)
		{
			Debug.Log("not calibrated yet...");

		}


	}

	/*
	[Space(20)]
	[Range(0f,1f)]
	public float toePositionOffset = .3f;
	[Range(0f,1f)]
	public float toeRotationOffset = .7f;

    void MimicLeftFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:LeftFoot";
		// string animEndtName = "mixamorig:LeftToeBase";
		string animEndtName = "mixamorig:LeftToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;
	}
	void MimicRightFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:RightFoot";
		// string animEndtName = "mixamorig:RightToeBase";
		string animEndtName = "mixamorig:RightToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}


		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;

	}
	*/

	public void OnReset(Quaternion resetRotation)
	{
		LazyInitialize();

        if (!doFixedUpdate)
            return;

            if (_usingMocapAnimatorController)
		{
			_mocapAnimController.OnReset();

		}
		else
		{
			Debug.Log("I am resetting the reference animation with MxMAnimator (no _mocapController)");

			//GetComponent<MxMAnimator>().enabled = false;

			//GetComponent<MxMAnimator>().enabled = true;



		}





		transform.position = _resetPosition;
		// handle character controller skin width
		var characterController = GetComponent<CharacterController>();
		if (characterController != null)
		{
			var pos = transform.position;
			pos.y += characterController.skinWidth;
			transform.position = pos;
		}
		transform.rotation = resetRotation;
		if (_isGeneratedProcedurally)
			MimicAnimationArtanimInProceduralWorld();
		else
	        MimicAnimationArtanim();
	}

    public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other)
	{
		LazyInitialize();

		//if (string.Compare(other.name, "Terrain", true) !=0)
		if (other.layer != LayerMask.NameToLayer("Ground"))
				return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 1f;
		}
	}
	public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
	{
		LazyInitialize();

		if (other.layer != LayerMask.NameToLayer("Ground"))
				return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 0f;
		}
	}   
    public void CopyStatesTo(GameObject target)
    {
		LazyInitialize();

        var targets = target.GetComponentsInChildren<ArticulationBody>().ToList();
		if (targets?.Count == 0)
			return;
        var root = targets.First(x=>x.isRoot);
		root.gameObject.SetActive(false);
        foreach (var targetRb in targets)
        {
			var stat = GetComponentsInChildren<Rigidbody>().First(x=>x.name == targetRb.name);
            targetRb.transform.position = stat.position;
            targetRb.transform.rotation = stat.rotation;
            if (targetRb.isRoot)
            {
                targetRb.TeleportRoot(stat.position, stat.rotation);
            }
			float stiffness = 0f;
			float damping = 10000f;
			if (targetRb.twistLock == ArticulationDofLock.LimitedMotion)
			{
				var drive = targetRb.xDrive;
				drive.stiffness = stiffness;
				drive.damping = damping;
				targetRb.xDrive = drive;
			}			
            if (targetRb.swingYLock == ArticulationDofLock.LimitedMotion)
			{
				var drive = targetRb.yDrive;
				drive.stiffness = stiffness;
				drive.damping = damping;
				targetRb.yDrive = drive;
			}
            if (targetRb.swingZLock == ArticulationDofLock.LimitedMotion)
			{
				var drive = targetRb.zDrive;
				drive.stiffness = stiffness;
				drive.damping = damping;
				targetRb.zDrive = drive;
			}
        }
		root.gameObject.SetActive(true);
    }	   
	public Vector3 SnapTo(Vector3 snapPosition)
	{
		snapPosition.y = transform.position.y;
		var snapDistance = snapPosition-transform.position;
		transform.position = snapPosition;
		return snapDistance;
	}

	public List<Rigidbody> GetRigidBodies()
	{
		LazyInitialize();
		return GetComponentsInChildren<Rigidbody>().ToList();
	}
}