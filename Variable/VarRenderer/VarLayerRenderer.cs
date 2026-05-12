using Godot;
using System.Collections.Generic;

internal sealed partial class VarLayerRenderer : Control
{
    private readonly VarRenderer _owner;
    private readonly VarRenderStateTracker _renderStateTracker;
    private readonly Dictionary<Var, VarRenderStyle> _renderStylesByVar = new();
    private VarRendererConfig _config;

    public VarLayerRenderer(VarRenderer owner, VarRenderStateTracker renderStateTracker, VarRendererConfig config)
    {
        Name = nameof(VarLayerRenderer);
        _owner = owner;
        _renderStateTracker = renderStateTracker;
        _config = config;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public void InjectConfig(VarRendererConfig config)
    {
        _config = config;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (Var renderedVar in _owner.RenderedVars)
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
        _renderStateTracker.Update(renderedVar, 0.0);
        Vector2 renderPosition = _renderStateTracker.Get(renderedVar).DisplayPosition;
        if (!_owner.IsWorldPositionInsideMap(renderPosition))
        {
            return;
        }

        VarRenderStyle renderStyle = GetStyle(renderedVar);

        if (_config.RenderDetectRange)
        {
            DrawRange(stats.DetectRange, stats, renderPosition, renderStyle.DetectRangeColor, _config.DetectRangeFillAlpha);
        }

        if (_config.RenderAttackRange)
        {
            DrawRange(stats.AttackRange, stats, renderPosition, renderStyle.AttackRangeColor, _config.AttackRangeFillAlpha);
        }

        if (_config.RenderVarBody)
        {
            DrawCircle(_owner.WorldToScreen(renderPosition), _config.BodyRadius * _config.Zoom, renderStyle.BodyColor);
        }

        if (_config.RenderDirection)
        {
            DrawDirection(stats, renderPosition, renderStyle.DirectionColor);
        }
    }

    public void SetDefaultStyle(Var renderedVar)
    {
        if (renderedVar != null)
        {
            _renderStylesByVar[renderedVar] = CreateDefaultStyle();
        }
    }

    public void SetStyle(Var renderedVar, Color bodyColor, Color attackRangeColor, Color detectRangeColor, Color directionColor)
    {
        if (renderedVar == null)
        {
            return;
        }

        _renderStylesByVar[renderedVar] = new VarRenderStyle
        {
            BodyColor = bodyColor,
            AttackRangeColor = attackRangeColor,
            DetectRangeColor = detectRangeColor,
            DirectionColor = directionColor
        };
    }

    public void RemoveStyle(Var renderedVar)
    {
        if (renderedVar != null)
        {
            _renderStylesByVar.Remove(renderedVar);
        }
    }

    public void ClearStyles()
    {
        _renderStylesByVar.Clear();
    }

    private VarRenderStyle GetStyle(Var renderedVar)
    {
        if (!_renderStylesByVar.TryGetValue(renderedVar, out VarRenderStyle renderStyle))
        {
            renderStyle = CreateDefaultStyle();
            _renderStylesByVar[renderedVar] = renderStyle;
        }

        return renderStyle;
    }

    private VarRenderStyle CreateDefaultStyle()
    {
        return new VarRenderStyle
        {
            BodyColor = _config.BodyColor,
            AttackRangeColor = _config.AttackRangeColor,
            DetectRangeColor = _config.DetectRangeColor,
            DirectionColor = _config.DirectionColor
        };
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
        if (!_owner.IsCellInsideMap(cell))
        {
            return;
        }

        Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(cell));
        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
        Color fillColor = color;
        fillColor.A = fillAlpha;

        DrawRect(cellRect, fillColor);
        DrawRect(cellRect, color, false, _config.RangeOutlineWidth * _config.Zoom);
    }

    private void DrawDirection(VarStats stats, Vector2 renderPosition, Color directionColor)
    {
        Vector2 direction = GetDrawableDirection(stats.Direction);
        Vector2 start = _owner.WorldToScreen(renderPosition);
        Vector2 end = _owner.WorldToScreen(renderPosition + direction * _config.DirectionLength);

        DrawLine(start, end, directionColor, _config.DirectionLineWidth * _config.Zoom);

        Vector2 localDirection = end - start;
        if (localDirection.LengthSquared() <= MathConstants.EpsilonSquared)
        {
            return;
        }

        localDirection = localDirection.Normalized();
        Vector2 leftHead = localDirection.Rotated(Mathf.DegToRad(150.0f)) * _config.DirectionHeadLength;
        Vector2 rightHead = localDirection.Rotated(Mathf.DegToRad(-150.0f)) * _config.DirectionHeadLength;

        DrawLine(end, end + leftHead * _config.Zoom, directionColor, _config.DirectionLineWidth * _config.Zoom);
        DrawLine(end, end + rightHead * _config.Zoom, directionColor, _config.DirectionLineWidth * _config.Zoom);
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
