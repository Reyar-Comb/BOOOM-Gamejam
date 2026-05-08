using Godot;
using System;
using System.Collections.Generic;
using Cosmosity.Pathfinders;

public partial class MapDataGenerationDemo : Control
{
    [Export] public int MapWidth { get; set; } = 72;
    [Export] public int MapHeight { get; set; } = 44;
    [Export] public int RegionCount { get; set; } = 8;
    [Export] public int RegionSeedTileDistance { get; set; } = 8;
    [Export] public float Randomness { get; set; } = 2.0f;
    [Export] public float CellSize { get; set; } = 14.0f;
    [Export] public Vector2I VarStartCell { get; set; } = new(4, 4);
    [Export] public float VarMoveSpeed { get; set; } = 360.0f;
    [Export] public float TickScale { get; set; } = 1.0f;
    [Export] public Color GridLineColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.18f);
    [Export] public Color BridgeColor { get; set; } = new(1.0f, 0.97f, 0.82f);
    [Export] public Color VarColor { get; set; } = Colors.OrangeRed;
    [Export] public Color PathColor { get; set; } = Colors.White;
    [Export] public Color TargetColor { get; set; } = Colors.LimeGreen;
    [Export] public Color UnreachableTargetColor { get; set; } = Colors.Red;

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

    private MapData _mapData = null!;
    private Button _regenerateButton = null!;
    private Label _statusLabel = null!;
    private VarManager _varManager = null!;
    private AStarPathfinder _pathfinder = null!;
    private Var _controlledVar = null!;
    private List<Vector2I> _activePath = new();
    private Vector2I? _targetCell;
    private bool _targetReachable = false;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsPreset(LayoutPreset.FullRect);

        _varManager = new VarManager();
        AddChild(_varManager);

        _regenerateButton = new Button
        {
            Text = "Regenerate",
            CustomMinimumSize = new Vector2(150.0f, 42.0f)
        };
        _regenerateButton.Pressed += RegenerateMap;
        AddChild(_regenerateButton);

        _statusLabel = new Label
        {
            Text = "Loading...",
            CustomMinimumSize = new Vector2(760.0f, 42.0f)
        };
        AddChild(_statusLabel);

        RegenerateMap();
    }

    public override void _Process(double delta)
    {
        _varManager?.Tick(delta * TickScale);
        TrimActivePath();
        UpdateStatusLabel();
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || !mouseButton.Pressed
            || mouseButton.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (TryPathfindToMouse(mouseButton.Position))
        {
            AcceptEvent();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            PositionUi();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_mapData == null)
        {
            return;
        }

        Rect2 mapRect = GetMapRect();
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.08f, 0.09f, 0.10f));
        DrawMapTiles(mapRect);
        DrawBridges(mapRect);
        DrawMapGrid(mapRect);
        DrawActivePath(mapRect);
        DrawTargetCell(mapRect);
        DrawControlledVar(mapRect);
    }

    private void RegenerateMap()
    {
        _mapData = new MapData(MapWidth, MapHeight, RegionSeedTileDistance);
        _mapData.CreateRegions(RegionCount, Randomness);
        _pathfinder = AStarPathfinder.CreateBuilder()
            .SetMapData(_mapData)
            .UseDiagonal(Pathfinder.DiagonalType.Never)
            .UseHeuristic(Pathfinder.HeuristicType.Manhattan)
            .Build();

        _varManager.Initialize(_mapData);
        EnsureControlledVar();
        ResetControlledVar();

        _activePath.Clear();
        _targetCell = null;
        _targetReachable = false;

        PositionUi();
        UpdateStatusLabel();
        QueueRedraw();
    }

    private void PositionUi()
    {
        if (_regenerateButton != null)
        {
            _regenerateButton.Position = new Vector2(18.0f, 18.0f);
        }

        if (_statusLabel != null)
        {
            _statusLabel.Position = new Vector2(184.0f, 20.0f);
        }
    }

    private void DrawMapTiles(Rect2 mapRect)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int regionId = _mapData.GetRegion(x, y);
                Rect2 tileRect = new(
                    mapRect.Position + new Vector2(x * tileSize.X, y * tileSize.Y),
                    tileSize + Vector2.One);

                DrawRect(tileRect, GetRegionColor(regionId));
            }
        }
    }

    private void DrawMapGrid(Rect2 mapRect)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        for (int x = 0; x <= MapWidth; x++)
        {
            float px = mapRect.Position.X + x * tileSize.X;
            DrawLine(new Vector2(px, mapRect.Position.Y), new Vector2(px, mapRect.End.Y), GridLineColor);
        }

        for (int y = 0; y <= MapHeight; y++)
        {
            float py = mapRect.Position.Y + y * tileSize.Y;
            DrawLine(new Vector2(mapRect.Position.X, py), new Vector2(mapRect.End.X, py), GridLineColor);
        }
    }

    private void DrawBridges(Rect2 mapRect)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        float markerSize = Mathf.Max(4.0f, Mathf.Min(tileSize.X, tileSize.Y) * 0.58f);
        float lineWidth = Mathf.Max(2.0f, Mathf.Min(tileSize.X, tileSize.Y) * 0.18f);

        foreach (MapData.BridgeConnection bridge in _mapData.GetBridges())
        {
            Vector2 centerA = GetTileCenter(mapRect, tileSize, bridge.A);
            Vector2 centerB = GetTileCenter(mapRect, tileSize, bridge.B);
            DrawLine(centerA, centerB, BridgeColor, lineWidth);
            DrawRect(new Rect2(centerA - Vector2.One * markerSize * 0.5f, Vector2.One * markerSize), BridgeColor);
            DrawRect(new Rect2(centerB - Vector2.One * markerSize * 0.5f, Vector2.One * markerSize), BridgeColor);
        }
    }

    private void DrawActivePath(Rect2 mapRect)
    {
        if (_controlledVar?.Stats == null || _activePath.Count == 0)
        {
            return;
        }

        Vector2 previous = WorldToMapPosition(mapRect, _controlledVar.Stats.Position);
        Vector2 tileSize = GetTileSize(mapRect);
        float radius = Mathf.Max(3.0f, Mathf.Min(tileSize.X, tileSize.Y) * 0.18f);
        float lineWidth = Mathf.Max(2.0f, Mathf.Min(tileSize.X, tileSize.Y) * 0.18f);
        Color pathColor = WithAlpha(PathColor, 0.72f);

        foreach (Vector2I cell in _activePath)
        {
            Vector2 current = GetTileCenter(mapRect, tileSize, cell);
            DrawLine(previous, current, pathColor, lineWidth);
            DrawCircle(current, radius, pathColor);
            previous = current;
        }
    }

    private void DrawTargetCell(Rect2 mapRect)
    {
        if (!_targetCell.HasValue)
        {
            return;
        }

        Vector2 tileSize = GetTileSize(mapRect);
        Color color = _targetReachable ? TargetColor : UnreachableTargetColor;
        Rect2 targetRect = GetTileRect(mapRect, tileSize, _targetCell.Value);
        DrawRect(targetRect, WithAlpha(color, 0.24f));
        DrawRect(targetRect, color, false, Mathf.Max(2.0f, Mathf.Min(tileSize.X, tileSize.Y) * 0.16f));
    }

    private void DrawControlledVar(Rect2 mapRect)
    {
        if (_controlledVar?.Stats == null)
        {
            return;
        }

        Vector2 tileSize = GetTileSize(mapRect);
        float tileEdge = Mathf.Min(tileSize.X, tileSize.Y);
        Vector2 center = WorldToMapPosition(mapRect, _controlledVar.Stats.Position);
        float radius = Mathf.Max(6.0f, tileEdge * 0.34f);

        DrawCircle(center, radius + 2.0f, new Color(0.0f, 0.0f, 0.0f, 0.35f));
        DrawCircle(center, radius, VarColor);

        Vector2 direction = _controlledVar.Stats.Direction;
        if (direction.LengthSquared() > MathConstants.EpsilonSquared)
        {
            Vector2 end = center + direction.Normalized() * radius * 1.4f;
            DrawLine(center, end, Colors.White, Mathf.Max(2.0f, tileEdge * 0.12f));
        }
    }

    private bool TryPathfindToMouse(Vector2 mousePosition)
    {
        if (_mapData == null || _pathfinder == null || _controlledVar?.Stats == null)
        {
            return false;
        }

        Rect2 mapRect = GetMapRect();
        if (!mapRect.HasPoint(mousePosition))
        {
            return false;
        }

        Vector2I targetCell = MapPositionToCell(mapRect, mousePosition);
        if (!IsInMap(targetCell))
        {
            return false;
        }

        Vector2I startCell = Grid.WorldToGrid(_controlledVar.Stats.Position);
        _targetCell = targetCell;
        _activePath.Clear();

        List<Vector2I> path = _pathfinder.Run(startCell, targetCell, _mapData.GetRegion(startCell.X, startCell.Y));
        if (path == null || path.Count == 0)
        {
            _targetReachable = false;
            UpdateStatusLabel();
            QueueRedraw();
            return true;
        }

        _targetReachable = true;
        _activePath = path;
        _controlledVar.MoveTo(Grid.GridToWorld(targetCell));
        UpdateStatusLabel();
        QueueRedraw();
        return true;
    }

    private void EnsureControlledVar()
    {
        if (_controlledVar != null)
        {
            return;
        }

        _controlledVar = new Var
        {
            Stats = new VarStats
            {
                MaxHealth = 100,
                AttackSpeedMult = 1.0f,
                AttackFrameInterval = 20,
                MoveSpeed = VarMoveSpeed,
                AttackDamage = 0,
                AttackRange = CreateRange(),
                DetectRange = CreateRange(),
                Direction = Vector2.Right,
                VarTeam = VarStats.Team.Friendly
            }
        };

        _varManager.AddVar(_controlledVar);
    }

    private void ResetControlledVar()
    {
        Vector2I startCell = ClampCellToMap(VarStartCell);
        _controlledVar.Stats.Position = Grid.GridToWorld(startCell);
        _controlledVar.Stats.Direction = Vector2.Right;
        _controlledVar.Stats.MoveSpeed = VarMoveSpeed;
        _controlledVar.SetPath(new List<Vector2I> { startCell });
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null || _controlledVar?.Stats == null || _mapData == null)
        {
            return;
        }

        Vector2I currentCell = Grid.WorldToGrid(_controlledVar.Stats.Position);
        string targetText = _targetCell.HasValue
            ? $"{_targetCell.Value} {(_targetReachable ? $"path {_activePath.Count}" : "unreachable")}"
            : "none";

        _statusLabel.Text =
            $"Var pathfinding test | cell {currentCell} | region {_mapData.GetRegion(currentCell.X, currentCell.Y)} | target {targetText} | left-click a tile";
    }

    private void TrimActivePath()
    {
        if (_controlledVar?.Stats == null || _activePath.Count == 0)
        {
            return;
        }

        while (_activePath.Count > 0
            && _controlledVar.Stats.Position.DistanceSquaredTo(Grid.GridToWorld(_activePath[0])) <= MathConstants.EpsilonSquared)
        {
            _activePath.RemoveAt(0);
        }
    }

    private Rect2 GetMapRect()
    {
        Vector2 requestedSize = new(MapWidth * CellSize, MapHeight * CellSize);
        float maxWidth = Mathf.Max(1.0f, Size.X - 48.0f);
        float maxHeight = Mathf.Max(1.0f, Size.Y - 96.0f);
        float scale = Mathf.Min(maxWidth / requestedSize.X, maxHeight / requestedSize.Y);
        Vector2 mapSize = requestedSize * Mathf.Min(1.0f, scale);
        Vector2 position = new((Size.X - mapSize.X) * 0.5f, Mathf.Max(76.0f, (Size.Y - mapSize.Y) * 0.5f));
        return new Rect2(position, mapSize);
    }

    private Vector2 GetTileSize(Rect2 mapRect)
    {
        return new Vector2(mapRect.Size.X / MapWidth, mapRect.Size.Y / MapHeight);
    }

    private static Rect2 GetTileRect(Rect2 mapRect, Vector2 tileSize, Vector2I cell)
    {
        return new Rect2(
            mapRect.Position + new Vector2(cell.X * tileSize.X, cell.Y * tileSize.Y),
            tileSize);
    }

    private static Vector2 GetTileCenter(Rect2 mapRect, Vector2 tileSize, Vector2I cell)
    {
        return mapRect.Position + new Vector2(
            (cell.X + 0.5f) * tileSize.X,
            (cell.Y + 0.5f) * tileSize.Y);
    }

    private Vector2 WorldToMapPosition(Rect2 mapRect, Vector2 worldPosition)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        return mapRect.Position + new Vector2(
            worldPosition.X / Grid.CellSize * tileSize.X,
            worldPosition.Y / Grid.CellSize * tileSize.Y);
    }

    private Vector2I MapPositionToCell(Rect2 mapRect, Vector2 mapPosition)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        int x = Mathf.FloorToInt((mapPosition.X - mapRect.Position.X) / tileSize.X);
        int y = Mathf.FloorToInt((mapPosition.Y - mapRect.Position.Y) / tileSize.Y);
        return new Vector2I(x, y);
    }

    private bool IsInMap(Vector2I cell)
    {
        return cell.X >= 0 && cell.X < MapWidth && cell.Y >= 0 && cell.Y < MapHeight;
    }

    private Vector2I ClampCellToMap(Vector2I cell)
    {
        return new Vector2I(
            Mathf.Clamp(cell.X, 0, MapWidth - 1),
            Mathf.Clamp(cell.Y, 0, MapHeight - 1));
    }

    private static Color GetRegionColor(int regionId)
    {
        if (regionId <= 0)
        {
            return new Color(0.16f, 0.17f, 0.18f);
        }

        return RegionPalette[(regionId - 1) % RegionPalette.Length];
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

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }
}
