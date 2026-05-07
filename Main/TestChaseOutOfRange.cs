using Godot;
using System.Collections.Generic;

public partial class TestChaseOutOfRange : Node2D
{
    [Export] public VarManager VarManager { get; set; } = null!;
    [Export] public BattleManager BattleManager { get; set; } = null!;

    private static readonly Vector2I LeadStartCell = new(2, 5);
    private static readonly Vector2I LeadAttackCell = new(5, 5);
    private static readonly Vector2I SupportStartCell = new(6, 0);
    private static readonly Vector2I SupportAttackCell = new(6, 4);
    private static readonly Vector2I HostileCell = new(6, 6);

    private static readonly IReadOnlyList<Vector2I> LeadPath = new List<Vector2I>
    {
        new(3, 5),
        new(4, 5),
        LeadAttackCell
    };

    private static readonly IReadOnlyList<Vector2I> SupportPath = new List<Vector2I>
    {
        new(6, 1),
        new(6, 2),
        new(6, 3),
        SupportAttackCell
    };

    private Var _leadFriendly = null!;
    private Var _supportFriendly = null!;
    private Var _hostile = null!;
    private Label _infoLabel = null!;
    private VarRenderer _varRenderer = null!;
    private CombatPhase _phase = CombatPhase.MovingToAmbush;
    private long _ambushTick = -1;
    private bool _leadDeathRecorded = false;
    private bool _hostileDeathRecorded = false;

    private enum CombatPhase
    {
        MovingToAmbush,
        SynchronizedAttack,
        HostileKilledLead,
        SupportKilledHostile
    }

    public override void _Ready()
    {
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");
        BattleManager ??= GetNodeOrNull<BattleManager>("BattleManager");

        VarRange emptyRange = CreateRange();

        _leadFriendly = CreateVar(
            LeadStartCell,
            Colors.OrangeRed,
            maxHealth: 56,
            moveSpeed: 100.0f,
            attackDamage: 14,
            attackFrameInterval: 14,
            attackRange: emptyRange,
            detectRange: emptyRange,
            team: VarStats.Team.Friendly);

        _supportFriendly = CreateVar(
            SupportStartCell,
            Colors.Gold,
            maxHealth: 140,
            moveSpeed: 80.0f,
            attackDamage: 20,
            attackFrameInterval: 12,
            attackRange: emptyRange,
            detectRange: emptyRange,
            team: VarStats.Team.Friendly);

        _hostile = CreateVar(
            HostileCell,
            Colors.DeepSkyBlue,
            maxHealth: 105,
            moveSpeed: 0.0f,
            attackDamage: 28,
            attackFrameInterval: 10,
            attackRange: emptyRange,
            detectRange: emptyRange,
            team: VarStats.Team.Hostile);

        _leadFriendly.SetPath(new List<Vector2I>(LeadPath));
        _supportFriendly.SetPath(new List<Vector2I>(SupportPath));

        UpdateInfoLabel();
    }

    public override void _Process(double delta)
    {
        UpdateScenario();
        UpdateInfoLabel();
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawGrid(new Rect2I(1, 0, 8, 8));
        DrawPlannedPath(LeadStartCell, LeadPath, Colors.OrangeRed);
        DrawPlannedPath(SupportStartCell, SupportPath, Colors.Gold);
        DrawAmbushCells();
        DrawTargetLinks();
        DrawHealthBars();
    }

    private void UpdateScenario()
    {
        if (_phase == CombatPhase.MovingToAmbush && AreFriendliesReady())
        {
            StartSynchronizedAttack();
        }

        if (!_leadDeathRecorded && IsDead(_leadFriendly))
        {
            _leadDeathRecorded = true;
            _phase = CombatPhase.HostileKilledLead;
        }

        if (!_hostileDeathRecorded && IsDead(_hostile))
        {
            _hostileDeathRecorded = true;
            _phase = CombatPhase.SupportKilledHostile;
        }
    }

    private bool AreFriendliesReady()
    {
        return IsAliveAtCell(_leadFriendly, LeadAttackCell)
            && IsAliveAtCell(_supportFriendly, SupportAttackCell);
    }

