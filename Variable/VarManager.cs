using Cosmosity.Pathfinders;
using Godot;
using StarlightBT.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
public partial class VarManager : Node
{
    private MapData _mapData = null!;
    private GameData _gameData = null!;
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
    public readonly List<Var> Vars = new();
    private readonly Dictionary<Var, Callable> _onDeathCallablesByVar = new();
    private ReadOnlyCollection<Var> ReadOnlyVars => field ??= Vars.AsReadOnly();
    private Blackboard _sharedBlackboard = new();

    // public override void _PhysicsProcess(double delta)
    // {
    //     foreach (var var in _vars)
    //     {
    //         var.PhysicsUpdate(delta);
    //     }
    // }
    public void Initialize(MapData mapData, GameData gameData)
    {
        _mapData = mapData;
        _gameData = gameData;
        _sharedBlackboard.Set("MapData", _mapData);
        _sharedBlackboard.Set("Vars", ReadOnlyVars);
        _sharedBlackboard.Set("GameData", _gameData);

        AStarPathfinder pathfinder = AStarPathfinder.CreateBuilder()
            .SetMapData(_mapData)
            .UseDiagonal(Pathfinder.DiagonalType.Never)
            .UseHeuristic(Pathfinder.HeuristicType.Manhattan)
            .Build();
        _sharedBlackboard.Set("Pathfinder", pathfinder);
    }
    public void Tick(double delta)
    {
        List<Var> varsToRemove = VarListPool.Get();
        foreach (var var in Vars)
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
            Vars.Remove(var);
        }
        VarListPool.Return(varsToRemove);
    }
    public override void _Process(double delta)
    {
        foreach (var var in Vars)
        {
            var.FrameUpdate(delta);
        }
    }
    public void AddVar(Var var)
    {
        Vars.Add(var);
        ConnectOnDeath(var);

        var.Initialize(_sharedBlackboard);
        var.InitStatsWithGameData(_gameData);
        _gameData.SkillManager.OnVarCreated(new VarCreationInfo { Var = var });
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

    public int CountVar(VarStats.VarType type)
    {
        int count = 0;
        foreach (var var in Vars)
        {
            if (var.Stats.Type == type)
            {
                count++;
            }
        }
        return count;
    }

    public Var GetVarByName(string name)
    {
        foreach (var var in Vars)
        {
            if (var.Stats.Name == name)
            {
                return var;
            }
        }
        return null!;
    }
}
