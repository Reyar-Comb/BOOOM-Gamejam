using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class VarRenderer : Control, IVarRenderer
{
    internal const float Epsilon = 1e-6f;

    [Signal] public delegate void HoveredGridCellChangedEventHandler(Vector2I cell, bool hasCell);

    private VarRendererConfig _config;

    [Export]
    public VarRendererConfig Config
    {
        get => _config;
        set
        {
            _config = value;
            RefreshInjectedConfig();
        }
    }

    [Export] public BattleManager BattleManager { get; set; } = null!;

    private readonly List<Var> _renderedVars = new();
    private readonly VarBackgroundRenderer _backgroundRenderer;
    private readonly VarMapRenderer _mapRenderer;
    private readonly VarGridRenderer _gridRenderer;
    private readonly VarRippleRenderer _rippleRenderer;
    private readonly VarRenderStateTracker _renderStateTracker;
    private readonly VarLayerRenderer _varLayerRenderer;
    private MapData _mapData = null!;
    private Vector2I? _hoveredGridCell;
    private bool _isPanning = false;

    public event Action<Vector2I?> HoveredGridCellUpdated;

    public Vector2I? HoveredGridCell => _hoveredGridCell;
    internal IReadOnlyList<Var> RenderedVars => _renderedVars;
    internal MapData MapData => _mapData;
    internal VarRendererConfig ActiveConfig => _config ??= CreateWritableConfig(_config);

    public VarRenderer()
    {
        _config = CreateWritableConfig(null);
        _backgroundRenderer = new VarBackgroundRenderer(_config);
        _mapRenderer = new VarMapRenderer(this, _config);
        _gridRenderer = new VarGridRenderer(this, _config);
        _rippleRenderer = new VarRippleRenderer(this, _config);
        _renderStateTracker = new VarRenderStateTracker(this, _config);
        _varLayerRenderer = new VarLayerRenderer(this, _renderStateTracker, _config);
    }

    public override void _Ready()
    {
        CacheConfigInMemory();
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.Click;
        ClipContents = true;

        if (Size == Vector2.Zero)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = GetViewportRect().Size;
        }

        AddChild(_backgroundRenderer);
        AddChild(_mapRenderer);
        AddChild(_gridRenderer);
        AddChild(_rippleRenderer);
        AddChild(_varLayerRenderer);

        MouseExited += OnMouseExited;
        QueueRenderersRedraw();
    }

    private void CacheConfigInMemory()
    {
        _config = CreateWritableConfig(_config);
        RefreshInjectedConfig();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            ClampZoomToMapBounds();
            ClampViewCenterToMapBounds();
            QueueRenderersRedraw();
        }
    }

    public void Initialize(MapData mapData)
    {
        _mapData = mapData;
        ClampZoomToMapBounds();
        ClampViewCenterToMapBounds();
        QueueRenderersRedraw();
    }

    public void AddVar(Var var)
    {
        if (var == null || _renderedVars.Contains(var))
        {
            return;
        }
        GD.Print("Adding var to renderer: ");
        _renderedVars.Add(var);
        _varLayerRenderer.SetDefaultStyle(var);
        QueueRenderersRedraw();
    }

    public void AddVar(Var var, Color bodyColor)
    {
        AddVar(var, bodyColor, bodyColor, WithAlpha(bodyColor, ActiveConfig.DetectRangeColor.A), ActiveConfig.DirectionColor);
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

        _varLayerRenderer.SetStyle(var, bodyColor, attackRangeColor, detectRangeColor, directionColor);
        QueueRenderersRedraw();
    }

    public void RemoveVar(Var var)
    {
        if (var == null)
        {
            return;
        }

        _renderedVars.Remove(var);
        _renderStateTracker.Remove(var);
        _varLayerRenderer.RemoveStyle(var);
        QueueRenderersRedraw();
    }

    public void ClearVars()
    {
        _renderedVars.Clear();
        _renderStateTracker.Clear();
        _varLayerRenderer.ClearStyles();
        QueueRenderersRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                UpdateHoveredGridCell(mouseButton.Position);
                TryStartClickRipple(mouseButton);
                break;
            case InputEventMouseMotion mouseMotion:
                UpdateHoveredGridCell(mouseMotion.Position);
                break;
        }

        if (!ActiveConfig.EnableViewControls)
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
            _renderStateTracker.Update(renderedVar, delta);
        }
        _rippleRenderer.UpdateRipples(delta);
        QueueRenderersRedraw();
    }

    public void ResetView()
    {
        ViewCenterWorld = _mapData == null ? Vector2.Zero : GetMapWorldRect().GetCenter();
        Zoom = 1.0f;
        ClampZoomToMapBounds();
        ClampViewCenterToMapBounds();
        QueueRenderersRedraw();
    }

    private void PruneDeadVars()
    {
        for (int index = _renderedVars.Count - 1; index >= 0; index--)
        {
            Var renderedVar = _renderedVars[index];
            if (renderedVar?.IsDead == true || renderedVar?.Stats == null)
            {
                _renderedVars.RemoveAt(index);
                _renderStateTracker.Remove(renderedVar);
                _varLayerRenderer.RemoveStyle(renderedVar);
            }
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
            ZoomAt(mouseButton.Position, ActiveConfig.ZoomStep);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
        {
            ZoomAt(mouseButton.Position, 1.0f / ActiveConfig.ZoomStep);
            AcceptEvent();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Middle || mouseButton.ButtonIndex == MouseButton.Right)
        {
            _isPanning = mouseButton.Pressed;
            AcceptEvent();
        }
    }

    private void TryStartClickRipple(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed || !HoveredGridCell.HasValue)
        {
            return;
        }

        _rippleRenderer.AddRipple(HoveredGridCell.Value);
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        if (!_isPanning)
        {
            return;
        }

        ViewCenterWorld -= mouseMotion.Relative / Zoom;
        ClampViewCenterToMapBounds();
        QueueRenderersRedraw();
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

        Vector2I gridCell = Grid.WorldToGrid(ScreenToWorld(localMousePosition));
        SetHoveredGridCell(IsCellInsideMap(gridCell) ? gridCell : null);
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
        VarRendererConfig config = ActiveConfig;
        float newZoom = Mathf.Clamp(config.Zoom * zoomFactor, GetMinimumAllowedZoom(), config.MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, Zoom))
        {
            return;
        }

        Zoom = newZoom;
        ViewCenterWorld = worldBeforeZoom - (screenPosition - Size / 2.0f) / Zoom;
        ClampViewCenterToMapBounds();
        QueueRenderersRedraw();
    }

    internal bool IsWorldPositionInsideMap(Vector2 worldPosition)
    {
        return IsCellInsideMap(Grid.WorldToGrid(worldPosition));
    }

    internal bool IsCellInsideMap(Vector2I cell)
    {
        return _mapData == null || _mapData.ContainsCell(cell);
    }

    private void ClampZoomToMapBounds()
    {
        VarRendererConfig config = ActiveConfig;
        config.Zoom = Mathf.Clamp(config.Zoom, GetMinimumAllowedZoom(), config.MaxZoom);
    }

    private float GetMinimumAllowedZoom()
    {
        VarRendererConfig config = ActiveConfig;
        if (_mapData == null || Size == Vector2.Zero || Grid.CellSize <= 0)
        {
            return config.MinZoom;
        }

        float mapWorldWidth = _mapData.Width * Grid.CellSize;
        float mapWorldHeight = _mapData.Height * Grid.CellSize;
        if (mapWorldWidth <= Epsilon || mapWorldHeight <= Epsilon)
        {
            return config.MinZoom;
        }

        float mapFitZoom = Mathf.Max(Size.X / mapWorldWidth, Size.Y / mapWorldHeight);
        return Mathf.Min(Mathf.Max(config.MinZoom, mapFitZoom), config.MaxZoom);
    }

    private void ClampViewCenterToMapBounds()
    {
        VarRendererConfig config = ActiveConfig;
        if (_mapData == null || Size == Vector2.Zero || config.Zoom <= Epsilon || Grid.CellSize <= 0)
        {
            return;
        }

        Vector2 halfViewportWorldSize = Size / (2.0f * config.Zoom);
        Rect2 mapWorldRect = GetMapWorldRect();
        Vector2 mapCenter = mapWorldRect.GetCenter();

        float minX = mapWorldRect.Position.X + halfViewportWorldSize.X;
        float maxX = mapWorldRect.End.X - halfViewportWorldSize.X;
        float minY = mapWorldRect.Position.Y + halfViewportWorldSize.Y;
        float maxY = mapWorldRect.End.Y - halfViewportWorldSize.Y;

        ViewCenterWorld = new Vector2(
            minX <= maxX ? Mathf.Clamp(ViewCenterWorld.X, minX, maxX) : mapCenter.X,
            minY <= maxY ? Mathf.Clamp(ViewCenterWorld.Y, minY, maxY) : mapCenter.Y);
    }

    private Rect2 GetMapWorldRect()
    {
        Vector2 topLeft = Grid.GridToWorld(0, 0) - Vector2.One * Grid.CellSize / 2.0f;
        return new Rect2(topLeft, new Vector2(_mapData.Width, _mapData.Height) * Grid.CellSize);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }

    private Vector2 ViewCenterWorld
    {
        get => ActiveConfig.ViewCenterWorld;
        set => ActiveConfig.ViewCenterWorld = value;
    }

    private float Zoom
    {
        get => ActiveConfig.Zoom;
        set => ActiveConfig.Zoom = value;
    }

    private void RefreshInjectedConfig()
    {
        VarRendererConfig config = ActiveConfig;
        _backgroundRenderer.InjectConfig(config);
        _mapRenderer.InjectConfig(config);
        _gridRenderer.InjectConfig(config);
        _rippleRenderer.InjectConfig(config);
        _renderStateTracker.InjectConfig(config);
        _varLayerRenderer.InjectConfig(config);
        QueueRenderersRedraw();
    }

    private void QueueRenderersRedraw()
    {
        QueueRedraw();
        _backgroundRenderer?.QueueRedraw();
        _mapRenderer?.QueueRedraw();
        _gridRenderer?.QueueRedraw();
        _rippleRenderer?.QueueRedraw();
        _varLayerRenderer?.QueueRedraw();
    }

    private static VarRendererConfig CreateWritableConfig(VarRendererConfig source)
    {
        VarRendererConfig defaultConfig = VarRendererConfig.GetDefault();
        if (source == null || ReferenceEquals(source, defaultConfig))
        {
            return (VarRendererConfig)defaultConfig.Duplicate();
        }

        return (VarRendererConfig)source.Duplicate();
    }
}
