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
    protected override void OnPhysicsUpdate(double delta)
    {
        if (IsEnemyInRange())
        {
            RequestTransition("Attack");
        }
    }
    private bool IsEnemyInRange()
    {
        if (Stats.AttackRange == null || Vars == null)
        {
            return false;
        }

        HashSet<Vector2I> enemyCells = new();
        foreach (var var in Vars)
        {
            if (var == null || var.Stats == null) continue;
            if (var.Stats == Stats) continue;

            enemyCells.Add(Grid.WorldToGrid(var.Stats.Position));
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        foreach (Vector2I targetCell in Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (enemyCells.Contains(targetCell))
            {
                return true;
            }
        }

        return false;
    }
}
