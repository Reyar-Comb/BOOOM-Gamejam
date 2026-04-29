using Godot;
using StarlightBT.Data;
using StarlightStateTree;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

public partial class Var : RefCounted
{
    public VarStats Stats { get; set; }
    protected STRoot _stateTree = null!;
    protected Blackboard _blackboard = null!;
    public void Initialize()
    {
        SetupStateTree();
    }
    public void SetPath(List<Vector2I> path)
    {
        if (path == null || path.Count == 0) return;

        _blackboard.Set("CurrentPath", path);
        _blackboard.Set("CurrentPathIndex", 0);
        _blackboard.Set("IsWalking", true);
    }
    public void PhysicsUpdate(double delta)
    {
        _stateTree.PhysicsUpdate(delta);
    }
    public void FrameUpdate(double delta)
    {
        _stateTree.FrameUpdate(delta);
    }
    protected virtual void SetupStateTree()
    {
        _stateTree = new STRoot
        {
            InitialState = "Move",
            AllowRepeatedEnterAndExit = false
        };

        var moveState = new Var_MoveState();

        _stateTree.AddChild(moveState);

        _blackboard = new();

        _blackboard.Set("Stats", Stats);
        _blackboard.Set("CurrentPath", new List<Vector2I>());
        _blackboard.Set("IsWalking", false);
        _blackboard.Set("CurrentPathIndex", 0);

        _stateTree.Initialize(_blackboard);
    }
}
