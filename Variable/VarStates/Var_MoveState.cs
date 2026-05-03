using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;

public partial class Var_MoveState : STNode
{
    public override string Name => "Move";
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
    protected override void OnEnter()
    {
        GD.Print("Entered Move State");
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
        Stats.Position += Stats.Direction.ToVector2() * stepLength;
        if (Stats.Position.DistanceSquaredTo(nextPos) > stepLength * stepLength) return;

        Stats.Position = nextPos;
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
