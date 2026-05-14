using Godot;
using System;
using System.Collections.Generic;
namespace StarlightBT.Data;

public class Blackboard : ICleanable
{
	public Blackboard ParentBlackboard { get; set; }
	private Dictionary<string, object> _data = new();
	public void Set<T>(string key, T value, int parentLevel = 0)
	{
		Blackboard current = this;
		for (int i = 0; i < parentLevel; i++)
		{
			current = current.ParentBlackboard;
		}
		if (current == null)
		{
			GD.PushError($"Cannot set key '{key}' at parent level {parentLevel} because it exceeds the hierarchy.");
			return;
		}
		current._data[key] = value;
	}
	public T Get<T>(string key)
	{
		if (TryGet<T>(key, out var value))
		{
			return value;
		}

		if (TryGetRaw(key, out var rawValue))
		{
			string actualType = rawValue?.GetType().FullName ?? "null";
			string expectedType = typeof(T).FullName ?? typeof(T).Name;
			GD.PushError($"Key '{key}' exists in blackboard but cannot be read as type '{expectedType}'. Actual value type: '{actualType}'.");
			return default;
		}

		GD.PushError($"Key '{key}' not found in blackboard or any parent blackboard.");
		return default;
	}

	public bool TryGet<T>(string key, out T value)
	{
		if (_data.TryGetValue(key, out var rawValue))
		{
			if (rawValue is T typedValue)
			{
				value = typedValue;
				return true;
			}

			bool canAcceptNull = rawValue == null
				&& (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null);
			if (canAcceptNull)
			{
				value = default;
				return true;
			}
		}

		if (ParentBlackboard != null)
		{
			return ParentBlackboard.TryGet(key, out value);
		}

		value = default;
		return false;
	}

	private bool TryGetRaw(string key, out object value)
	{
		if (_data.TryGetValue(key, out value))
		{
			return true;
		}

		if (ParentBlackboard != null)
		{
			return ParentBlackboard.TryGetRaw(key, out value);
		}

		value = null;
		return false;
	}

	public bool Contains(string key, bool includeParent = true)
	{
		if (_data.ContainsKey(key)) return true;
		if (!includeParent || ParentBlackboard == null) return false;
		return ParentBlackboard.Contains(key, true);
	}
	public void Remove(string key) => _data.Remove(key);
	public void Clear() => _data.Clear();
	public void Cleanup()
	{
		_data.Clear();
		ParentBlackboard = null;
	}
}
