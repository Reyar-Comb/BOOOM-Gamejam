using Godot;
using System;
using System.Collections.Generic;

public class NumericData
{
    private const string DATA_PATH = "res://GameData/Data.tres";
    private readonly NumericDataResource _resource = null;
    private Dictionary<string, int> _data = new Dictionary<string, int>();
    public NumericData()
    {
        _resource = ResourceLoader.Load<NumericDataResource>(DATA_PATH);
        Reset();
    }
    public void Reset()
    {
        foreach (var kvp in _resource.Data)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }
    public int Get(string key)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return value;
        }
        GD.PushError($"NumericData: Key '{key}' not found.");
        return 0;
    }
    public int Set(string key, int value)
    {
        _data[key] = value;
        return value;
    }
}
