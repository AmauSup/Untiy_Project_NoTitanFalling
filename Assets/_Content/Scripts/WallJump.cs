using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Titanfall 2-style wall run and wall jump based on momentum conservation.
///
/// Core philosophy:
///   - The wall redirects the player's momentum, it doesn't replace it.
///   - All forces are additive on top of Player.ExternalVelocity.
///   - The CharacterController naturally handles sliding along wall surfaces.
///
/// Requires Player.cs to expose ExternalVelocity (public Vector3) and include it in SetMovement.
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

        [Tooltip("Raycast distance to detect walls on each side")]
        public float WallCheckDistance = 0.8f;

        [Tooltip("Minimum total horizontal speed required to enter wall run (m/s)")]
        public float MinWallRunSpeed = 2f;

        [Header("Wall Run")]

        [Tooltip("Counter-force per second applied to gravity while wall running (m/s²).\n" +
                 "Net vertical accel = Player gravity (-20) + this value.\n" +
                 "16 → net -4 m/s²  |  18 → net -2 m/s²  |  20 → hover")]
        public float WallGravityCounter = 16f;

        [Tooltip("Minimum fall speed while wall running (m/s, negative = downward)")]
        public float WallRunMinFallSpeed = -2f;

        [Tooltip("Additive tangential acceleration along the wall surface (m/s²)")]
        public float WallTangentialAccel = 5f;

        [Tooltip("Soft speed cap for the ExternalVelocity tangential component (m/s).\n" +
                 "Excess is removed gradually instead of clamped.")]
        public float WallMaxTangentialSpeed = 8f;

        [Tooltip("Maximum continuous wall run duration (s)")]
        public float MaxWallRunTime = 3f;

        [Header("Wall Jump")]

        [Tooltip("Fraction of ExternalVelocity retained on wall jump (0 = full reset, 1 = full keep)")]
        [Range(0.7f, 1f)]
        public float MomentumRetention = 0.9f;

        [Tooltip("Impulse away from the wall on wall jump (m/s)")]
        public float WallJumpPush = 5f;

        [Tooltip("Upward velocity set on wall jump (m/s). Overrides Player's normal jump force.")]
        public float WallJumpUpForce = 12f;

        [Tooltip("Forward tangential impulse on wall jump (m/s)")]
        public float WallJumpForwardBoost = 3f;

        [Tooltip("Time before the same wall can be used again (s)")]
        public float SameWallCooldown = 0.25f;
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

    // Current wall geometry — updated each frame during wall run
    private Vector3 _wallNormal;
    private Vector3 _wallTangent;

    // ── Wall run ──────────────────────────────────────────────────────────────
    private bool _isWallRunning;
    private float _wallRunTimer;

    // ── Same-wall cooldown ────────────────────────────────────────────────────
    private Vector3 _lastWallNormal;   // normal of the last wall the player jumped from
    private float _wallCooldownTimer;

    // Jump input captured in Update(), consumed in LateUpdate()
    private bool _jumpTriggeredThisFrame;

    // ── Public read-only state (usable by camera or animation scripts) ────────
    public bool IsWallRunning => _isWallRunning;
    public bool IsWallOnRight => _isWallRight && _isWallRunning;
    public bool IsWallOnLeft  => _isWallLeft  && _isWallRunning;

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
        _jumpTriggeredThisFrame = _jumpAction.triggered;

        if (_wallCooldownTimer > 0f)
            _wallCooldownTimer -= Time.deltaTime;

        CheckForWall();
    }

    void LateUpdate()
    {
        // LateUpdate runs after ALL Update() calls.
        // ExternalVelocity written here is picked up by Player.SetMovement on the NEXT frame.
        HandleWallRun();
        HandleWallJump();
    }

    #endregion

    // =========================================================================
    #region Wall Logic

    private void CheckForWall()
    {
        _isWallRight = Physics.Raycast(transform.position,  transform.right,
                                       out _rightWallHit, _settings.WallCheckDistance, _settings.WallLayer);
        _isWallLeft  = Physics.Raycast(transform.position, -transform.right,
                                       out _leftWallHit,  _settings.WallCheckDistance, _settings.WallLayer);

        if (!_isWallRight && !_isWallLeft)
            _isWallRunning = false;
    }

    private void HandleWallRun()
    {
        bool wasWallRunning = _isWallRunning;

        // ── Entry conditions ──────────────────────────────────────────────────
        bool touchingWall  = _isWallRight || _isWallLeft;
        bool hasSpeed      = TotalHorizontalSpeed() >= _settings.MinWallRunSpeed;
        bool timeRemaining = _wallRunTimer < _settings.MaxWallRunTime;

        // Same-wall cooldown: block re-grab if the wall normal is nearly identical
        RaycastHit hit           = _isWallRight ? _rightWallHit : _leftWallHit;
        Vector3 candidateNormal  = hit.normal;
        bool coolingDown         = _wallCooldownTimer > 0f &&
                                   Vector3.Dot(candidateNormal, _lastWallNormal) > 0.9f;

        if (touchingWall && !_player.State.IsGrounded && hasSpeed && timeRemaining && !coolingDown)
        {
            _isWallRunning  = true;
            _wallRunTimer  += Time.deltaTime;

            _wallNormal  = candidateNormal;
            _wallTangent = ComputeWallTangent(_wallNormal);

            // ── First wall contact this air sequence ──────────────────────────
            if (!wasWallRunning)
            {
                _player._settings.MaxJumps = 2;

                // Project ExternalVelocity onto the wall plane:
                // remove only the component pushing INTO the wall, keep the rest.
                float intoWall = Vector3.Dot(_player.ExternalVelocity, _wallNormal);
                if (intoWall < 0f)
                    _player.ExternalVelocity -= _wallNormal * intoWall;
            }

            // ── Gravity reduction ─────────────────────────────────────────────
            // Partially counteract Player's gravity (-20 m/s²) so the player slides
            // down slowly rather than falling. Only active while falling.
            if (_player.State.Velocity.y < 0f)
            {
                _player.State.Velocity.y += _settings.WallGravityCounter * Time.deltaTime;
                _player.State.Velocity.y  = Mathf.Max(_player.State.Velocity.y, _settings.WallRunMinFallSpeed);
            }

            // ── Tangential acceleration ───────────────────────────────────────
            // Additive push forward along the wall. Player's input speed (from Player.SetVelocity)
            // is preserved; this force layers on top via ExternalVelocity.
            float tangentialSpeed = Vector3.Dot(_player.ExternalVelocity, _wallTangent);

            if (tangentialSpeed < _settings.WallMaxTangentialSpeed)
            {
                _player.ExternalVelocity += _wallTangent * _settings.WallTangentialAccel * Time.deltaTime;
            }
            else
            {
                // Soft cap: bleed off excess gradually instead of a hard cut
                float excess = tangentialSpeed - _settings.WallMaxTangentialSpeed;
                _player.ExternalVelocity -= _wallTangent * (excess * 3f * Time.deltaTime);
            }
        }
        else
        {
            _isWallRunning = false;

            if (_player.State.IsGrounded)
            {
                // Full reset on landing
                _wallRunTimer      = 0f;
                _wallCooldownTimer = 0f;
                _lastWallNormal    = Vector3.zero;
                // ExternalVelocity is left to decay naturally via Player.ExtraForcesDrag
            }
        }
    }

    private void HandleWallJump()
    {
        if (!_jumpTriggeredThisFrame) return;
        if (!_isWallRunning)          return;

        // ── Additive wall jump ────────────────────────────────────────────────
        //
        // ExternalVelocity (horizontal momentum):
        //   existing * retention  +  push away from wall  +  forward boost along wall
        //
        // velocity.y (vertical):
        //   set directly to WallJumpUpForce, overriding Player.SetJump this frame.
        //   No retention on y — gives a clean upward impulse regardless of current fall speed.
        //
        float r = _settings.MomentumRetention;

        _player.ExternalVelocity = _player.ExternalVelocity * r
                                 + _wallNormal  * _settings.WallJumpPush
                                 + _wallTangent * _settings.WallJumpForwardBoost;

        _player.State.Velocity.y = _settings.WallJumpUpForce;

        // Register cooldown so the player cannot immediately re-grab the same wall
        _lastWallNormal    = _wallNormal;
        _wallCooldownTimer = _settings.SameWallCooldown;

        _isWallRunning = false;
        _wallRunTimer  = 0f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float TotalHorizontalSpeed()
    {
        Vector3 h = _player.State.HorizontalVelocity
                  + new Vector3(_player.ExternalVelocity.x, 0f, _player.ExternalVelocity.z);
        return h.magnitude;
    }

    private Vector3 ComputeWallTangent(Vector3 wallNormal)
    {
        // Horizontal tangent to the wall surface: Cross(wallNormal, up)
        Vector3 tangent = Vector3.Cross(wallNormal, Vector3.up).normalized;

        // Choose the sign that aligns with the player's current motion direction
        Vector3 motion = _player.State.HorizontalVelocity
                       + new Vector3(_player.ExternalVelocity.x, 0f, _player.ExternalVelocity.z);

        if (0.01f > motion.sqrMagnitude)
            motion = transform.forward;

        if (Vector3.Dot(tangent, motion) < 0f)
            tangent = -tangent;

        return tangent;
    }

    #endregion
}
