using Godot;
using System.Collections.Generic;

public partial class ConsoleTest : Node2D
{
	[Export] public VarManager VarManager { get; set; } = null!;
	[Export] public BattleManager BattleManager { get; set; } = null!;
	[Export] public ConsoleManager ConsoleManager { get; set; } = null!;

	// Grid area: 11×11 cells = 550×550 px, world x:[-200, 350], y:[-200, 350]
	private static readonly Rect2I GridArea = new(-4, -4, 11, 11);
	private static readonly int GridCellSize = Grid.CellSize;

	// Pre-placed hostile positions (fit within grid)
	private static readonly Vector2I[] HostileStartCells =
	{
		new(3, 1),
		new(5, -2),
		new(1, -3),
	};

	// Pre-placed friendly (static guard)
	private static readonly Vector2I GuardStartCell = new(-2, 0);

	private readonly List<Var> _hostileVars = new();
	private readonly List<Var> _friendlyVars = new();
	private Var _guardVar = null!;

	private int _varCounter = 0;

	// UI nodes
	private RichTextLabel _logDisplay = null!;
	private ScrollContainer _scrollContainer = null!;
	private LineEdit _inputX = null!;
	private LineEdit _inputY = null!;
	private Label _infoLabel = null!;
	private Slider _timeSlider = null!;
	private VarRenderer _varRenderer = null!;

	public override void _Ready()
	{
		// Resolve references
		VarManager ??= GetNodeOrNull<VarManager>("VarManager");
		BattleManager ??= GetNodeOrNull<BattleManager>("BattleManager");
		ConsoleManager ??= GetNodeOrNull<ConsoleManager>("ConsoleManager");

		// Center the grid in the map area (left portion of screen)
		Grid.SetOffset(200, 200);

		// Wire ConsoleManager log signal
		if (ConsoleManager != null)
		{
			ConsoleManager.LogAdded += OnLogAdded;
			AddLog("[System] ConsoleManager connected. Auto-logging active.");
		}
		else
		{
			GD.PushWarning("ConsoleTest: ConsoleManager not found in scene.");
		}

		// Build UI
		BuildUI();

		// Create pre-placed vars
		CreatePrePlacedVars();

		AddLog("[System] ConsoleTest ready. Pre-placed 3 hostile + 1 guard. Click 'Create Var' to deploy.");
	}

	public override void _ExitTree()
	{
		if (ConsoleManager != null)
		{
			ConsoleManager.LogAdded -= OnLogAdded;
		}
	}

	public override void _Process(double delta)
	{
		UpdateInfoLabel();
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawGrid(GridArea);
		DrawHostileMarkers();
		DrawGuardMarker();
	}

	// ==================== DRAWING (参照 TestChaseOutOfRange) ====================

	private void DrawGrid(Rect2I area)
	{
		for (int x = area.Position.X; x < area.End.X; x++)
		{
			for (int y = area.Position.Y; y < area.End.Y; y++)
			{
				Vector2 topLeft = Grid.GridToWorld(new Vector2I(x, y)) - Vector2.One * GridCellSize / 2.0f;
				DrawRect(new Rect2(topLeft, Vector2.One * GridCellSize), new Color(1, 1, 1, 0.05f), false, 1.0f);
			}
		}
	}

	private void DrawHostileMarkers()
	{
		foreach (var v in _hostileVars)
		{
			if (IsAlive(v))
			{
				DrawCellMarker(Grid.WorldToGrid(v.Stats.Position), Colors.DeepSkyBlue, "E");
			}
		}
	}

	private void DrawGuardMarker()
	{
		if (IsAlive(_guardVar))
		{
			DrawCellMarker(Grid.WorldToGrid(_guardVar.Stats.Position), Colors.Gold, "G");
		}
	}

	private void DrawCellMarker(Vector2I cell, Color color, string label)
	{
		Vector2 center = Grid.GridToWorld(cell);
		Vector2 topLeft = center - Vector2.One * GridCellSize / 2.0f;
		DrawRect(new Rect2(topLeft, Vector2.One * GridCellSize), WithAlpha(color, 0.10f));
		DrawRect(new Rect2(topLeft, Vector2.One * GridCellSize), WithAlpha(color, 0.7f), false, 2.0f);
		DrawString(ThemeDB.FallbackFont, center + new Vector2(-6.0f, 6.0f), label,
			HorizontalAlignment.Left, -1.0f, 16, color);
	}

	// ==================== UI BUILDING ====================

