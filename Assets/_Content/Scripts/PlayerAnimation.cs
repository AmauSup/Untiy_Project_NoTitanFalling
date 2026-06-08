using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    public static PlayerAnimation Instance { get; private set; }

    [System.Serializable]
    public class References
    {
        public Animator Anim;
    }

    [System.Serializable]
    private class PlayerAnimationStateMapper
    {
        public Player.PlayerState PlayerState;
        public string AnimatorState;
        public string BlockingState;
        public string Trigger;
    }

    [SerializeField] private References _references;
    [SerializeField, ReadOnly] private PlayerAnimationStateMapper[] _stateMapper;

    void OnValidate()
    {
        Init();
    }

    void Awake()
    {
        Init();

        if (!Instance)
            Instance = this;
    }

    void LateUpdate()
    {
        UpdateAnimation();
    }

    private void Init()
    {
        _stateMapper = new PlayerAnimationStateMapper[10];

        _stateMapper[0] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Idle,
            AnimatorState = "move",
            BlockingState = "",
            Trigger = "trigger_move",
        };

        _stateMapper[1] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Moving,
            AnimatorState = "move",
            BlockingState = "",
            Trigger = "trigger_move",
        };

        _stateMapper[2] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Jumping,
            AnimatorState = "jump",
            BlockingState = "fall",
            Trigger = "trigger_jump",
        };

        _stateMapper[3] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Falling,
            AnimatorState = "fall",
            BlockingState = "jump",
            Trigger = "trigger_fall",
        };

        _stateMapper[4] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Stunned,
            AnimatorState = "stun",
            BlockingState = "",
            Trigger = "trigger_stun",
        };

        _stateMapper[5] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Eliminated,
            AnimatorState = "eliminate",
            BlockingState = "",
            Trigger = "trigger_eliminate",
        };

        _stateMapper[6] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Loser,
            AnimatorState = "lose",
            BlockingState = "",
            Trigger = "trigger_lose",
        };

        _stateMapper[7] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.Winner,
            AnimatorState = "win",
            BlockingState = "",
            Trigger = "trigger_win",
        };

        _stateMapper[8] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.WallRunningLeft,
            AnimatorState = "runwallleft",
            BlockingState = "",
            Trigger = "trigger_runwallleft",
        };

        _stateMapper[9] = new PlayerAnimationStateMapper()
        {
            PlayerState = Player.PlayerState.WallRunningRight,
            AnimatorState = "runwallright",
            BlockingState = "",
            Trigger = "trigger_runwallright",
        };
    }

    private void UpdateAnimation()
    {
        if (!Player.Instance)
            return;

        if (_references.Anim.IsInTransition(0))
            return;

        AnimatorStateInfo currentState = _references.Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextState = _references.Anim.GetNextAnimatorStateInfo(0);

        PlayerAnimationStateMapper currentStateMapper = _stateMapper.FirstOrDefault(m => m.PlayerState == Player.Instance.State.CurrentState);

        if (currentStateMapper != null &&
            !currentState.IsName(currentStateMapper.AnimatorState) &&
            !nextState.IsName(currentStateMapper.AnimatorState) &&
            !currentState.IsName(currentStateMapper.BlockingState))
        {
            _references.Anim.SetTrigger(currentStateMapper.Trigger);
        }

        switch (Player.Instance.State.CurrentState)
        {
            case Player.PlayerState.Idle:
            case Player.PlayerState.Moving:
                _references.Anim.SetFloat("move", Player.Instance.State.HorizontalVelocity.magnitude);
                _references.Anim.SetFloat("run", Mathf.InverseLerp(0, .8f, Player.Instance.State.Running));
                _references.Anim.SetFloat("wall", 0.5f);
                break;
            case Player.PlayerState.WallRunningLeft:
            case Player.PlayerState.WallRunningRight:
                _references.Anim.SetFloat("move", Player.Instance.State.HorizontalVelocity.magnitude);
                _references.Anim.SetFloat("run", 1f);
                break;
        }
    }
}
