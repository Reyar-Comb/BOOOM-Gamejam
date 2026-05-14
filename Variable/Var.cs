using Godot;
using StarlightBT.Data;
using StarlightStateTree;
using System.Collections.Generic;
using System;
using Cosmosity.Pathfinders;

public partial class Var : RefCounted, ICleanable
{
    public VarStats Stats { get; set; }
    public bool IsDead { get; set; } = false;
    protected STRoot _stateTree = null!;
    protected Blackboard _blackboard = null!;
    private bool _isInitialized = false;
    private Callable _onDeathCallable;
    private bool _hasOnDeathCallable = false;
    private bool _isCleanedUp = false;
    private Vector2I _currentCell = Vector2I.Left;
    private int _currentRegionId = -1;
    private readonly HashSet<Var> _currentAttackers = new();
    private readonly HashSet<Var> _historyAttackers = new();
    // Record whose _attackers hashset this Var is currently in.
    private Var _registeredAttackTarget;

    private MapData MapData => _blackboard.Get<MapData>("MapData");
    private GameData GameData => _blackboard.Get<GameData>("GameData");

    [Signal]
    public delegate void OnDetectedEventHandler(DetectInfo detectInfo);

    [Signal]
    public delegate void OnAttackedEventHandler(int damage, Var source);

    [Signal]
    public delegate void OnOutOfDetectEventHandler(Var target);

    [Signal]
    public delegate void OnDamageReceivedEventHandler(AttackInfo attackInfo);
    public void Initialize(Blackboard parentBlackboard)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _blackboard = new()
        {
            ParentBlackboard = parentBlackboard
        };
        SetupStateTree();