	private void BuildUI()
	{
		var canvas = GetNodeOrNull<CanvasLayer>("CanvasLayer");
		if (canvas == null)
		{
			canvas = new CanvasLayer { Name = "CanvasLayer" };
			AddChild(canvas);
		}

		// --- Time Slider (top-left, 参照 TestAttack) ---
		_timeSlider = new Slider
		{
			Name = "TimeSlider",
			BattleManager = BattleManager,
			Position = new Vector2(24, 24),
			Size = new Vector2(200, 0)
		};
		if (BattleManager != null)
		{
			_timeSlider.Value = BattleManager.TickScale;
		}
		canvas.AddChild(_timeSlider);

		// --- Info Label (top-left, below slider) ---
		_infoLabel = new Label
		{
			Name = "InfoLabel",
			Position = new Vector2(24, 60),
			Size = new Vector2(400, 80)
		};
		_infoLabel.AddThemeFontSizeOverride("font_size", 14);
		_infoLabel.AddThemeColorOverride("font_color", new Color("#cccccc"));
		canvas.AddChild(_infoLabel);

		// --- Right Panel (fits within ~1150px viewport: map left 650px + panel right 420px) ---
		var panel = new Panel
		{
			Name = "ConsolePanel",
			Position = new Vector2(650, 0),
			Size = new Vector2(380, 648),
		};
		var panelStyle = new StyleBoxFlat
		{
			BgColor = new Color("#0d1117"),
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			BorderWidthBottom = 2,
			BorderColor = new Color("#30363d"),
			CornerRadiusTopLeft = 0,
			CornerRadiusTopRight = 0,
			CornerRadiusBottomLeft = 0,
			CornerRadiusBottomRight = 0
		};
		panel.AddThemeStyleboxOverride("panel", panelStyle);
		canvas.AddChild(panel);

		var panelVBox = new VBoxContainer
		{
			Position = new Vector2(12, 12),
			Size = new Vector2(356, 624)
		};
		panelVBox.AddThemeConstantOverride("separation", 8);
		panel.AddChild(panelVBox);

		// Title
		var title = new Label
		{
			Text = "⚙ CONSOLE",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeColorOverride("font_color", new Color("#00ff88"));
		title.AddThemeFontSizeOverride("font_size", 18);
		panelVBox.AddChild(title);

		// Log display
		var logLabel = new Label
		{
			Text = "── Output ──"
		};
		logLabel.AddThemeColorOverride("font_color", new Color("#888888"));
		logLabel.AddThemeFontSizeOverride("font_size", 11);
		panelVBox.AddChild(logLabel);

		_scrollContainer = new ScrollContainer
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_logDisplay = new RichTextLabel
		{
			BbcodeEnabled = true,
			ScrollFollowing = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		_logDisplay.AddThemeColorOverride("font_color", new Color("#00cc66"));
		_logDisplay.AddThemeFontSizeOverride("font_size", 12);

		_scrollContainer.AddChild(_logDisplay);
		panelVBox.AddChild(_scrollContainer);

		// Separator
		panelVBox.AddChild(new HSeparator());

		// --- Buttons ---
		var btnLabel = new Label { Text = "── Commands ──" };
		btnLabel.AddThemeColorOverride("font_color", new Color("#888888"));
		btnLabel.AddThemeFontSizeOverride("font_size", 11);
		panelVBox.AddChild(btnLabel);

		// Create button
		panelVBox.AddChild(MakeButton("Create Var", "#238636", OnCreateVar));

		// Move row: X input, Y input, Move button
		var moveRow = new HBoxContainer();
		moveRow.AddThemeConstantOverride("separation", 6);
		moveRow.AddChild(new Label { Text = "X:" });
		_inputX = new LineEdit
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = "0"
		};
		moveRow.AddChild(_inputX);
		moveRow.AddChild(new Label { Text = "Y:" });
		_inputY = new LineEdit
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = "0"
		};
		moveRow.AddChild(_inputY);
		moveRow.AddChild(MakeButton("Move", "#9c6b0a", OnMoveVar, 70));
		panelVBox.AddChild(moveRow);

		// Query button
		panelVBox.AddChild(MakeButton("Query Status", "#1a7f8c", OnQueryStatus));

		// Utility row
		var utilRow = new HBoxContainer();
		utilRow.AddThemeConstantOverride("separation", 6);
		utilRow.AddChild(MakeButton("Clear", "#333333", ClearLogs));
		utilRow.AddChild(MakeButton("List Vars", "#333355", OnListVars));
		panelVBox.AddChild(utilRow);
	}

