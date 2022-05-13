using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Kinematic;
using Mujoco;

namespace Kinematic
{

    /// <summary>
    /// Provides access to COM properties of ArticulationBodies or Rigidbodies arranged in a kinematic chain hierarchy
    /// </summary>
    public class BodyChain
    {
        protected IReadOnlyList<IKinematic> chain;

        protected float mass; //chain.Select(k => k.Mass).Sum();
        public float Mass {get => mass;}
        public Vector3 CenterOfMass {get => chain.Select(k => k.Mass * k.CenterOfMass).Sum() / Mass;}
        public Vector3 CenterOfMassVelocity {get => chain.Select(k => k.Mass * k.Velocity).Sum() / Mass;}
        public IEnumerable<Vector3> CentersOfMass {get => chain.Select(k => k.CenterOfMass);}
        public IEnumerable<Vector3> Velocities {get => chain.Select(k => k.Velocity);}
        public IEnumerable<Matrix4x4> TransformMatrices { get => chain.Select(k => k.TransformMatrix); }
        public Vector3 RootForward { get => chain[0].TransformMatrix.GetColumn(2);  }

        public BodyChain() { }

        public BodyChain(Transform chainRoot)
        {
            chain = GetKinematicChain(chainRoot);

            mass = chain.Select(k => k.Mass).Sum();
        }

        public BodyChain(IEnumerable<Transform> bodies)
        {
            chain = bodies.Select(b => b.GetComponent<ArticulationBody>() ? (IKinematic)
                                        new ArticulationBodyAdapter(b.GetComponent<ArticulationBody>()) :
                                            (b.GetComponent<Rigidbody>()?
                                            new RigidbodyAdapter(b.GetComponent<Rigidbody>()) :
                                                new MjBodyAdapter(b.GetComponent<MjBody>()))).ToList().AsReadOnly();

            mass = chain.Select(k => k.Mass).Sum();
        }

        //Recursively find a read-only list of IKinematic, independent if its Rigidbodies or ArticulationBodies
        protected IReadOnlyList<IKinematic> GetKinematicChain(Transform root)
        {
            if(root.GetComponentInChildren<ArticulationBody>())
            {
                return root.GetComponentsInChildren<ArticulationBody>().Select(ab =>new ArticulationBodyAdapter(ab)).ToList().AsReadOnly();
            }
            else if(root.GetComponentInChildren<Rigidbody>())
            {
                return root.GetComponentsInChildren<Rigidbody>().Select(rb => new RigidbodyAdapter(rb)).ToList().AsReadOnly();
            }
            else
            {
                return root.GetComponentsInChildren<MjBody>().Select(rb => new MjBodyAdapter(rb)).ToList().AsReadOnly();
            }

        }

    
    
    }

    #region Adapters for Rigidbody and ArticulationBody
    // As we support both Rigidbodies and ArticulationBodies, the adapter pattern is used to unify their operation inside BodyChain

    public interface IKinematic
    {
        public Vector3 Velocity { get; }

        public Vector3 AngularVelocity { get; }
        public float Mass { get; }
        public Vector3 CenterOfMass { get; }

        public Matrix4x4 TransformMatrix { get; }

        public Vector3 GetPointVelocity(Vector3 worldPoint);

        public Vector3 GetRelativePointVelocity(Vector3 localPoint);
        public string Name { get; }

        public GameObject gameObject { get; }

        public static IKinematic GetKinematic(Transform transform)
        {
            return transform.GetComponent<ArticulationBody>() ? (IKinematic)
                                        new ArticulationBodyAdapter(transform.GetComponent<ArticulationBody>()) :
                                            (transform.GetComponent<Rigidbody>() ?
                                            new RigidbodyAdapter(transform.GetComponent<Rigidbody>()) :
                                                new MjBodyAdapter(transform.GetComponent<MjBody>()));
        }
    }

    public class RigidbodyAdapter : IKinematic
    {
        readonly private Rigidbody rigidbody;

