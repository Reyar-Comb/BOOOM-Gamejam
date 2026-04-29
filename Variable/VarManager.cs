using Godot;
using System;
using System.Collections.Generic;

public partial class VarManager : Node
{
    private readonly List<Var> _vars = new();
    public override void _PhysicsProcess(double delta)
    {
        foreach (var var in _vars)
        {
            var.Update(delta);
        }
    }
    public void AddVar(Var var)
    {
        _vars.Add(var);
        var.Stats.OnDeath += () => _vars.Remove(var);
    }
}
