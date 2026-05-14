using Godot;

[GlobalClass]
public partial class VarRendererConfig : Resource
{
    public const string DefaultPath = "res://Variable/VarRenderer/VarRendererConfig.tres";

    private static VarRendererConfig _defaultConfig;

    [Export] public bool DrawBackground { get; set; } = false;
    [Export] public Color BackgroundColor { get; set; } = new(0.08f, 0.09f, 0.11f);
    [Export] public bool RenderMapRegions { get; set; } = true;
    [Export] public bool RenderMapBridges { get; set; } = true;
    [Export] public bool RenderMapFillReveal { get; set; } = true;
    [Export] public Color MapFillRevealStartColor { get; set; } = new(0.02f, 0.025f, 0.035f, 0.65f);
    [Export] public float MapFillRevealCellDelay { get; set; } = 0.001f;
    [Export] public float MapFillRevealCellDuration { get; set; } = 0.16f;
    [Export] public Color OccupiedRegionColor { get; set; } = new(0.28f, 0.72f, 0.42f);
    [Export] public Color UnoccupiedRegionColor { get; set; } = new(0.95f, 0.78f, 0.18f);
    [Export] public Color EnemyBaseRegionColor { get; set; } = new(0.9f, 0.16f, 0.12f);
    [Export] public float RegionFillAlpha { get; set; } = 0.82f;
    [Export] public Color BridgeColor { get; set; } = new(1.0f, 0.97f, 0.82f);
    [Export] public float BridgeLineWidth { get; set; } = 8.0f;
    [Export] public float BridgeMarkerSize { get; set; } = 24.0f;
    [Export] public bool RenderGrid { get; set; } = false;
    [Export] public Color GridColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.08f);
    [Export] public Color AxisGridColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.22f);
    [Export] public bool RenderHoveredGridCell { get; set; } = true;
    [Export] public Color HoveredGridCellColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.12f);
    [Export] public bool RenderClickRipple { get; set; } = true;
    [Export] public Color ClickRippleColor { get; set; } = new(0.64f, 0.88f, 1.0f, 0.38f);
    [Export] public Color ClickRippleOutlineColor { get; set; } = new(0.92f, 0.98f, 1.0f, 0.82f);
    [Export] public float ClickRippleDuration { get; set; } = 0.55f;
    [Export] public float ClickRippleRadius { get; set; } = 5.0f;
    [Export] public float ClickRippleRingWidth { get; set; } = 0.55f;
    [Export] public float ClickRippleOriginFlashPortion { get; set; } = 0.24f;
    [Export] public float ClickRippleOutlineWidth { get; set; } = 2.0f;
    [Export] public bool RenderDummyDeathRipple { get; set; } = true;
    [Export] public Color DummyDeathRippleColor { get; set; } = new(0.82f, 0.82f, 1.0f, 0.44f);
    [Export] public Color DummyDeathRippleOutlineColor { get; set; } = new(0.98f, 0.98f, 1.0f, 0.88f);
    [Export] public float DummyDeathRippleDuration { get; set; } = 0.8f;
    [Export] public float DummyDeathRippleRadius { get; set; } = 8.0f;
    [Export] public float DummyDeathRippleRingWidth { get; set; } = 1.2f;
    [Export] public float DummyDeathRippleOriginFlashPortion { get; set; } = 0.3f;
    [Export] public float DummyDeathRippleOutlineWidth { get; set; } = 3.0f;
    [Export] public bool RenderLogRipple { get; set; } = true;
    [Export] public Color LogInfoRippleColor { get; set; } = new(0.0f, 1.0f, 0.0f, 0.34f);
    [Export] public Color LogInfoRippleOutlineColor { get; set; } = new(0.58f, 1.0f, 0.58f, 0.82f);
    [Export] public Color LogWarningRippleColor { get; set; } = new(1.0f, 0.92f, 0.0f, 0.36f);
    [Export] public Color LogWarningRippleOutlineColor { get; set; } = new(1.0f, 0.98f, 0.54f, 0.86f);
    [Export] public Color LogErrorRippleColor { get; set; } = new(1.0f, 0.0f, 0.0f, 0.38f);
    [Export] public Color LogErrorRippleOutlineColor { get; set; } = new(1.0f, 0.52f, 0.52f, 0.9f);
    [Export] public float LogRippleDuration { get; set; } = 0.75f;
    [Export] public float LogRippleRadius { get; set; } = 7.0f;
    [Export] public float LogRippleRingWidth { get; set; } = 0.9f;
    [Export] public float LogRippleOriginFlashPortion { get; set; } = 0.28f;
    [Export] public float LogRippleOutlineWidth { get; set; } = 2.5f;
    [Export] public bool RenderVarBody { get; set; } = true;
    [Export] public bool RenderAttackRange { get; set; } = false;
    [Export] public bool RenderDetectRange { get; set; } = false;
    [Export] public bool RenderDirection { get; set; } = false;
    [Export] public bool EnableViewControls { get; set; } = true;
    [Export] public bool InterpolateRenderPosition { get; set; } = true;
    [Export] public bool UseBattleManagerInterpolationDuration { get; set; } = true;

    [Export] public Vector2 ViewCenterWorld { get; set; } = Vector2.Zero;
    [Export] public float Zoom { get; set; } = 1.0f;
    [Export] public float MinZoom { get; set; } = 0.25f;
    [Export] public float MaxZoom { get; set; } = 4.0f;
    [Export] public float ZoomStep { get; set; } = 1.1f;

    [Export] public float BodyRadius { get; set; } = 20.0f;
    [Export] public Color BodyColor { get; set; } = Colors.OrangeRed;

    [Export] public Color AttackRangeColor { get; set; } = Colors.OrangeRed;
    [Export] public float AttackRangeFillAlpha { get; set; } = 0.15f;

    [Export] public Color DetectRangeColor { get; set; } = Colors.DeepSkyBlue;
    [Export] public float DetectRangeFillAlpha { get; set; } = 0.08f;

    [Export] public float RangeOutlineWidth { get; set; } = 2.0f;

    [Export] public Color DirectionColor { get; set; } = Colors.White;
    [Export] public float DirectionLength { get; set; } = 34.0f;
    [Export] public float DirectionHeadLength { get; set; } = 10.0f;
    [Export] public float DirectionLineWidth { get; set; } = 3.0f;

    [Export] public float FallbackInterpolationDuration { get; set; } = 0.05f;
    [Export] public float MinimumInterpolationDuration { get; set; } = 0.0f;
    [Export] public float MaximumInterpolationDuration { get; set; } = 2.0f;
    [Export] public float SnapDistance { get; set; } = Grid.CellSize * 4.0f;
    [Export] public float IdleInterpolationResetDelay { get; set; } = 0.25f;

    public static VarRendererConfig GetDefault()
    {
        if (_defaultConfig != null)
        {
            return _defaultConfig;
        }

        _defaultConfig = ResourceLoader.Load<VarRendererConfig>(DefaultPath) ?? new VarRendererConfig();
        return _defaultConfig;
    }
}
