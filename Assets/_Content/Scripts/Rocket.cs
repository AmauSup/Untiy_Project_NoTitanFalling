using UnityEngine;

public class Rocket : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public Collider      Collider;
        public Rigidbody     Rigidbody;
        public ParticleSystem Fire;
        public GameObject    Explosion;
    }

    [System.Serializable]
    public class Settings
    {
        [Tooltip("Si true, le Rigidbody est kinematic (utilisé pour les roquettes portées avant le tir)")]
        public bool IsKinematic = false;

        [Tooltip("Lance la roquette automatiquement à l'activation du GameObject")]
        public bool LaunchOnEnable = false;

        [Tooltip("Vitesse de propulsion (force appliquée chaque frame, m/s)")]
        public float Speed = 20;

        [Tooltip("Durée de propulsion active avant que la roquette ne devienne balistique (s)")]
        public float Duration = 5;

        [Tooltip("Délai après le tir avant d'activer le collider — évite la collision avec le lanceur (s)")]
        public float GhostTime = 0;
    }

    [System.Serializable]
    public class State
    {
        public bool  Launched    = false;
        public bool  Disabled    = false;
        public float FlightTime  = 0;
    }

    [SerializeField] private References _references;
    [SerializeField] private Settings   _settings;
    [SerializeField, ReadOnly] private State _state;

    private Vector3 _lastPos;

    void OnEnable()
    {
        _references.Rigidbody.isKinematic = _settings.IsKinematic;

        if (_settings.GhostTime > 0)
            _references.Collider.enabled = false;

        if (_settings.LaunchOnEnable)
            Launch();
    }

    void Update()
    {
        if (_state.Launched && _state.FlightTime < _settings.Duration)
        {
            _references.Collider.enabled = _state.FlightTime > _settings.GhostTime;
            _references.Rigidbody.AddForce(transform.up * _settings.Speed * Time.deltaTime * 100);
            _state.FlightTime += Time.deltaTime;
        }
        else if (!_state.Disabled)
        {
            _state.Disabled = true;
            _references.Fire.Stop();
        }
    }

    void LateUpdate()
    {
        // Une fois la propulsion terminée, on oriente la roquette dans le sens de son déplacement
        if (!_state.Launched || _state.FlightTime <= _settings.Duration) return;

        Vector3 lookDir = transform.position - _lastPos;
        _lastPos = transform.position;

        if (lookDir.sqrMagnitude > .001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            targetRot *= Quaternion.Euler(90, 0, 0);
            _references.Rigidbody.MoveRotation(
                Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10));
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Ignore les collisions dans la première demi-seconde (GhostTime peut être 0)
        if (!_state.Launched || _state.FlightTime <= .5f) return;

        GameObject explosion = Instantiate(_references.Explosion);
        explosion.transform.position = transform.position;
        Destroy(gameObject);
    }

    [ContextMenu("Launch")]
    public void Launch()
    {
        transform.parent = null;
        _references.Rigidbody.isKinematic = false;

        _state.Launched   = true;
        _state.Disabled   = false;
        _state.FlightTime = 0;

        _references.Fire.Play();
        _lastPos = transform.position;
    }
}
