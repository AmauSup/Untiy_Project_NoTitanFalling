using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Air dash: propels the player forward in mid-air.
/// Recharges on ground landing and on first wall contact (WallJump component optional).
/// </summary>
[RequireComponent(typeof(Player))]
public class AirDash : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        [Header("Air Dash")]

        [Tooltip("Forward impulse applied on dash (m/s)")]
        public float DashForce = 15f;

        [Tooltip("Cooldown between two dashes (s)")]
        public float Cooldown = 0.8f;

        [Tooltip("Number of air dashes available per airborne phase")]
        public int MaxAirDashes = 1;

        [Header("Input")]

        [Tooltip("Keyboard key that triggers the dash")]
        public Key KeyboardKey = Key.LeftShift;

        [Tooltip("Use gamepad West button (X on Xbox) as well")]
        public bool UseGamepadWest = true;

        [Tooltip("Input buffer window (s) — the dash fires within this delay after pressing the key,\n" +
                 "even if the player is not yet airborne at the exact moment of the press.")]
        public float InputBuffer = 0.15f;
    }

    [SerializeField] private Settings _settings;

    private Player   _player;
    private WallJump _wallJump;   // optional — auto-detected at Awake

    private float _cooldownTimer;
    private int   _dashesLeft;

    // Recharge tracking
    private bool _wasGrounded;
    private bool _wasWallRunning;

    // Input buffer: stores how many seconds remain before the buffered press expires
    private float _dashBufferTimer;

    public bool CanDash => _dashesLeft > 0 && _cooldownTimer <= 0f && !_player.State.IsGrounded;

    #region Unity Lifecycle

    void Awake()
    {
        _player   = GetComponent<Player>();
        _wallJump = GetComponent<WallJump>(); // null if WallJump not present — handled gracefully
        _dashesLeft = _settings.MaxAirDashes;
    }

    void Update()
    {
        // ── Input buffer ──────────────────────────────────────────────────────
        // Pressing the key starts a countdown. The dash fires whenever CanDash
        // becomes true within that window — so a press just before leaving the
        // ground still registers.
        if (IsDashPressed())
            _dashBufferTimer = _settings.InputBuffer;
        else if (_dashBufferTimer > 0f)
            _dashBufferTimer -= Time.deltaTime;

        // ── Cooldown ──────────────────────────────────────────────────────────
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        // ── Recharge on landing ───────────────────────────────────────────────
        bool isGrounded = _player.State.IsGrounded;
        if (isGrounded && !_wasGrounded)
            _dashesLeft = _settings.MaxAirDashes;
        _wasGrounded = isGrounded;

        // ── Recharge on first wall contact ────────────────────────────────────
        if (_wallJump != null)
        {
            bool isWallRunning = _wallJump.IsWallRunning;
            if (isWallRunning && !_wasWallRunning)
                _dashesLeft = _settings.MaxAirDashes;
            _wasWallRunning = isWallRunning;
        }
    }

    void LateUpdate()
    {
        if (_dashBufferTimer <= 0f) return;
        if (!CanDash)               return;

        _dashBufferTimer = 0f; // consume the buffered press
        PerformDash();
    }

    #endregion

    #region Dash Logic

    private bool IsDashPressed()
    {
        if (Keyboard.current != null && (Keyboard.current[_settings.KeyboardKey]?.wasPressedThisFrame ?? false))
            return true;

        if (_settings.UseGamepadWest && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void PerformDash()
    {
        Vector3 dashDir = GetDashDirection();

        // Replace horizontal ExternalVelocity with the dash impulse.
        // Vertical component is preserved so a wall-jump upward force is not cancelled.
        _player.ExternalVelocity = new Vector3(
            dashDir.x * _settings.DashForce,
            _player.ExternalVelocity.y,
            dashDir.z * _settings.DashForce
        );

        _dashesLeft--;
        _cooldownTimer = _settings.Cooldown;
    }

    private Vector3 GetDashDirection()
    {
        // Prefer the player's current movement direction (Player.cs rotates the character to face it)
        Vector3 horizontal = _player.State.HorizontalVelocity;
        if (horizontal.sqrMagnitude > 0.01f)
            return horizontal.normalized;

        // Fall back to camera forward if the player is stationary
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0f;
        return camForward.sqrMagnitude > 0.001f ? camForward.normalized : transform.forward;
    }

    #endregion
}
