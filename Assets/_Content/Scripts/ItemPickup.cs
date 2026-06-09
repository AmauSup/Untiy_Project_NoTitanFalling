using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Tooltip("Unique identifier for this item")]
    [SerializeField] private string _itemId;

    private static readonly List<ItemPickup> _registry = new List<ItemPickup>();

    void Awake()
    {
        _registry.Add(this);
    }

    void OnDestroy()
    {
        _registry.Remove(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (PlayerInventory.Instance == null) return;
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null) return;
        if (string.IsNullOrEmpty(_itemId)) return;

        PlayerInventory.Instance.PickUp(_itemId);
        SetPickedUp(true);
    }

    private void SetPickedUp(bool pickedUp)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = !pickedUp;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = !pickedUp;
    }

    public static void RespawnAll()
    {
        foreach (ItemPickup item in _registry)
            item.SetPickedUp(false);
    }
}
