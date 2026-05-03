using Cosmosity.Pathfinders;
using Godot;
using StarlightBT.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public partial class VarManager : Node
{
    private readonly List<Var> _vars = new();
    private ReadOnlyCollection<Var> ReadOnlyVars => field ??= _vars.AsReadOnly();
    private Blackboard _sharedBlackboard = new();

    // public override void _PhysicsProcess(double delta)
    // {
    //     foreach (var var in _vars)
    //     {
    //         var.PhysicsUpdate(delta);
    //     }
    // }

    public void Tick(double delta)
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

        _sharedBlackboard.Set("Vars", ReadOnlyVars);

        AStarPathfinder pathfinder = AStarPathfinder.CreateBuilder()
            .SetRect(-200, -200, 400, 400)
            .UseDiagonal(Pathfinder.DiagonalType.Never)
            .UseHeuristic(Pathfinder.HeuristicType.Manhattan)
            .Build();
        _sharedBlackboard.Set("Pathfinder", pathfinder);
        
        var.Initialize(_sharedBlackboard);
    }
}