	private static Button MakeButton(string text, string colorHex, System.Action onPressed, float minWidth = 0)
	{
		var btn = new Button
		{
			Text = text,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		if (minWidth > 0)
		{
			btn.CustomMinimumSize = new Vector2(minWidth, 0);
		}

		var style = new StyleBoxFlat
		{
			BgColor = new Color(colorHex),
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			BorderWidthTop = 1,
			BorderWidthBottom = 1,
			BorderColor = new Color("#ffffff22"),
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 4,
			ContentMarginBottom = 4
		};
		btn.AddThemeStyleboxOverride("normal", style);

		var hoverStyle = style.Duplicate() as StyleBoxFlat;
		hoverStyle!.BgColor = new Color(colorHex).Lightened(0.2f);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);

		btn.AddThemeColorOverride("font_color", Colors.White);
		btn.AddThemeFontSizeOverride("font_size", 12);

		btn.Pressed += onPressed;
		return btn;
	}

	// ==================== VAR CREATION (参照 TestAttack) ====================

	private Var CreateVarAt(Vector2I startCell, VarStats.Team team, Color color,
		int maxHealth = 100, float moveSpeed = 120f, int attackDamage = 15,
		int attackFrameInterval = 20, bool includeRange = true)
	{
		VarRange attackRange;
		VarRange detectRange;

		if (includeRange)
		{
			attackRange = CreateRange(
				new Vector2I(0, 1), new Vector2I(0, 2),
				new Vector2I(1, 1), new Vector2I(-1, 1));
			detectRange = CreateRange(
				new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(0, 3),
				new Vector2I(1, 1), new Vector2I(-1, 1),
				new Vector2I(1, 2), new Vector2I(-1, 2));
		}
		else
		{
			attackRange = CreateRange();
			detectRange = CreateRange();
		}

		Var var = new()
		{
			Stats = new VarStats
			{
				MaxHealth = maxHealth,
				AttackSpeedMult = 1.0f,
				AttackFrameInterval = attackFrameInterval,
				MoveSpeed = moveSpeed,
				AttackRange = attackRange,
				DetectRange = detectRange,
				Position = Grid.GridToWorld(startCell),
				Direction = team == VarStats.Team.Hostile ? Vector2.Left : Vector2.Right,
				VarTeam = team,
				AttackDamage = attackDamage
			}
		};

		VarManager.AddVar(var);
		CreateRenderer(var, color);
		return var;
	}

	private void CreateRenderer(Var var, Color color)
	{
		_varRenderer ??= new VarRenderer
		{
			BattleManager = BattleManager,
			Config = new VarRendererConfig
			{
				BodyRadius = 18.0f,
				BodyColor = Colors.OrangeRed,
				AttackRangeColor = Colors.OrangeRed,
				DetectRangeColor = WithAlpha(Colors.OrangeRed, 0.65f),
				DirectionColor = Colors.White,
				RenderVarBody = true,
				RenderAttackRange = true,
				RenderDetectRange = true,
				RenderDirection = true
			}
		};

		if (_varRenderer.GetParent() == null)
		{
			AddChild(_varRenderer);
		}

		_varRenderer.AddVar(var, color, color, WithAlpha(color, 0.65f), Colors.White);
	}

	private static VarRange CreateRange(params Vector2I[] relativeCells)
	{
		var cells = new Godot.Collections.Array<Vector2I>();
		foreach (Vector2I cell in relativeCells)
		{
			cells.Add(cell);
		}
		return new VarRange { RelativeCells = cells };
	}

	// ==================== PRE-PLACED VARS ====================

	private void CreatePrePlacedVars()
	{
		// 3 hostile enemies at different positions, stationary
		foreach (Vector2I cell in HostileStartCells)
		{
			Var hostile = CreateVarAt(cell, VarStats.Team.Hostile, Colors.DeepSkyBlue,
				maxHealth: 60, moveSpeed: 0f, attackDamage: 12,
				attackFrameInterval: 24, includeRange: true);
			_hostileVars.Add(hostile);
			ConsoleManager?.RegisterVar(hostile);
		}

		// 1 friendly guard
		_guardVar = CreateVarAt(GuardStartCell, VarStats.Team.Friendly, Colors.Gold,
			maxHealth: 80, moveSpeed: 0f, attackDamage: 10,
			attackFrameInterval: 30, includeRange: true);
		_friendlyVars.Add(_guardVar);
		ConsoleManager?.RegisterVar(_guardVar);

		AddLog($"[System] Pre-placed {_hostileVars.Count} enemies + 1 guard. All registered for auto-logging.");
	}

	// ==================== BUTTON HANDLERS ====================

