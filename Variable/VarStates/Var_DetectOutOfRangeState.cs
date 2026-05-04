using Godot;
using System;
using StarlightBT.Data;
using Cosmosity.Pathfinders;
using StarlightStateTree;
public partial class Var_DetectOutOfRangeState : STNode
{
    public override string Name => "DetectOutOfRange";

    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
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

    private Pathfinder Pathfinder
    {
        get => _blackboard.Get<Pathfinder>("Pathfinder");
    }

    private bool IsWalking
    {
        get => _blackboard.Get<bool>("IsWalking");
        set => _blackboard.Set("IsWalking", value);
    }
    protected override void OnEnter()
    {
        IsWalking = false;
    }
    protected override void OnPhysicsUpdate(double delta)
    {
        if (IsCurrentTargetInRange()) return;

        Var chaseTarget = CurrentAttackTarget;
        if (chaseTarget?.Stats == null || Pathfinder == null)
        {
            CurrentAttackTarget = null;
            RequestTransition("Idle");
            return;
        }

        // Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        // Vector2I targetCell = Grid.WorldToGrid(chaseTarget.Stats.Position);
        // var chasePath = Pathfinder.Run(selfCell, targetCell);
        // if (chasePath == null || chasePath.Count == 0)
        // {
        //     CurrentAttackTarget = null;
        //     RequestTransition("Idle");
        //     return;
        // }

        // Self.SetPath(chasePath);

        // Delegate pathfinding task to the DetectInDetectRangeState
        // Set IsWalkng = true in advance to avoid 1-frame delay in movement if transit to Idle
        IsWalking = true;
        RequestTransition("Move");
        return;
    }

    private bool IsCurrentTargetInRange()
    {
        if (Stats.AttackRange == null || CurrentAttackTarget?.Stats == null)
        {
            return false;
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(CurrentAttackTarget.Stats.Position);

        foreach (Vector2I attackCell in Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction))
        {
            if (attackCell != targetCell)
            {
                continue;
            }

            Stats.AttackDirection = (CurrentAttackTarget.Stats.Position - Stats.Position).Normalized();
            return true;
        }

        return false;
    }
}
