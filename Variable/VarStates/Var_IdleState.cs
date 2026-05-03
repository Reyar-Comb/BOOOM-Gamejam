using Godot;
using System;
using StarlightBT.Data;
using System.Collections.Generic;
using StarlightStateTree;
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

    protected override void OnPhysicsUpdate(double delta)
    {
        if (!HasPendingMove || CurrentPath == null || CurrentPath.Count == 0)
        {
            return;
        }

        HasPendingMove = false;
        IsWalking = true;
        RequestTransition("Move");
    }
}
