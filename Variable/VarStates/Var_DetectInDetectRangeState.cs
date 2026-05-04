using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using Cosmosity.Pathfinders;
public partial class Var_DetectInDetectRangeState : STNode
{
    public override string Name => "DetectInDetectRange";

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

    private Pathfinder Pathfinder
    {
        get => _blackboard.Get<Pathfinder>("Pathfinder");
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
        if (Stats.DetectRange == null || Vars == null)
        {
            return;
        }

        var enemiesByCell = _blackboard.Get<Dictionary<Vector2I, Var>>("EnemiesByCell");

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        foreach (Vector2I targetCell in Stats.DetectRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (enemiesByCell.TryGetValue(targetCell, out Var enemy2))
            {
                Self.SetPath(Pathfinder.Run(selfCell, targetCell));
                CurrentAttackTarget = enemy2;
                return;
            }
        }
        CurrentAttackTarget = null;
    }
}
