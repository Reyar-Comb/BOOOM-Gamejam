using Godot;
using System;

internal sealed partial class VarGridRenderer : Control
{
    private readonly VarRenderer _owner;
    private VarRendererConfig _config;

    public VarGridRenderer(VarRenderer owner, VarRendererConfig config)
    {
        Name = nameof(VarGridRenderer);
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
        if (_config.Zoom <= VarRenderer.Epsilon || Grid.CellSize <= 0)
        {
            return;
        }

        DrawHoveredGridCell();

        if (!_config.RenderGrid)
        {
            return;
        }

        Vector2 topLeftWorld = _owner.ScreenToWorld(Vector2.Zero);
        Vector2 bottomRightWorld = _owner.ScreenToWorld(_owner.Size);
        Vector2I minCell = Grid.WorldToGrid(new Vector2(
            Mathf.Min(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Min(topLeftWorld.Y, bottomRightWorld.Y)));
        Vector2I maxCell = Grid.WorldToGrid(new Vector2(
            Mathf.Max(topLeftWorld.X, bottomRightWorld.X),
            Mathf.Max(topLeftWorld.Y, bottomRightWorld.Y)));

        int startX = minCell.X - 1;
        int endX = maxCell.X + 1;
        int startY = minCell.Y - 1;
        int endY = maxCell.Y + 1;

        if (_owner.MapData != null)
        {
            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            endX = Math.Min(endX, _owner.MapData.Width - 1);
            endY = Math.Min(endY, _owner.MapData.Height - 1);
        }

        if (startX > endX || startY > endY)
        {
            return;
        }

        float left = GetGridLineScreenX(startX);
        float right = GetGridLineScreenX(endX + 1);
        float top = GetGridLineScreenY(startY);
        float bottom = GetGridLineScreenY(endY + 1);

        for (int x = startX; x <= endX + 1; x++)
        {
            float screenX = GetGridLineScreenX(x);
            Color lineColor = x == 0 ? _config.AxisGridColor : _config.GridColor;
            DrawLine(new Vector2(screenX, top), new Vector2(screenX, bottom), lineColor, 2.0f);
        }

        for (int y = startY; y <= endY + 1; y++)
        {
            float screenY = GetGridLineScreenY(y);
            Color lineColor = y == 0 ? _config.AxisGridColor : _config.GridColor;
            DrawLine(new Vector2(left, screenY), new Vector2(right, screenY), lineColor, 2.0f);
        }
    }

    private void DrawHoveredGridCell()
    {
        if (!_config.RenderHoveredGridCell || !_owner.HoveredGridCell.HasValue)
        {
            return;
        }

        Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(_owner.HoveredGridCell.Value));
        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);
        DrawRect(cellRect, _config.HoveredGridCellColor);
    }

    private float GetGridLineScreenX(int cellX)
    {
        return _owner.WorldToScreen(Grid.GridToWorld(cellX, 0)).X - Grid.CellSize * _config.Zoom / 2.0f;
    }

    private float GetGridLineScreenY(int cellY)
    {
        return _owner.WorldToScreen(Grid.GridToWorld(0, cellY)).Y - Grid.CellSize * _config.Zoom / 2.0f;
    }
}
