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

	[Export] public ConsoleManager ConsoleManager { get; private set; } = null!;

	[Export] public GamePanelNavigator PanelNavigator { get; private set; } = null!;

	[Export] public VarRenderer VarRenderer { get; private set; } = null!;

	public long CurrentTick { get; private set; } = 0;

	public BattleState State { get; private set; } = BattleState.Running;

	public double TickInterval => 1.0 / TickRate;

	private double _accumulator = 0.0;

	private bool _isTicking = false;
	
	private MapData _mapData = null!;
	
	private GameData _gameData = null!;
	public long GameTime = 0;



	public override void _Ready()
	{
		Instance = this;
		VarManager.SetPhysicsProcess(false);

		_mapData = new MapData(60, 80);
		_mapData.CreateRegions(6);
		_gameData = new GameData();
		VarManager.Initialize(_mapData, _gameData);
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



	public void RegisterVar(VarStats.VarType type, Vector2I position)
	{
		Var var = new();
		VarStats template = type switch
		{
			VarStats.VarType.Int => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Int/IntStats.tres"),
			VarStats.VarType.Float => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Float/FloatStats.tres"),
			VarStats.VarType.Char => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Char/CharStats.tres"),
			VarStats.VarType.Bool => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Bool/BoolStats.tres"),
			VarStats.VarType.Long => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Long/LongStats.tres"),
			VarStats.VarType.Double => ResourceLoader.Load<VarStats>("res://Variable/VarResources/Double/DoubleStats.tres"),
			VarStats.VarType.LongDouble => ResourceLoader.Load<VarStats>("res://Variable/VarResources/LongDouble/LongDoubleStats.tres"),
			_ => throw new ArgumentException($"Unsupported VarStats.Type: {type}")
		};


		var.Stats = (VarStats)template.Duplicate(true);
		var.Stats.SetGridPosition(position);
		var.Stats.Type = type;
		var.Stats.Name = $"{type}_{VarManager.CountVar(type) + 1}";
		VarManager.AddVar(var);
		VarRenderer.AddVar(var);
		ConsoleManager.RegisterVar(var);
		PanelNavigator.RefreshVarList();
		GD.Print($"Registered var of type {type} at position {position}");
	}

	public void MoveVar(Var var, Vector2I newPosition)
	{
		var.MoveTo(Grid.GridToWorld(newPosition));
		ConsoleManager.MoveVar(var, Grid.GridToWorld(newPosition));
	}

	public void QueryVarLocation(Var var)
	{
		ConsoleManager.QueryLocation(var);
	}

	public void QueryVarHealth(Var var)
	{
		ConsoleManager.QueryHealth(var);
	}
}
