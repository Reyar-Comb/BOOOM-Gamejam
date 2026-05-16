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

    private readonly Dictionary<VarStats.VarType, int> _friendlyVarTypeCounts = new();
    private readonly Dictionary<VarStats.VarType, int> _hostileVarTypeCounts = new();

    [Signal] public delegate void VarListUpdatedEventHandler();
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
    private readonly List<Var> _friendlyVars = new();
    private readonly List<Var> _hostileVars = new();
    private readonly Dictionary<Var, Callable> _onDeathCallablesByVar = new();
    private readonly Dictionary<Var, Callable> _onDamageReceivedCallablesByVar = new();
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
    private void ConnectSignals(Var var)
    {
        ConnectOnDeath(var);
        ConnectOnDamageReceived(var);
    }
    private void DisconnectSignals(Var var)
    {
        DisconnectOnDeath(var);
        DisconnectOnDamageReceived(var);
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
            GD.Print($"Removing var {var.Stats.Name} of type {var.Stats.Type} from VarManager.");
            DisconnectSignals(var);
            RemoveFromTeamLists(var);
            Vars.Remove(var);
            if (var.Stats.VarTeam == VarStats.Team.Friendly)
            {
                _friendlyVarTypeCounts[var.Stats.Type]--;
            }
            else if (var.Stats.VarTeam == VarStats.Team.Hostile)
            {
                _hostileVarTypeCounts[var.Stats.Type]--;
            }
            var.Cleanup();
            EmitSignal(SignalName.VarListUpdated);
        }
        VarListPool.Return(varsToRemove);
        UpdateDynamicRegionStates();

    }
    private void BroadcastTeam(VarStats.Team team, VarStats.VarType type, Vector2I fromCell)
    {
        List<Var> targetList = team switch
        {
            VarStats.Team.Friendly => _friendlyVars,
            VarStats.Team.Hostile => _hostileVars,
            _ => Vars
        };
        foreach (var var in targetList)
        {
            if (var == null || var.IsDead || var.Stats == null)
            {
                continue;
            }
            // GD.Print($"Broadcasting to var {var.Stats.Name} of type {var.Stats.Type} at cell {Grid.WorldToGrid(var.Stats.Position)}");
            var.OnBroadcastReceived(type, fromCell);
        }
    }
    public override void _Process(double delta)
    {
        foreach (var var in Vars)
        {
            var.FrameUpdate(delta);
        }
    }
    public void AddVar(Var var, bool applyGameData = true)
    {
        Vars.Add(var);
        if (var.Stats.VarTeam == VarStats.Team.Friendly)
        {
            _friendlyVars.Add(var);
        }
        else if (var.Stats.VarTeam == VarStats.Team.Hostile)
        {
            _hostileVars.Add(var);
        }
        ConnectSignals(var);
        var.Initialize(_sharedBlackboard);
        if (applyGameData)
        {
            var.InitStatsWithGameData(_gameData);
            _gameData.SkillManager.OnVarCreated(new VarCreationInfo { Var = var });
        }
        EmitSignal(SignalName.VarListUpdated);
        UpdateDynamicRegionStates();
        var.NotifyRegionEntryIfNeeded(Grid.WorldToGrid(var.Stats.Position));

        Cheat(var);
    }
    private void Cheat(Var var)
    {
        if (var.Stats.VarTeam != VarStats.Team.Friendly) return;

        var.Stats.AttackDamage = 10000;
        var.Stats.MaxHealth = 10000;
        var.Stats.CurrentHealth = 10000;
        var.Stats.MoveSpeed = 800;
        var.Stats.AttackFrameInterval = 0;
    }
    public void ClearAllVars()
    {
        foreach (Var var in Vars)
        {
            DisconnectSignals(var);
            var.Cleanup();
        }

        Vars.Clear();
        _friendlyVars.Clear();
        _hostileVars.Clear();
        _friendlyVarTypeCounts.Clear();
        _hostileVarTypeCounts.Clear();
        _onDeathCallablesByVar.Clear();
        _onDamageReceivedCallablesByVar.Clear();
        EmitSignal(SignalName.VarListUpdated);
        UpdateDynamicRegionStates();
    }

    private void RemoveFromTeamLists(Var var)
    {
        _friendlyVars.Remove(var);
        _hostileVars.Remove(var);
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
        BattleManager.Instance.TokenManager.AddToken(_gameData.NumericData.Get("DeathTokenRefund"));
        UpdateDynamicRegionStates();
    }

    private void UpdateDynamicRegionStates()
    {
        if (_mapData == null)
        {
            return;
        }

        _mapData.ResetDynamicRegionStates();
        foreach (Var var in _hostileVars)
        {
            if (var == null || var.IsDead || var.Stats == null)
            {
                continue;
            }

            Vector2I cell = Grid.WorldToGrid(var.Stats.Position);
            int regionId = _mapData.GetRegion(cell.X, cell.Y);
            if (regionId <= 0 || regionId == MapData.EnemyBaseRegionId)
            {
                continue;
            }

            _mapData.SetRegionState(regionId, MapData.RegionState.Unoccupied);
        }
    }
    private void ConnectOnDamageReceived(Var var)
    {
        Callable onDamageReceivedCallable = Callable.From((AttackInfo attackInfo) => OnVarDamageReceived(attackInfo));
        _onDamageReceivedCallablesByVar[var] = onDamageReceivedCallable;
        var.Connect(Var.SignalName.OnDamageReceived, onDamageReceivedCallable);
    }
    private void DisconnectOnDamageReceived(Var var)
    {
        if (!_onDamageReceivedCallablesByVar.TryGetValue(var, out Callable onDamageReceivedCallable))
        {
            return;
        }

        if (var.IsConnected(Var.SignalName.OnDamageReceived, onDamageReceivedCallable))
        {
            var.Disconnect(Var.SignalName.OnDamageReceived, onDamageReceivedCallable);
        }

        _onDamageReceivedCallablesByVar.Remove(var);
    }
    private void OnVarDamageReceived(AttackInfo attackInfo)
    {
        VarStats.Team team = attackInfo.Target.Stats.VarTeam;
        VarStats.VarType type = attackInfo.Target.Stats.Type;
        Vector2I fromCell = Grid.WorldToGrid(attackInfo.Target.Stats.Position);
        BroadcastTeam(team, type, fromCell);
    }

    public enum CountQueryType
    {
        Total,
        Friendly,
        Hostile
    }

    public string GenerateVarName(VarStats.VarType type, VarStats.Team team, bool isHovering = false)
    {
        if (isHovering)
        {
            return $"{type} Hover";
        }
        Dictionary<VarStats.VarType, int> varTypeCounts = team == VarStats.Team.Friendly ? _friendlyVarTypeCounts : _hostileVarTypeCounts;
        int count = varTypeCounts.GetValueOrDefault(type, 0) + 1;
        varTypeCounts[type] = count;
        GD.Print("Type " + type + "Team" + team + " count: " + count);
        return $"{type}_{count}";
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
