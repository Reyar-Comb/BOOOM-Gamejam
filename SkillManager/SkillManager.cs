using Godot;
using StarlightBT.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

public partial class SkillManager : Node
{
    [Export] private StatList _statList = null;
    private Blackboard _statValues = new();
    private Blackboard _stats = new();
    public override void _Ready()
    {
        ResetStats();
    }
    public void ResetStats()
    {
        foreach (var stat in _statList.AvailableStats)
        {
            _statValues.Set(stat.StatName, stat.Value);
            _stats.Set(stat.StatName, stat);
        }
    }
    public T GetStat<T>(string statName)
        where T : INumber<T>
    {
        if (_statValues.TryGet<T>(statName, out var value))
        {
            return value;
        }
        GD.PushError($"Stat '{statName}' not found.");
        return default;
    }
    public T ApplyStatOperation<T>(string statName, float baseValue)
        where T : INumber<T>
    {
        return _stats.Get<StatResource>(statName).Apply<T>(baseValue);
    }
    public IEnumerable<StatResource> GetAllStats()
    {
        for (int i = 0; i < _statList.AvailableStats.Length; i++)
        {
            var stat = _statList.AvailableStats[i];
            yield return stat;
        }
    }
}
