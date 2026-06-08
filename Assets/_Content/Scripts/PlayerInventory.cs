using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [SerializeField, ReadOnly] private List<string> _itemList = new List<string>();

    public UnityEvent<string> OnItemPickedUp;
    public UnityEvent<string> OnItemRemoved;

    private readonly HashSet<string> _items = new HashSet<string>();

    public IReadOnlyCollection<string> Items => _items;

    void Awake()
    {
        if (!Instance) Instance = this;
    }

    public void PickUp(string itemId)
    {
        if (!_items.Add(itemId)) return;
        _itemList.Add(itemId);
        OnItemPickedUp?.Invoke(itemId);
    }

    public bool Has(string itemId) => _items.Contains(itemId);

    public void Remove(string itemId)
    {
        if (!_items.Remove(itemId)) return;
        _itemList.Remove(itemId);
        OnItemRemoved?.Invoke(itemId);
    }

    public void Clear()
    {
        _items.Clear();
        _itemList.Clear();
    }
}
