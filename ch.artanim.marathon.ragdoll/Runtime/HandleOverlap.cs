using UnityEngine;

namespace Unity.MLAgents
{
    public class HandleOverlap : MonoBehaviour
    {
        public GameObject Parent;

        void OnEnable()
        {
            enabled = false;
            if (Parent == null)
                return;
            var collider = GetComponent<Collider>();
            var parentCollider = Parent.GetComponent<Collider>();
            if (collider == null || parentCollider == null)
                return;
            // Debug.Log($"Physics.IgnoreCollision: {collider.name} and {parentCollider.name}");
            Physics.IgnoreCollision(collider, parentCollider);
        }
    }
}
