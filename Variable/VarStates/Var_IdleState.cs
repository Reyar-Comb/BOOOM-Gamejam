using Godot;
using System;
using StarlightBT.Data;
using System.Collections.Generic;
using StarlightStateTree;
using Cosmosity.Pathfinders;
public partial class Var_IdleState : STNode
{
    public override string Name => "Idle";

    private bool HasPendingMove
    {
        get => _blackboard.Get<bool>("HasPendingMove");
        set => _blackboard.Set("HasPendingMove", value);
    }

    private bool IsWalking
    {
        get => _blackboard.Get<bool>("IsWalking");
        set => _blackboard.Set("IsWalking", value);
    }

    private List<Vector2I> CurrentPath
    {
        get => _blackboard.Get<List<Vector2I>>("CurrentPath");
    }

    private Var Self => _blackboard.Get<Var>("Self");

    private Pathfinder Pathfinder => _blackboard.Get<Pathfinder>("Pathfinder");

    private MapData MapData => _blackboard.Get<MapData>("MapData");

    private Var CurrentAttackTarget => _blackboard.Get<Var>("CurrentAttackTarget");

    private float EnemyRandomMoveInterval => _blackboard.Get<float>("EnemyRandomMoveInterval");

    private bool IsBugAttacked
    {
        get => _blackboard.Get<bool>("IsBugAttacked");
        set => _blackboard.Set("IsBugAttacked", value);
    }
    private float EnemyRandomMoveTimeRemaining
    {
        get => _blackboard.Get<float>("EnemyRandomMoveTimeRemaining");
        set => _blackboard.Set("EnemyRandomMoveTimeRemaining", value);
    }

    private bool _stopMovingToBug = false;
    protected override void OnPhysicsUpdate(double delta)
    {
        Vector2I selfCell = Grid.WorldToGrid(Self.Stats.Position);
        Vector2I bugCell = _blackboard.Get<Vector2I>("BugCell");
        float dist = 0f;
        if (!_stopMovingToBug) dist = Self.Stats.Position.DistanceTo(Grid.GridToWorld(bugCell));

        if (IsBugAttacked &&
        (dist < Grid.CellSize || Mathf.IsEqualApprox(dist, Grid.CellSize)) && !_stopMovingToBug)
        {
            _stopMovingToBug = true;
        }

        if (IsBugAttacked && (CurrentPath == null || CurrentPath.Count == 0) && !_stopMovingToBug)
        {
            Debug.Print("Move to bug");
            int region = MapData.GetRegion(selfCell.X, selfCell.Y);
            List<Vector2I> path = Pathfinder.Run(selfCell, bugCell, region);
            if (path.Count != 0) path.RemoveAt(path.Count - 1);
            Self.SetPath(path);
            return;
        }

        if (TryStartEnemyRandomMove(selfCell, delta))
        {
            return;
        }

        if (!HasPendingMove)
        {
            return;
        }

        HasPendingMove = false;
        IsWalking = true;
        RequestTransition("Move");
    }

    private bool TryStartEnemyRandomMove(Vector2I selfCell, double delta)
    {
        if (!ShouldRandomMove())
        {
            ResetEnemyRandomMoveTimer();
            return false;
        }

        EnemyRandomMoveTimeRemaining -= (float)delta;
        if (EnemyRandomMoveTimeRemaining > 0.0f)
        {
            return false;
        }

        ResetEnemyRandomMoveTimer();
        int region = MapData.GetRegion(selfCell.X, selfCell.Y);
        if (region <= 0)
        {
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            Vector2I targetCell = MapData.GetRandomPositionInRegion(region);
            if (targetCell == selfCell)
            {
                continue;
            }

            List<Vector2I> path = Pathfinder.Run(selfCell, targetCell, region);
            if (path == null || path.Count == 0)
            {
                continue;
            }

            Self.SetPath(path);
            return true;
        }

        return false;
    }

    private bool ShouldRandomMove()
    {
        return Self.Stats.VarTeam == VarStats.Team.Hostile
            && (!_blackboard.Get<bool>("IsBugAttacked") || _stopMovingToBug)
            && CurrentAttackTarget == null
            && !HasPendingMove
            // && (CurrentPath == null || CurrentPath.Count == 0)
            && Pathfinder != null
            && MapData != null;
    }

    private void ResetEnemyRandomMoveTimer()
    {
        EnemyRandomMoveTimeRemaining = EnemyRandomMoveInterval;
    }
}

