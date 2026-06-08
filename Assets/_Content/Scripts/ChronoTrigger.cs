using UnityEngine;

public class ChronoTrigger : MonoBehaviour
{
    [Tooltip("Item IDs the player must carry to stop the chrono. Leave empty to stop without condition.")]
    [SerializeField] private string[] _requiredItems;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;

        if (!ChronoManager.Instance.IsRunning)
        {
            ChronoManager.Instance.StartChrono();
            return;
        }

        // Chrono running: stop only if player has all required items
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
