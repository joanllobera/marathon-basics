using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MocapRangeOfMotionAnimatorController : MonoBehaviour
{
    Animator _anim;
    Animation _animation;
    public Motion[] Motions;

    // Start is called before the first frame update
    void Start()
    {
        _anim = GetComponent<Animator>();
        foreach (var motion in Motions)
        {
            // _anim.CrossFade()
            // _anim.()
            // _animation.CrossFadeQueued(motion.name);
        }
        // _animation = GetComponent<Animation>();
        // // _characterController = GetComponent<CharacterController>();
        // // _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        // // _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        // // _targetDirection = Quaternion.Euler(0, 90, 0);
        // // var ragDoll = _spawnableEnv.GetComponentInChildren<RagDollAgent>( true);//we include inactive childs
        // _animation.CrossFadeQueued("jump");
        // _animation.CrossFadeQueued("animation2");
        // _animation.CrossFadeQueued("animation3");
        // var anim = _animation["animation3"];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // Called each physics step (so long as the Animator component is set to Animate Physics) after FixedUpdate to override root motion.
    void OnAnimatorMove()
    {
        // do nothing
    }
}
