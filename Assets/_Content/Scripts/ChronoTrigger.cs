using UnityEngine;

public class ChronoTrigger : MonoBehaviour
{
    public enum Mode { Start, Stop }

    [SerializeField] private Mode _mode = Mode.Start;

    [Tooltip("Item IDs the player must carry to stop the chrono. Only used in Stop mode. Leave empty to stop without condition.")]
    [SerializeField] private string[] _requiredItems;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;

        if (_mode == Mode.Start)
        {
            if (!ChronoManager.Instance.IsRunning)
                ChronoManager.Instance.StartChrono();
            return;
        }

        // Stop mode: chrono must be running
        if (!ChronoManager.Instance.IsRunning) return;

        // Check required items
        if (_requiredItems != null && _requiredItems.Length > 0)
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv == null) return;
            foreach (string id in _requiredItems)
                if (!inv.Has(id)) return;
        }

        ChronoManager.Instance.StopChrono();
    }
}
