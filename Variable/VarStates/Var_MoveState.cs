using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;

public partial class Var_MoveState : STNode
{
    public override string Name => "Move";
    private int _currentRegionId = -1;
    private Vector2I _currentCell = Vector2I.Left;

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
        _blackboard.Set(
            "EnemyRandomMoveTimeRemaining",
            _blackboard.Get<float>("EnemyRandomMoveInterval"));
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
        Self.NotifyRegionEntryIfNeeded(CurrentPath[CurrentPathIndex]);
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
        CurrentPath?.Clear();
        CurrentPathIndex = 0;
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
