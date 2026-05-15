using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum BattleState
{
	Running,
	Paused,
	Choice,
	Replay,
	End,
	BeforeWaveEnd
}

// public readonly struct BattleTickContext
// {
// 	public long Tick { get; }
// 	public double TickDelta { get; }
// 	public BattleManager Manager { get; }

// 	public BattleTickContext(long tick, double tickDelta, BattleManager manager)
// 	{
// 		Tick = tick;
// 		TickDelta = tickDelta;
// 		Manager = manager;
// 	}
// }

public partial class BattleManager : Node
{
	private const int VarUnlockChoiceWaveLimit = 5;

	public static BattleManager Instance { get; private set; } = null!;
	[Export] public int TickRate = 20;

	[Export] public float TickScale = 1f;

	[Export] public double WaveFinishedDelay = 0.4;

	[Export] public double SkillChoiceDelay = 0.6;

	[Export] public VarManager VarManager { get; private set; } = null!;

	[Export] public ConsoleManager ConsoleManager { get; private set; } = null!;

	[Export] public GamePanelNavigator PanelNavigator { get; private set; } = null!;

	[Export] public VarRenderer VarRenderer { get; private set; } = null!;

	[Export] public TokenManager TokenManager { get; private set; } = null!;

	[Export] public ChoicePanel ChoicePanel { get; private set; } = null!;

	[Export] public DeletePanel DeletePanel { get; private set; } = null!;

	[Export] public InitialSkills InitialSkills { get; private set; } = null!;

	[Export] public SkillCardList SkillCardList { get; private set; } = null!;

	public long CurrentTick { get; private set; } = 0;

	public int CurrentWave { get; private set; } = 0;

	public BattleState State { get; private set; } = BattleState.Running;

	public double TickInterval => 1.0 / TickRate;

	public ColorData ColorData => _colorData;

	private double _accumulator = 0.0;

	private bool _isTicking = false;

	private bool _isWaveTransitioning = false;

	private MapData _mapData = null!;

	private GameData _gameData = null!;

	private ColorData _colorData = null!;

	public long GameTime = 0;

	private Dictionary<VarStats.VarType, VarStats> _varStatsTemplates = new Dictionary<VarStats.VarType, VarStats>();

	private RandomNumberGenerator _rg = new();

	private bool _isWaveFinished = false;

	private WaveConfigProvider _waveConfigProvider = new();

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

		_waveConfigProvider.Load();
		_mapData = new MapData(80, 46);
		_gameData = new GameData();
		_colorData = new ColorData();

