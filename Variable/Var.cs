using Godot;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;

public partial class Var : RefCounted
{
    public VarStats Stats { get; set; }
    protected STRoot _stateTree = null!;
    protected Blackboard _blackboard = null!;
    private bool _isInitialized = false;
    public void Initialize(Blackboard parentBlackboard)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _blackboard = new()
        {
            ParentBlackboard = parentBlackboard
        };
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
        var idleState = new Var_IdleState();
        var moveState = new Var_MoveState();
        var attackState = new Var_AttackState();
        var detectState = new Var_DetectState();

        _stateTree = new STRoot
        {
            InitialState = "Move",
            AllowRepeatedEnterAndExit = false
        };
        _stateTree.AddChild(detectState);
        detectState.AddChild(idleState);
        detectState.AddChild(moveState);
        
        _stateTree.AddChild(attackState);

        _blackboard.Set("Stats", Stats);
        _blackboard.Set("CurrentPath", new List<Vector2I>());
        _blackboard.Set("IsWalking", false);
        _blackboard.Set("CurrentPathIndex", 0);

        _stateTree.Initialize(_blackboard);
    }
}
