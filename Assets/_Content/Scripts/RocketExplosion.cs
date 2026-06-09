using UnityEngine;

public class RocketExplosion : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public SphereCollider  Collider;
        public ParticleSystem  Explosion;
    }

    [System.Serializable]
    public class Settings
    {
        [Tooltip("Rayon maximum atteint par la sphère d'explosion (m)")]
        public float Radius = 5f;

        [Tooltip("Vitesse d'expansion du rayon (m/s)")]
        public float ExpansionSpeed = 15f;

        [Tooltip("Durée de vie du GameObject après l'explosion (s)")]
        public float Lifetime = 5f;
    }

    [SerializeField] private References _references;
    [SerializeField] private Settings   _settings;

    private float _radius;
    private float _age;

    void Awake()
    {
        _radius = 1f;
        _references.Explosion.Play();
    }

    void Update()
    {
        _radius += Time.deltaTime * _settings.ExpansionSpeed;

        // Le collider disparaît une fois le rayon max atteint, mais les particules continuent
        _references.Collider.radius = _radius <= _settings.Radius ? _radius : 0f;

        _age += Time.deltaTime;
        if (_age > _settings.Lifetime)
            Destroy(gameObject);
    }
}
