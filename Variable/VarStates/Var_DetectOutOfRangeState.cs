using Godot;
using System;
using System.Collections.Generic;
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

    private MapData MapData => _blackboard.Get<MapData>("MapData");
    private GameData GameData => _blackboard.Get<GameData>("GameData");
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
        if (chaseTarget == null || chaseTarget.IsDead || Pathfinder == null)
        {
            CurrentAttackTarget = null;
            RequestTransition("Idle");
            return;
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(chaseTarget.Stats.Position);
        int region = MapData.GetRegion(selfCell.X, selfCell.Y);
        var chasePath = Pathfinder.Run(selfCell, targetCell, region);
        if (chasePath == null || chasePath.Count == 0)
        {
            CurrentAttackTarget = null;
            RequestTransition("Idle");
            return;
        }

        Self.SetPath(chasePath);
        IsWalking = true;
        Self.EmitSignal(Var.SignalName.OnOutOfDetect, chaseTarget);
        RequestTransition("Move");
        return;
    }

    private bool IsCurrentTargetInRange()
    {
        if (Stats.AttackRange == null || CurrentAttackTarget == null || CurrentAttackTarget.IsDead || GameData == null)
        {
            CurrentAttackTarget = null;
            return false;
        }

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(CurrentAttackTarget.Stats.Position);

        AttackRangeQueryInfo queryInfo = new()
        {
            Source = Self,
            OriginCell = selfCell,
            FacingDirection = Stats.Direction,
            AttackRange = Stats.AttackRange,
            DetectRange = Stats.DetectRange
        };

        IEnumerable<Vector2I> attackCells;
        if (Stats.VarTeam == VarStats.Team.Friendly)
        {
            attackCells = GameData.SkillManager.OnAttackRangeQuery(
                queryInfo,
                Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction));
        }
        else
        {
            attackCells = Stats.AttackRange.EnumerateTargetCells(selfCell, Stats.Direction);
        }
        foreach (Vector2I attackCell in attackCells)
        {
            if (MapData.GetRegion(attackCell.X, attackCell.Y) != MapData.GetRegion(selfCell.X, selfCell.Y))
            {
                continue;
            }
            if (attackCell != targetCell)
            {
                continue;
            }

            Stats.Direction = (CurrentAttackTarget.Stats.Position - Stats.Position).Normalized();
            return true;
        }

        return false;
    }
}
