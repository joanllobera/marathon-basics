using System.Collections;
using System.Collections.Generic;
//using System;

using UnityEngine;


using Unity.MLAgents.Policies;
using Unity.MLAgents;
using System.Linq;
using Unity.Barracuda;
using System.ComponentModel;

public class TrainingEnvironmentGenerator : MonoBehaviour
{



    [Header("The animated character:")]


    [SerializeField]
    Animator characterReference;

    [SerializeField]
    Transform characterReferenceHead;

    [SerializeField]
    Transform characterReferenceRoot;

    [Tooltip("do not include the root nor the neck")]
    [SerializeField]
    Transform[] characterSpine;


    [Tooltip("fingers will be excluded from physics-learning")]
    [SerializeField]
    Transform[] characterReferenceHands;

    //we assume here is the end-effector, but feet have an articulaiton (sensors will be placed on these and their immediate parents)
    //strategy to be checked: if for a quadruped we add the 4 feet here, does it work?
    [Tooltip("same as above but not taking into account fingers. Put the last joint")]
    [SerializeField]
    Transform[] characterReferenceFeet;


    [Header("How we want the generated assets stored:")]

    [SerializeField]
    string AgentName;

    [SerializeField]
    string TrainingEnvName;



    [Header("Configuration options:")]
    [SerializeField]
    string LearningConfigName;

    [Range(0, 359)]
    public int MinROMNeededForJoint = 0;


    [Tooltip("body mass in grams/ml")]
    [SerializeField]
    float massdensity = 1.01f;


    
    [SerializeField]
    string trainingLayerName = "marathon";

    //[SerializeField]
    //ROMparserSwingTwist ROMparser;

    [SerializeField]
    public RangeOfMotionValues info2store;


    [Header("Prefabs to generate training environment:")]
    [SerializeField]
    ManyWorlds.SpawnableEnv referenceSpawnableEnvironment;

    [SerializeField]
    Material trainerMaterial;

    [SerializeField]
    PhysicMaterial colliderMaterial;



    //things generated procedurally that we store to configure after the generation of the environment:
    [HideInInspector]
    [SerializeField]
    Animator character4training;

    [HideInInspector]
    [SerializeField]
    Animator character4synthesis;

    [HideInInspector][SerializeField]
    ManyWorlds.SpawnableEnv _outcome;


    


    [HideInInspector]
    [SerializeField]
    List<ArticulationBody> articulatedJoints;

    [HideInInspector]
    [SerializeField]
    ArticulationMusclesSimplified muscleteam;

    public ManyWorlds.SpawnableEnv Outcome{ get { return _outcome; } }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void GenerateTrainingEnv() {

        character4training = Instantiate(characterReference.gameObject).GetComponent<Animator>();
        character4training.gameObject.SetActive(true);

        //we assume there is an animated controller
        //
        //
        //possibly, we will need to add  controllers):



        if (character4training.GetComponent<IAnimationController>() == null)
            character4training.gameObject.AddComponent<DefaultAnimationController>();



        MapAnim2Ragdoll mca =character4training.gameObject.AddComponent<MapAnim2Ragdoll>();
        //mca.IsGeneratedProcedurally = true;

        character4training.gameObject.AddComponent<TrackBodyStatesInWorldSpace>();
        character4training.name = "Source:" + AgentName;

        SkinnedMeshRenderer[] renderers = character4training.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer r in renderers) {

            Material[] mats = r.sharedMaterials;
            for (int i =0; i < mats.Length; i++) {
                mats[i] = trainerMaterial;
            }
            r.sharedMaterials = mats;
        }

        



        character4synthesis = Instantiate(characterReference.gameObject).GetComponent<Animator>();
        character4synthesis.gameObject.SetActive(true);

        character4synthesis.name = "Result:" + AgentName ;


        //we remove everything except the transform
        UnityEngine.Component[] list = character4synthesis.GetComponents(typeof(UnityEngine.Component));
        foreach (UnityEngine.Component c in list)
        {

            if (c is Transform || c is Animator || c is CharacterController)
            {
            }
            else
            {
                DestroyImmediate(c);

            }

        }

        character4synthesis.GetComponent<Animator>().runtimeAnimatorController = null;




        MapRagdoll2Anim rca = character4synthesis.gameObject.AddComponent<MapRagdoll2Anim>();
      

        _outcome = Instantiate(referenceSpawnableEnvironment).GetComponent<ManyWorlds.SpawnableEnv>();
        _outcome.name = TrainingEnvName;


