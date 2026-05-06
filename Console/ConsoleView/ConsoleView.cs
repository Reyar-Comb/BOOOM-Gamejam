using Godot;
using System;
using System.Collections.Generic;

public partial class ConsoleView : Control
{
    [Export] public ConsoleManager ConsoleManager { get; set; } = null!;
    [Export] public VarManager VarManager { get; set; } = null!;
    [Export] public BattleManager BattleManager { get; set; } = null!;

    private RichTextLabel _logDisplay = null!;
    private ScrollContainer _scrollContainer = null!;
    private readonly List<Var> _testVars = new();
    private int _varCounter = 0;

    public override void _Ready()
    {
        // Auto-find references if not set
        ConsoleManager ??= GetNodeOrNull<ConsoleManager>("ConsoleManager");
        VarManager ??= GetNodeOrNull<VarManager>("VarManager");
        BattleManager ??= GetNodeOrNull<BattleManager>("BattleManager");

        BuildUI();

        if (ConsoleManager != null)
        {
            ConsoleManager.LogAdded += OnLogAdded;
        }
        else
        {
            AddLog("⚠ ConsoleManager not found! Please add it to the scene.");
        }

        if (VarManager == null)
        {
            AddLog("⚠ VarManager not found! Var creation tests will be limited.");
        }

        if (BattleManager == null)
        {
            AddLog("⚠ BattleManager not found! Time display may be affected.");
        }
    }

    public override void _ExitTree()
    {
        if (ConsoleManager != null)
        {
            ConsoleManager.LogAdded -= OnLogAdded;
        }

        // Clean up test vars
        foreach (var v in _testVars)
        {
            ConsoleManager?.UnsubscribeVarEvents(v);
        }
        _testVars.Clear();
    }

