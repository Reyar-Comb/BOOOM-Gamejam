using Godot;
using System;

internal sealed partial class VarMapRenderer : Control
{
    private static readonly Color[] RegionPalette =
    [
        new(0.90f, 0.28f, 0.24f),
        new(0.18f, 0.62f, 0.87f),
        new(0.28f, 0.72f, 0.42f),
        new(0.95f, 0.68f, 0.22f),
        new(0.58f, 0.42f, 0.86f),
        new(0.19f, 0.75f, 0.68f),
        new(0.91f, 0.40f, 0.65f),
        new(0.62f, 0.72f, 0.22f),
        new(0.45f, 0.56f, 0.96f),
        new(0.86f, 0.47f, 0.18f),
    ];

    private readonly VarRenderer _owner;
    private VarRendererConfig _config;

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
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_owner.MapData == null || _config.Zoom <= VarRenderer.Epsilon || Grid.CellSize <= 0)
        {
            return;
        }

        if (_config.RenderMapRegions)
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
        Color color = regionId <= 0
            ? _config.EmptyRegionColor
            : RegionPalette[(regionId - 1) % RegionPalette.Length];
        color.A = _config.RegionFillAlpha;
        return color;
    }
}