        ProcRagdollAgent ragdollMarathon = generateRagDollFromAnimatedSource(rca, _outcome);


        Transform[] ts= ragdollMarathon.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in ts) {
            t.gameObject.layer = LayerMask.NameToLayer(trainingLayerName);
        }







        addTrainingParameters(rca, ragdollMarathon);


        //UNITY BUG
        //This below seems to make it crash. My guess is that:
        /*
        ArticulationBody is a normal component, but it also does some odd thing: it affects the physical simluation, since it is a rigidBody plus an articulatedJoint. The way it is defined is that it has the properties of a rigidBody, but it ALSO uses the hierarchy of transforms  to define a chain of articulationJoints, with their rotation constraints, etc. Most notably, the ArticulationBody that is highest in the root gets assigned a property automatically, "isRoot", which means it's physics constraints are different.My guess is that when you change the hierarchy in the editor, at some point in the future the chain of ArticulationBody recalculates who is the root, and changes its properties. However since this relates to physics, it is not done in the same thread.
If the script doing this is short, it works because this is finished before the update of the ArticulationBody chain is triggered. But when I add more functionality, the script lasts longer, and then it crashes. This is why I kept getting those Physx errors, and why it kept happening in a not-so-reliable way, because we do not have any way to know when this recalculation is done.The fact that ArticulationBody is a fairly recent addition to Unity also makes me suspect they did not debug it properly.The solution seems to be to do all the setup while having the game objects that have articulationBody components with no hierarchy changes, and also having the rootgameobject inactive. When I do this,  I am guessing it does not trigger the update of the ArticulationBody chain.

        I seem to have a reliable crash:

            1.if I use ragdollroot.gameObject.SetActive(true) at the end of my configuration script, it crashes.
            2.if I comment that line, it does not.
            3.if I set it to active manually, through the editor, after running the script with that line commented, it works.

        */
        //ragdoll4training.gameObject.SetActive(true);


        _outcome.GetComponent<RenderingOptions>().ragdollcontroller = ragdollMarathon.gameObject;


        character4training.transform.SetParent(_outcome.transform);
        _outcome.GetComponent<RenderingOptions>().movementsource = character4training.gameObject;

        character4synthesis.transform.SetParent(_outcome.transform);


        //ragdoll4training.gameObject.SetActive(true);




    }


    /*
    public void activateRagdoll() {

        RagDollAgent ragdoll4training = _outcome.GetComponentInChildren<RagDollAgent>(true);
        if(ragdoll4training!=null)
            ragdoll4training.gameObject.SetActive(true);


    }
    */


