using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private float _distance = 4f;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _pauseAtEnd = 0f;

    private Vector3 _startPosition;
    private Vector3 _topPosition;
    private float _timer;
    private float _pauseTimer;
    private bool _goingUp = true;

    private void Start()
    {
        _startPosition = transform.position;
        _topPosition = _startPosition + Vector3.up * _distance;
    }

    private void Update()
    {
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.deltaTime;
            return;
        }

        _timer += Time.deltaTime * _speed;
        float t = Mathf.Clamp01(_timer);

        Vector3 from = _goingUp ? _startPosition : _topPosition;
        Vector3 to   = _goingUp ? _topPosition   : _startPosition;

        transform.position = Vector3.Lerp(from, to, t);

        if (t >= 1f)
        {
            _timer = 0f;
            _goingUp = !_goingUp;
            _pauseTimer = _pauseAtEnd;
        }
    }

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