    private void BuildUI()
    {
        // Set up the root Control to fill the screen
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;

        // Background
        var bg = new ColorRect
        {
            Color = new Color("#0a0a0a"),
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 1,
            AnchorBottom = 1
        };
        AddChild(bg);

        // Main vertical layout
        var mainVBox = new VBoxContainer
        {
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 1,
            AnchorBottom = 1
        };
        mainVBox.AddThemeConstantOverride("separation", 8);
        AddChild(mainVBox);

        // === TITLE BAR ===
        var titleBar = new HBoxContainer();
        var title = new Label
        {
            Text = "⚙ CONSOLE TEST PANEL",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", new Color("#00ff88"));
        title.AddThemeFontSizeOverride("font_size", 20);
        titleBar.AddChild(title);
        mainVBox.AddChild(titleBar);

        // === LOG DISPLAY AREA ===
        var logLabel = new Label
        {
            Text = "── Console Output ──",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        logLabel.AddThemeColorOverride("font_color", new Color("#888888"));
        logLabel.AddThemeFontSizeOverride("font_size", 12);
        mainVBox.AddChild(logLabel);

        _scrollContainer = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FollowFocus = true
        };

        _logDisplay = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _logDisplay.AddThemeColorOverride("font_color", new Color("#00cc66"));
        _logDisplay.AddThemeFontSizeOverride("font_size", 13);

        _scrollContainer.AddChild(_logDisplay);
        mainVBox.AddChild(_scrollContainer);

        // === SEPARATOR ===
        var sep = new HSeparator();
        mainVBox.AddChild(sep);

        // === BUTTON AREA ===
        var btnSectionLabel = new Label
        {
            Text = "── Test Queries ──",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        btnSectionLabel.AddThemeColorOverride("font_color", new Color("#888888"));
        btnSectionLabel.AddThemeFontSizeOverride("font_size", 12);
        mainVBox.AddChild(btnSectionLabel);

        // Row 1: Var creation & query
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 8);

        var btnCreateVar = CreateButton("Create Var", "#0077cc", () => OnCreateTestVar());
        row1.AddChild(btnCreateVar);

        var btnQueryStatus = CreateButton("Query Status", "#008855", () => OnQueryStatus());
        row1.AddChild(btnQueryStatus);

        var btnMoveVar = CreateButton("Move Var", "#aa6600", () => OnMoveVar());
        row1.AddChild(btnMoveVar);

        var btnRegisterVar = CreateButton("Register Var", "#5555aa", () => OnRegisterVar());
        row1.AddChild(btnRegisterVar);

        mainVBox.AddChild(row1);

        // Row 2: Signal simulation
        var row2 = new HBoxContainer();
        row2.AddThemeConstantOverride("separation", 8);

        var btnSimAttack = CreateButton("Simulate Attack", "#cc4400", () => OnSimulateAttack());
        row2.AddChild(btnSimAttack);

        var btnSimDetect = CreateButton("Simulate Detect", "#cc8800", () => OnSimulateDetect());
        row2.AddChild(btnSimDetect);

        var btnSimDeath = CreateButton("Simulate Death", "#cc0000", () => OnSimulateDeath());
        row2.AddChild(btnSimDeath);

        var btnSimOutOfDetect = CreateButton("Simulate Lost Detect", "#886600", () => OnSimulateOutOfDetect());
        row2.AddChild(btnSimOutOfDetect);

        mainVBox.AddChild(row2);

        // Row 3: Utility
        var row3 = new HBoxContainer();
        row3.AddThemeConstantOverride("separation", 8);

        var btnClear = CreateButton("Clear Logs", "#555555", () => ClearLogs());
        row3.AddChild(btnClear);

        var btnListVars = CreateButton("List Test Vars", "#335555", () => OnListTestVars());
        row3.AddChild(btnListVars);

        var btnUnsubAll = CreateButton("Unsubscribe All", "#883333", () => OnUnsubscribeAll());
        row3.AddChild(btnUnsubAll);

        mainVBox.AddChild(row3);

        AddLog("[System] ConsoleView initialized. Ready for testing.");
    }

    private Button CreateButton(string text, string colorHex, Action onPressed)
    {
        var btn = new Button
        {
            Text = text,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        // Style the button
        var style = new StyleBoxFlat
        {
            BgColor = new Color(colorHex),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color("#ffffff33"),
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

        var pressedStyle = style.Duplicate() as StyleBoxFlat;
        pressedStyle!.BgColor = new Color(colorHex).Darkened(0.2f);
        btn.AddThemeStyleboxOverride("pressed", pressedStyle);

        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeFontSizeOverride("font_size", 12);

        btn.Pressed += onPressed;
        return btn;
    }

    // ==================== TEST METHODS ====================

    /// <summary>
    /// Creates a test Var with minimal stats, adds to VarManager, then registers with ConsoleManager.
    /// </summary>
    private void OnCreateTestVar()
    {
        if (VarManager == null)
        {
            AddLog("[Error] VarManager is not available. Cannot create Var.");
            return;
        }
        if (ConsoleManager == null)
        {
            AddLog("[Error] ConsoleManager is not available. Cannot register Var.");
            return;
        }

        _varCounter++;
        string varName = $"TestVar_{_varCounter}";

        // Create minimal VarRange
        VarRange attackRange = CreateMinimalRange(new Vector2I(0, 1), new Vector2I(1, 0), new Vector2I(-1, 0));
        VarRange detectRange = CreateMinimalRange(
            new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(0, 3),
            new Vector2I(1, 1), new Vector2I(-1, 1), new Vector2I(1, 2), new Vector2I(-1, 2));

        // Random position for variety
        var random = new Random();
        float posX = random.Next(100, 400);
        float posY = random.Next(100, 400);

        Var var = new()
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = 120.0f,
                AttackDamage = 15,
                AttackRange = attackRange,
                DetectRange = detectRange,
                Position = new Vector2(posX, posY),
                Direction = Vector2.Right,
                VarTeam = VarStats.Team.Friendly
            }
        };

        VarManager.AddVar(var);
        _testVars.Add(var);

        // Register with ConsoleManager - this will subscribe events and log CreateAck
        ConsoleManager.RegisterVar(var);

        AddLog($"[System] Created and registered {varName} at ({posX:F0}, {posY:F0})");
    }

    /// <summary>
    /// Queries the status of the most recently created test Var.
    /// </summary>
    private void OnQueryStatus()
    {
        if (ConsoleManager == null)
        {
            AddLog("[Error] ConsoleManager is not available.");
            return;
        }

        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];
        ConsoleManager.QueryVarStatus(target);
    }

    /// <summary>
    /// Moves the most recently created test Var to a random position.
    /// </summary>
    private void OnMoveVar()
    {
        if (ConsoleManager == null)
        {
            AddLog("[Error] ConsoleManager is not available.");
            return;
        }

        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];
        var random = new Random();
        Vector2 newPos = new(random.Next(50, 500), random.Next(50, 500));
        ConsoleManager.MoveVar(target, newPos);

