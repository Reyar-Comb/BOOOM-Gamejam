using Godot;
using System;

internal sealed partial class VarMapRenderer : Control
{
    private readonly VarRenderer _owner;
    private VarRendererConfig _config;
    private float _revealElapsed;
    private bool _revealCompleted;

    public VarMapRenderer(VarRenderer owner, VarRendererConfig config)
    {
        Name = nameof(VarMapRenderer);
        _owner = owner;
        _config = config;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public void InjectConfig(VarRendererConfig config)
    {
        _config = config;
        RestartReveal();
        QueueRedraw();
    }

    public void RestartReveal()
    {
        _revealElapsed = 0.0f;
        _revealCompleted = !_config.RenderMapFillReveal;
    }

    public void UpdateReveal(double delta)
    {
        if (_revealCompleted || !_config.RenderMapFillReveal)
        {
            return;
        }

        if (_owner.MapData == null)
        {
            return;
        }

        _revealElapsed += (float)delta;
        float totalDuration = GetCellStartDelay(_owner.MapData.Width - 1, _owner.MapData.Height - 1)
            + Mathf.Max(_config.MapFillRevealCellDuration, VarRenderer.Epsilon);
        _revealCompleted = _revealElapsed >= totalDuration;
    }

    public override void _Draw()
    {
        if (_owner.MapData == null || _config.Zoom <= VarRenderer.Epsilon || Grid.CellSize <= 0)
        {
            return;
        }

        if (_config.RenderMapFillReveal)
        {
            DrawFillReveal();
        }
        else if (_config.RenderMapRegions)
        {
            DrawRegions();
        }

        if (_config.RenderMapBridges)
        {
            DrawBridges();
        }
    }

    private void DrawRegions()
    {
        if (!TryGetVisibleCellBounds(out int startX, out int endX, out int startY, out int endY))
        {
            return;
        }

        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(x, y));
                Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize + Vector2.One);
                DrawRect(cellRect, GetRegionColor(_owner.MapData.GetRegion(x, y)));
            }
        }
    }

    private void DrawFillReveal()
    {
        if (_revealCompleted)
        {
            if (_config.RenderMapRegions)
            {
                DrawRegions();
            }

            return;
        }

        if (_config.RenderMapRegions)
        {
            DrawRegions();
        }

        if (!TryGetVisibleCellBounds(out int startX, out int endX, out int startY, out int endY))
        {
            return;
        }

        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        float cellDuration = Mathf.Max(_config.MapFillRevealCellDuration, VarRenderer.Epsilon);
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                float progress = GetCellRevealProgress(x, y, cellDuration);
                if (progress >= 1.0f)
                {
                    continue;
                }

                Vector2 revealSize = cellSize * EaseOutCubic(progress);
                Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(x, y));
                Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize + Vector2.One);
                Rect2 revealRect = new(cellCenter - revealSize / 2.0f, revealSize);
                DrawUnrevealedCellArea(cellRect, revealRect, GetRevealCoverColor());
            }
        }
    }

    private Color GetRevealCoverColor()
    {
        return _config.RenderMapRegions ? _config.BackgroundColor : _config.MapFillRevealStartColor;
    }

    private float GetCellRevealProgress(int x, int y, float cellDuration)
    {
        if (_revealCompleted)
        {
            return 1.0f;
        }

        float cellElapsed = _revealElapsed - GetCellStartDelay(x, y);
        return Mathf.Clamp(cellElapsed / cellDuration, 0.0f, 1.0f);
    }

    private float GetCellStartDelay(int x, int y)
    {
        return Math.Max(0.0f, _config.MapFillRevealCellDelay) * (x + y * _owner.MapData.Width);
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1.0f - Mathf.Clamp(value, 0.0f, 1.0f);
        return 1.0f - inverse * inverse * inverse;
    }

    private void DrawUnrevealedCellArea(Rect2 cellRect, Rect2 revealRect, Color coverColor)
    {
        float left = cellRect.Position.X;
        float top = cellRect.Position.Y;
        float right = cellRect.End.X;
        float bottom = cellRect.End.Y;

        float revealLeft = Mathf.Clamp(revealRect.Position.X, left, right);
        float revealTop = Mathf.Clamp(revealRect.Position.Y, top, bottom);
        float revealRight = Mathf.Clamp(revealRect.End.X, left, right);
        float revealBottom = Mathf.Clamp(revealRect.End.Y, top, bottom);

        DrawRect(new Rect2(new Vector2(left, top), new Vector2(right - left, revealTop - top)), coverColor);
        DrawRect(new Rect2(new Vector2(left, revealBottom), new Vector2(right - left, bottom - revealBottom)), coverColor);
        DrawRect(new Rect2(new Vector2(left, revealTop), new Vector2(revealLeft - left, revealBottom - revealTop)), coverColor);
        DrawRect(new Rect2(new Vector2(revealRight, revealTop), new Vector2(right - revealRight, revealBottom - revealTop)), coverColor);
    }

    private void DrawBridges()
    {
        float markerSize = Mathf.Max(2.0f, _config.BridgeMarkerSize * _config.Zoom);
        float lineWidth = Mathf.Max(1.0f, _config.BridgeLineWidth * _config.Zoom);

        foreach (MapData.BridgeConnection bridge in _owner.MapData.GetBridges())
        {
            if (!_owner.IsCellInsideMap(bridge.A) || !_owner.IsCellInsideMap(bridge.B))
            {
                continue;
            }

            Vector2 centerA = _owner.WorldToScreen(Grid.GridToWorld(bridge.A));
            Vector2 centerB = _owner.WorldToScreen(Grid.GridToWorld(bridge.B));

            if (!IsPointNearViewport(centerA, markerSize) && !IsPointNearViewport(centerB, markerSize))
            {
                continue;
            }

            DrawLine(centerA, centerB, _config.BridgeColor, lineWidth);
            DrawMarker(centerA, markerSize);
            DrawMarker(centerB, markerSize);
        }
    }

    private void DrawMarker(Vector2 center, float markerSize)
    {
        Rect2 markerRect = new(center - Vector2.One * markerSize * 0.5f, Vector2.One * markerSize);
        DrawRect(markerRect, _config.BridgeColor);
    }

    private bool TryGetVisibleCellBounds(out int startX, out int endX, out int startY, out int endY)
    {
        Vector2 topLeftWorld = _owner.ScreenToWorld(Vector2.Zero);
        Vector2 bottomRightWorld = _owner.ScreenToWorld(_owner.Size);
        Vector2I minCell = Grid.WorldToGrid(new Vector2(
            Mathf.Min(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Min(topLeftWorld.Y, bottomRightWorld.Y)));
        Vector2I maxCell = Grid.WorldToGrid(new Vector2(
            Mathf.Max(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Max(topLeftWorld.Y, bottomRightWorld.Y)));

        startX = Math.Max(minCell.X - 1, 0);
        startY = Math.Max(minCell.Y - 1, 0);
        endX = Math.Min(maxCell.X + 1, _owner.MapData.Width - 1);
        endY = Math.Min(maxCell.Y + 1, _owner.MapData.Height - 1);
        return startX <= endX && startY <= endY;
    }

    private bool IsPointNearViewport(Vector2 point, float margin)
    {
        return point.X >= -margin
            && point.Y >= -margin
            && point.X <= Size.X + margin
            && point.Y <= Size.Y + margin;
    }

    private Color GetRegionColor(int regionId)
    {
        Color color = regionId > 0 && _owner.MapData.GetRegionOccupied(regionId)
            ? _config.OccupiedRegionColor
            : _config.UnoccupiedRegionColor;
        color.A = _config.RegionFillAlpha;
        return color;
    }
}
