using Godot;
using System;
using System.Collections.Generic;

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

	[Export] public TokenManager TokenManager { get; private set; } = null!;

	public long CurrentTick { get; private set; } = 0;

	public BattleState State { get; private set; } = BattleState.Running;

	public double TickInterval => 1.0 / TickRate;

	private double _accumulator = 0.0;

	private bool _isTicking = false;

	private MapData _mapData = null!;

	private GameData _gameData = null!;

	public long GameTime = 0;

	private Dictionary<VarStats.VarType, VarStats> _varStatsTemplates = new Dictionary<VarStats.VarType, VarStats>();

	private RandomNumberGenerator _rg = new();

	private bool _isWaveFinished = false;
	private VarStats.VarType[] _spawnableTypes = [
		VarStats.VarType.Int,
		VarStats.VarType.Float,
		VarStats.VarType.Bool,
		VarStats.VarType.Char,
		VarStats.VarType.Long,
		VarStats.VarType.Double,
		VarStats.VarType.LongDouble
	];
	public override void _Ready()
	{
		Instance = this;
		VarManager.SetPhysicsProcess(false);

		_mapData = new MapData(80, 60);
		_mapData.CreateRegions(6);
		_gameData = new GameData();
		VarManager.Initialize(_mapData, _gameData);
		VarRenderer.Initialize(_mapData);
		TokenManager.Initialize(_gameData);

		StartWave();
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
		if (_isWaveFinished)
		{
			StartWave();
			_isWaveFinished = false;
		}

		CurrentTick++;
		_isTicking = true;

		var context = new BattleTickContext(CurrentTick, TickInterval, this);

		VarManager.Tick(TickInterval);
		TokenManager.Tick(TickInterval);
		GameTime += (long)(TickInterval * 1000); // Convert to milliseconds
	}

	public void OnDie()
	{
		State = BattleState.End;
		GD.Print("Game Over!");
	}

	public void TogglePause()
	{
		if (State == BattleState.Running)
		{
			State = BattleState.Paused;
		}
		else if (State == BattleState.Paused)
		{
			State = BattleState.Running;
		}
	}

	public String GetTimeString()
	{
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(GameTime);
		return timeSpan.ToString(@"mm\:ss");
	}

	public void StartWave()
	{
		CurrentTick = 0;
		// GameTime = 0;
		_accumulator = 0.0;
		_isTicking = true;
		_isWaveFinished = false;
		State = BattleState.Running;

		ConsoleManager?.UnsubscribeAllVarEvents();
		VarManager?.ClearAllVars();
		VarRenderer?.ClearVars();

		_gameData.Reset();
		_gameData.SkillManager.ApplyOwnedSkills(_gameData);
		_gameData.SkillManager.OnWaveStarted();

		TokenManager?.Reset();
		PanelNavigator?.RefreshVarList();

		SpawnEnemies(8);
	}
	private Color GetRenderColor(VarStats.VarType type, VarStats.Team team)
	{
		if (type == VarStats.VarType.Dummy)
		{
			return Colors.Gray;
		}
		if (team == VarStats.Team.Friendly)
		{
			return Colors.OrangeRed;
		}
		return Colors.DeepSkyBlue;
	}
	private VarManager.CountQueryType GetQueryType(VarStats.Team team)
	{
		if (team == VarStats.Team.Friendly) return VarManager.CountQueryType.Friendly;
		if (team == VarStats.Team.Hostile) return VarManager.CountQueryType.Hostile;
		return VarManager.CountQueryType.Total;
	}
	public Var RegisterVar(
		VarStats.VarType type,
		Vector2I position,
		VarStats.Team team = VarStats.Team.Friendly, bool isHovering = false)
	{
		Var var = new();

		if (!_varStatsTemplates.TryGetValue(type, out var template))
		{
			template = ResourceLoader.Load<VarStats>($"res://Variable/VarResources/{type}/{type}Stats.tres");
			_varStatsTemplates[type] = template;
		}

		var.Stats = (VarStats)template.Duplicate(true);
		var.Stats.SetGridPosition(position);
		var.Stats.Type = type;
		var.Stats.VarTeam = team;
		var.Stats.Name = $"{type}_{VarManager.CountVar(type, GetQueryType(team)) + 1}";
		if (team == VarStats.Team.Hostile)
		{
			var.Stats.Name += "_Enemy";
		}
		if (isHovering)
		{
			TokenManager.OnHoverRegisterVar(var);
			return var;
		}
		if (team == VarStats.Team.Friendly)
		{
			TokenManager.RegisterVar(var);
		}

		VarManager.AddVar(var);

		Color color = GetRenderColor(type, team);
		VarRenderer.AddVar(var, color);

		if (team == VarStats.Team.Friendly)
		{
			ConsoleManager.RegisterVar(var);
			PanelNavigator.RefreshVarList();
		}
		GD.Print($"Registered var of type {type} at position {position}");
		return var;
	}
	private void OnEnemyDie(Var enemy)
	{
		if (enemy.Stats.Type != VarStats.VarType.Dummy) return;

		_isWaveFinished = true;
	}
	public Var SpawnEnemy(VarStats.VarType type, Vector2I position)
	{
		var enemy = RegisterVar(type, position, VarStats.Team.Hostile);
		enemy.Stats.OnDeath += () => OnEnemyDie(enemy);
		return enemy;
	}
	private int GetRandomSpawnRegionId()
	{
		float p = (float)_rg.Randf();
		if (p < 0.5f) return 2;
		return _rg.RandiRange(3, 6);
	}
	public void SpawnEnemies(int count)
	{
		count = Math.Max(count, 1);
		for (int i = 0; i < count - 1; i++)
		{
			VarStats.VarType type = _spawnableTypes[_rg.RandiRange(0, _spawnableTypes.Length - 1)];
			Vector2I position = _mapData.GetRandomPositionInRegion(GetRandomSpawnRegionId());
			RegisterVar(type, position, VarStats.Team.Hostile);
		}
		SpawnEnemy(VarStats.VarType.Dummy, _mapData.GetRandomPositionInRegion(2));
	}
	public void MoveVar(Var var, Vector2I newPosition, bool isHovering = false)
	{
		if (isHovering)
		{
			TokenManager.OnHoverMoveVar(var);
			return;
		}
		TokenManager.MoveVar(var);
		var.MoveTo(Grid.GridToWorld(newPosition));
		ConsoleManager.MoveVar(var, Grid.GridToWorld(newPosition));
	}

	public void QueryVarLocation(Var var, bool isHovering = false)
	{
		if (isHovering)
		{
			TokenManager.OnHoverQueryVarLocation(var);
			return;
		}
		TokenManager.QueryVarLocation(var);
		ConsoleManager.QueryLocation(var);
	}

	public void QueryVarHealth(Var var, bool isHovering = false)
	{
		if (isHovering)
		{
			TokenManager.OnHoverQueryVarHealth(var);
			return;
		}
		TokenManager.QueryVarHealth(var);
		ConsoleManager.QueryHealth(var);
	}

	public void ClearCostRef()
	{
		TokenManager.ClearCostRef();
	}

	public void ExchangeToken(bool isHovering = false)
	{
		if (isHovering)
		{
			TokenManager.OnHoverExchangeToken();
			return;
		}
		TokenManager.ExchangeToken();
	}



	// TEST METHODS

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Pressed)
			{
				if (keyEvent.Keycode == Key.Escape)
				{
					TogglePause();
					return;
				}
				if (keyEvent.Keycode == Key.O)
				{
					ExchangeToken();
					return;
				}
				if (keyEvent.Keycode == Key.P)
				{
					ExchangeToken(isHovering: true);
					return;
				}
				if (keyEvent.Keycode == Key.L)
				{
					ClearCostRef();
					return;
				}
			}
		}

	}
}
