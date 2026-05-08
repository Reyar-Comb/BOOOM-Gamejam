using Godot;
using System;
using System.Collections.Generic;

public class NumericData
{
    private const string DATA_PATH = "res://GameData/Data.tres";
    private readonly NumericDataResource _resource = null;
    private Dictionary<string, float> _data = new Dictionary<string, float>();
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
    public float Get(string key)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return value;
        }
        GD.PushError($"NumericData: Key '{key}' not found.");
        return 0f;
    }
    public float Set(string key, float value)
    {
        _data[key] = value;
        return value;
    }
}
