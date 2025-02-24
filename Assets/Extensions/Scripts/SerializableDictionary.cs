using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField] private List<KeyValuePairExtension<TKey, TValue>> items = new();

    private Dictionary<TKey, TValue> dictionary = new();

    public TValue this[TKey key] => dictionary[key];

    public void Initialize()
    {
        dictionary.Clear();
        foreach (var item in items)
        {
            if (!dictionary.ContainsKey(item.Key))
            {
                dictionary[item.Key] = item.Value;
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            items.Add(new KeyValuePairExtension<TKey, TValue>(key, value));
            dictionary[key] = value;
        }
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        return dictionary;
    }
}

[Serializable]
struct KeyValuePairExtension<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public KeyValuePairExtension(TKey key, TValue value)
    {
        this.Key = key;
        this.Value = value;
    }
}
