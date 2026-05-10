using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;

public partial class Var_MoveState : STNode
{
    public override string Name => "Move";
    private int _currentRegionId = -1;
    private Vector2I _currentCell;

    private bool IsWalking
    {
        get => _blackboard.Get<bool>("IsWalking");
        set => _blackboard.Set("IsWalking", value);
    }
    private List<Vector2I> CurrentPath
    {
        get => _blackboard.Get<List<Vector2I>>("CurrentPath");
    }
    private int CurrentPathIndex
    {
        get => _blackboard.Get<int>("CurrentPathIndex");
        set => _blackboard.Set("CurrentPathIndex", value);
    }
    private VarStats Stats
    {
        get => _blackboard.Get<VarStats>("Stats");
        set => _blackboard.Set("Stats", value);
    }

    private Var Self => _blackboard.Get<Var>("Self");
    private MapData MapData => _blackboard.Get<MapData>("MapData");
    private GameData GameData => _blackboard.Get<GameData>("GameData");

    protected override void OnEnter()
    {
        UpdateCurrentRegion();
    }

    protected override void OnPhysicsUpdate(double delta)
    {
        if (!IsWalking || CurrentPath == null)
        {
            FinishMovement();
            return;
        }

        if (!TryGetNextTargetPosition(out Vector2 nextPos))
        {
            FinishMovement();
            return;
        }

        Stats.Direction = (nextPos - Stats.Position).ToFacingDirection();
        float stepLength = Stats.MoveSpeed * (float)delta;
        Stats.Position += Stats.Direction * stepLength;
        if (Stats.Position.DistanceSquaredTo(nextPos) > stepLength * stepLength) return;

        Stats.Position = nextPos;
        NotifyRegionEntryIfNeeded(CurrentPath[CurrentPathIndex]);
        CurrentPathIndex++;
        if (CurrentPathIndex >= CurrentPath.Count)
        {
            FinishMovement();
        }
        return;
    }

    private void FinishMovement()
    {
        IsWalking = false;
        RequestTransition("Idle");
    }

    private void UpdateCurrentRegion()
    {
        if (Stats == null || MapData == null)
        {
            _currentRegionId = -1;
            _currentCell = default;
            return;
        }

        _currentCell = Grid.WorldToGrid(Stats.Position);
        _currentRegionId = MapData.GetRegion(_currentCell.X, _currentCell.Y);
    }

    private void NotifyRegionEntryIfNeeded(Vector2I reachedCell)
    {
        if (MapData == null || GameData == null || Self == null)
        {
            _currentCell = reachedCell;
            return;
        }

        int reachedRegion = MapData.GetRegion(reachedCell.X, reachedCell.Y);
        if (reachedRegion == _currentRegionId)
        {
            _currentCell = reachedCell;
            return;
        }

        RegionEnteredInfo info = new()
        {
            Var = Self,
            MapData = MapData,
            FromCell = _currentCell,
            ToCell = reachedCell,
            FromRegion = _currentRegionId,
            ToRegion = reachedRegion
        };

        _currentCell = reachedCell;
        _currentRegionId = reachedRegion;
        GameData.SkillManager.OnRegionEntered(info);
    }

    private bool TryGetNextTargetPosition(out Vector2 nextPos)
    {
        while (CurrentPathIndex < CurrentPath.Count)
        {
            Vector2 candidatePos = Grid.GridToWorld(CurrentPath[CurrentPathIndex]);
            if (Stats.Position.DistanceSquaredTo(candidatePos) > MathConstants.EpsilonSquared)
            {
                nextPos = candidatePos;
                return true;
            }

            Stats.Position = candidatePos;
            CurrentPathIndex++;
        }

        nextPos = Vector2.Zero;
        return false;
    }
}
