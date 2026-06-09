using UnityEngine;
using UnityEngine.InputSystem;
using static Player;

public class PlayerRockerLauncher : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public InputActionAsset InputActions;
        public GameObject RocketPrefab;
        public GameObject LauncherMesh;
        public Transform HatchJoint;
        public Transform RootJoint;
        public Transform YawJoint;
        public Transform PitchJoint;
        public Transform RocketOrigin;
    }

    [System.Serializable]
    public class Settings
    {
        public bool InfiniteAmo;
    }

    [System.Serializable]
    public class StateContainer
    {
        public int RocketCount;
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;
    [SerializeField, ReadOnly] private StateContainer _state;

    private InputAction _targetAction;
    private InputAction _launchAction;
    private Camera _camera;
    private Rocket _rocket;
    private Vector3 _rootPos;
    private float _targeting;
    

    void Awake()
    {
        //if (!Instance)
        //    Instance = this;

        _rootPos = _references.RootJoint.transform.localPosition;

        // Inputs
        _targetAction = _references.InputActions.FindActionMap("Player").FindAction("Target");
        _launchAction = _references.InputActions.FindActionMap("Player").FindAction("Attack");

        // Camera
        _camera = Camera.main;
    }

    void OnEnable()
    {
        _targetAction?.Enable();
        _launchAction?.Enable();

        SetLauncher();
    }

    void OnDisable()
    {
        _targetAction?.Disable();
        _launchAction?.Disable();
    }

    void Update()
    {
        float t = Time.deltaTime;

        HandleTargeting(t);
        SetLauncher();
        HandleLaunch();
        ManageRockets();
    }

    private void HandleTargeting(float deltaTime)
    {
        bool executeTarget = _targetAction.IsPressed() && _rocket;

        _targeting = Mathf.Clamp01(_targeting + deltaTime * 2 * (executeTarget ? 1 : -1));
    }

    private void SetLauncher()
    {
        _references.LauncherMesh.SetActive(_targeting > 0);
        _references.RocketOrigin.gameObject.SetActive(_targeting > 0);
        _references.HatchJoint.localRotation = Quaternion.Slerp(Quaternion.Euler(0, -45, 0), Quaternion.Euler(-75, -45, 0), _targeting);
        _references.RootJoint.localPosition = _rootPos + Vector3.up * Mathf.Lerp(-.8f, 0, _targeting);
        
        Quaternion yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up));
        yaw = Quaternion.Slerp(_references.RootJoint.rotation, yaw, _targeting);
        
        Quaternion pitch = Quaternion.Slerp(Quaternion.Euler(0, 0, 0), Quaternion.Euler(_camera.transform.eulerAngles.x + 45, 0, 0), Mathf.InverseLerp(.6f, 1, _targeting));

        _references.YawJoint.rotation = yaw;
        _references.PitchJoint.localRotation = pitch;
    }

    private void HandleLaunch()
    {
        if (_rocket && _targeting == 1 && _launchAction.triggered)
        {
            _rocket.Launch();
            _rocket = null;
        }
    }

    private void ManageRockets()
    {
        if ((_state.RocketCount > 0 || _settings.InfiniteAmo) && !_rocket && _targeting == 0)
        {
            GameObject obj = Instantiate(_references.RocketPrefab, _references.RocketOrigin);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
            obj.transform.localScale = Vector3.one;

            _rocket = obj.GetComponent<Rocket>();
        }
    }
}
