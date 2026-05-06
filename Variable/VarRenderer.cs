using Godot;
using System;

[GlobalClass]
public partial class VarRenderer : Node2D, IVarRenderer
{
    [Export] public bool RenderVarBody { get; set; } = true;
    [Export] public bool RenderAttackRange { get; set; } = false;
    [Export] public bool RenderDetectRange { get; set; } = false;
    [Export] public bool RenderDirection { get; set; } = false;
    [Export] public bool InterpolateRenderPosition { get; set; } = true;
    [Export] public bool UseBattleManagerInterpolationDuration { get; set; } = true;
    [Export] public BattleManager BattleManager { get; set; } = null!;

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

    private Var _renderedVar = null!;
    private Vector2 _displayPosition;
    private Vector2 _lastObservedPosition;
    private Vector2 _interpolationStartPosition;
    private Vector2 _interpolationTargetPosition;
    private double _interpolationElapsed;
    private double _interpolationDuration;
    private double _timeSinceLastPositionChange;
    private double _timeSinceInterpolationFinished;
    private bool _hasInterpolationState = false;

    public Var RenderedVar
    {
        get => _renderedVar;
        set
        {
            _renderedVar = value;
            ResetInterpolationState();
            QueueRedraw();
        }
    }

    public void SetVar(Var var)
    {
        RenderedVar = var;
    }

    public void ClearVar()
    {
        RenderedVar = null;
    }

    public override void _Process(double delta)
    {
        UpdateRenderPosition(delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_renderedVar?.Stats == null)
        {
            return;
        }

        VarStats stats = _renderedVar.Stats;
        UpdateRenderPosition(0.0);
        Vector2 renderPosition = _displayPosition;

        if (RenderDetectRange)
        {
            DrawRange(stats.DetectRange, stats, renderPosition, DetectRangeColor, DetectRangeFillAlpha);
        }

        if (RenderAttackRange)
        {
            DrawRange(stats.AttackRange, stats, renderPosition, AttackRangeColor, AttackRangeFillAlpha);
        }

        if (RenderVarBody)
        {
            DrawCircle(ToLocal(renderPosition), BodyRadius, BodyColor);
        }

        if (RenderDirection)
        {
            DrawDirection(stats, renderPosition);
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
        Vector2 cellCenter = ToLocal(Grid.GridToWorld(cell));
        Vector2 cellSize = Vector2.One * Grid.CellSize;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
        Color fillColor = color;
        fillColor.A = fillAlpha;

        DrawRect(cellRect, fillColor);
        DrawRect(cellRect, color, false, RangeOutlineWidth);
    }

    private void DrawDirection(VarStats stats, Vector2 renderPosition)
    {
        Vector2 direction = GetDrawableDirection(stats.Direction);
        Vector2 start = ToLocal(renderPosition);
        Vector2 end = ToLocal(renderPosition + direction * DirectionLength);

        DrawLine(start, end, DirectionColor, DirectionLineWidth);

        Vector2 localDirection = end - start;
        if (localDirection.LengthSquared() <= MathConstants.EpsilonSquared)
        {
            return;
        }

        localDirection = localDirection.Normalized();
        Vector2 leftHead = localDirection.Rotated(Mathf.DegToRad(150.0f)) * DirectionHeadLength;
        Vector2 rightHead = localDirection.Rotated(Mathf.DegToRad(-150.0f)) * DirectionHeadLength;

        DrawLine(end, end + leftHead, DirectionColor, DirectionLineWidth);
        DrawLine(end, end + rightHead, DirectionColor, DirectionLineWidth);
    }

    private void UpdateRenderPosition(double delta)
    {
        if (_renderedVar?.Stats == null)
        {
            ResetInterpolationState();
            return;
        }

        VarStats stats = _renderedVar.Stats;
        Vector2 logicalPosition = stats.Position;

        if (!_hasInterpolationState)
        {
            InitializeInterpolationState(logicalPosition);
            return;
        }

        _timeSinceLastPositionChange += delta;

        if (logicalPosition.DistanceSquaredTo(_lastObservedPosition) > MathConstants.EpsilonSquared)
        {
            BeginPositionInterpolation(stats, logicalPosition);
        }

        AdvancePositionInterpolation(delta);
    }

    private void InitializeInterpolationState(Vector2 position)
    {
        _displayPosition = position;
        _lastObservedPosition = position;
        _interpolationStartPosition = position;
        _interpolationTargetPosition = position;
        _interpolationElapsed = 0.0;
        _interpolationDuration = 0.0;
        _timeSinceLastPositionChange = 0.0;
        _timeSinceInterpolationFinished = 0.0;
        _hasInterpolationState = true;
    }

    private void BeginPositionInterpolation(VarStats stats, Vector2 logicalPosition)
    {
        Vector2 previousLogicalPosition = _lastObservedPosition;
        double observedInterval = HasBeenSettledForTooLong() ? 0.0 : _timeSinceLastPositionChange;
        float displayDistance = _displayPosition.DistanceTo(logicalPosition);

        _lastObservedPosition = logicalPosition;
        _timeSinceLastPositionChange = 0.0;
        _timeSinceInterpolationFinished = 0.0;

        if (!InterpolateRenderPosition
            || displayDistance <= MathConstants.EpsilonSquared
            || ShouldSnap(displayDistance))
        {
            SnapToPosition(logicalPosition);
            return;
        }

        _interpolationStartPosition = _displayPosition;
        _interpolationTargetPosition = logicalPosition;
        _interpolationElapsed = 0.0;
        _interpolationDuration = CalculateInterpolationDuration(stats, previousLogicalPosition, logicalPosition, observedInterval);
    }

    private bool ShouldSnap(float distance)
    {
        return SnapDistance > 0.0f && distance > SnapDistance;
    }

    private bool HasBeenSettledForTooLong()
    {
        return IdleInterpolationResetDelay >= 0.0f
            && _timeSinceInterpolationFinished > IdleInterpolationResetDelay;
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

    private void AdvancePositionInterpolation(double delta)
    {
        if (_interpolationDuration <= 0.0)
        {
            _displayPosition = _interpolationTargetPosition;
            _timeSinceInterpolationFinished += delta;
            return;
        }

        if (_interpolationElapsed >= _interpolationDuration)
        {
            _displayPosition = _interpolationTargetPosition;
            _timeSinceInterpolationFinished += delta;
            return;
        }

        _interpolationElapsed = Math.Min(_interpolationElapsed + delta, _interpolationDuration);
        float interpolationWeight = (float)(_interpolationElapsed / _interpolationDuration);
        _displayPosition = _interpolationStartPosition.Lerp(_interpolationTargetPosition, interpolationWeight);
        _timeSinceInterpolationFinished = 0.0;
    }

    private void SnapToPosition(Vector2 position)
    {
        _displayPosition = position;
        _interpolationStartPosition = position;
        _interpolationTargetPosition = position;
        _interpolationElapsed = 0.0;
        _interpolationDuration = 0.0;
        _timeSinceInterpolationFinished = 0.0;
    }

    private void ResetInterpolationState()
    {
        _displayPosition = Vector2.Zero;
        _lastObservedPosition = Vector2.Zero;
        _interpolationStartPosition = Vector2.Zero;
        _interpolationTargetPosition = Vector2.Zero;
        _interpolationElapsed = 0.0;
        _interpolationDuration = 0.0;
        _timeSinceLastPositionChange = 0.0;
        _timeSinceInterpolationFinished = 0.0;
        _hasInterpolationState = false;
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
}
