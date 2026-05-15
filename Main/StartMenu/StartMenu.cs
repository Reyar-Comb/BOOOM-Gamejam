using Godot;
using System.Threading.Tasks;

public partial class StartMenu : Control
{
    private const string GameScenePath = "res://Main/MainGame/MainGame.tscn";
    private const float NormalScale = 1.0f;
    private const float HoverScale = 1.08f;
    private const float PressedScale = 0.96f;
    private const double ScaleTweenDuration = 0.08;

    private VarRenderer _background = null!;
    private Control _menuRoot = null!;
    private TextureRect _title = null!;
    private TextureButton _startButton = null!;
    private TextureButton _optionButton = null!;
    private TextureButton _quitButton = null!;
    private bool _isStarting;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _background = GetNode<VarRenderer>("VarRendererBackground");
        _menuRoot = GetNode<Control>("MenuRoot");
        _title = GetNode<TextureRect>("MenuRoot/Title");
        _startButton = GetNode<TextureButton>("MenuRoot/Start");
        _optionButton = GetNode<TextureButton>("MenuRoot/Option");
        _quitButton = GetNode<TextureButton>("MenuRoot/Quit");

        _background.Initialize(new MapData(96, 54));

        _startButton.Pressed += OnStartPressed;
        _optionButton.Pressed += TriggerClickRippleAtMouse;
        _quitButton.Pressed += OnQuitPressed;
        BindButtonScaleFeedback(_startButton);
        BindButtonScaleFeedback(_optionButton);
        BindButtonScaleFeedback(_quitButton);

        LayoutMenu();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            LayoutMenu();
        }
    }

    private void LayoutMenu()
    {
        if (_menuRoot == null || Size == Vector2.Zero)
        {
            return;
        }

        float titleWidth = Mathf.Clamp(Size.X * 0.36f, 520.0f, 760.0f);
        float buttonWidth = Mathf.Clamp(titleWidth * 0.5f, 280.0f, 390.0f);
        float buttonHeight = Mathf.Clamp(buttonWidth * 0.32f, 88.0f, 124.0f);
        float left = Mathf.Clamp(Size.X * 0.115f, 80.0f, 260.0f);
        float top = Mathf.Clamp(Size.Y * 0.11f, 76.0f, 140.0f);
        float titleHeight = titleWidth * GetTextureAspectHeight(_title.Texture);
        float gap = Mathf.Clamp(Size.Y * 0.035f, 28.0f, 52.0f);

        _menuRoot.Position = new Vector2(left, top);
        _menuRoot.Size = new Vector2(titleWidth, titleHeight + gap + buttonHeight * 3.0f + gap * 2.0f);

        _title.Position = Vector2.Zero;
        _title.Size = new Vector2(titleWidth, titleHeight);

        float buttonLeft = titleWidth * 0.13f;
        float y = titleHeight + gap;
        LayoutButton(_startButton, buttonLeft, y, buttonWidth, buttonHeight);
        y += buttonHeight + gap;
        LayoutButton(_optionButton, buttonLeft, y, buttonWidth, buttonHeight);
        y += buttonHeight + gap;
        LayoutButton(_quitButton, buttonLeft, y, buttonWidth, buttonHeight);
    }

    private static void LayoutButton(TextureButton button, float x, float y, float width, float height)
    {
        button.Position = new Vector2(x, y);
        button.Size = new Vector2(width, height);
        button.CustomMinimumSize = button.Size;
        button.PivotOffset = button.Size * 0.5f;
    }

    private void BindButtonScaleFeedback(TextureButton button)
    {
        button.MouseEntered += () => TweenButtonScale(button, HoverScale);
        button.MouseExited += () => TweenButtonScale(button, NormalScale);
        button.ButtonDown += () => TweenButtonScale(button, PressedScale);
        button.ButtonUp += () => TweenButtonScale(button, button.IsHovered() ? HoverScale : NormalScale);
    }

    private void TweenButtonScale(TextureButton button, float targetScale)
    {
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "scale", Vector2.One * targetScale, ScaleTweenDuration);
    }

    private static float GetTextureAspectHeight(Texture2D texture)
    {
        if (texture == null || texture.GetWidth() <= 0)
        {
            return 0.35f;
        }

        return texture.GetHeight() / (float)texture.GetWidth();
    }

    private async void OnStartPressed()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        TriggerBigRippleAtMouse();

        await DelaySeconds(0.35);
        GetTree().ChangeSceneToFile(GameScenePath);
    }

    private void OnQuitPressed()
    {
        TriggerClickRippleAtMouse();
        GetTree().Quit();
    }

    private void TriggerClickRippleAtMouse()
    {
        Vector2I? cell = GetMouseGridCell();
        if (cell.HasValue)
        {
            _background.AddRipple(cell.Value);
        }
    }

    private void TriggerBigRippleAtMouse()
    {
        Vector2I? cell = GetMouseGridCell();
        if (cell.HasValue)
        {
            _background.AddBugDeathRipple(cell.Value);
        }
    }

    private Vector2I? GetMouseGridCell()
    {
        Vector2 localMouse = _background.GetLocalMousePosition();
        if (!new Rect2(Vector2.Zero, _background.Size).HasPoint(localMouse))
        {
            return null;
        }

        return Grid.WorldToGrid(_background.ScreenToWorld(localMouse));
    }

    private async Task DelaySeconds(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
