using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Stunned,
        Eliminated,
        Loser,
        Winner,
        WallRunning,
    }

    [System.Serializable]
    public class WallRun
    {
        [Tooltip("Can the player run along walls?")]
        public bool CanRunOnWalls = true;

        [Tooltip("Layers considered as walkable wall")]
        public LayerMask WallLayer = 0;

        [Tooltip("Stick duration to wall before fall")]
        public float StickDuration = 3;

        [Tooltip("Gravity applyed when sticked to wall")]
        public float StickedGravity = -5;
    }

    [System.Serializable]
    public class Settings
    {
        [Header("Movements")]

        [Tooltip("Walk speed in km/h")]
        public float WalkSpeed = 15f;

        [Tooltip("Run speed in km/h")]
        public float RunSpeed = 30f;

        [Tooltip("Jump force in m/s")]
        public float JumpForce = 8f;

        [Tooltip("Player rotation speed towards movement direction")]
        public float RotationSpeed = 10f;

        [Tooltip("Ground detection tolerance")]
        public float GroundTolerance = 0.2f;

        [Tooltip("Layers considered as ground")]
        public LayerMask GroundLayer = 1;

        [Tooltip("Layers considered as death zone")]
        public LayerMask DeathLayer = 0;

        [Header("Forces")]

        [Tooltip("Decay rate of extra forces (m/s²)")]
        public float ExtraForcesDrag = 8f;

        [Tooltip("Maximum number of jumps (1 = single jump, 2 = double jump)")]
        public int MaxJumps = 2;

        [Header("Debug")]

        [Tooltip("GUI logs of current state")]
        public bool StateLogs;

        [Header("Features")]

        public WallRun WallRun;
    }

    [System.Serializable]
    public class References
    {
        public CharacterController Controller;
        public InputActionAsset InputActions;
    }

    [System.Serializable]
    public class StateContainer
    {
        [Tooltip("Current player state")]
        public PlayerState CurrentState = PlayerState.Idle;

        [Tooltip("Is player paused?")]
        public bool IsPaused = false;

        [Tooltip("Is player sticked to wall?")]
        public bool IsStickedToWall;

        [Tooltip("Can player stick to walls?")]
        public bool CanStickToWalls;

        [Tooltip("Current running state")]
        [Range(0, 1)] public float Running;

        [Tooltip("Animation center of gravity")]
        [Range(-1, 1)] public float AnimationCenter;

        [Tooltip("Elapsed time since sticked to wall")]
        [Range(-1, 1)] public float StickedTime;

        [Tooltip("Current velocity in m/s")]
        public Vector3 Velocity;

        [Tooltip("Ground transform evaluated as parent")]
        public Transform Ground;

        public bool IsGrounded => Ground;
        public float VerticalVelocity => Velocity.y;
        public Vector3 HorizontalVelocity => new Vector3(Velocity.x, 0, Velocity.z);
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;
    [SerializeField, ReadOnly] private StateContainer _state;

    public StateContainer State => _state;
    public Settings PlayerSettings => _settings;

    public Vector3 ExternalVelocity;

    #region Constants
    private const float KMH_TO_MS = 1 / 3.6f;
    private const float STICK_FORCE = -5f;
    private const float GRAVITY = -20f;
    private const float MAX_GRAVITY = -50f;
    #endregion

    #region Private Fields
    // Inputs
    private InputAction _moveAction;
    private InputAction _runAction;
    private InputAction _jumpAction;

    // Camera
    private Camera _camera;

    // Ground
    private Vector3 _groundCheckRayOffset;
    private Vector3 _groundCheckSphereOffset;
    private float _groundCheckRadius;
    private Collider[] _overlapResults = new Collider[1];
    private Vector3 _wallDirection;
    private Vector3 _wallPosition;
    private Vector3 _lastPlatformPosition;
    private Quaternion _lastPlatformRotation;
    private Vector3 _platformVelocity;

    // Jump counter
    private int _jumpsLeft;
    #endregion

    #region Unity Lifecycle
    /*void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(.2f, 1, .7f, .3f);
        Gizmos.DrawSphere(_groundCheckSphereOffset, _groundCheckRadius);
    }*/

    void Awake()
    {
        if (!Instance)
            Instance = this;

        // Inputs
        _moveAction = _references.InputActions.FindActionMap("Player").FindAction("Move");
        _runAction = _references.InputActions.FindActionMap("Player").FindAction("Sprint");
        _jumpAction = _references.InputActions.FindActionMap("Player").FindAction("Jump");

        // Camera
        _camera = Camera.main;

        // Ground check geometry
        CharacterController cc = _references.Controller;
        _groundCheckRayOffset = cc.center + Vector3.up * (-cc.height * .5f - cc.skinWidth + _settings.GroundTolerance);
        _groundCheckSphereOffset = cc.center + Vector3.up * (-cc.height * .5f + cc.radius - cc.skinWidth - _settings.GroundTolerance);
        _groundCheckRadius = cc.radius;

        _jumpsLeft = _settings.MaxJumps;
        _state.CanStickToWalls = true;
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _runAction?.Enable();
        _jumpAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _runAction?.Disable();
        _jumpAction?.Disable();
    }

    void Update()
    {
        float t = Time.deltaTime;

        CheckGround(t);
        SetGravity(t);
        SetVelocity(t);
        SetJump();
        DecayExternalVelocity(t);
        SetMovement(t);
        SetState();
    }
    #endregion

    #region Player Logic
    private void CheckGround(float deltaTime)
    {
        // Raycast for center contact
        RaycastHit rayInfo;
        Vector3 rayOrigin = transform.position + _groundCheckRayOffset;
        bool rayHit = Physics.Raycast(rayOrigin, Vector3.down, out rayInfo, _settings.GroundTolerance * 2f, _settings.GroundLayer);

        // OverlapSphere for edge contact
        Vector3 sphereOrigin = transform.position + _groundCheckSphereOffset;
        int overlapCount = Physics.OverlapSphereNonAlloc(sphereOrigin, _groundCheckRadius, _overlapResults, _settings.GroundLayer);
        bool sphereHit = overlapCount > 0;

        bool wasGrounded = _state.IsGrounded;
        bool wasStickedToWall = _state.IsStickedToWall;
        bool isGrounded = rayHit || sphereHit;

        // Wall run
        bool isStickedToWall = false;
        if (!isGrounded && _settings.WallRun.CanRunOnWalls && _state.CanStickToWalls && _state.VerticalVelocity < 0)
        {
            int[] wallCheckAngles = new int[6] { 80, 70, 60, 50, 40, 30 };
            int raySign = 1;

            for (int i = 0; i < wallCheckAngles.Length; i++)
            {
                if (isStickedToWall)
                    break;

                for (int u = -1; u <= 1; u += 2)
                {
                    Vector3 rayDir = Quaternion.Euler(0, wallCheckAngles[i] * u, 0) * transform.forward;
                    float rayOffset = _references.Controller.radius * .8f;
                    float raySize = _references.Controller.radius * .7f;

                    isStickedToWall = Physics.Raycast(rayOrigin + rayDir * rayOffset, rayDir, out rayInfo, raySize, _settings.WallRun.WallLayer);
                    Debug.DrawRay(rayOrigin + rayDir * rayOffset, rayDir * raySize, isStickedToWall ? Color.green : Color.red);

                    if (isStickedToWall)
                    {
                        raySign = u;
                        goto AfterRaycastWall;
                    }
                }
            }
            AfterRaycastWall:

            if (!_state.IsStickedToWall && isStickedToWall)
                _state.StickedTime = 0;
            else if (_state.IsStickedToWall && !isStickedToWall)
                _state.CanStickToWalls = true;

            _state.IsStickedToWall = isStickedToWall;

            if (_state.IsStickedToWall)
            {
                _wallDirection = Vector3.ProjectOnPlane(Vector3.Cross(rayInfo.normal, Vector3.up), Vector3.up) * Mathf.Sign(Vector3.Cross(transform.forward, rayInfo.normal).y);
                _wallPosition = rayInfo.point;
                _state.AnimationCenter = raySign;
                _state.Running = 1;

                if (_state.StickedTime >= _settings.WallRun.StickDuration)
                {
                    _state.IsStickedToWall = false;
                    _state.CanStickToWalls = false;
                }
            }
        }
        else if (isGrounded)
        {
            _state.IsStickedToWall = false;
            _state.CanStickToWalls = true;
        }

        if (!isStickedToWall)
            _state.AnimationCenter = 0;

        if (isGrounded || isStickedToWall)
        {
            bool justLanded      = !wasGrounded      && isGrounded;
            bool justGrabbedWall = !wasStickedToWall && isStickedToWall;
            if (justLanded || justGrabbedWall)
                _jumpsLeft = _settings.MaxJumps;

            Transform currentGround = rayHit ? rayInfo.collider.transform : _overlapResults[0].transform;

            // Initialize references when landing on a new surface to prevent teleporting
            if (currentGround != _state.Ground)
            {
                _state.Ground = currentGround;
                _lastPlatformPosition = _state.Ground.position;
                _lastPlatformRotation = _state.Ground.rotation;

                _platformVelocity.y = 0;

                return;
            }

            // Rotate player around platform pivot
            Quaternion rotationDelta = _state.Ground.rotation * Quaternion.Inverse(_lastPlatformRotation);
            float platformYaw = rotationDelta.eulerAngles.y;

            if (Mathf.Abs(platformYaw) > .001f)
            {
                Vector3 dir = transform.position - _state.Ground.position;
                dir = Quaternion.Euler(0, platformYaw, 0) * dir;
                transform.position = _state.Ground.position + dir;
                transform.Rotate(0, platformYaw, 0);
            }

            // Translation delta
            Vector3 platformDelta = _state.Ground.position - _lastPlatformPosition;
            transform.position += platformDelta;

            // Store current state for next frame
            _lastPlatformPosition = _state.Ground.position;
            _lastPlatformRotation = _state.Ground.rotation;

            // Sync physics broadphase to prevents CC from seeing stale overlap
            Physics.SyncTransforms();

            // Reset platform velocity
            _platformVelocity = Vector3.zero;
        }
        else
        {
            // Inherit platform velocity when player left the ground
            if (wasGrounded && _state.Ground != null)
            {
                _platformVelocity = (_state.Ground.position - _lastPlatformPosition) / Time.deltaTime;
            }
            // Decay velocity when player is in the air
            else
            {
                Vector3 platformVelocity = Vector3.MoveTowards(_platformVelocity, Vector3.zero, _settings.ExtraForcesDrag * deltaTime);
                platformVelocity.y = _platformVelocity.y;
                _platformVelocity = platformVelocity;
            }

            _state.Ground = null;
        }
    }

    private void SetGravity(float deltaTime)
    {
        if (_state.IsStickedToWall)
        {
            _state.Velocity.y = _settings.WallRun.StickedGravity * Mathf.Pow(_state.StickedTime / _settings.WallRun.StickDuration, 2);
        }
        else if (_state.IsGrounded && _state.Velocity.y < 0)
        {
            _state.Velocity.y = STICK_FORCE;
        }
        else
        {
            if (_platformVelocity.y > 0)
            {
                _platformVelocity.y += GRAVITY * deltaTime;

                if (_platformVelocity.y < 0)
                    _state.Velocity.y += _platformVelocity.y;
            }
            else
            {
                _state.Velocity.y += GRAVITY * deltaTime;
            }

            _state.Velocity.y = Mathf.Max(_state.Velocity.y, MAX_GRAVITY);
        }
    }

    private void SetVelocity(float deltaTime)
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();

        bool run = _runAction.IsPressed();
        _state.Running = Mathf.Clamp01(_state.Running + deltaTime * 4 * (run ? 1 : -1));

        float speed = Mathf.Lerp(_settings.WalkSpeed, _settings.RunSpeed, _state.Running) * KMH_TO_MS;

        Vector3 moveInput = new Vector3(input.x, 0, input.y); // Convert input 2D to move 3D
        if (_state.IsStickedToWall)
        {
            _state.StickedTime += deltaTime * Mathf.Lerp(4, 1, moveInput.magnitude);
            moveInput = _wallDirection * Mathf.Sqrt(1 - _state.StickedTime / _settings.WallRun.StickDuration);
        }
        else
        {
            moveInput = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0) * moveInput; // Rotate move toward camera
        }
        moveInput *= speed; // Muliply move by speed

        _state.Velocity.x = moveInput.x;
        _state.Velocity.z = moveInput.z;

        // Rotate Player
        if (moveInput.sqrMagnitude > .001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            if (_state.IsStickedToWall)
                targetRot = Quaternion.LookRotation(_wallDirection);

            float t = _settings.RotationSpeed * deltaTime;
            Vector3 euler = Quaternion.Slerp(transform.rotation, targetRot, t).eulerAngles;

            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }

    private void SetJump()
    {
        if (!_jumpAction.triggered) return;
        if (_jumpsLeft <= 0 && !_state.IsStickedToWall) return;

        _state.Velocity.y = _settings.JumpForce;
        _state.Ground = null;

        if (_state.IsStickedToWall)
        {
            _state.IsStickedToWall = false;
            _state.CanStickToWalls = true;
            _jumpsLeft = _settings.MaxJumps;
        }
        else
        {
            _jumpsLeft--;
        }

        _groundCheckRadius = _references.Controller.radius + _settings.GroundTolerance;
    }

    private void DecayExternalVelocity(float deltaTime)
    {
        ExternalVelocity = Vector3.MoveTowards(ExternalVelocity, Vector3.zero, _settings.ExtraForcesDrag * deltaTime);
    }

    private void SetMovement(float deltaTime)
    {
        Vector3 motion = _state.Velocity + _platformVelocity + ExternalVelocity;

        if (_state.IsStickedToWall)
        {
            Vector3 wallDelta = _wallPosition - transform.position;
            wallDelta -= wallDelta.normalized * _references.Controller.radius;
            motion += wallDelta;
        }

        _references.Controller.Move(motion * deltaTime);
    }

    private void SetState()
    {
        if (State.IsGrounded)
        {
            if (State.HorizontalVelocity.sqrMagnitude > .1f)
                State.CurrentState = PlayerState.Moving;
            else
                State.CurrentState = PlayerState.Idle;
        }
        else if (State.IsStickedToWall)
        {
            State.CurrentState = PlayerState.WallRunning;
        }
        else
        {
            if (State.VerticalVelocity > 0)
                State.CurrentState = PlayerState.Jumping;
            else
                State.CurrentState = PlayerState.Falling;
        }
    }
    #endregion
}
