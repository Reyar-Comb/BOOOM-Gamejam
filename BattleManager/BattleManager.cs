using Godot;
using System;

public enum BattleState
{
    Running,
    Paused,
    Choice,
    Replay,
    End
}

public readonly struct BattleTickContext
{
    public long Tick { get; }
    public double TickDelta { get; }
    public BattleManager Manager { get; }

    public BattleTickContext(long tick, double tickDelta, BattleManager manager)
    {
        Tick = tick;
        TickDelta = tickDelta;
        Manager = manager;
    }
}

public partial class BattleManager : Node
{
    public static BattleManager Instance { get; private set; } = null!;
    [Export] public int TickRate = 20;

    [Export] public float TickScale = 1f;

    [Export] public VarManager VarManager { get; private set; } = null!;

    public long CurrentTick { get; private set; } = 0;

    public BattleState State { get; private set; } = BattleState.Running;

    public double TickInterval => 1.0 / TickRate;

    private double _accumulator = 0.0;

    private bool _isTicking = false;
    
    private MapData _mapData = null!;

    public long GameTime = 0;

    public override void _Ready()
    {
        Instance = this;
        VarManager.SetPhysicsProcess(false);

        _mapData = new MapData(60, 80);
        _mapData.CreateRegions(6);
        VarManager.Initialize(_mapData);
    }

    public override void _Process(double delta)
    {
        if (State != BattleState.Running) return;

        _accumulator += delta * TickScale;
        
        while (_accumulator >= TickInterval)
        {
            Tick();
            _accumulator -= TickInterval;
        }
    }

    private void Tick()
    {
        CurrentTick++;
        _isTicking = true;

        var context = new BattleTickContext(CurrentTick, TickInterval, this);

        VarManager.Tick(TickInterval);
        GameTime += (long)(TickInterval * 1000); // Convert to milliseconds
    }

    public String GetTimeString()
    {
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(GameTime);
        return timeSpan.ToString(@"mm\:ss\.fff");
    }
}