using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Déclenche un UnityEvent quand le joueur entre dans le trigger.
/// </summary>
public class PlayerEvent : MonoBehaviour
{
    [Tooltip("Événement déclenché à l'entrée du joueur dans le trigger")]
    [SerializeField] private UnityEvent _onPlayer;

    void OnTriggerEnter(Collider col)
    {
        if (Player.Instance && Player.Instance.gameObject == col.gameObject)
            _onPlayer?.Invoke();
    }
}
