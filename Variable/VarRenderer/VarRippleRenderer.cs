using Godot;
using System;
using System.Collections.Generic;

internal sealed partial class VarRippleRenderer : Control
{
    private sealed class Ripple
    {
        public Vector2I Origin { get; init; }
        public double Elapsed { get; set; }
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

        _ripples.Add(new Ripple { Origin = origin });
        QueueRedraw();
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
            if (ripple.Elapsed >= _config.ClickRippleDuration)
            {
                _ripples.RemoveAt(index);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_config.RenderClickRipple
            || _ripples.Count == 0
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
        float duration = Mathf.Max(_config.ClickRippleDuration, VarRenderer.Epsilon);
        float progress = Mathf.Clamp((float)(ripple.Elapsed / duration), 0.0f, 1.0f);
        float radius = progress * _config.ClickRippleRadius;
        float ringWidth = Mathf.Max(_config.ClickRippleRingWidth, 0.25f);
        int maxCellDistance = Mathf.CeilToInt(radius + ringWidth + 1.0f);

        DrawCell(ripple.Origin, GetOriginFlashAlpha(progress));

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
                DrawCell(cell, ringAlpha);
            }
        }
    }

    private float GetOriginFlashAlpha(float progress)
    {
        float flashDuration = Mathf.Max(_config.ClickRippleOriginFlashPortion, VarRenderer.Epsilon);
        return Mathf.Clamp(1.0f - progress / flashDuration, 0.0f, 1.0f);
    }

    private void DrawCell(Vector2I cell, float alphaMultiplier)
    {
        if (alphaMultiplier <= 0.0f || !_owner.IsCellInsideMap(cell))
        {
            return;
        }

        Vector2 cellCenter = _owner.WorldToScreen(Grid.GridToWorld(cell));
        Vector2 cellSize = Vector2.One * Grid.CellSize * _config.Zoom;
        Rect2 cellRect = new(cellCenter - cellSize / 2.0f, cellSize);

        Color fillColor = _config.ClickRippleColor;
        fillColor.A *= alphaMultiplier;
        DrawRect(cellRect, fillColor);

        Color outlineColor = _config.ClickRippleOutlineColor;
        outlineColor.A *= alphaMultiplier;
        DrawRect(cellRect, outlineColor, false, Mathf.Max(1.0f, _config.ClickRippleOutlineWidth * _config.Zoom));
    }
}
