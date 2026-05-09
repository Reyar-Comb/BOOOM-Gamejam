using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class VarRenderer : Control, IVarRenderer
{
    private const float Epsilon = 1e-6f;

    [Signal] public delegate void HoveredGridCellChangedEventHandler(Vector2I cell, bool hasCell);

    [Export] public bool DrawBackground { get; set; } = false;
    [Export] public Color BackgroundColor { get; set; } = new(0.08f, 0.09f, 0.11f);
    [Export] public bool RenderGrid { get; set; } = false;
    [Export] public Color GridColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.08f);
    [Export] public Color AxisGridColor { get; set; } = new(1.0f, 1.0f, 1.0f, 0.22f);
    [Export] public bool RenderVarBody { get; set; } = true;
    [Export] public bool RenderAttackRange { get; set; } = false;
    [Export] public bool RenderDetectRange { get; set; } = false;
    [Export] public bool RenderDirection { get; set; } = false;
    [Export] public bool EnableViewControls { get; set; } = true;
    [Export] public bool InterpolateRenderPosition { get; set; } = true;
    [Export] public bool UseBattleManagerInterpolationDuration { get; set; } = true;
    [Export] public BattleManager BattleManager { get; set; } = null!;

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

    private sealed class RenderState
    {
        public Vector2 DisplayPosition;
        public Vector2 LastObservedPosition;
        public Vector2 InterpolationStartPosition;
        public Vector2 InterpolationTargetPosition;
        public double InterpolationElapsed;
        public double InterpolationDuration;
        public double TimeSinceLastPositionChange;
        public double TimeSinceInterpolationFinished;
        public bool HasInterpolationState;
    }

    private sealed class RenderStyle
    {
        public Color BodyColor;
        public Color AttackRangeColor;
        public Color DetectRangeColor;
        public Color DirectionColor;
    }

    private readonly List<Var> _renderedVars = new();
    private readonly Dictionary<Var, RenderState> _renderStatesByVar = new();
    private readonly Dictionary<Var, RenderStyle> _renderStylesByVar = new();
    private Var _renderedVar = null!;
    private Vector2I? _hoveredGridCell;
    private bool _isPanning = false;

    public event Action<Vector2I?> HoveredGridCellUpdated;

    public Vector2I? HoveredGridCell => _hoveredGridCell;

    public Var RenderedVar
    {
        get => _renderedVar;
        set
        {
            _renderedVars.Clear();
            _renderStatesByVar.Clear();
            _renderStylesByVar.Clear();
            _renderedVar = value;
            if (value != null)
            {
                _renderedVars.Add(value);
                _renderStylesByVar[value] = CreateDefaultStyle();
            }
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.Click;
        ClipContents = true;

        if (Size == Vector2.Zero)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = GetViewportRect().Size;
        }

        MouseExited += OnMouseExited;
    }

    public void SetVar(Var var)
    {
        RenderedVar = var;
    }

    public void ClearVar()
    {
        RenderedVar = null;
        QueueRedraw();
    }

    public void AddVar(Var var)
    {
        if (var == null || _renderedVars.Contains(var))
        {
            return;
        }
        GD.Print("Adding var to renderer: ");
        _renderedVars.Add(var);
        _renderStylesByVar[var] = CreateDefaultStyle();
        _renderedVar ??= var;
        QueueRedraw();
    }

    public void AddVar(Var var, Color bodyColor)
    {
        AddVar(var, bodyColor, bodyColor, WithAlpha(bodyColor, DetectRangeColor.A), DirectionColor);
    }

    public void AddVar(Var var, Color bodyColor, Color attackRangeColor, Color detectRangeColor, Color directionColor)
    {
        if (var == null)
        {
            return;
        }

        if (!_renderedVars.Contains(var))
        {
            _renderedVars.Add(var);
        }

        _renderStylesByVar[var] = new RenderStyle
        {
            BodyColor = bodyColor,
            AttackRangeColor = attackRangeColor,
            DetectRangeColor = detectRangeColor,
            DirectionColor = directionColor
        };
        _renderedVar ??= var;
        QueueRedraw();
    }

    public void RemoveVar(Var var)
    {
        if (var == null)
        {
            return;
        }

        _renderedVars.Remove(var);
        _renderStatesByVar.Remove(var);
        _renderStylesByVar.Remove(var);
        if (_renderedVar == var)
        {
            _renderedVar = _renderedVars.Count > 0 ? _renderedVars[0] : null;
        }
        QueueRedraw();
    }

    public void ClearVars()
    {
        _renderedVars.Clear();
        _renderStatesByVar.Clear();
        _renderStylesByVar.Clear();
        _renderedVar = null;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                UpdateHoveredGridCell(mouseButton.Position);
                break;
            case InputEventMouseMotion mouseMotion:
                UpdateHoveredGridCell(mouseMotion.Position);
                break;
        }

        if (!EnableViewControls)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                HandleMouseButton(mouseButton);
                break;
            case InputEventMouseMotion mouseMotion:
                HandleMouseMotion(mouseMotion);
                break;
        }
    }

    public override void _Process(double delta)
    {
        PruneDeadVars();
        foreach (Var renderedVar in _renderedVars)
        {
            UpdateRenderPosition(renderedVar, delta);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (DrawBackground)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);
        }

        if (RenderGrid)
        {
            DrawGrid();
        }

        PruneDeadVars();
        foreach (Var renderedVar in _renderedVars)
        {
            DrawVar(renderedVar);
        }
    }

    private void DrawVar(Var renderedVar)
    {
        if (renderedVar?.Stats == null || renderedVar.IsDead)
        {
            return;
        }

        VarStats stats = renderedVar.Stats;
        UpdateRenderPosition(renderedVar, 0.0);
        Vector2 renderPosition = GetRenderState(renderedVar).DisplayPosition;
        RenderStyle renderStyle = GetRenderStyle(renderedVar);

        if (RenderDetectRange)
        {
            DrawRange(stats.DetectRange, stats, renderPosition, renderStyle.DetectRangeColor, DetectRangeFillAlpha);
        }

        if (RenderAttackRange)
        {
            DrawRange(stats.AttackRange, stats, renderPosition, renderStyle.AttackRangeColor, AttackRangeFillAlpha);
        }

        if (RenderVarBody)
        {
            DrawCircle(WorldToScreen(renderPosition), BodyRadius * Zoom, renderStyle.BodyColor);
        }

        if (RenderDirection)
        {
            DrawDirection(stats, renderPosition, renderStyle.DirectionColor);
        }
    }

    private void DrawRange(VarRange range, VarStats stats, Vector2 renderPosition, Color color, float fillAlpha)
    {
        if (range == null)
        {
            return;
        }

        Vector2I originCell = Grid.WorldToGrid(renderPosition);
        foreach (Vector2I cell in range.EnumerateTargetCells(originCell, stats.Direction))
        {
            DrawRangeCell(cell, color, fillAlpha);
        }
    }

    private void DrawRangeCell(Vector2I cell, Color color, float fillAlpha)
    {
        Vector2 cellCenter = WorldToScreen(Grid.GridToWorld(cell));
        Vector2 cellSize = Vector2.One * Grid.CellSize * Zoom;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
        Color fillColor = color;
        fillColor.A = fillAlpha;

        DrawRect(cellRect, fillColor);
        DrawRect(cellRect, color, false, RangeOutlineWidth * Zoom);
    }

    private void DrawDirection(VarStats stats, Vector2 renderPosition, Color directionColor)
    {
        Vector2 direction = GetDrawableDirection(stats.Direction);
        Vector2 start = WorldToScreen(renderPosition);
        Vector2 end = WorldToScreen(renderPosition + direction * DirectionLength);

        DrawLine(start, end, directionColor, DirectionLineWidth * Zoom);

        Vector2 localDirection = end - start;
        if (localDirection.LengthSquared() <= MathConstants.EpsilonSquared)
        {
            return;
        }

        localDirection = localDirection.Normalized();
        Vector2 leftHead = localDirection.Rotated(Mathf.DegToRad(150.0f)) * DirectionHeadLength;
        Vector2 rightHead = localDirection.Rotated(Mathf.DegToRad(-150.0f)) * DirectionHeadLength;

        DrawLine(end, end + leftHead * Zoom, directionColor, DirectionLineWidth * Zoom);
        DrawLine(end, end + rightHead * Zoom, directionColor, DirectionLineWidth * Zoom);
    }

    private void UpdateRenderPosition(Var renderedVar, double delta)
    {
        if (renderedVar?.Stats == null)
        {
            ResetInterpolationState(renderedVar);
            return;
        }

        RenderState renderState = GetRenderState(renderedVar);
        VarStats stats = renderedVar.Stats;
        Vector2 logicalPosition = stats.Position;

        if (!renderState.HasInterpolationState)
        {
            InitializeInterpolationState(renderState, logicalPosition);
            return;
        }

        renderState.TimeSinceLastPositionChange += delta;

        if (logicalPosition.DistanceSquaredTo(renderState.LastObservedPosition) > MathConstants.EpsilonSquared)
        {
            BeginPositionInterpolation(renderState, stats, logicalPosition);
        }

        AdvancePositionInterpolation(renderState, delta);
    }

    private void InitializeInterpolationState(RenderState renderState, Vector2 position)
    {
        renderState.DisplayPosition = position;
        renderState.LastObservedPosition = position;
        renderState.InterpolationStartPosition = position;
        renderState.InterpolationTargetPosition = position;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
        renderState.HasInterpolationState = true;
    }

    private void BeginPositionInterpolation(RenderState renderState, VarStats stats, Vector2 logicalPosition)
    {
        Vector2 previousLogicalPosition = renderState.LastObservedPosition;
        double observedInterval = HasBeenSettledForTooLong(renderState) ? 0.0 : renderState.TimeSinceLastPositionChange;
        float displayDistance = renderState.DisplayPosition.DistanceTo(logicalPosition);

        renderState.LastObservedPosition = logicalPosition;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;

        if (!InterpolateRenderPosition
            || displayDistance <= MathConstants.EpsilonSquared
            || ShouldSnap(displayDistance))
        {
            SnapToPosition(renderState, logicalPosition);
            return;
        }

        renderState.InterpolationStartPosition = renderState.DisplayPosition;
        renderState.InterpolationTargetPosition = logicalPosition;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = CalculateInterpolationDuration(stats, previousLogicalPosition, logicalPosition, observedInterval);
    }

    private bool ShouldSnap(float distance)
    {
        return SnapDistance > 0.0f && distance > SnapDistance;
    }

    private bool HasBeenSettledForTooLong(RenderState renderState)
    {
        return IdleInterpolationResetDelay >= 0.0f
            && renderState.TimeSinceInterpolationFinished > IdleInterpolationResetDelay;
    }

    private double CalculateInterpolationDuration(VarStats stats, Vector2 previousLogicalPosition, Vector2 logicalPosition, double observedInterval)
    {
        float logicalStepDistance = previousLogicalPosition.DistanceTo(logicalPosition);
        float duration = FallbackInterpolationDuration;

        if (TryGetBattleManagerInterpolationDuration(out double battleManagerDuration))
        {
            duration = (float)battleManagerDuration;
        }
        else if (observedInterval > 0.0)
        {
            duration = (float)observedInterval;
        }
        else if (stats.MoveSpeed > 0.001f && logicalStepDistance > 0.001f)
        {
            duration = logicalStepDistance / stats.MoveSpeed;
        }

        float minimumDuration = Mathf.Max(0.0f, MinimumInterpolationDuration);
        float maximumDuration = Mathf.Max(minimumDuration, MaximumInterpolationDuration);
        return Mathf.Clamp(duration, minimumDuration, maximumDuration);
    }

    private bool TryGetBattleManagerInterpolationDuration(out double duration)
    {
        duration = 0.0;
        if (!UseBattleManagerInterpolationDuration || BattleManager == null || BattleManager.TickScale <= 0.0f)
        {
            return false;
        }

        duration = BattleManager.TickInterval / BattleManager.TickScale;
        return duration > 0.0;
    }

    private void AdvancePositionInterpolation(RenderState renderState, double delta)
    {
        if (renderState.InterpolationDuration <= 0.0)
        {
            renderState.DisplayPosition = renderState.InterpolationTargetPosition;
            renderState.TimeSinceInterpolationFinished += delta;
            return;
        }

        if (renderState.InterpolationElapsed >= renderState.InterpolationDuration)
        {
            renderState.DisplayPosition = renderState.InterpolationTargetPosition;
            renderState.TimeSinceInterpolationFinished += delta;
            return;
        }

        renderState.InterpolationElapsed = Math.Min(renderState.InterpolationElapsed + delta, renderState.InterpolationDuration);
        float interpolationWeight = (float)(renderState.InterpolationElapsed / renderState.InterpolationDuration);
        renderState.DisplayPosition = renderState.InterpolationStartPosition.Lerp(renderState.InterpolationTargetPosition, interpolationWeight);
        renderState.TimeSinceInterpolationFinished = 0.0;
    }

    private void SnapToPosition(RenderState renderState, Vector2 position)
    {
        renderState.DisplayPosition = position;
        renderState.InterpolationStartPosition = position;
        renderState.InterpolationTargetPosition = position;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
    }

    private void ResetInterpolationState(Var renderedVar)
    {
        if (renderedVar == null || !_renderStatesByVar.TryGetValue(renderedVar, out RenderState renderState))
        {
            return;
        }

        renderState.DisplayPosition = Vector2.Zero;
        renderState.LastObservedPosition = Vector2.Zero;
        renderState.InterpolationStartPosition = Vector2.Zero;
        renderState.InterpolationTargetPosition = Vector2.Zero;
        renderState.InterpolationElapsed = 0.0;
        renderState.InterpolationDuration = 0.0;
        renderState.TimeSinceLastPositionChange = 0.0;
        renderState.TimeSinceInterpolationFinished = 0.0;
        renderState.HasInterpolationState = false;
    }

    public void ResetView()
    {
        ViewCenterWorld = _renderedVar?.Stats?.Position ?? Vector2.Zero;
        Zoom = 1.0f;
        QueueRedraw();
    }

    private RenderState GetRenderState(Var renderedVar)
    {
        if (!_renderStatesByVar.TryGetValue(renderedVar, out RenderState renderState))
        {
            renderState = new RenderState();
            _renderStatesByVar[renderedVar] = renderState;
        }

        return renderState;
    }

    private RenderStyle GetRenderStyle(Var renderedVar)
    {
        if (!_renderStylesByVar.TryGetValue(renderedVar, out RenderStyle renderStyle))
        {
            renderStyle = CreateDefaultStyle();
            _renderStylesByVar[renderedVar] = renderStyle;
        }

        return renderStyle;
    }

    private RenderStyle CreateDefaultStyle()
    {
        return new RenderStyle
        {
            BodyColor = BodyColor,
            AttackRangeColor = AttackRangeColor,
            DetectRangeColor = DetectRangeColor,
            DirectionColor = DirectionColor
        };
    }

    private void PruneDeadVars()
    {
        for (int index = _renderedVars.Count - 1; index >= 0; index--)
        {
            Var renderedVar = _renderedVars[index];
            if (renderedVar?.IsDead == true || renderedVar?.Stats == null)
            {
                _renderedVars.RemoveAt(index);
                _renderStatesByVar.Remove(renderedVar);
                _renderStylesByVar.Remove(renderedVar);
            }
        }

        if (_renderedVar?.IsDead == true || _renderedVar?.Stats == null)
        {
            _renderedVar = _renderedVars.Count > 0 ? _renderedVars[0] : null;
        }
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Size / 2.0f + (worldPosition - ViewCenterWorld) * Zoom;
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        if (Zoom <= Epsilon)
        {
            return ViewCenterWorld;
        }

        return ViewCenterWorld + (screenPosition - Size / 2.0f) / Zoom;
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
        {
            ZoomAt(mouseButton.Position, ZoomStep);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
        {
            ZoomAt(mouseButton.Position, 1.0f / ZoomStep);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Middle || mouseButton.ButtonIndex == MouseButton.Right)
        {
            _isPanning = mouseButton.Pressed;
            AcceptEvent();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        if (!_isPanning)
        {
            return;
        }

        ViewCenterWorld -= mouseMotion.Relative / Zoom;
        QueueRedraw();
        AcceptEvent();
    }

    private void OnMouseExited()
    {
        SetHoveredGridCell(null);
    }

    private void UpdateHoveredGridCell(Vector2 localMousePosition)
    {
        if (!new Rect2(Vector2.Zero, Size).HasPoint(localMousePosition))
        {
            SetHoveredGridCell(null);
            return;
        }

        SetHoveredGridCell(Grid.WorldToGrid(ScreenToWorld(localMousePosition)));
    }

    private void SetHoveredGridCell(Vector2I? gridCell)
    {
        if (_hoveredGridCell == gridCell)
        {
            return;
        }

        _hoveredGridCell = gridCell;
        HoveredGridCellUpdated?.Invoke(_hoveredGridCell);

        Vector2I signalCell = _hoveredGridCell ?? Vector2I.Zero;
        EmitSignal(SignalName.HoveredGridCellChanged, signalCell, _hoveredGridCell.HasValue);
    }

    private void ZoomAt(Vector2 screenPosition, float zoomFactor)
    {
        Vector2 worldBeforeZoom = ScreenToWorld(screenPosition);
        float newZoom = Mathf.Clamp(Zoom * zoomFactor, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, Zoom))
        {
            return;
        }

        Zoom = newZoom;
        ViewCenterWorld = worldBeforeZoom - (screenPosition - Size / 2.0f) / Zoom;
        QueueRedraw();
    }

    private void DrawGrid()
    {
        if (Zoom <= Epsilon || Grid.CellSize <= 0)
        {
            return;
        }

        Vector2 topLeftWorld = ScreenToWorld(Vector2.Zero);
        Vector2 bottomRightWorld = ScreenToWorld(Size);
        Vector2I minCell = Grid.WorldToGrid(new Vector2(
            Mathf.Min(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Min(topLeftWorld.Y, bottomRightWorld.Y)));
        Vector2I maxCell = Grid.WorldToGrid(new Vector2(
            Mathf.Max(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Max(topLeftWorld.Y, bottomRightWorld.Y)));

        for (int x = minCell.X - 1; x <= maxCell.X + 1; x++)
        {
            for (int y = minCell.Y - 1; y <= maxCell.Y + 1; y++)
            {
                Vector2 cellCenter = WorldToScreen(Grid.GridToWorld(x, y));
                Vector2 cellSize = Vector2.One * Grid.CellSize * Zoom;
                Rect2 rect = new(cellCenter - cellSize / 2.0f, cellSize);
                Color lineColor = x == 0 || y == 0 ? AxisGridColor : GridColor;
                DrawRect(rect, lineColor, false, 1.0f);
            }
        }
    }

    private static Vector2 GetDrawableDirection(Vector2 direction)
    {
        if (direction.LengthSquared() > MathConstants.EpsilonSquared)
        {
            return direction.Normalized();
        }

        Vector2I facingDirection = direction.ToFacingDirection();
        return new Vector2(facingDirection.X, facingDirection.Y);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }
}
