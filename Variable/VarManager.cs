using Godot;
using System;
using System.Collections.Generic;

public partial class VarManager : Node
{
    private List<Var> _vars = new();
    public override void _PhysicsProcess(double delta)
    {
        foreach (var var in _vars)
        {
            var.Step(delta);
        }
    }
    public void AddVar(Var var)
    {
        _vars.Add(var);
    }
}
