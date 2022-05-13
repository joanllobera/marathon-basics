using UnityEngine;


public class AnimationMocapController : MonoBehaviour, IAnimationController
{
    Animator _anim;

    CharacterController _characterController;

    Quaternion _targetDirection;    // direction we want to move towards

    public Vector3 MovementVelocity;

    public void OnEnable()
    {
        OnAgentInitialize();
    }

    public void OnAgentInitialize()
    {
        if (!_anim)
            _anim = GetComponent<Animator>();

        if (!_characterController)
            _characterController = GetComponent<CharacterController>();

        _targetDirection = Quaternion.Euler(0, 90, 0);
        MovementVelocity = Vector3.zero;
    }
    public void OnReset()
    {
        _targetDirection = Quaternion.Euler(0, 90, 0);
        // MovementVelocity = Vector3.zero;
        // _anim.Rebind();
        // _anim.Update(0f);
    }
    void OnAnimatorMove()
    {
        if (_anim == null)
            return;
        var movement = _anim.deltaPosition;
        movement.y = 0f;
        MovementVelocity = movement / Time.deltaTime;
        this.transform.position += movement;
    }
    public Vector3 GetDesiredVelocity()
    {
        return MovementVelocity;
    }
}