    private void StartSynchronizedAttack()
    {
        _ambushTick = BattleManager?.CurrentTick ?? 0;
        _phase = CombatPhase.SynchronizedAttack;

        _leadFriendly.Stats.AttackRange = CreateRange(new Vector2I(1, 1));
        _leadFriendly.Stats.DetectRange = _leadFriendly.Stats.AttackRange;
        _leadFriendly.Stats.Direction = (_hostile.Stats.Position - _leadFriendly.Stats.Position).Normalized();

        _supportFriendly.Stats.AttackRange = CreateRange(new Vector2I(0, 2));
        _supportFriendly.Stats.DetectRange = _supportFriendly.Stats.AttackRange;
        _supportFriendly.Stats.Direction = new Vector2(0.0f, 1.0f);

        _hostile.Stats.AttackRange = CreateRange(
            new Vector2I(-1, 1),
            new Vector2I(1, 1),
            new Vector2I(0, 2),
            new Vector2I(2, 0));
        _hostile.Stats.DetectRange = _hostile.Stats.AttackRange;
        _hostile.Stats.Direction = new Vector2(0.0f, -1.0f);
    }

    private Var CreateVar(
        Vector2I startCell,
        Color color,
        int maxHealth,
        float moveSpeed,
        int attackDamage,
        int attackFrameInterval,
        VarRange attackRange,
        VarRange detectRange,
        VarStats.Team team)
    {
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
                Direction = new Vector2(0.0f, 1.0f),
                VarTeam = team,
                AttackDamage = attackDamage
            }
        };

        VarManager.AddVar(var);
        CreateRenderer(var, color);
        return var;
    }

    private void DrawGrid(Rect2I area)
    {
        for (int x = area.Position.X; x < area.End.X; x++)
        {
            for (int y = area.Position.Y; y < area.End.Y; y++)
            {
                Vector2 topLeft = Grid.GridToWorld(new Vector2I(x, y)) - Vector2.One * Grid.CellSize / 2.0f;
                DrawRect(new Rect2(topLeft, Vector2.One * Grid.CellSize), new Color(1, 1, 1, 0.06f), false, 1.0f);
            }
        }
    }

    private void DrawPlannedPath(Vector2I startCell, IReadOnlyList<Vector2I> path, Color color)
    {
        Vector2 previous = Grid.GridToWorld(startCell);
        Color lineColor = WithAlpha(color, 0.45f);

        foreach (Vector2I cell in path)
        {
            Vector2 current = Grid.GridToWorld(cell);
            DrawLine(previous, current, lineColor, 3.0f);
            DrawCircle(current, 5.0f, lineColor);
            previous = current;
        }
    }

    private void DrawAmbushCells()
    {
        DrawCellMarker(LeadAttackCell, Colors.OrangeRed, "L");
        DrawCellMarker(SupportAttackCell, Colors.Gold, "S");
        DrawCellMarker(HostileCell, Colors.DeepSkyBlue, "H");
    }

    private void DrawCellMarker(Vector2I cell, Color color, string label)
    {
        Vector2 center = Grid.GridToWorld(cell);
        Vector2 topLeft = center - Vector2.One * Grid.CellSize / 2.0f;
        Color fill = WithAlpha(color, 0.12f);
        DrawRect(new Rect2(topLeft, Vector2.One * Grid.CellSize), fill);
        DrawRect(new Rect2(topLeft, Vector2.One * Grid.CellSize), WithAlpha(color, 0.8f), false, 2.0f);
        DrawString(ThemeDB.FallbackFont, center + new Vector2(-6.0f, 6.0f), label, HorizontalAlignment.Left, -1.0f, 16, color);
    }

    private void CreateRenderer(Var var, Color color)
    {
        _varRenderer ??= new VarRenderer
        {
            BodyRadius = 18.0f,
            BodyColor = Colors.OrangeRed,
            AttackRangeColor = Colors.OrangeRed,
            DetectRangeColor = WithAlpha(Colors.OrangeRed, 0.65f),
            DirectionColor = Colors.White,
            BattleManager = BattleManager,
            RenderVarBody = true,
            RenderAttackRange = true,
            RenderDetectRange = true,
            RenderDirection = true
        };

        if (_varRenderer.GetParent() == null)
        {
            AddChild(_varRenderer);
        }

        _varRenderer.AddVar(var, color, color, WithAlpha(color, 0.65f), Colors.White);
    }

    private void DrawTargetLinks()
    {
        if (!IsAlive(_hostile))
        {
            return;
        }

        DrawTargetLink(_leadFriendly, _hostile, Colors.OrangeRed);
        DrawTargetLink(_supportFriendly, _hostile, Colors.Gold);
    }

    private void DrawTargetLink(Var source, Var target, Color color)
    {
        if (!IsAlive(source) || !IsAlive(target))
        {
            return;
        }

        DrawLine(source.Stats.Position, target.Stats.Position, WithAlpha(color, 0.35f), 2.0f);
    }

    private void DrawHealthBars()
    {
        DrawHealthBar(_leadFriendly, Colors.OrangeRed);
        DrawHealthBar(_supportFriendly, Colors.Gold);
        DrawHealthBar(_hostile, Colors.DeepSkyBlue);
    }

    private void DrawHealthBar(Var var, Color color)
    {
        if (!IsAlive(var))
        {
            return;
        }

        const float width = 42.0f;
        const float height = 5.0f;
        Vector2 topLeft = var.Stats.Position + new Vector2(-width / 2.0f, -32.0f);
        float healthPercent = Mathf.Clamp((float)var.Stats.CurrentHealth / var.Stats.MaxHealth, 0.0f, 1.0f);

        DrawRect(new Rect2(topLeft, new Vector2(width, height)), new Color(0, 0, 0, 0.55f));
        DrawRect(new Rect2(topLeft, new Vector2(width * healthPercent, height)), color);
    }

    private void UpdateInfoLabel()
    {
        if (_infoLabel == null)
        {
            return;
        }

        _infoLabel.Text =
            "Complex out-of-range / synchronized attack test\n" +
            "Orange + Gold: Friendly Vars    Blue: Hostile Var\n" +
            $"Tick: {BattleManager.CurrentTick}    Phase: {GetPhaseText()}\n" +
            $"Orange: {GetVarStatus(_leadFriendly)}    Gold: {GetVarStatus(_supportFriendly)}    Blue: {GetVarStatus(_hostile)}\n" +
            $"Ambush tick: {(_ambushTick >= 0 ? _ambushTick.ToString() : "waiting for both Friendly Vars")}\n" +
            "Flow: Orange reaches diagonal range first, Gold reaches range second, both attack Blue together.\n" +
            "Blue counters Orange until Orange dies; Gold survives and finishes Blue.";
    }

    private string GetPhaseText()
    {
        return _phase switch
        {
            CombatPhase.MovingToAmbush => "Friendlies are moving into staggered attack cells",
            CombatPhase.SynchronizedAttack => "Both Friendly Vars are attacking the Hostile Var",
            CombatPhase.HostileKilledLead => "Hostile killed Orange; Gold is continuing the fight",
            CombatPhase.SupportKilledHostile => "Gold killed the Hostile Var",
            _ => _phase.ToString()
        };
    }

    private string GetVarStatus(Var var)
    {
        if (var == null)
        {
            return "missing";
        }

        if (var.IsDead || var.Stats == null)
        {
            return "dead";
        }

        Vector2I cell = Grid.WorldToGrid(var.Stats.Position);
        return $"{var.Stats.CurrentHealth}/{var.Stats.MaxHealth} hp @ {cell}";
    }

    private static bool IsAlive(Var var)
    {
        return var != null && !var.IsDead && var.Stats != null;
    }

    private static bool IsDead(Var var)
    {
        return var != null && (var.IsDead || var.Stats == null);
    }

    private static bool IsAliveAtCell(Var var, Vector2I cell)
    {
        return IsAlive(var) && Grid.WorldToGrid(var.Stats.Position) == cell;
    }

    private static VarRange CreateRange(params Vector2I[] relativeCells)
    {
        var cells = new Godot.Collections.Array<Vector2I>();
        foreach (Vector2I relativeCell in relativeCells)
        {
            cells.Add(relativeCell);
        }

        return new VarRange
        {
            RelativeCells = cells
        };
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }
}
