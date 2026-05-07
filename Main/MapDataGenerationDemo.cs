using Godot;
using System;

public partial class MapDataGenerationDemo : Control
{
    [Export] public int MapWidth { get; set; } = 72;
    [Export] public int MapHeight { get; set; } = 44;
    [Export] public int RegionCount { get; set; } = 8;
    [Export] public int RegionSeedTileDistance { get; set; } = 8;
    [Export] public float Randomness { get; set; } = 2.0f;
    [Export] public float CellSize { get; set; } = 14.0f;
    [Export] public Color GridLineColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.18f);

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

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsPreset(LayoutPreset.FullRect);

        _regenerateButton = new Button
        {
            Text = "Regenerate",
            CustomMinimumSize = new Vector2(150.0f, 42.0f)
        };
        _regenerateButton.Pressed += RegenerateMap;
        AddChild(_regenerateButton);

        RegenerateMap();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            PositionButton();
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
        DrawMapGrid(mapRect);
    }

    private void RegenerateMap()
    {
        _mapData = new MapData(MapWidth, MapHeight, RegionSeedTileDistance);
        _mapData.CreateRegions(RegionCount, Randomness);
        PositionButton();
        QueueRedraw();
    }

    private void PositionButton()
    {
        if (_regenerateButton == null)
        {
            return;
        }

        _regenerateButton.Position = new Vector2(18.0f, 18.0f);
    }

    private void DrawMapTiles(Rect2 mapRect)
    {
        Vector2 tileSize = GetTileSize(mapRect);
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                int regionId = _mapData.GetIndex(x, y);
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

    private static Color GetRegionColor(int regionId)
    {
        if (regionId <= 0)
        {
            return new Color(0.16f, 0.17f, 0.18f);
        }

        return RegionPalette[(regionId - 1) % RegionPalette.Length];
    }
}
