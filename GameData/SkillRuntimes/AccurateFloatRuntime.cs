using Godot;
using System;
using System.Collections.Generic;

public class AccurateFloatRuntime : ISkillRuntime
{
    private const int SameAxisDamageBonus = 3;

    public void OnWaveStarted() {}
    public void OnVarCreated(VarCreationInfo info) {}

    public void OnBeforeAttack(AttackInfo info)
    {
        Var source = info?.Source;
        Var target = info?.Target;
        if (source?.Stats == null || target?.Stats == null)
        {
            return;
        }

        if (source.Stats.Type != VarStats.VarType.Float)
        {
            return;
        }

        Vector2I sourceCell = Grid.WorldToGrid(source.Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(target.Stats.Position);
        if (sourceCell.X == targetCell.X || sourceCell.Y == targetCell.Y)
        {
            info.Damage += SameAxisDamageBonus;
        }
    }

    public void OnDetected(DetectInfo info) {}
    public IEnumerable<Vector2I> OnAttackRangeQuery(AttackRangeQueryInfo info, IEnumerable<Vector2I> rangeCells)
    {
        return rangeCells;
    }
    public void OnTokenOperation() {}
}