	private void OnCreateVar()
	{
		if (VarManager == null)
		{
			AddLog("[Error] VarManager not available.");
			return;
		}

		_varCounter++;

		// Create at a random position near the left side (friendly spawn area)
		var random = new System.Random();
		Vector2I spawnCell = new(random.Next(-2, 1), random.Next(-3, 3));
		Vector2I targetCell = new(random.Next(2, 7), random.Next(-3, 3));

		Var friendly = CreateVarAt(spawnCell, VarStats.Team.Friendly, Colors.OrangeRed,
			maxHealth: 100, moveSpeed: 100f, attackDamage: 15,
			attackFrameInterval: 20, includeRange: true);

		// Set path so it starts moving toward enemy territory
		friendly.SetPath(new List<Vector2I> { spawnCell, targetCell });

		_friendlyVars.Add(friendly);
		ConsoleManager?.RegisterVar(friendly);

		AddLog($"[System] Created Var_{_varCounter} at {spawnCell}, moving to {targetCell}.");
	}

	private void OnMoveVar()
	{
		if (_friendlyVars.Count <= 1) // only guard exists
		{
			AddLog("[Warning] Create a Var first before moving.");
			return;
		}

		// Move the last created friendly var (skip guard at index 0)
		Var target = _friendlyVars[^1];

		if (!int.TryParse(_inputX.Text, out int gridX) || !int.TryParse(_inputY.Text, out int gridY))
		{
			AddLog("[Error] Invalid coordinates. Enter integers for X and Y.");
			return;
		}

		Vector2I targetCell = new(gridX, gridY);
		Vector2 newPosition = Grid.GridToWorld(targetCell);

		// ConsoleManager now handles pathfinding + SetPath internally
		ConsoleManager?.MoveVar(target, newPosition);

		AddLog($"[System] Ordered move to grid ({gridX}, {gridY}).");
	}

	private void OnQueryStatus()
	{
		if (ConsoleManager == null) return;

		if (_friendlyVars.Count == 0)
		{
			AddLog("[Warning] No friendly vars to query.");
			return;
		}

		// Query the last created friendly
		Var target = _friendlyVars[^1];
		ConsoleManager.QueryVarStatus(target);
	}

	private void OnListVars()
	{
		AddLog($"[System] === Friendly: {_friendlyVars.Count}, Hostile: {_hostileVars.Count} ===");
		for (int i = 0; i < _friendlyVars.Count; i++)
		{
			Var v = _friendlyVars[i];
			AddLog($"  Friendly[{i}]: {VarSummary(v)}");
		}
		for (int i = 0; i < _hostileVars.Count; i++)
		{
			Var v = _hostileVars[i];
			AddLog($"  Hostile[{i}]: {VarSummary(v)}");
		}
	}

	private static string VarSummary(Var v)
	{
		if (v == null || v.Stats == null) return "NULL";
		Vector2I cell = Grid.WorldToGrid(v.Stats.Position);
		string dead = v.IsDead ? " [DEAD]" : "";
		return $"HP={v.Stats.CurrentHealth}/{v.Stats.MaxHealth} @ {cell}{dead}";
	}

	// ==================== LOGGING ====================

	private void OnLogAdded(string formattedLog)
	{
		AddLog(formattedLog);
	}

	private void AddLog(string message)
	{
		_logDisplay?.AppendText(message + "\n");
		CallDeferred(nameof(ScrollToBottom));
	}

	private void ScrollToBottom()
	{
		if (_scrollContainer != null)
		{
			_scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
		}
	}

	private void ClearLogs()
	{
		_logDisplay?.Clear();
		AddLog("[System] Logs cleared.");
	}

	// ==================== INFO LABEL ====================

	private void UpdateInfoLabel()
	{
		if (_infoLabel == null) return;

		int aliveFriendly = 0;
		int aliveHostile = 0;
		foreach (var v in _friendlyVars) if (IsAlive(v)) aliveFriendly++;
		foreach (var v in _hostileVars) if (IsAlive(v)) aliveHostile++;

		_infoLabel.Text =
			$"Tick: {BattleManager?.CurrentTick ?? 0}    " +
			$"Friendly: {aliveFriendly}    Hostile: {aliveHostile}\n" +
			"Orange: Player Vars    Blue: Enemies    Gold: Guard\n" +
			"Enemies detect & attack automatically. Watch the console!";
	}

	// ==================== HELPERS ====================

	private static bool IsAlive(Var var)
	{
		return var != null && !var.IsDead && var.Stats != null;
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		color.A = alpha;
		return color;
	}
}
