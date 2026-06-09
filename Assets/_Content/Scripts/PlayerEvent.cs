using UnityEngine;
using UnityEngine.Events;

public class PlayerEvent : MonoBehaviour
{
    [SerializeField] private UnityEvent _onPlayer;

    void OnTriggerEnter(Collider col)
    {
        if (Player.Instance && Player.Instance.gameObject == col.gameObject)
        {
            _onPlayer?.Invoke();
        }
    }
}
