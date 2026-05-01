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
        if (!IsWalking || CurrentPath == null || CurrentPathIndex >= CurrentPath.Count) return;

        Vector2 nextPos = Grid.GridToWorld(CurrentPath[CurrentPathIndex]);
        Stats.Direction = (nextPos - Stats.Position).Normalized();
        float stepLength = Stats.MoveSpeed * (float)delta;
        Stats.Position += Stats.Direction * stepLength;
        if (Stats.Position.DistanceSquaredTo(nextPos) > stepLength * stepLength) return;
        
        Stats.Position = nextPos;
        CurrentPathIndex++;
        if (CurrentPathIndex >= CurrentPath.Count)
        {
            IsWalking = false;
        }
        return;
    }
}
