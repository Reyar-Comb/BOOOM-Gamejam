using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using Cosmosity.Pathfinders;
public partial class Var_DetectInAttackRange : STNode
{
    public override string Name => "DetectInAttackRange";

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

    private Var Self
    {
        get => _blackboard.Get<Var>("Self");
    }
    protected override void OnPhysicsUpdate(double delta)
    {
        TryGetEnemyInRange();
    }
    
    private void TryGetEnemyInRange()
    {
        if (Stats.AttackRange == null || Vars == null)
        {
            return;
        }

        Dictionary<Vector2I, Var> enemiesByCell = new();
        foreach (var var in Vars)
        {
            if (var == null || var.Stats == null) continue;
            if (var.Stats == Stats) continue;

            Vector2I enemyCell = Grid.WorldToGrid(var.Stats.Position);
            enemiesByCell.TryAdd(enemyCell, var);
        }

        _blackboard.Set("EnemiesByCell", enemiesByCell);

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        foreach (Vector2I targetCell in Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (enemiesByCell.TryGetValue(targetCell, out Var enemy))
            {
                CurrentAttackTarget = enemy;
                RequestTransition("Attack");
            }
        }
    }
}
