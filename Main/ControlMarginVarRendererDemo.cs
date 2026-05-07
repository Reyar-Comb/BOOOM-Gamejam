using Godot;

public partial class ControlMarginVarRendererDemo : Control
{
    private VarRenderer _varRenderer = null!;

    public override void _Ready()
    {
        BuildBattlefieldUi();
        AddDemoVars();
    }

    private void BuildBattlefieldUi()
    {
        var marginContainer = new MarginContainer
        {
            Name = "CenterHalfScreenBattlefield",
            AnchorLeft = 0.25f,
            AnchorTop = 0.25f,
            AnchorRight = 0.75f,
            AnchorBottom = 0.75f,
            OffsetLeft = 0.0f,
            OffsetTop = 0.0f,
            OffsetRight = 0.0f,
            OffsetBottom = 0.0f
        };
        AddChild(marginContainer);

        _varRenderer = new VarRenderer
        {
            Name = "VarRenderer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            DrawBackground = true,
            RenderGrid = true,
            RenderVarBody = true,
            RenderAttackRange = true,
            RenderDetectRange = true,
            RenderDirection = true,
            BodyRadius = 18.0f,
            DirectionLength = 42.0f,
            ViewCenterWorld = Grid.GridToWorld(1, 1),
            Zoom = 1.0f
        };
        marginContainer.AddChild(_varRenderer);
    }

    private void AddDemoVars()
    {
        _varRenderer.AddVar(CreateDemoVar(new Vector2I(0, 0), Vector2.Right), Colors.OrangeRed);
        _varRenderer.AddVar(CreateDemoVar(new Vector2I(3, 1), Vector2.Left), Colors.DeepSkyBlue);
        _varRenderer.AddVar(CreateDemoVar(new Vector2I(1, 3), Vector2.Down), Colors.Gold);
    }

    private static Var CreateDemoVar(Vector2I cell, Vector2 direction)
    {
        return new Var
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = 0.0f,
                AttackDamage = 10,
                AttackRange = CreateRange(new Vector2I(0, 1), new Vector2I(0, 2), new Vector2I(1, 1), new Vector2I(-1, 1)),
                DetectRange = CreateRange(new Vector2I(0, 1), new Vector2I(1, 0), new Vector2I(0, -1), new Vector2I(-1, 0)),
                Position = Grid.GridToWorld(cell),
                Direction = direction,
                VarTeam = VarStats.Team.Neutral
            }
        };
    }

    private static VarRange CreateRange(params Vector2I[] cells)
    {
        var relativeCells = new Godot.Collections.Array<Vector2I>();
        foreach (Vector2I cell in cells)
        {
            relativeCells.Add(cell);
        }

        return new VarRange
        {
            RelativeCells = relativeCells
        };
    }
}