        _onDeathCallable = Callable.From(OnDeath);
        Stats.Connect(VarStats.SignalName.OnDeath, _onDeathCallable);
        _hasOnDeathCallable = true;
    }
    public void InitStatsWithGameData(GameData data)
    {
        Stats.MaxHealth += data.NumericData.Get("HealthBonus");
        Stats.CurrentHealth = Stats.MaxHealth;
        Stats.AttackDamage += data.NumericData.Get("AttackBonus");
        Stats.Defense += data.NumericData.Get("DefenseBonus");
        Stats.MoveSpeed += data.NumericData.Get("MoveSpeedBonus");

        if (Stats.Type is VarStats.VarType.Int or VarStats.VarType.Long)
        {
            Stats.Defense += data.NumericData.Get("IntegerDefenseBonus");
        }

        if (Stats.Type is VarStats.VarType.Float or VarStats.VarType.Double or VarStats.VarType.LongDouble)
        {
            Stats.AttackDamage += data.NumericData.Get("FloatingPointAttackBonus");
        }
    }
    public void Cleanup()
    {
        if (_isCleanedUp) return;
        _isCleanedUp = true;

        StopAttacking();
        ClearAttackers();
        DisconnectOnDeath();
        Stats = null;
        _stateTree?.Cleanup();
        _stateTree = null;
        _blackboard?.Cleanup();
        _blackboard = null;
    }
    private void OnDeath()
    {
        IsDead = true;
        StopAttacking();
        ClearAttackers();
        _stateTree?.ForceTransition("Death");
    }
    private void DisconnectOnDeath()
    {
        if (Stats == null || !_hasOnDeathCallable)
        {
            return;
        }

        if (Stats.IsConnected(VarStats.SignalName.OnDeath, _onDeathCallable))
        {
            Stats.Disconnect(VarStats.SignalName.OnDeath, _onDeathCallable);
        }

        _onDeathCallable = default;
        _hasOnDeathCallable = false;
    }
    public void SetPath(List<Vector2I> path)
    {
        if (path == null || path.Count == 0) return;

        InitializeFacingFromPath(path);
        _blackboard.Set("CurrentPath", path);
        _blackboard.Set("CurrentPathIndex", 0);
        _blackboard.Set("HasPendingMove", true);
    }

    public void MoveTo(Vector2 worldTarget)
    {
        var pathfinder = _blackboard.Get<Pathfinder>("Pathfinder");
        if (pathfinder == null) return;

        Vector2I selfCell = Grid.WorldToGrid(Stats.Position);
        Vector2I targetCell = Grid.WorldToGrid(worldTarget);
        if (selfCell == targetCell) return;

        var path = pathfinder.Run(selfCell, targetCell, MapData.GetRegion(selfCell.X, selfCell.Y));
        SetPath(path);
    }
    public void PhysicsUpdate(double delta)
    {
        _stateTree.PhysicsUpdate(delta);
    }
    public void FrameUpdate(double delta)
    {
        _stateTree.FrameUpdate(delta);
    }
    public void NotifyRegionEntryIfNeeded(Vector2I reachedCell)
    {
        if (MapData == null || GameData == null)
        {
            _currentCell = reachedCell;
            return;
        }

        int reachedRegion = MapData.GetRegion(reachedCell.X, reachedCell.Y);
        if (reachedRegion == _currentRegionId)
        {
            _currentCell = reachedCell;
            return;
        }

        RegionEnteredInfo info = new()
        {
            Var = this,
            MapData = MapData,
            FromCell = _currentCell,
            ToCell = reachedCell,
            FromRegion = _currentRegionId,
            ToRegion = reachedRegion
        };

        _currentCell = reachedCell;
        _currentRegionId = reachedRegion;
        GameData.SkillManager.OnRegionEntered(info);
        // GD.Print($"Var {Stats.Name} entered region {reachedRegion} at cell {reachedCell}");
    }
    public void ReceiveDamage(AttackInfo atkInfo)
    {
        PruneAttackers();

        atkInfo.Attackers = _currentAttackers;
        atkInfo.Defense = Stats.Defense;
        atkInfo.Vars = _blackboard.Get<IReadOnlyList<Var>>("Vars");
        atkInfo.MapData = MapData;
        GameData.SkillManager.OnBeforeAttack(atkInfo);
        int finalDamage = atkInfo.Damage;

        // directionFactor is in [1, 2], 1 if attacked from front, 2 if from back.
        // Vector2 facingDirection = Stats.Direction;
        // float directionFactor = atkInfo.GetFromDirection(Stats.Position).Dot(facingDirection) * -0.5f + 1.5f;
        // if (Stats.Type == VarStats.VarType.Dummy)
        // {
        //     directionFactor = 1f;
        // }
        // finalDamage = (int)(finalDamage * directionFactor);
        finalDamage = Math.Max(0, finalDamage - atkInfo.Defense);
        EmitSignal(SignalName.OnDamageReceived, atkInfo);
        if (!_historyAttackers.Contains(atkInfo.Source))
        {
            _historyAttackers.Add(atkInfo.Source);
            EmitSignal(SignalName.OnAttacked, finalDamage, atkInfo.Source);
        }

        Stats.CurrentHealth -= finalDamage;
        if (IsDead) return;

        if (_blackboard.Get<Var>("CurrentAttackTarget") == null && Stats.Type != VarStats.VarType.Bool && Stats.Type != VarStats.VarType.Dummy)
        {
            MoveTo(atkInfo.Source.Stats.Position);
        }
    }
    public void OnBroadcastReceived(VarStats.VarType fromType, Vector2I fromCell)
    {
        if (_blackboard == null || IsDead)
        {
            return;
        }

        if (fromType == VarStats.VarType.Dummy)
        {
            _blackboard.Set("IsDummyAttacked", true);
            _blackboard.Set("DummyCell", fromCell);
        }
    }
    public void BeginAttacking(Var target)
    {
        if (target == null || target.IsDead)
        {
            StopAttacking();
            return;
        }

        if (_registeredAttackTarget == target)
        {
            return;
        }

        StopAttacking();
        _registeredAttackTarget = target;
        target.AddAttacker(this);
    }

    public void StopAttacking()
    {
        if (_registeredAttackTarget == null)
        {
            return;
        }

        Var previousTarget = _registeredAttackTarget;
        _registeredAttackTarget = null;
        previousTarget.RemoveAttacker(this);
    }

    private void AddAttacker(Var attacker)
    {
        if (attacker == null || attacker == this || attacker.IsDead)
        {
            return;
        }

        _currentAttackers.Add(attacker);
    }

    private void RemoveAttacker(Var attacker)
    {
        if (attacker == null)
        {
            return;
        }

        _currentAttackers.Remove(attacker);
    }

    private void ClearAttackers()
    {
        _currentAttackers.Clear();
    }

    private void PruneAttackers()
    {
        _currentAttackers.RemoveWhere(attacker => attacker == null || attacker.IsDead);
    }
    protected virtual void SetupStateTree()
    {
        var idleState = new Var_IdleState();
        var moveState = new Var_MoveState();
        var attackState = new Var_AttackState();
        var detectOutOfRangeState = new Var_DetectOutOfRangeState();
        var detectInDetectRangeState = new Var_DetectInDetectRangeState();
        var detectInAttackRangeState = new Var_DetectInAttackRange();
        var deathState = new Var_DeathState();

        _stateTree = new STRoot
        {
            InitialState = "Idle",
            AllowRepeatedEnterAndExit = false
        };
        _stateTree.AddChild(detectInAttackRangeState);
        detectInAttackRangeState.AddChild(detectInDetectRangeState);
        detectInDetectRangeState.AddChild(idleState);
        detectInDetectRangeState.AddChild(moveState);

        _stateTree.AddChild(detectOutOfRangeState);
        detectOutOfRangeState.AddChild(attackState);

        _stateTree.AddChild(deathState);

        _blackboard.Set("Stats", Stats);
        _blackboard.Set("CurrentPath", new List<Vector2I>());
        _blackboard.Set("IsWalking", false);
        _blackboard.Set("HasPendingMove", false);
        _blackboard.Set("CurrentPathIndex", 0);
        _blackboard.Set("CurrentAttackTarget", (Var)null);
        _blackboard.Set("Self", this);
        _blackboard.Set("IsAttacked", false);
        _blackboard.Set("IsDummyAttacked", false);
        _blackboard.Set("DummyCell", Vector2I.Zero);
        _blackboard.Set("EnemyRandomMoveInterval", 2.0f);
        _blackboard.Set("EnemyRandomMoveTimeRemaining", 2.0f);

        _stateTree.Initialize(_blackboard);
    }
    private void InitializeFacingFromPath(List<Vector2I> path)
    {
        if (Stats == null)
        {
            return;
        }

        Vector2 currentPosition = Stats.Position;

        foreach (Vector2I gridPoint in path)
        {
            Vector2 nextPosition = Grid.GridToWorld(gridPoint);
            Vector2 nextDirection = nextPosition - currentPosition;

            if (nextDirection.LengthSquared() <= MathConstants.EpsilonSquared)
            {
                currentPosition = nextPosition;
                continue;
            }

            Stats.Direction = nextDirection.ToFacingDirection();
            return;
        }
    }
}