        public RigidbodyAdapter(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public Vector3 Velocity => rigidbody.velocity;

        public Vector3 LocalVelocity => rigidbody.transform.parent.InverseTransformDirection(Velocity);

        public Vector3 AngularVelocity => rigidbody.angularVelocity;
        public Vector3 LocalAngularVelocity => rigidbody.transform.parent.InverseTransformDirection(rigidbody.angularVelocity);

        public float Mass => rigidbody.mass;

        public Vector3 CenterOfMass => rigidbody.transform.TransformPoint(rigidbody.centerOfMass);

        public string Name => rigidbody.name;

        public Matrix4x4 TransformMatrix => rigidbody.transform.localToWorldMatrix;

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            return rigidbody.GetPointVelocity(worldPoint);
        }

        public Vector3 GetRelativePointVelocity(Vector3 localPoint)
        {
            return rigidbody.GetRelativePointVelocity(localPoint);
        }

        public GameObject gameObject { get => rigidbody.gameObject; }
    }

    public class ArticulationBodyAdapter : IKinematic
    {
        readonly private ArticulationBody articulationBody;

        public ArticulationBodyAdapter(ArticulationBody articulationBody)
        {
            this.articulationBody = articulationBody;
        }

        public Vector3 Velocity => articulationBody.velocity;
        public Vector3 LocalVelocity => articulationBody.transform.parent.InverseTransformDirection(articulationBody.velocity);

        public Vector3 AngularVelocity => articulationBody.angularVelocity;
        public Vector3 LocalAngularVelocity => articulationBody.transform.parent.InverseTransformDirection(articulationBody.angularVelocity);

        public float Mass => articulationBody.mass;

        public Vector3 CenterOfMass => articulationBody.transform.TransformPoint(articulationBody.centerOfMass);

        public string Name => articulationBody.name;

        public Matrix4x4 TransformMatrix => articulationBody.transform.localToWorldMatrix;

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            return articulationBody.GetPointVelocity(worldPoint);
        }

        public Vector3 GetRelativePointVelocity(Vector3 localPoint)
        {
            return articulationBody.GetRelativePointVelocity(localPoint);
        }

        public GameObject gameObject { get => articulationBody.gameObject; }
    }

    public class MjBodyAdapter : IKinematic
    {
        readonly private MjBody mjBody;

        readonly private MjScene scene;

        readonly private float mass;

        readonly private Transform inertialTransform;

        readonly private Vector3 inertiaLocalPos;

        readonly private Matrix4x4 inertiaRelMatrix;

        public MjBodyAdapter(MjBody mjBody)
        {
            this.mjBody = mjBody;
            scene = MjScene.Instance; 
            mass = mjBody.GetMass();
            inertiaLocalPos = mjBody.GetLocalCenterOfMass();
            inertiaRelMatrix = mjBody.GetLocalCenterOfMassMatrix();

            inertialTransform = mjBody.transform.GetComponentInDirectChildren<BoxCollider>().transform; // Could be queried for Matrix and pos, but is unfortunately not up to date with the Mj simulation
        }

        public Vector3 Velocity => mjBody.GlobalVelocity();

        public Vector3 AngularVelocity => mjBody.GlobalAngularVelocity();

        public float Mass => mass;

        public Vector3 CenterOfMass =>mjBody.GetTransformMatrix().MultiplyPoint3x4(inertiaLocalPos);

        public string Name => mjBody.name;

        public Matrix4x4 TransformMatrix => mjBody.GetTransformMatrix() * inertiaRelMatrix;

        

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            
            return Vector3.Cross((worldPoint - CenterOfMass), AngularVelocity) + Velocity;
        }

        public Vector3 GetRelativePointVelocity(Vector3 localPoint)
        {
            return Vector3.Cross(localPoint, AngularVelocity) + Velocity;
        }

        public GameObject gameObject { get => mjBody.gameObject; }
    }
    #endregion
}

