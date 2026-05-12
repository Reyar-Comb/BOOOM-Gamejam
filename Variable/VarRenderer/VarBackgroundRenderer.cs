using Godot;

internal sealed partial class VarBackgroundRenderer : Control
{
    private VarRendererConfig _config;

    public VarBackgroundRenderer(VarRendererConfig config)
    {
        Name = nameof(VarBackgroundRenderer);
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
        if (!_config.DrawBackground)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), _config.BackgroundColor);
    }
}