        // Also actually set the path for the Var to move
        var path = new List<Vector2I>
        {
            Grid.WorldToGrid(target.Stats.Position),
            Grid.WorldToGrid(newPos)
        };
        target.SetPath(path);
    }

    /// <summary>
    /// Registers an already-created but unregistered Var with ConsoleManager.
    /// </summary>
    private void OnRegisterVar()
    {
        if (ConsoleManager == null || VarManager == null)
        {
            AddLog("[Error] ConsoleManager or VarManager is not available.");
            return;
        }

        // Create a var but don't auto-register
        _varCounter++;
        VarRange range = CreateMinimalRange(new Vector2I(0, 1));
        var random = new Random();

        Var var = new()
        {
            Stats = new VarStats
            {
                MaxHealth = 80,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = 100.0f,
                AttackDamage = 10,
                AttackRange = range,
                DetectRange = range,
                Position = new Vector2(random.Next(100, 400), random.Next(100, 400)),
                Direction = Vector2.Right,
                VarTeam = VarStats.Team.Friendly
            }
        };

        VarManager.AddVar(var);
        _testVars.Add(var);

        // Manually register (same as OnCreateTestVar but more explicit)
        ConsoleManager.RegisterVar(var);
        AddLog($"[System] Manually registered TestVar_{_varCounter}");
    }

    /// <summary>
    /// Simulates an attack on the last test Var by directly emitting the signal.
    /// </summary>
    private void OnSimulateAttack()
    {
        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];

        // Need a "source" var for the attack. Use the second-to-last or create a dummy.
        Var attacker;
        if (_testVars.Count >= 2)
        {
            attacker = _testVars[^2];
        }
        else
        {
            // Create a minimal dummy attacker
            attacker = new Var
            {
                Stats = new VarStats
                {
                    MaxHealth = 50,
                    Position = target.Stats.Position + new Vector2(100, 0),
                    VarTeam = VarStats.Team.Hostile
                }
            };
        }

        AttackInfo atkInfo = new()
        {
            Source = attacker,
            Damage = 25
        };

        target.ReceiveDamage(atkInfo);
        AddLog($"[System] Simulated attack from {attacker} with 25 damage on {target}");
    }

    /// <summary>
    /// Simulates a detection event by emitting OnDetected signal.
    /// </summary>
    private void OnSimulateDetect()
    {
        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];

        // Create a dummy detected var
        Var detected = new Var
        {
            Stats = new VarStats
            {
                MaxHealth = 30,
                Position = target.Stats.Position + new Vector2(60, 30),
                Direction = Vector2.Left,
                VarTeam = VarStats.Team.Hostile
            }
        };

        target.EmitSignal(Var.SignalName.OnDetected, detected);
        AddLog($"[System] Simulated detection of {detected} by {target}");
    }

    /// <summary>
    /// Simulates a death event by setting health to 0 on the last test Var.
    /// </summary>
    private void OnSimulateDeath()
    {
        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];
        target.Stats.CurrentHealth = 0;
        AddLog($"[System] Simulated death of {target}");
    }

    /// <summary>
    /// Simulates losing detection by emitting OnOutOfDetect signal.
    /// </summary>
    private void OnSimulateOutOfDetect()
    {
        if (_testVars.Count == 0)
        {
            AddLog("[Warning] No test vars available. Create a Var first.");
            return;
        }

        Var target = _testVars[^1];
        Var lostTarget = new Var
        {
            Stats = new VarStats
            {
                MaxHealth = 30,
                Position = target.Stats.Position + new Vector2(200, 0),
                VarTeam = VarStats.Team.Hostile
            }
        };

        target.EmitSignal(Var.SignalName.OnOutOfDetect, lostTarget);
        AddLog($"[System] Simulated lost detection of {lostTarget} by {target}");
    }

    /// <summary>
    /// Lists all current test vars and their status.
    /// </summary>
    private void OnListTestVars()
    {
        AddLog($"[System] === Current Test Vars ({_testVars.Count}) ===");
        for (int i = 0; i < _testVars.Count; i++)
        {
            Var v = _testVars[i];
            string status = v.IsDead ? "DEAD" : $"HP={v.Stats?.CurrentHealth}/{v.Stats?.MaxHealth}";
            string pos = v.Stats != null ? $"({v.Stats.Position.X:F0}, {v.Stats.Position.Y:F0})" : "N/A";
            AddLog($"  [{i}] {v} | {status} | Pos={pos} | Team={v.Stats?.VarTeam}");
        }
    }

    /// <summary>
    /// Unsubscribes all test vars from ConsoleManager event logging.
    /// </summary>
    private void OnUnsubscribeAll()
    {
        if (ConsoleManager == null) return;

        foreach (var v in _testVars)
        {
            ConsoleManager.UnsubscribeVarEvents(v);
        }
        AddLog($"[System] Unsubscribed all {_testVars.Count} test vars from ConsoleManager.");
    }

    // ==================== HELPERS ====================

    private static VarRange CreateMinimalRange(params Vector2I[] relativeCells)
    {
        var cells = new Godot.Collections.Array<Vector2I>();
        foreach (Vector2I cell in relativeCells)
        {
            cells.Add(cell);
        }
        return new VarRange { RelativeCells = cells };
    }

    private void OnLogAdded(string formattedLog)
    {
        AddLog(formattedLog);
    }

    private void AddLog(string message)
    {
        if (_logDisplay == null) return;

        _logDisplay.AppendText(message + "\n");

        // Auto-scroll to bottom
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
}
