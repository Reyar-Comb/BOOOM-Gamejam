using Godot;
using System;

public static class Grid
{
    private static float _offsetX = 0;
    private static float _offsetY = 0;
    private const int CellSize = 50;
    public static void SetOffset(float x, float y)
    {
        _offsetX = x;
        _offsetY = y;
    }
    public static Vector2I WorldToGrid(Vector2 worldPos)
    {
        int x = (int)Math.Floor((worldPos.X - _offsetX) / CellSize);
        int y = (int)Math.Floor((worldPos.Y - _offsetY) / CellSize);
        return new Vector2I(x, y);
    }
    public static Vector2I WorldToGrid(float x, float y)
    {
        int gridX = (int)Math.Floor((x - _offsetX) / CellSize);
        int gridY = (int)Math.Floor((y - _offsetY) / CellSize);
        return new Vector2I(gridX, gridY);
    }
    public static Vector2 GridToWorld(Vector2I gridPos)
    {
        float x = gridPos.X * CellSize + _offsetX + CellSize / 2f;
        float y = gridPos.Y * CellSize + _offsetY + CellSize / 2f;
        return new Vector2(x, y);
    }
    public static Vector2 GridToWorld(int x, int y)
    {
        float gridX = x * CellSize + _offsetX + CellSize / 2f;
        float gridY = y * CellSize + _offsetY + CellSize / 2f;
        return new Vector2(gridX, gridY);
    }
}
