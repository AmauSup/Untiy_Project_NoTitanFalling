using UnityEngine;
using UnityEngine.WSA;

public class Rocket : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public Collider Collider;
        public Rigidbody Rigidbody;
        public ParticleSystem Fire;
        public GameObject Explosion;
    }

    [System.Serializable]
    public class Settings
    {
        public bool IsKinematic = false;
        public bool LaunchOnEnable = false;
        public float Speed = 20;
        public float Duration = 5;
        public float GostTime = 0;
    }
    
    [System.Serializable]
    public class State
    {
        public bool Launched = false;
        public bool Disabled = false;
        public float FlightTime = 0;
    }

    [SerializeField] private References _references;
    [SerializeField] private Settings _settings;
    [SerializeField, ReadOnly] private State _state;

    private Vector3 _lastPos;
    
    void OnEnable()
    {
        _references.Rigidbody.isKinematic = _settings.IsKinematic;

        if (_settings.GostTime > 0)
            _references.Collider.enabled = false;

        if (_settings.LaunchOnEnable)
            Launch();
    }

    void Update()
    {
        if (_state.Launched && _state.FlightTime < _settings.Duration)
        {
            Vector3 force = transform.up * _settings.Speed * Time.deltaTime * 100;

            _references.Collider.enabled = _state.FlightTime > _settings.GostTime;
            _references.Rigidbody.AddForce(force);

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
        if (_state.Launched && _state.FlightTime > _settings.Duration)
        {
            Vector3 currentPos = transform.position;
            Vector3 lookDir = currentPos - _lastPos;
            _lastPos = transform.position;

            if (lookDir.sqrMagnitude > .001f)
            {
                lookDir = lookDir.normalized;

                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                targetRot *= Quaternion.Euler(90, 0, 0);
                targetRot = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10);

                _references.Rigidbody.MoveRotation(targetRot);
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (_state.Launched && _state.FlightTime > .5f)
        {
            GameObject explosion = Instantiate(_references.Explosion);
            explosion.transform.position = transform.position;

            Destroy(gameObject);
        }
    }

    [ContextMenu("Launch")]
    public void Launch()
    {
        transform.parent = null;

        _references.Rigidbody.isKinematic = false;

        _state.Launched = true;
        _state.Disabled = false;
        _state.FlightTime = 0;

        _references.Fire.Play();

        _lastPos = transform.position;
    }
}
