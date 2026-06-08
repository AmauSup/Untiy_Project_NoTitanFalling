using System.Collections;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Tooltip("Where the player is sent after the death animation")]
    [SerializeField] private Transform _respawnPoint;

    [Tooltip("Delay in seconds before respawn (should match death animation length)")]
    [SerializeField] private float _respawnDelay = 1.5f;

    void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>()
                     ?? other.GetComponentInParent<Player>();
        if (player == null) return;
        if (player.State.CurrentState == Player.PlayerState.Eliminated) return;

        StartCoroutine(HandleDeath(player));
    }

    private IEnumerator HandleDeath(Player player)
    {
        // Freeze movement and trigger death animation
        player.State.Velocity = Vector3.zero;
        player.ExternalVelocity = Vector3.zero;
        player.State.CurrentState = Player.PlayerState.Eliminated;

        yield return new WaitForSeconds(_respawnDelay);

        player.Respawn(_respawnPoint.position, _respawnPoint.rotation);
    }
}
