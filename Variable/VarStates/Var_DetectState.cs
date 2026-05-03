using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
public partial class Var_DetectState : STNode
{
    public override string Name => "Detect";

    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }

    private IReadOnlyList<Var> Vars
    {
        get => _blackboard.Get<IReadOnlyList<Var>>("Vars");
    }

    private Var CurrentAttackTarget
    {
        get => _blackboard.Get<Var>("CurrentAttackTarget");
        set => _blackboard.Set("CurrentAttackTarget", value);
    }

    protected override void OnPhysicsUpdate(double delta)
    {
        if (TryGetEnemyInRange(out Var target))
        {
            CurrentAttackTarget = target;
            RequestTransition("Attack");
        }
    }

    private bool TryGetEnemyInRange(out Var target)
    {
        target = null;
        if (Stats.AttackRange == null || Vars == null)
        {
            return false;
        }

        Dictionary<Vector2I, Var> enemiesByCell = new();
        foreach (var var in Vars)
        {
            if (var == null || var.Stats == null) continue;
            if (var.Stats == Stats) continue;

            Vector2I enemyCell = Grid.WorldToGrid(var.Stats.Position);
            enemiesByCell.TryAdd(enemyCell, var);
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        foreach (Vector2I targetCell in Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (enemiesByCell.TryGetValue(targetCell, out Var enemy))
            {
                Stats.AttackDirection = (enemy.Stats.Position - Stats.Position).Normalized();
                target = enemy;
                return true;
            }
        }

        return false;
    }
}
