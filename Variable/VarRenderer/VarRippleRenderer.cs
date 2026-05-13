using Godot;
using System;
using System.Collections.Generic;

internal sealed partial class VarRippleRenderer : Control
{
    private sealed class Ripple
    {
        public Vector2I Origin { get; init; }
        public double Elapsed { get; set; }
        public float Duration { get; init; }
        public float Radius { get; init; }
        public float RingWidth { get; init; }
        public float OriginFlashPortion { get; init; }
        public float OutlineWidth { get; init; }
        public Color Color { get; init; }
        public Color OutlineColor { get; init; }
    }

    private readonly VarRenderer _owner;
    private readonly List<Ripple> _ripples = new();
    private VarRendererConfig _config;

    public VarRippleRenderer(VarRenderer owner, VarRendererConfig config)
    {
        Name = nameof(VarRippleRenderer);
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

    public void AddRipple(Vector2I origin)
    {
        if (!_config.RenderClickRipple)
        {
            return;
        }

        _ripples.Add(new Ripple
        {
            Origin = origin,
            Duration = _config.ClickRippleDuration,
            Radius = _config.ClickRippleRadius,
            RingWidth = _config.ClickRippleRingWidth,
            OriginFlashPortion = _config.ClickRippleOriginFlashPortion,
            OutlineWidth = _config.ClickRippleOutlineWidth,
            Color = _config.ClickRippleColor,
            OutlineColor = _config.ClickRippleOutlineColor,
        });
        QueueRedraw();
    }

    public void AddDummyDeathRipple(Vector2I origin)
    {
        if (!_config.RenderDummyDeathRipple)
        {
            return;
        }

        _ripples.Add(new Ripple
        {
            Origin = origin,
            Duration = _config.DummyDeathRippleDuration,
            Radius = _config.DummyDeathRippleRadius,
            RingWidth = _config.DummyDeathRippleRingWidth,
            OriginFlashPortion = _config.DummyDeathRippleOriginFlashPortion,
            OutlineWidth = _config.DummyDeathRippleOutlineWidth,
            Color = _config.DummyDeathRippleColor,
            OutlineColor = _config.DummyDeathRippleOutlineColor,
        });
        QueueRedraw();
    }

    public void AddLogRipple(Vector2I origin, LogType logType)
    {
        if (!_config.RenderLogRipple)
        {
            return;
        }

        (Color color, Color outlineColor) = GetLogRippleColors(logType);
        _ripples.Add(new Ripple
        {
            Origin = origin,
            Duration = _config.LogRippleDuration,
            Radius = _config.LogRippleRadius,
            RingWidth = _config.LogRippleRingWidth,
            OriginFlashPortion = _config.LogRippleOriginFlashPortion,
            OutlineWidth = _config.LogRippleOutlineWidth,
            Color = color,
            OutlineColor = outlineColor,
        });
        QueueRedraw();
    }

    private (Color Color, Color OutlineColor) GetLogRippleColors(LogType logType)
    {
        return logType switch
        {
            LogType.Info => (_config.LogInfoRippleColor, _config.LogInfoRippleOutlineColor),
            LogType.Warning => (_config.LogWarningRippleColor, _config.LogWarningRippleOutlineColor),
            LogType.Error => (_config.LogErrorRippleColor, _config.LogErrorRippleOutlineColor),
            _ => (_config.LogInfoRippleColor, _config.LogInfoRippleOutlineColor),
        };
    }

    public void UpdateRipples(double delta)
    {
        if (_ripples.Count == 0)
        {
            return;
        }

        for (int index = _ripples.Count - 1; index >= 0; index--)
        {
            Ripple ripple = _ripples[index];
            ripple.Elapsed += delta;
            if (ripple.Elapsed >= Mathf.Max(ripple.Duration, VarRenderer.Epsilon))
            {
                _ripples.RemoveAt(index);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_ripples.Count == 0
            || _config.Zoom <= VarRenderer.Epsilon
            || Grid.CellSize <= 0)
        {
            return;
        }

        foreach (Ripple ripple in _ripples)
        {
            DrawRipple(ripple);
        }
    }

    private void DrawRipple(Ripple ripple)
    {
        float duration = Mathf.Max(ripple.Duration, VarRenderer.Epsilon);
        float progress = Mathf.Clamp((float)(ripple.Elapsed / duration), 0.0f, 1.0f);
        float radius = progress * ripple.Radius;
        float ringWidth = Mathf.Max(ripple.RingWidth, 0.25f);
        int maxCellDistance = Mathf.CeilToInt(radius + ringWidth + 1.0f);

        DrawCell(ripple, ripple.Origin, GetOriginFlashAlpha(ripple, progress));

        for (int y = ripple.Origin.Y - maxCellDistance; y <= ripple.Origin.Y + maxCellDistance; y++)
        {
            for (int x = ripple.Origin.X - maxCellDistance; x <= ripple.Origin.X + maxCellDistance; x++)
            {
                Vector2I cell = new(x, y);
                if (cell == ripple.Origin || !_owner.IsCellInsideMap(cell))
                {
                    continue;
                }

                float distance = new Vector2(x - ripple.Origin.X, y - ripple.Origin.Y).Length();
                float distanceFromRing = Mathf.Abs(distance - radius);
                if (distanceFromRing > ringWidth)
                {
                    continue;
                }

                float ringAlpha = 1.0f - distanceFromRing / ringWidth;
                ringAlpha *= 1.0f - progress;
                DrawCell(ripple, cell, ringAlpha);
            }
        }
    }

    private float GetOriginFlashAlpha(Ripple ripple, float progress)
    {
        float flashDuration = Mathf.Max(ripple.OriginFlashPortion, VarRenderer.Epsilon);
        return Mathf.Clamp(1.0f - progress / flashDuration, 0.0f, 1.0f);
    }

    private void DrawCell(Ripple ripple, Vector2I cell, float alphaMultiplier)
    {
        if (alphaMultiplier <= 0.0f || !_owner.IsCellInsideMap(cell))
        {
            return;
        }

        Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(cell));
        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);

        Color fillColor = ripple.Color;
        fillColor.A *= alphaMultiplier;
        DrawRect(cellRect, fillColor);

        Color outlineColor = ripple.OutlineColor;
        outlineColor.A *= alphaMultiplier;
        DrawRect(cellRect, outlineColor, false, Mathf.Max(1.0f, ripple.OutlineWidth * _config.Zoom));
    }
}