ProcRagdollAgent  generateRagDollFromAnimatedSource( MapRagdoll2Anim target, ManyWorlds.SpawnableEnv trainingenv) {

   
        GameObject temp = GameObject.Instantiate(target.gameObject);
        

        //1. we remove everything we do not need:

        //1.1 we remove meshes
        SkinnedMeshRenderer[] renderers = temp.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer rend in renderers)
        {

            if(rend.gameObject != null)
                DestroyImmediate(rend.gameObject);

        }


        //1.2 we remove everything except the transforms
        UnityEngine.Component[] list = temp.GetComponents(typeof(UnityEngine.Component));
        foreach (UnityEngine.Component c in list) {

            if (c is Transform)
            {
            }
            else {
                DestroyImmediate(c);

            }

        }

        //1.3 we also remove any renderers from the ragdoll
        var meshNamesToDelete = temp.GetComponentsInChildren<MeshRenderer>()
            .Select(x=>x.name)
            .ToArray();
        foreach (var name in meshNamesToDelete)
        {
            var toDel = temp.GetComponentsInChildren<Transform>().First(x=>x.name == name);
            toDel.transform.parent = null;
            GameObject.DestroyImmediate(toDel.gameObject);
        }
        

        temp.name = "Ragdoll:" + AgentName ;
        muscleteam=  temp.AddComponent<ArticulationMusclesSimplified>();
        temp.transform.position = target.transform.position;
        temp.transform.rotation = target.transform.rotation;


        //1.4 we drop the sons of the limbs (to avoid including fingers in the following procedural steps)

        Transform[] pack = temp.GetComponentsInChildren<Transform>();
        Transform root = pack.First<Transform>(x => x.name == characterReferenceRoot.name);
        Transform[] joints = root.transform.GetComponentsInChildren<Transform>();
        List<Transform> childstodelete = new List<Transform>();

        foreach (Transform t in characterReferenceHands) {
            string limbname = t.name;// + "(Clone)";
            //Debug.Log("checking sons of: " + limbname);
            Transform limb = joints.First<Transform>(x => x.name == limbname);

            Transform[] sons = limb.GetComponentsInChildren<Transform>();
            foreach (Transform t2 in sons)
            {
                if(t2.name != limb.name)
                    childstodelete.Add(t2);

            }
        }

        Transform headJoint = joints.First<Transform>(x => x.name == characterReferenceHead.name);
        //if there are eyes or head top, we also remove them
        foreach (Transform child in headJoint)
        {
                childstodelete.Add(child);

        }
        






        List<Transform> listofjoints = new List<Transform>(joints);
        foreach (Transform t2 in childstodelete)
        {
            listofjoints.Remove(t2);
            t2.DetachChildren();//otherwise, it tries to destroy the children later, and fails.
            DestroyImmediate(t2.gameObject);
        }



        //2. We add articulationBody and Collider components

        //2.1 for each remaining joint we add an articulationBody and a collider for each bone
        joints = listofjoints.ToArray();
        articulatedJoints = new List<ArticulationBody>();

        List<Collider> colliders = new List<Collider>();

        List<HandleOverlap> hos = new List<HandleOverlap>();

        foreach (Transform j in joints) {

     
            ArticulationBody ab = j.gameObject.AddComponent<ArticulationBody>();
            ab.anchorRotation = Quaternion.identity;
            ab.mass = 0.1f;
            ab.jointType = ArticulationJointType.FixedJoint;
            articulatedJoints.Add(ab);

            string namebase = j.name.Replace("(Clone)", "");//probably not needed

            j.name = "articulation:" + namebase;

            GameObject go = new GameObject();
            go.transform.position = j.gameObject.transform.position;
            go.transform.parent = j.gameObject.transform;
            go.name = "collider:" + namebase;
            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            c.material = colliderMaterial;
            c.height = .06f;
            c.radius = c.height;
            colliders.Add(c);


           HandleOverlap ho= go.AddComponent<HandleOverlap>();
            hos.Add(ho);

            go.AddComponent<IgnoreColliderForObservation>();

        }


        //2.1bis we also handle the Parents in HandleOverlap:
        foreach (HandleOverlap ho in hos) {



            //the dad is the articulationBody, the grandDad is the articulationBody's parent
            string nameDadRef = ho.transform.parent.parent.name;

            if (nameDadRef == null) {
                //Debug.Log("my name is: " + ho.name + "and I have no granddad");
                continue;
            }


            string nameColliderDadRef = "collider:" + nameDadRef.Replace("articulation:", "");



            Collider colliderDad = colliders.FirstOrDefault<Collider>(c => c.name.Equals(nameColliderDadRef));
            if (colliderDad == null)
            {
                //Debug.Log("my name is: " + ho.name + "  and I have no collider Dad named " + nameColliderDadRef);
                //this should happen only with the hips collider, because the granddad is not part of the articulationBody hierarchy
                continue;

            }

            ho.Parent = colliderDad.gameObject;
            //Debug.Log("set up " + ho.name + "  with dad: " + colliderDad.name);

        }




        //CAUTION! it is important to deactivate the hierarchy BEFORE we add ArticulationBody members,
        // Doing it afterwards, and having it active, makes the entire thing crash 
        temp.transform.parent = trainingenv.transform;
        temp.gameObject.SetActive(false);


        //2.2 we configure the colliders, in general

        //  List<string> colliderNamesToDelete = new List<string>();
        foreach (CapsuleCollider c in colliders)
        {
            string namebase = c.name.Replace("collider:", "");
            Transform j = joints.First(x=>x.name=="articulation:" + namebase);
            // if not ArticulationBody, skip
            var articulatedDad = j.GetComponent<ArticulationBody>();
            if (articulatedDad == null)
                continue;


            //this works when childCount = 1
            j = joints.FirstOrDefault(x=>x.transform.parent.name=="articulation:" + namebase);
            if (j==null) //these are the end points: feets and hands and head

            {
                if (namebase == characterReferenceHead.name)
                {
                    //neck displacement, relative to spine2:
                    Vector3 displacement = c.transform.parent.position - c.transform.parent.parent.position;
                    c.transform.position += displacement;
                    c.radius = 2 * c.radius;

                }

                //Debug.Log("no joint found associated with: " + "articulation:"  + namebase);
                // mark to delete as is an end point
                //colliderNamesToDelete.Add(c.name);
                continue;
            }

            ArticulationBody ab = j.GetComponent<ArticulationBody>();
            if (ab == null || ab.transform == joints[0])
                continue;
            Vector3 dadPosition = articulatedDad.transform.position;

            Vector3 sonPosition = j.transform.position;

            if (articulatedDad.transform.childCount > 2) //2 is the next AB plus the collider
            {
                sonPosition = Vector3.zero;
                int i = 0;
                foreach (ArticulationBody tmp in articulatedDad.GetComponentsInChildren<ArticulationBody>()) {
                    sonPosition += tmp.transform.position;
                    i++;
                }
                sonPosition /= i;
            }


      

            //ugly but it seems to work.
            Vector3 direction = (dadPosition - sonPosition).normalized;
            float[] directionarray = new float[3] { Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z) };
            float maxdir = Mathf.Max(directionarray);
            List<float> directionlist = new List<float>(directionarray);
            c.direction = directionlist.IndexOf(maxdir);

            if(namebase != characterReferenceRoot.name)
                c.center = (sonPosition - dadPosition) / 2.0f;

            //to calculate the weight
            c.height = Vector3.Distance(dadPosition, sonPosition);
            c.radius = c.height / 7;
            ab = c.transform.parent.GetComponent<ArticulationBody>();
            ab.mass = massdensity *  Mathf.PI * c.radius *c.radius *c.height * Mathf.Pow(10,3); //we are aproximating as a cylinder, assuming it wants it in kg





        }
        // we do not delete end colliders as seams to delete feet
        // foreach (var name in colliderNamesToDelete)
        // {
        //     var toDel = colliders.First(x=>x.name == name);
        //     colliders.Remove(toDel);
        //     GameObject.DestroyImmediate(toDel);
        // }


        //2.3 we add a special treatment for the elements in the spine to have larger colliders: 


        //we estimate the width of the character:
        float bodyWidth = 0;

        foreach (Transform spineJoint in characterSpine) {

            int childCount = spineJoint.childCount;
      

            if (childCount > 1)//so, shoulders plus neck
            {
                int testIndex = 0;

                Vector2 range = Vector2.zero;
                foreach (Transform child in spineJoint)
                {
                    
                    if (child.localPosition.x < range[0])
                        range[0] = child.localPosition.x;
                    if (child.localPosition.x > range[1])
                        range[1] = child.localPosition.x;

                    testIndex++;
                }

                bodyWidth = 2.5f * (range[1] - range[0]);

                Debug.Log("spineJoint " + spineJoint.name + "has " + childCount +  "childs  and iterates" +  testIndex +    "which gives a body width estimated at: " + bodyWidth);

                //TODO: remove colliders in shoulders.

            }


        }


        //we put the right orientation and size:
        foreach (CapsuleCollider c in colliders)
        {

            string namebase = c.name.Replace("collider:", "");

            Transform spineJoint = characterSpine.FirstOrDefault(x => x.name == namebase);
            if (spineJoint == null) {
                if (namebase == characterReferenceRoot.name)//we check if it is the root
                    spineJoint = characterReferenceRoot;
                else
                    continue;

            }

            Transform j = joints.First(x => x.name == "articulation:" + namebase);
            // if not ArticulationBody, skip
            var aDad = j.GetComponent<ArticulationBody>();
            Vector3 dadPosition = aDad.transform.position;

                       

            //we put it horizontal
            c.direction = 0;// direction.x;

            
            float wide = c.height;//the height was previously calculated as a distance between successors
            c.radius = wide / 2;


            //c.height = 2 * wide;
            c.height = bodyWidth;



           
            aDad.mass = massdensity * Mathf.PI * c.radius * c.radius * c.height * Mathf.Pow(10, 3); //we are aproximating as a cylinder, assuming it wants it in kg
           

        }



        //we remove colliders from the shoulders, since they interfer with the collision structure
        foreach (Transform spineJoint in characterSpine)
        {

            int childCount = spineJoint.childCount;


            if (childCount > 1)//so, shoulders plus neck
            {
               
                foreach (Transform child in spineJoint)
                {

                    //a.we find the corresponding collider

                    string nameRef = child.name;


                    string nameColliderRef = "collider:" + nameRef;

                    Collider C2delete = colliders.FirstOrDefault<Collider>(x => x.name.Equals(nameColliderRef));


                    //b.we find the collider corresponding to its child, update the Parent chain to skip the reference cllider
                    Transform t = child.GetChild(0);
                    Collider Cson = colliders.FirstOrDefault<Collider>(x => x.name.Equals("collider:" + t.name));

                    Cson.GetComponent<HandleOverlap>().Parent = C2delete.GetComponent<HandleOverlap>().Parent;

                    //c.we destruct the corresponding collider
                    colliders.Remove(C2delete);
                    DestroyImmediate(C2delete.gameObject);

                }

            }


        }









        //2.4 we add sensors in feet to detect collisions
        addSensorsInFeet(root);

        //since we replaced 2 capsulecollider by two box collideR:
        colliders = new List<Collider>(root.GetComponentsInChildren<Collider>());



        //2.5 we make sure the key elements are added in the observations:


        foreach (Collider c in colliders)
        {

            string namebase = c.name.Replace("collider:", "");

            List<Transform> stuff2observe = new List<Transform>(characterReferenceHands);
            stuff2observe.AddRange(characterReferenceFeet);
            stuff2observe.Add(characterReferenceHead);
            stuff2observe.Add(characterReferenceRoot);
            Transform observationJoint = stuff2observe.FirstOrDefault(x => x.name == namebase);
            if (observationJoint == null)
            {
                 continue;

            }

            IgnoreColliderForObservation ig = c.GetComponent<IgnoreColliderForObservation>();
            if (ig != null)
                DestroyImmediate(ig);

        }



        //I add reference to the ragdoll, the articulationBodyRoot:
        target.ArticulationBodyRoot = root.GetComponent<ArticulationBody>();

        foreach (var articulationBody in root.GetComponentsInChildren<ArticulationBody>())
        {
            var overlap = articulationBody.gameObject.AddComponent<HandleOverlap>();
            overlap.Parent = target.ArticulationBodyRoot.gameObject;
        }




        //at this stage, every single articulatedBody is root. Check it out with the script below
        /*
        foreach (Transform j in joints)
        {
            ArticulationBody ab = j.transform.GetComponent<ArticulationBody>();
            if (ab.isRoot)
            {
                Debug.Log(ab.name + "is root ");
            }
        }
        */




        ProcRagdollAgent _ragdoll4training = temp.AddComponent<ProcRagdollAgent>();
        //      _ragdoll4training.transform.parent = trainingenv.transform;
        //_ragdoll4training.transform.SetParent(trainingenv.transform);

        _ragdoll4training.CameraTarget = root;


        // add the muscles. WE DID EARLIER
        //generateMuscles();

        //  the motor update mode is chosen with the motor update rule


        return _ragdoll4training;

    }


    void addSensorsInFeet(Transform root) {

        //I add the sensors in the feet:
        Transform[] pack2 = root.GetComponentsInChildren<Transform>();
        foreach (Transform t in characterReferenceFeet)
        {

            Transform footDadRef = t.parent;
            Transform footDad = pack2.First<Transform>(x => x.name == "articulation:" + footDadRef.name);
            Collider[] footColliders = footDad.GetComponentsInChildren<Collider>();


            Transform foot = pack2.First<Transform>(x => x.name == "articulation:" + t.name);

            // the sensor is based on the collider
            //Collider[] footColliders = foot.GetComponentsInChildren<Collider>();
            Collider footCollider = footColliders.First<Collider>(x => x.name.Contains(t.name));

            BoxCollider myBox = footCollider.gameObject.AddComponent<BoxCollider>();
            myBox.size = new Vector3(0.08f, 0.03f, 0.08f);//these is totally adjusted by hand


            myBox.name = footCollider.name;
            DestroyImmediate(footCollider);


            addSensor2FootCollider(myBox);

            //we update the list:
            footColliders = footDad.GetComponentsInChildren<Collider>();

            //we also add it to its father:
            Collider footColliderDad = footColliders.First<Collider>(x => x.name.Contains(footDadRef.name));

            BoxCollider myBoxDad = footColliderDad.gameObject.AddComponent<BoxCollider>();
            myBoxDad.size = new Vector3(0.08f, 0.05f, 0.15f);//these is totally adjusted by hand


            myBoxDad.transform.position = new Vector3(myBoxDad.transform.position.x, myBox.transform.position.y, myBoxDad.transform.position.z);

            myBoxDad.name = footColliderDad.name;
            DestroyImmediate(footColliderDad);


            addSensor2FootCollider(myBoxDad);



        }
    }


    void addSensor2FootCollider(Collider footCollider) {

        GameObject sensorGameObject = GameObject.Instantiate(footCollider.gameObject);

        sensorGameObject.name = sensorGameObject.name.Replace("(Clone)", "");
        sensorGameObject.name += "_sensor";
        Collider sensor = sensorGameObject.GetComponent<Collider>();
        sensor.isTrigger = true;
        // sensorGameObject.AddComponent<HandleOverlap>();
        sensorGameObject.AddComponent<SensorBehavior>();
        sensorGameObject.transform.parent = footCollider.transform.parent;
        sensorGameObject.transform.position = footCollider.transform.position;
        sensorGameObject.transform.rotation = footCollider.transform.rotation;


    }


    //it needs to go after adding ragdollAgent or it automatically ads an Agent, which generates conflict
    void addTrainingParameters(MapRagdoll2Anim target, ProcRagdollAgent temp) {



        BehaviorParameters bp = temp.gameObject.GetComponent<BehaviorParameters>();

        bp.BehaviorName = LearningConfigName;



        DecisionRequester dr =temp.gameObject.AddComponent<DecisionRequester>();
        dr.DecisionPeriod = 2;
        dr.TakeActionsBetweenDecisions = true;



        Rewards2Learn dcrew = temp.gameObject.AddComponent<Rewards2Learn>();
        dcrew.headname = "articulation:" + characterReferenceHead.name;
        dcrew.targetedRootName = "articulation:" + characterReferenceRoot.name; //it should be it's son, but let's see



        temp.MaxStep = 2000;
        temp.FixedDeltaTime = 0.0125f;
        temp.RequestCamera = true;


        temp.gameObject.AddComponent<SensorObservations>();
        Observations2Learn dcobs = temp.gameObject.AddComponent<Observations2Learn>();
        dcobs.targetedRootName = characterReferenceRoot.name;  // target.ArticulationBodyRoot.name;

        dcobs.targetedRootName = "articulation:" + characterReferenceRoot.name; //it should be it's son, but let's see

        MapRangeOfMotion2Constraints rom = temp.gameObject.AddComponent<MapRangeOfMotion2Constraints>();

        //used when we do not parse
        rom.info2store = info2store;
        

    }






    public void GenerateRangeOfMotionParser() {

        
        ROMparserSwingTwist rom = gameObject.GetComponentInChildren<ROMparserSwingTwist>();
        if (rom == null) {
            GameObject go = new GameObject();
            go.name = "ROM-parser";
            go.transform.parent = gameObject.transform;
            rom = go.AddComponent<ROMparserSwingTwist>();



        }


        rom.info2store = info2store;
        rom.theAnimator = characterReference;
        rom.skeletonRoot = characterReferenceRoot;

        rom.targetRagdollRoot = character4synthesis.GetComponent<MapRagdoll2Anim>().ArticulationBodyRoot;

        rom.trainingEnv = _outcome;

    }

    void generateMuscles() {

        //muscles

        foreach(ArticulationBody ab in articulatedJoints) { 

            ArticulationMusclesSimplified.MusclePower muscle = new ArticulationMusclesSimplified.MusclePower();
            muscle.PowerVector = new Vector3(40, 40, 40);


            muscle.Muscle = ab.name;

            if (muscleteam.MusclePowers == null)
                muscleteam.MusclePowers = new List<ArticulationMusclesSimplified.MusclePower>();

            muscleteam.MusclePowers.Add(muscle);

        }

      


    }






    public void Prepare4RangeOfMotionParsing()
    {
        _outcome.gameObject.SetActive(false);
        characterReference.gameObject.SetActive(true);

    }


    public void Prepare4EnvironmentStorage()
    {

        characterReference.gameObject.SetActive(false);

        _outcome.gameObject.SetActive(true);
      
       // RagDollAgent ra = _outcome.GetComponentInChildren<RagDollAgent>(true);
       // ra.gameObject.SetActive(true);





    }

    public void ApplyROMasConstraintsAndConfigure() {

        MapRangeOfMotion2Constraints ROMonRagdoll = Outcome.GetComponentInChildren<MapRangeOfMotion2Constraints>(true);
        //ROMonRagdoll.MinROMNeededForJoint = MinROMNeededForJoint;

        if (ROMonRagdoll.info2store == null) {
            ROMonRagdoll.info2store = this.info2store;

        }

        ROMonRagdoll.ConfigureTrainingForRagdoll(MinROMNeededForJoint);

        
        generateMuscles();

        ROMonRagdoll.GetComponent<DecisionRequester>().DecisionPeriod = 2;

        ROMonRagdoll.gameObject.SetActive(true); 
    }
}
