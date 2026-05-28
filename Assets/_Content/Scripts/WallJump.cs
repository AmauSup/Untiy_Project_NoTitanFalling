using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Adds wall running and wall jumping to the Player component.
/// Attach to the same GameObject as Player.
/// Assign the same InputActionAsset as in Player's References.
/// </summary>
[RequireComponent(typeof(Player))]
public class WallJump : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        [Header("Wall Detection")]

        [Tooltip("Layers considered as walls")]
        public LayerMask WallLayer;

        [Tooltip("Raycast distance to detect a wall on each side")]
        public float WallCheckDistance = 0.7f;

        [Header("Wall Run")]

        [Tooltip("Gravity counter-force applied while wall running (m/s²)")]
        public float WallRunGravityCounter = 18f;

        [Tooltip("Minimum vertical speed allowed while wall running (m/s, negative = downward)")]
        public float WallRunMinFallSpeed = -2f;

        [Tooltip("Maximum continuous wall run duration (s)")]
        public float MaxWallRunTime = 2.5f;

        [Header("Wall Jump")]

        [Tooltip("Upward velocity applied on wall jump (m/s) — should be >= Player JumpForce")]
        public float WallJumpUpForce = 12f;

        [Tooltip("Lateral push away from the wall on wall jump (m/s)")]
        public float WallJumpSideForce = 7f;

        [Tooltip("Forward impulse on wall jump (m/s)")]
        public float WallJumpForwardForce = 4f;

        [Tooltip("Duration over which the lateral force blends back to player input (s)")]
        public float WallJumpSideDuration = 0.35f;

        [Header("Wall Momentum")]

        [Tooltip("Instant speed boost applied on first wall contact (km/h)")]
        public float WallInitialBoost = 18f;

        [Tooltip("Speed lost per second while wall running (km/h per second)")]
        public float WallSpeedDrag = 6f;

        [Tooltip("Speed lost per second after leaving the wall in air (km/h per second)")]
        public float AirFrictionAfterWall = 9f;
    }

    [System.Serializable]
    public class References
    {
        [Tooltip("Same InputActionAsset as the one assigned to Player")]
        public InputActionAsset InputActions;
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;

    // ── Components ────────────────────────────────────────────────────────────
    private Player _player;
    private InputAction _jumpAction;

    // ── Wall detection ────────────────────────────────────────────────────────
    private bool _isWallRight;
    private bool _isWallLeft;
    private RaycastHit _rightWallHit;
    private RaycastHit _leftWallHit;

    // ── Wall run ──────────────────────────────────────────────────────────────
    private bool _isWallRunning;
    private float _wallRunTimer;

    // ── Wall jump ─────────────────────────────────────────────────────────────
    private bool _wallJumpActive;
    private Vector3 _wallJumpLateralForce; // horizontal direction set at jump moment
    private float _wallJumpTimeLeft;

    // ── Wall momentum ─────────────────────────────────────────────────────────
    private float _wallMomentumSpeed;   // accumulated speed bonus (km/h)
    private float _originalSpeed = -1f; // Player base speed saved before boost (-1 = not saved)

    // Jump input captured in Update(), used in LateUpdate()
    private bool _jumpTriggeredThisFrame;

    // ── Public read-only state ────────────────────────────────────────────────
    public bool IsWallRunning => _isWallRunning;
    public bool IsWallJumping => _wallJumpActive;

    // =========================================================================
    #region Unity Lifecycle

    void Awake()
    {
        _player     = GetComponent<Player>();
        _jumpAction = _references.InputActions.FindActionMap("Player").FindAction("Jump");
    }

    void OnEnable()  => _jumpAction?.Enable();
    void OnDisable() => _jumpAction?.Disable();

    void Update()
    {
        // Cache the jump trigger here — triggered is only reliable inside Update
        _jumpTriggeredThisFrame = _jumpAction.triggered;

        CheckForWall();
    }

    void LateUpdate()
    {
        // LateUpdate runs after ALL Update() calls, including Player's.
        // This lets us read / override the velocity that Player.cs wrote this frame.
        HandleWallRun();
        HandleWallJump();
        ApplyWallJumpLateralForce();
    }

    #endregion

    // =========================================================================
    #region Wall Logic

    /// <summary>
    /// Raycasts to the left and right to detect nearby walls.
    /// Also clears _isWallRunning immediately when no wall is found.
    /// </summary>
    private void CheckForWall()
    {
        _isWallRight = Physics.Raycast(transform.position,  transform.right,
                                       out _rightWallHit, _settings.WallCheckDistance, _settings.WallLayer);
        _isWallLeft  = Physics.Raycast(transform.position, -transform.right,
                                       out _leftWallHit,  _settings.WallCheckDistance, _settings.WallLayer);

        if (!_isWallRight && !_isWallLeft)
            _isWallRunning = false;
    }

    /// <summary>
    /// Enables wall running while the player is airborne, touching a wall and moving.
    /// Counteracts gravity so the player slides slowly down instead of falling freely.
    /// </summary>
    private void HandleWallRun()
    {
        bool wasWallRunning = _isWallRunning;
        bool touchingWall   = _isWallRight || _isWallLeft;
        bool hasSpeed       = _player.State.HorizontalVelocity.sqrMagnitude > 1f;
        bool timeRemaining  = _wallRunTimer < _settings.MaxWallRunTime;

        if (touchingWall && !_player.State.IsGrounded && hasSpeed && timeRemaining)
        {
            _isWallRunning  = true;
            _wallRunTimer  += Time.deltaTime;

            // First wall contact: save base speed and apply instant boost
            if (!wasWallRunning)
            {
                _player._settings.MaxJumps = 1;
                if (_originalSpeed < 0f)
                    _originalSpeed = _player._settings.Speed;
                // Keep current momentum if already faster than the initial boost
                _wallMomentumSpeed = Mathf.Max(_wallMomentumSpeed, _settings.WallInitialBoost);
            }

            // Gradually lose speed the longer the player stays on the wall
            _wallMomentumSpeed = Mathf.Max(0f, _wallMomentumSpeed - _settings.WallSpeedDrag * Time.deltaTime);
            _player._settings.Speed = _originalSpeed + _wallMomentumSpeed;

            // Counteract gravity: the player "clings" and slides down slowly
            if (_player.State.Velocity.y < 0)
            {
                _player.State.Velocity.y += _settings.WallRunGravityCounter * Time.deltaTime;
                _player.State.Velocity.y  = Mathf.Max(_player.State.Velocity.y, _settings.WallRunMinFallSpeed);
            }
        }
        else
        {
            _isWallRunning = false;

            if (_player.State.IsGrounded)
            {
                // Landing: restore base speed and reset all counters
                if (_originalSpeed >= 0f)
                {
                    _player._settings.Speed = _originalSpeed;
                    _originalSpeed          = -1f;
                }
                _wallRunTimer      = 0f;
                _wallMomentumSpeed = 0f;
            }
            else if (_wallMomentumSpeed > 0f && _originalSpeed >= 0f)
            {
                // Air friction after leaving the wall: speed decays gradually
                _wallMomentumSpeed = Mathf.Max(0f, _wallMomentumSpeed - _settings.AirFrictionAfterWall * Time.deltaTime);
                _player._settings.Speed = _originalSpeed + _wallMomentumSpeed;

                if (_wallMomentumSpeed <= 0f)
                {
                    _player._settings.Speed = _originalSpeed;
                    _originalSpeed          = -1f;
                }
            }
        }
    }

    /// <summary>
    /// Applies a wall jump when Jump is pressed during a wall run.
    ///
    /// Vertical velocity is overridden immediately (runs after Player.SetJump).
    /// Horizontal push is stored and blended over WallJumpSideDuration frames
    /// by ApplyWallJumpLateralForce().
    /// </summary>
    private void HandleWallJump()
    {
        if (!_jumpTriggeredThisFrame) return;
        if (!_isWallRunning)          return;

        // Normal of the wall we are running on = direction away from it
        Vector3 wallNormal = _isWallRight ? _rightWallHit.normal : _leftWallHit.normal;

        // ── Vertical launch ──────────────────────────────────────────────────
        // Overrides what Player.SetJump already set this frame (WallJumpUpForce > JumpForce)
        _player.State.Velocity.y = _settings.WallJumpUpForce;

        // ── Horizontal launch (applied frame-by-frame below) ─────────────────
        _wallJumpLateralForce   = wallNormal         * _settings.WallJumpSideForce
                                + transform.forward  * _settings.WallJumpForwardForce;
        _wallJumpLateralForce.y = 0f;

        _wallJumpActive   = true;
        _wallJumpTimeLeft = _settings.WallJumpSideDuration;

        
        // Exit wall run
        _isWallRunning = false;
        _wallRunTimer  = 0f;
    }

    /// <summary>
    /// Each LateUpdate during the wall jump window, blends the horizontal velocity
    /// from the wall jump direction back toward the player's input velocity.
    ///
    /// ratio 1 → 0 : full wall jump push → full player control
    ///
    /// Because Player.SetVelocity already wrote x/z this frame, we read that
    /// as the "player input" side of the blend — so control is restored naturally.
    /// </summary>
    private void ApplyWallJumpLateralForce()
    {
        if (!_wallJumpActive) return;

        _wallJumpTimeLeft -= Time.deltaTime;

        if (_wallJumpTimeLeft <= 0f)
        {
            _wallJumpActive = false;
            return;
        }

        float ratio = _wallJumpTimeLeft / _settings.WallJumpSideDuration; // 1 → 0

        // Player.SetVelocity already set this frame's input-based x/z
        Vector3 inputVelocity = new Vector3(_player.State.Velocity.x, 0f, _player.State.Velocity.z);

        // Lerp: start = full wall jump push, end = full player input
        Vector3 blended = Vector3.Lerp(inputVelocity, _wallJumpLateralForce, ratio);

        _player.State.Velocity.x = blended.x;
        _player.State.Velocity.z = blended.z;
    }

    #endregion
}
