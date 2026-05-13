using Godot;
using System;
using Godot.Collections;

public class ColorData
{
	private const string DATA_PATH = "res://GameData/ColorResource/ColorData.tres";
	private readonly ColorDataResource _resource = null;
	private Dictionary<string, Color> _data = new Dictionary<string, Color>();
	public ColorData()
	{
		_resource = ResourceLoader.Load<ColorDataResource>(DATA_PATH);
		Reset();
	}
	public void Reset()
	{
		foreach (var kvp in _resource.Data)
		{
			_data[kvp.Key] = kvp.Value;
		}
	}
	public Color Get(string key)
	{
		if (_data.TryGetValue(key, out var value))
		{
			return value;
		}
		GD.PushError($"ColorData: Key '{key}' not found.");
		return Colors.White;
	}
}
