using Godot;
using System;
using System.Collections.Generic;

internal sealed partial class VarPieceRenderer : Control
{
    private sealed class Piece
    {
        public Texture2D Texture { get; init; }
        public Var Var { get; init; }


        public Vector2 DisplayPosition { get; set; }

        public Vector2 TargetPosition { get; set; }
        public bool IsAnimating { get; set; }
        public float Elapsed { get; set; }


        public float CornerRadius { get; init; }
        public Color Color { get; init; }
        public Color BorderColor { get; init; }
        public float BorderWidth { get; init; }
    }

    private readonly VarRenderer _owner;
    private readonly Dictionary<Var, Piece> _pieces = new();
    private VarRendererConfig _config;

    public VarPieceRenderer(VarRenderer owner, VarRendererConfig config)
    {
        Name = nameof(VarPieceRenderer);
        _owner = owner;
        _config = config;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Ready()
    {
        SetProcess(true);
    }

    public void InjectConfig(VarRendererConfig config)
    {
        _config = config;
        QueueRedraw();
    }


    public override void _Process(double delta)
    {
        bool anyAnimating = false;

        foreach (var piece in _pieces.Values)
        {
            if (!piece.IsAnimating)
                continue;

            float duration = _config.VarPieceAnimationDuration;
            piece.Elapsed += (float)delta;

            if (piece.Elapsed >= duration)
            {
                piece.DisplayPosition = piece.TargetPosition;
                piece.IsAnimating = false;
            }
            else
            {
                float t = piece.Elapsed / duration;
                piece.DisplayPosition = piece.DisplayPosition.Lerp(piece.TargetPosition, t);
            }

            anyAnimating = true;
        }

        if (anyAnimating)
            QueueRedraw();
    }


    public override void _Draw()
    {
        if (_config.Zoom <= VarRenderer.Epsilon)
        {
            return;
        }

        foreach (var piece in _pieces.Values)
        {
            DrawPiece(piece);
        }
    }

    private void DrawPiece(Piece piece)
    {
        // 1. 世界坐标 → 屏幕坐标（与 VarLayerRenderer 一致，自动跟随平移/缩放）
        Vector2 screenCenter = _owner.WorldToScreen(piece.DisplayPosition);

        // 2. 方块尺寸（世界空间 × 缩放 = 屏幕像素）
        float squareWorldSize = _config.VarPieceGridSize.X;
        float squareScreenSize = squareWorldSize * _config.Zoom;
        float halfSquare = squareScreenSize / 2f;

        // 3. 先画方块底衬
        var squareRect = new Rect2(
            screenCenter.X - halfSquare,
            screenCenter.Y - halfSquare,
            squareScreenSize,
            squareScreenSize);

        var pieceStyle = new StyleBoxFlat();
        pieceStyle.BgColor = piece.Color;
        pieceStyle.BorderColor = piece.BorderColor;
        pieceStyle.SetBorderWidthAll((int)piece.BorderWidth);
        pieceStyle.SetCornerRadiusAll((int)(piece.CornerRadius * _config.Zoom));
        DrawStyleBox(pieceStyle, squareRect);

        // 4. 纹理独立尺寸，等比压缩后居中覆盖在方块之上
        float texWorldSize = _config.VarPieceTextureSize;
        float texScreenSize = texWorldSize * _config.Zoom;
        Vector2 texSize = piece.Texture.GetSize();
        float maxTexDim = Mathf.Max(texSize.X, texSize.Y);
        if (maxTexDim > 0)
        {
            float texScale = texScreenSize / maxTexDim;
            Vector2 finalTexSize = texSize * texScale;
            var texRect = new Rect2(
                screenCenter.X - finalTexSize.X / 2f,
                screenCenter.Y - finalTexSize.Y / 2f,
                finalTexSize.X,
                finalTexSize.Y);
            DrawTextureRect(piece.Texture, texRect, false);
        }
    }


    public void AddOrUpdatePiece(Var var, Vector2 origin)
    {
        if (!_config.VarTypeTextures.TryGetValue(var.Stats.Type, out var texture))
        {
            GD.PrintErr($"No texture found for VarType {var.Stats.Type}");
            return;
        }

        if (_pieces.TryGetValue(var, out var piece))
        {
            // 已有 → 位置变化则触发动画
            if (piece.TargetPosition.DistanceSquaredTo(origin) > 0.01f)
            {
                piece.TargetPosition = origin;
                piece.Elapsed = 0f;
                piece.IsAnimating = true;
            }
        }
        else
        {
            // 新建 → 直接放置（无动画）
            var color = var.Stats.VarTeam switch
            {
                VarStats.Team.Friendly => _config.FriendlyPieceColor,
                VarStats.Team.Hostile => _config.EnemyPieceColor,
                _ => Colors.White
            };
            if (var.Stats.Type == VarStats.VarType.Bug)
            {
                color = _config.BugPieceColor;
            }

            _pieces[var] = new Piece
            {
                Texture = texture,
                Var = var,
                DisplayPosition = origin,
                TargetPosition = origin,
                CornerRadius = _config.VarPieceCornerRadius,
                Color = color,
                BorderColor = _config.VarPieceBorderColor,
                BorderWidth = _config.VarPieceBorderWidth,
            };
        }

        QueueRedraw();
    }

    public void RemovePiece(Var var)
    {
        if (var != null && _pieces.Remove(var))
        {
            QueueRedraw();
        }
    }
    public void RemoveAll()
    {
        _pieces.Clear();
    }
}