using Cosmosity.Pathfinders;
using Godot;
using StarlightBT.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
public partial class VarManager : Node
{
    [Export] private SkillManager _skillManager = null;
    public static class VarListPool
    {
        private static readonly ConcurrentBag<List<Var>> _pool = new ConcurrentBag<List<Var>>();

        public static List<Var> Get()
        {
            if (_pool.TryTake(out var list))
            {
                return list;
            }
            return new List<Var>();
        }

        public static void Return(List<Var> list)
        {
            list.Clear();
            _pool.Add(list);
        }
    }
    private readonly List<Var> _vars = new();
    private readonly Dictionary<Var, Callable> _onDeathCallablesByVar = new();
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
        List<Var> varsToRemove = VarListPool.Get();
        foreach (var var in _vars)
        {
            if (var.IsDead)
            {
                varsToRemove.Add(var);
                continue;
            }
            var.PhysicsUpdate(delta);
        }
        foreach (var var in varsToRemove)
        {
            DisconnectOnDeath(var);
            var.Cleanup();
            _vars.Remove(var);
        }
        VarListPool.Return(varsToRemove);
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
        ConnectOnDeath(var);
        _sharedBlackboard.Set("Vars", ReadOnlyVars);

        AStarPathfinder pathfinder = AStarPathfinder.CreateBuilder()
            .SetRect(-200, -200, 400, 400)
            .UseDiagonal(Pathfinder.DiagonalType.Never)
            .UseHeuristic(Pathfinder.HeuristicType.Manhattan)
            .Build();
        _sharedBlackboard.Set("Pathfinder", pathfinder);

        var.Initialize(_sharedBlackboard);
        var.InitStats(_skillManager);
    }

    private void ConnectOnDeath(Var var)
    {
        Callable onDeathCallable = Callable.From(() => OnVarDeath(var));
        _onDeathCallablesByVar[var] = onDeathCallable;
        var.Stats.Connect(VarStats.SignalName.OnDeath, onDeathCallable);
    }

    private void DisconnectOnDeath(Var var)
    {
        if (!_onDeathCallablesByVar.TryGetValue(var, out Callable onDeathCallable))
        {
            return;
        }

        if (var.Stats != null && var.Stats.IsConnected(VarStats.SignalName.OnDeath, onDeathCallable))
        {
            var.Stats.Disconnect(VarStats.SignalName.OnDeath, onDeathCallable);
        }

        _onDeathCallablesByVar.Remove(var);
    }

    private void OnVarDeath(Var var)
    {
        var.IsDead = true;
    }
}
