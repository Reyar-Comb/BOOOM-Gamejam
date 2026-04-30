using Godot;
using StarlightBT.Data;
using System;
using System.Collections.Generic;

public partial class VarManager : Node
{
    private readonly List<Var> _vars = new();
    private Blackboard _sharedBlackboard = new();
    public override void _PhysicsProcess(double delta)
    {
        foreach (var var in _vars)
        {
            var.PhysicsUpdate(delta);
        }
    }
    public override void _Process(double delta)
    {
        foreach (var var in _vars)
        {
            var.FrameUpdate(delta);
        }
    }
    public void AddVar(Var var)
    {
        _vars.Add(var);
        var.Stats.OnDeath += () => _vars.Remove(var);
        _sharedBlackboard.Set("Vars", _vars.AsReadOnly<Var>());
        var.Initialize(_sharedBlackboard);
    }
}
