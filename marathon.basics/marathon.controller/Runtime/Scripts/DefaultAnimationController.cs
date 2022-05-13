using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//This is a very simple animation controller that does nothing.
//We will use it when we generate a physics ragdoll from an animated character
//that has no specific animationController (i.e., a component that implements the IAnimaitonController interface)
public class DefaultAnimationController : MonoBehaviour, IAnimationController
{


    [SerializeField]
    Animator _anim;



    public void OnEnable()
    {

        OnAgentInitialize();
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OnAgentInitialize() {

        if (!_anim)
            _anim = GetComponent<Animator>();


    }
    public void OnReset() { 
    
    
    
    }
    public Vector3 GetDesiredVelocity() {
        //TODO: check if this is really what we want, we may need the root velocity
        return _anim.angularVelocity;
    
    
    }


}