		VarManager.Initialize(_mapData, _gameData);
		VarRenderer.Initialize(_mapData);
		TokenManager.Initialize(_gameData);
		Log.Initialize(_colorData);
		if (ConsoleManager != null)
		{
			ConsoleManager.LogCreated += OnLogCreated;
		}
		PanelNavigator.SetGameData(_gameData);
		PanelNavigator.SetMapData(_mapData);
		InitAlertBorder();
		InitSkillCards();
		StartWave();
	}

	public override void _ExitTree()
	{
		if (ConsoleManager != null)
		{
			ConsoleManager.LogCreated -= OnLogCreated;
		}
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
		if (_isWaveFinished && !_isWaveTransitioning)
		{
			_ = HandleWaveFinishedAsync();
			return;
		}

		CurrentTick++;
		_isTicking = true;

		// var context = new BattleTickContext(CurrentTick, TickInterval, this);

		VarManager.Tick(TickInterval);
		TokenManager.Tick(TickInterval);
		GameTime += (long)(TickInterval * 1000); // Convert to milliseconds
	}

	private async Task HandleWaveFinishedAsync()
	{
		_isWaveTransitioning = true;
		_accumulator = 0.0;
		State = BattleState.BeforeWaveEnd;

		await FinishWave();
		State = BattleState.Choice;
		await ChooseUpgrades();

		VarRenderer.RestartReveal();
		StartWave();
		_isWaveFinished = false;
		_isWaveTransitioning = false;
	}

	private async Task WaitSeconds(double seconds)
	{
		if (seconds <= 0.0)
		{
			return;
		}

		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
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

	public string GetTimeString()
	{
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(GameTime);
		return timeSpan.ToString(@"mm\:ss");
	}

	private void OnLogCreated(Log log)
	{
		if (log?.ReportedCell == null || VarRenderer == null)
		{
			return;
		}

		VarRenderer.AddLogRipple(log.ReportedCell.Value, log.Type);

		Var actor = VarManager.GetVarByName(log.Actor);
		Var objective = VarManager.GetVarByName(log.Objective);
		if (log is LocationAck || log is MoveCompletedAck)
		{
			VarRenderer.AddOrUpdatePiece(actor, actor.Stats.Position);
		} 
		else if (log is DetectedWarning || log is AttackedWarning || log is CreateAck)
		{
			VarRenderer.AddOrUpdatePiece(objective, objective.Stats.Position);
		}
		else if (log is EnemyRepairedInfo)
		{
			VarRenderer.RemovePiece(objective);
		}
		else if (log is DeathError)
		{
			VarRenderer.RemovePiece(actor);
		}
	}

	private WaveConfig AdvanceWave()
	{
		CurrentWave++;
		WaveConfig waveConfig = _waveConfigProvider.GetConfig(CurrentWave);
		CurrentTick = 0;
		// GameTime = 0;
		_accumulator = 0.0;
		_isTicking = true;
		_isWaveFinished = false;
		_isWaveTransitioning = false;

		return waveConfig;
	}
	private void Refresh()
	{
		ConsoleManager?.UnsubscribeAllVarEvents();
		VarManager?.ClearAllVars();
		VarRenderer?.ClearVars();
		VarRenderer?.ClearPiece();
		_gameData.Reset();
		TokenManager?.Reset();
	}
	public void StartWave()
	{
		WaveConfig waveConfig = AdvanceWave();
		State = BattleState.Running;

		Refresh();
		_mapData.CreateRegions(waveConfig.RegionCount);

		if (CurrentWave == 1)
		{
			AddInitialSkills();
		}

		_gameData.SkillManager.ApplyOwnedSkills(_gameData);
		_gameData.SkillManager.OnWaveStarted();

		GD.Print($"Wave {CurrentWave} started.");
		SpawnEnemies(waveConfig);

		PanelNavigator.RedrawAddButton();
		SkillCardList.RefreshCards();
	}

	private void AddInitialSkills()
	{
		if (InitialSkills == null)
		{
			return;
		}

		foreach (Skill skill in InitialSkills.CreateSkills())
		{
			_gameData.SkillManager.OwnedSkills.Add(skill);
			GD.Print($"Added initial skill: {skill.Name}");
		}
	}

	private async Task FinishWave()
	{
		await WaitSeconds(WaveFinishedDelay);
	}
	private async Task ChooseUpgrades()
	{
		bool isSkillFull = _gameData.SkillManager.OwnedSkills.Count == 5;
		List<Skill> skillsToRemove = null;
		if (isSkillFull)
		{
			skillsToRemove = _gameData.SkillManager.OwnedSkills;
		}

		await WaitSeconds(SkillChoiceDelay);

		List<Upgrade> choices = CurrentWave <= VarUnlockChoiceWaveLimit
			? _gameData.GetRandomUpgradeChoices()
			: _gameData.GetRandomSkillChoices();
		// List<Upgrade> choices = _gameData.GetRandomSkillChoices();
		if (choices.Count == 0)
		{
			return;
		}

		if (ChoicePanel == null)
		{
			GD.PushError("Cannot present upgrade choices because BattleManager.ChoicePanel is not assigned.");
			return;
		}

		Upgrade upgrade = await ChoicePanel.ChooseUpgradeAsync(choices);
		if (upgrade == null)
		{
			return;
		}

		upgrade.Apply(_gameData);

		if (isSkillFull)
		{
			Skill skillToDelete = await DeletePanel.ChooseSkillToDeleteAsync(skillsToRemove);
			if (skillToDelete != null)
			{
				_gameData.SkillManager.OwnedSkills.Remove(skillToDelete);
				GD.Print($"Deleted skill: {skillToDelete.Name}");
			}
		}
		GD.Print($"Wave finished. Applied upgrade: {upgrade.Name}");
	}

	private Color GetRenderColor(VarStats.VarType type, VarStats.Team team)
	{
		if (type == VarStats.VarType.Bug)
		{
			return _colorData.Get("RenderBugVar");
		}
		if (team == VarStats.Team.Friendly)
		{
			return _colorData.Get("RenderFriendlyVar");
		}
		return _colorData.Get("RenderHostileVar");
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
		// if (team == VarStats.Team.Friendly && !_gameData.IsVarTypeUnlocked(type))
		// {
		// 	GD.Print($"Cannot register locked var type: {type}");
		// 	return null;
		// }

		// if (team == VarStats.Team.Friendly && !_gameData.SkillManager.CanCreateVar(type))
		// {
		// 	GD.Print($"Cannot register var type disabled by skills: {type}");
		// 	TokenManager.ClearCostRef();
		// 	return null;
		// }

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
		var.Stats.Name = VarManager.GenerateVarName(type, team, isHovering);
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

		VarManager.AddVar(var, team == VarStats.Team.Friendly);

		Color color = GetRenderColor(type, team);
		VarRenderer.AddVar(var, color);

		ConsoleManager?.RegisterVar(var);
		// GD.Print($"Registered var of type {type} at position {position}");
		return var;
	}
	private void OnEnemyDie(Var enemy)
	{
		if (enemy.Stats.Type != VarStats.VarType.Bug) return;

		_isWaveFinished = true;
	}
	public Var SpawnEnemy(VarStats.VarType type, Vector2I position)
	{
		var enemy = RegisterVar(type, position, VarStats.Team.Hostile);
		enemy.Stats.OnDeath += () => OnEnemyDie(enemy);
		return enemy;
	}
	private int GetRandomSpawnRegionId(WaveConfig config)
	{
		float p = (float)_rg.Randf();
		if (p < config.EnemyBaseSpawnProbability || config.RegionCount <= 2)
		{
			return MapData.EnemyBaseRegionId;
		}

		return _rg.RandiRange(3, config.RegionCount);
	}

	private void SpawnEnemies(WaveConfig config)
	{
		int count = Math.Max(config.EnemyCount, 1);
		for (int i = 0; i < count - 1; i++)
		{
			VarStats.VarType type = config.GetRandomEnemyType(_rg, _spawnableTypes);
			Vector2I position = _mapData.GetRandomPositionInRegion(GetRandomSpawnRegionId(config));
			RegisterVar(type, position, VarStats.Team.Hostile);
		}
		SpawnEnemy(VarStats.VarType.Bug, _mapData.GetRandomPositionInRegion(MapData.EnemyBaseRegionId));
	}
	public void MoveVar(Var var, Vector2I newPosition, bool isHovering = false)
	{
		if (isHovering)
		{
			TokenManager.OnHoverMoveVar(var);
			return;
		}
		TokenManager.MoveVar(var);
		var.MarkMoveAsCommand();
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

	public void OnVarMoveCompleted(Var var)
	{
		ConsoleManager.OnVarMoveCompleted(var);
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

	public async void InitAlertBorder()
	{
		await ToSignal(GetTree().CurrentScene, Node.SignalName.Ready);
		
		AlertBorder alertBorder = GetTree().Root.GetNode<AlertBorder>("MainGame/CanvasLayer/AlertBorder");
		alertBorder.GameData = _gameData;
	}

	public void InitSkillCards()
	{
		SkillCardList.SkillManager = _gameData.SkillManager;
	}
}
