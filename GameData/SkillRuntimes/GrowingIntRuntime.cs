using Godot;
using System;
using System.Collections.Generic;

public class GrowingIntRuntime : ISkillRuntime
{
    private int _healthBonus;
    private int _attackBonus;
    private int _defenseBonus;

    public void OnVarCreated(VarCreationInfo info)
    {
        VarStats stats = info?.Var?.Stats;
        if (stats == null || stats.Type != VarStats.VarType.Int)
        {
            return;
        }

        ApplyCurrentBonuses(stats);
        AddRandomFutureBonus();
    }

    private void ApplyCurrentBonuses(VarStats stats)
    {
        if (_healthBonus > 0)
        {
            stats.MaxHealth += _healthBonus;
            stats.CurrentHealth = stats.MaxHealth;
        }

        stats.AttackDamage += _attackBonus;
        stats.Defense += _defenseBonus;
    }

    private void AddRandomFutureBonus()
    {
        switch (Random.Shared.Next(3))
        {
            case 0:
                _healthBonus++;
                break;
            case 1:
                _attackBonus++;
                break;
            case 2:
                _defenseBonus++;
                break;
        }
    }
}
