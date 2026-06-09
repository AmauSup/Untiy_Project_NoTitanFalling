using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class PlayerCamera : MonoBehaviour
{
    [System.Serializable]
    private class Settings
    {
        [Header("Common")]

        [Tooltip("Camera rotation sensitivity")]
        public float LookSensitivity = 20f;

        [Header("Third Person")]

        [Tooltip("Camera follow smoothness (0 = instant, 1 = smooth)")]
        [Range(0, 1)]
        public float FollowSmoothness = 0.1f;

        [Tooltip("Camera distance from player")]
        public float Distance = 5f;

        [Tooltip("Vertical offset from player")]
        public float VerticalOffset = 2f;

        [Tooltip("Default pitch angle (TPS)")]
        public float TPSDefaultPitch = 20f;

        [Tooltip("Min pitch angle (TPS)")]
        public float TPSMinPitch = -30f;

        [Tooltip("Max pitch angle (TPS)")]
        public float TPSMaxPitch = 60f;

        [Header("First Person")]

        [Tooltip("Vertical offset from Target pivot to the eye / head position")]
        public float HeadHeight = 1.6f;

        [Tooltip("Default pitch angle (FPS)")]
        public float FPSDefaultPitch = 0f;

        [Tooltip("Min pitch angle (FPS — looking down)")]
        public float FPSMinPitch = -80f;

        [Tooltip("Max pitch angle (FPS — looking up)")]
        public float FPSMaxPitch = 80f;
    }

    [System.Serializable]
    public class References
    {
        public InputActionAsset InputActions;
        public Transform Target;
    }

    [Tooltip("Switch between first-person and third-person view")]
    public bool IsFirstPerson = false;

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;

    private float _yaw;
    private float _pitch;
    private Vector3 _playerPosition;
    private InputAction _lookAction;
    private bool _wasFirstPerson;

    // =========================================================================
    #region Unity Lifecycle

    void Awake()
    {
        IsFirstPerson = PlayerPrefs.GetInt("FirstPerson", 0) == 1;
        
        Debug.Log("FirstPerson = " + IsFirstPerson);

        _lookAction = _references.InputActions.FindActionMap("Player").FindAction("Look");
    }

    void OnEnable()
    {
        _lookAction?.Enable();
        InitForMode(IsFirstPerson);
    }

    void OnDisable()
    {
        _lookAction?.Disable();
    }

    void LateUpdate()
    {
        float t = Time.deltaTime;

        // Re-initialise when mode changes at runtime
        if (IsFirstPerson != _wasFirstPerson)
        {
            InitForMode(IsFirstPerson);
            _wasFirstPerson = IsFirstPerson;
        }

        SetCursor();
        SetYawAndPitch(t);
        SetPosition(t);
    }

    #endregion

    // =========================================================================
    #region Camera Logic

    private void InitForMode(bool firstPerson)
    {
        _playerPosition = _references.Target ? _references.Target.position : Vector3.zero;
        _pitch          = firstPerson ? _settings.FPSDefaultPitch : _settings.TPSDefaultPitch;

        if (firstPerson)
            _yaw = _references.Target ? _references.Target.eulerAngles.y : 0f;
    }

    private void SetCursor()
    {
        if (!Application.isPlaying)
            return;

        bool lockCursor = !Player.Instance.State.IsPaused;

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !lockCursor;
    }

    private void SetYawAndPitch(float deltaTime)
    {
        if (Player.Instance && Player.Instance.State.IsPaused)
            return;

#if UNITY_EDITOR
        if ((Mouse.current.position.ReadValue() - new Vector2(Screen.width, Screen.height) * .5f).magnitude > 10)
            return;
#else
        if (!Application.isFocused) return;
#endif

        Vector2 lookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        float minPitch = IsFirstPerson ? _settings.FPSMinPitch : _settings.TPSMinPitch;
        float maxPitch = IsFirstPerson ? _settings.FPSMaxPitch : _settings.TPSMaxPitch;

        _pitch -= lookInput.y * _settings.LookSensitivity * deltaTime;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _yaw += lookInput.x * _settings.LookSensitivity * deltaTime;
    }

    private void SetPosition(float deltaTime)
    {
        if (IsFirstPerson)
            SetPositionFPS();
        else
            SetPositionTPS(deltaTime);
    }

    private void SetPositionFPS()
    {
        transform.SetPositionAndRotation(
            _references.Target.position + Vector3.up * _settings.HeadHeight,
            Quaternion.Euler(_pitch, _yaw, 0f));
    }

    private void SetPositionTPS(float deltaTime)
    {
        float t = (1.1f - _settings.FollowSmoothness) * 20f * deltaTime;
        _playerPosition = Vector3.Lerp(_playerPosition, _references.Target.position, t);

        Vector3 camPos = Vector3.back * _settings.Distance;
        camPos = Quaternion.Euler(_pitch, _yaw, 0f) * camPos;
        camPos += _playerPosition;

        Quaternion camRot = Quaternion.LookRotation(_playerPosition - camPos);

        camPos.y += _settings.VerticalOffset;

        transform.position = camPos;
        transform.rotation = camRot;
    }

    #endregion
}

