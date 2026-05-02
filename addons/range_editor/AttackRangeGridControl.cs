#nullable enable
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Booooom.Editor.RangeEditor;

[Tool]
public partial class AttackRangeGridControl : Control
{
    private const float DefaultCellSize = 48.0f;
    private const float MinCellSize = 20.0f;
    private const float MaxCellSize = 140.0f;

    private readonly HashSet<Vector2I> _selectedLocalCells = new();
    private Vector2 _panPixels = Vector2.Zero;
    private float _cellSize = DefaultCellSize;
    private int _viewRotationQuarterTurns;
    private bool _isPanning;
    private Vector2 _lastMousePosition = Vector2.Zero;
    private Vector2I? _hoveredLocalCell;

    public event Action? SelectionChanged;
    public event Action<Vector2I?>? HoverCellChanged;
    public event Action<int>? ViewRotationChanged;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0.0f, 320.0f);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.Click;
        ClipContents = true;

        MouseExited += OnMouseExited;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
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

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.11f, 0.12f, 0.14f));

        DrawSelectedCells();
        DrawGridLines();
        DrawOriginMarker();
        DrawFacingArrow();
        DrawHoverOutline();
    }

    public void LoadLocalCells(IEnumerable<Vector2I> cells)
    {
        _selectedLocalCells.Clear();

        foreach (Vector2I cell in cells.Distinct())
        {
            _selectedLocalCells.Add(cell);
        }

        QueueRedraw();
    }

    public IReadOnlyList<Vector2I> GetLocalCellsOrdered()
    {
        return _selectedLocalCells
            .OrderByDescending(cell => cell == Vector2I.Zero)
            .ThenByDescending(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToArray();
    }

    public bool IsOriginSelected()
    {
        return _selectedLocalCells.Contains(Vector2I.Zero);
    }

    public void RotateLeft()
    {
        SetViewRotation(_viewRotationQuarterTurns + 3);
    }

    public void RotateHalfTurn()
    {
        SetViewRotation(_viewRotationQuarterTurns + 2);
    }

    public void RotateRight()
    {
        SetViewRotation(_viewRotationQuarterTurns + 1);
    }

    public void ResetView()
    {
        _cellSize = DefaultCellSize;
        _panPixels = Vector2.Zero;
        QueueRedraw();
    }

    public string GetFacingName()
    {
        return _viewRotationQuarterTurns switch
        {
            0 => "Up",
            1 => "Right",
            2 => "Down",
            _ => "Left"
        };
    }

    public void SetViewRotation(int quarterTurns)
    {
        int normalizedTurns = NormalizeQuarterTurns(quarterTurns);
        if (_viewRotationQuarterTurns == normalizedTurns)
        {
            return;
        }

        _viewRotationQuarterTurns = normalizedTurns;
        UpdateHoveredCell(_lastMousePosition);
        ViewRotationChanged?.Invoke(_viewRotationQuarterTurns);
        QueueRedraw();
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        _lastMousePosition = mouseButton.Position;
        UpdateHoveredCell(mouseButton.Position);

        if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
        {
            ZoomAt(mouseButton.Position, 1.1f);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
        {
            ZoomAt(mouseButton.Position, 1.0f / 1.1f);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Middle || mouseButton.ButtonIndex == MouseButton.Right)
        {
            _isPanning = mouseButton.Pressed;
            _lastMousePosition = mouseButton.Position;
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
        {
            ToggleHoveredCell(mouseButton.Position);
            AcceptEvent();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        _lastMousePosition = mouseMotion.Position;

        if (_isPanning)
        {
            _panPixels += mouseMotion.Relative;
            QueueRedraw();
            AcceptEvent();
        }

        UpdateHoveredCell(mouseMotion.Position);
    }

    private void OnMouseExited()
    {
        _hoveredLocalCell = null;
        HoverCellChanged?.Invoke(null);
        QueueRedraw();
    }

    private void ToggleHoveredCell(Vector2 mousePosition)
    {
        Vector2I localCell = ScreenGridToLocal(RoundToScreenGridCell(PixelToScreenGrid(mousePosition)));

        if (!_selectedLocalCells.Add(localCell))
        {
            _selectedLocalCells.Remove(localCell);
        }

        SelectionChanged?.Invoke();
        QueueRedraw();
    }

    private void UpdateHoveredCell(Vector2 mousePosition)
    {
        Vector2I localCell = ScreenGridToLocal(RoundToScreenGridCell(PixelToScreenGrid(mousePosition)));
        if (_hoveredLocalCell == localCell)
        {
            return;
        }

        _hoveredLocalCell = localCell;
        HoverCellChanged?.Invoke(localCell);
        QueueRedraw();
    }

    private void ZoomAt(Vector2 mousePosition, float zoomFactor)
    {
        Vector2 screenGridBeforeZoom = PixelToScreenGrid(mousePosition);
        float newCellSize = Mathf.Clamp(_cellSize * zoomFactor, MinCellSize, MaxCellSize);
        if (Mathf.IsEqualApprox(newCellSize, _cellSize))
        {
            return;
        }

        _cellSize = newCellSize;

        Vector2 center = GetViewportCenter();
        _panPixels = mousePosition - center - ScreenGridToPixelVector(screenGridBeforeZoom);

        QueueRedraw();
    }

    private void DrawSelectedCells()
    {
        foreach (Vector2I localCell in _selectedLocalCells)
        {
            if (localCell == Vector2I.Zero)
            {
                continue;
            }

            Vector2I screenGridCell = LocalToScreenGrid(localCell);
            Rect2 rect = GetCellRect(screenGridCell);
            DrawRect(rect, new Color(0.25f, 0.65f, 1.0f, 0.24f));
            DrawRect(rect, new Color(0.44f, 0.78f, 1.0f), false, 2.0f);
        }
    }

    private void DrawGridLines()
    {
        Vector2 topLeftGrid = PixelToScreenGrid(Vector2.Zero);
        Vector2 bottomRightGrid = PixelToScreenGrid(Size);
        float minX = Mathf.Min(topLeftGrid.X, bottomRightGrid.X);
        float maxX = Mathf.Max(topLeftGrid.X, bottomRightGrid.X);
        float minY = Mathf.Min(topLeftGrid.Y, bottomRightGrid.Y);
        float maxY = Mathf.Max(topLeftGrid.Y, bottomRightGrid.Y);

        int verticalStart = Mathf.FloorToInt(minX - 0.5f) - 1;
        int verticalEnd = Mathf.CeilToInt(maxX + 0.5f) + 1;
        int horizontalStart = Mathf.FloorToInt(minY - 0.5f) - 1;
        int horizontalEnd = Mathf.CeilToInt(maxY + 0.5f) + 1;

        Color regularGridColor = new(0.26f, 0.28f, 0.33f, 0.7f);
        Color axisGridColor = new(0.56f, 0.59f, 0.65f, 0.9f);

        for (int index = verticalStart; index <= verticalEnd; index++)
        {
            float x = GetViewportCenter().X + _panPixels.X + (index + 0.5f) * _cellSize;
            Color lineColor = index is -1 or 0 ? axisGridColor : regularGridColor;
            DrawLine(new Vector2(x, 0.0f), new Vector2(x, Size.Y), lineColor, 1.0f);
        }

        for (int index = horizontalStart; index <= horizontalEnd; index++)
        {
            float y = GetViewportCenter().Y + _panPixels.Y - (index + 0.5f) * _cellSize;
            Color lineColor = index is -1 or 0 ? axisGridColor : regularGridColor;
            DrawLine(new Vector2(0.0f, y), new Vector2(Size.X, y), lineColor, 1.0f);
        }
    }

    private void DrawOriginMarker()
    {
        Rect2 rect = GetCellRect(Vector2I.Zero);
        Color fillColor = IsOriginSelected()
            ? new Color(0.25f, 0.65f, 1.0f, 0.24f)
            : new Color(0.95f, 0.81f, 0.33f, 0.12f);
        Color outlineColor = IsOriginSelected()
            ? new Color(0.44f, 0.78f, 1.0f)
            : new Color(0.95f, 0.81f, 0.33f);

        DrawRect(rect, fillColor);
        DrawRect(rect, outlineColor, false, 2.0f);
        DrawCircle(rect.GetCenter(), _cellSize * 0.18f, outlineColor);
    }

    private void DrawFacingArrow()
    {
        Vector2 originCenter = GetCellRect(Vector2I.Zero).GetCenter();
        Vector2I forwardScreenGrid = LocalToScreenGrid(new Vector2I(0, 1));
        Vector2 direction = ScreenGridToPixelVector(new Vector2(forwardScreenGrid.X, forwardScreenGrid.Y)).Normalized();
        if (direction == Vector2.Zero)
        {
            return;
        }

        Vector2 tip = originCenter + direction * (_cellSize * 0.78f);
        Vector2 tail = originCenter + direction * (_cellSize * 0.16f);
        Vector2 side = new Vector2(-direction.Y, direction.X) * (_cellSize * 0.12f);

        DrawLine(tail, tip, new Color(0.98f, 0.86f, 0.47f), 3.0f);
        DrawColoredPolygon(
            new[]
            {
                tip,
                tip - direction * (_cellSize * 0.24f) + side,
                tip - direction * (_cellSize * 0.24f) - side
            },
            new Color(0.98f, 0.86f, 0.47f),
            Array.Empty<Vector2>(),
            null);
    }

    private void DrawHoverOutline()
    {
        if (_hoveredLocalCell == null)
        {
            return;
        }

        Rect2 rect = GetCellRect(LocalToScreenGrid(_hoveredLocalCell.Value));
        DrawRect(rect, new Color(1.0f, 1.0f, 1.0f, 0.04f));
        DrawRect(rect, new Color(1.0f, 1.0f, 1.0f, 0.8f), false, 1.5f);
    }

    private Rect2 GetCellRect(Vector2I screenGridCell)
    {
        Vector2 cellCenter = GetViewportCenter() + _panPixels + ScreenGridToPixelVector(new Vector2(screenGridCell.X, screenGridCell.Y));
        Vector2 cellSize = Vector2.One * _cellSize;
        return new Rect2(cellCenter - cellSize / 2.0f, cellSize);
    }

    private Vector2 PixelToScreenGrid(Vector2 pixelPosition)
    {
        Vector2 offset = pixelPosition - GetViewportCenter() - _panPixels;
        return new Vector2(offset.X / _cellSize, -offset.Y / _cellSize);
    }

    private Vector2 ScreenGridToPixelVector(Vector2 screenGrid)
    {
        return new Vector2(screenGrid.X * _cellSize, -screenGrid.Y * _cellSize);
    }

    private Vector2I RoundToScreenGridCell(Vector2 screenGrid)
    {
        return new Vector2I(
            Mathf.FloorToInt(screenGrid.X + 0.5f),
            Mathf.FloorToInt(screenGrid.Y + 0.5f));
    }

    private Vector2I LocalToScreenGrid(Vector2I localCell)
    {
        return RotateClockwise(localCell, _viewRotationQuarterTurns);
    }

    private Vector2I ScreenGridToLocal(Vector2I screenGridCell)
    {
        return RotateClockwise(screenGridCell, 4 - _viewRotationQuarterTurns);
    }

    private Vector2 GetViewportCenter()
    {
        return Size / 2.0f;
    }

    private static Vector2I RotateClockwise(Vector2I value, int quarterTurns)
    {
        return NormalizeQuarterTurns(quarterTurns) switch
        {
            0 => value,
            1 => new Vector2I(value.Y, -value.X),
            2 => new Vector2I(-value.X, -value.Y),
            _ => new Vector2I(-value.Y, value.X)
        };
    }

    private static int NormalizeQuarterTurns(int quarterTurns)
    {
        int remainder = quarterTurns % 4;
        return remainder < 0 ? remainder + 4 : remainder;
    }
}
#endif
