using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float _distance = 4f;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _pauseAtEnd = 0f;

    [Header("Offset & Chain")]

    [Tooltip("Délai avant que cette plateforme commence à bouger (s)")]
    [SerializeField] private float _startOffset = 0f;

    [Tooltip("Plateforme qui doit avoir commencé à bouger avant celle-ci (optionnel)")]
    [SerializeField] private MovingPlatform _waitForPlatform = null;

    [Tooltip("Délai après que _waitForPlatform ait commencé, avant que celle-ci démarre (s)")]
    [SerializeField] private float _chainDelay = 0f;

    private Vector3 _startPosition;
    private Vector3 _topPosition;
    private float _timer;
    private float _pauseTimer;
    private bool _goingUp = true;

    // Gestion du démarrage
    private float _offsetTimer;
    private bool _started;
    private bool _chainTriggered;

    private void Start()
    {
        _startPosition = transform.position;
        _topPosition = _startPosition + Vector3.up * _distance;

        _offsetTimer = _startOffset;

        // Si pas de dépendance, on démarre le compte à rebours immédiatement
        if (_waitForPlatform == null)
            _started = false; // sera géré par _offsetTimer
        else
            _started = false; // attend que _waitForPlatform ait démarré
    }

    private void Update()
    {
        // ── Attente de la plateforme parente ─────────────────────────────────
        if (_waitForPlatform != null && !_chainTriggered)
        {
            if (!_waitForPlatform.HasStartedMoving)
                return;

            // Elle a démarré : on lance notre propre compte à rebours
            _chainTriggered = true;
            _offsetTimer = _chainDelay;
        }

        // ── Compte à rebours avant démarrage ─────────────────────────────────
        if (!_started)
        {
            // Si on attend une plateforme parente et qu'elle n'a pas encore démarré le chain, on bloque
            if (_waitForPlatform != null && !_chainTriggered)
                return;

            _offsetTimer -= Time.deltaTime;
            if (_offsetTimer > 0f)
                return;

            _started = true;
        }

        // ── Logique de mouvement (identique à l'original) ────────────────────
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.deltaTime;
            return;
        }

        _timer += Time.deltaTime * _speed;
        float t = Mathf.Clamp01(_timer);

        Vector3 from = _goingUp ? _startPosition : _topPosition;
        Vector3 to = _goingUp ? _topPosition : _startPosition;

        transform.position = Vector3.Lerp(from, to, t);

        if (t >= 1f)
        {
            _timer = 0f;
            _goingUp = !_goingUp;
            _pauseTimer = _pauseAtEnd;
        }
    }

    /// <summary>True dès que cette plateforme a commencé à se déplacer.</summary>
    public bool HasStartedMoving => _started;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + Vector3.up * _distance);
        Gizmos.DrawWireCube(origin + Vector3.up * _distance, transform.localScale);
    }
#endif
}