using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class StartMenu : Control
{
    private const string GameScenePath = "res://Main/MainGame/MainGame.tscn";
    private const float NormalScale = 1.0f;
    private const float HoverScale = 1.08f;
    private const float PressedScale = 0.96f;
    private const double ScaleTweenDuration = 0.08;

    private VarRenderer Background => field ??= GetNode<VarRenderer>("VarRendererBackground");
    private TextureRect Title => field ??= GetNode<TextureRect>("%Title");
    private TextureRect StartButton => field ??= GetNode<TextureRect>("%Start");
    private TextureRect OptionButton => field ??= GetNode<TextureRect>("%Option");
    private TextureRect QuitButton => field ??= GetNode<TextureRect>("%Quit");
    private OptionMenu OptionMenu => field ??= GetNode<OptionMenu>("OptionMenu");
    private Dictionary<TextureRect, bool> _isHovered = new();
    private Dictionary<TextureRect, Tween> _buttonTweens = new();
    private bool _isStarting;
    private float _elapsedTime = 0f;
    private const float RippleDelay = 0.05f;
    private RandomNumberGenerator _rg = new();
    private async Task FrameDelay(int frame = 3)
    {
        for (int i = 0; i < frame; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
    public override void _Process(double delta)
    {
        _elapsedTime -= (float)delta;
        if (_elapsedTime <= 0f)
        {
            _elapsedTime = RippleDelay;
            int x = _rg.RandiRange(43, 93);
            int y = _rg.RandiRange(21, 50);
            TriggerRippleAt(new Vector2I(x, y));
        }
    }

    public override async void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        Background.Initialize(new MapData(96, 54));

        StartButton.GuiInput += OnStartPressed;
        OptionButton.GuiInput += OnOptionPressed;
        QuitButton.GuiInput += OnQuitPressed;

        StartButton.MouseEntered += () => AudioManager.Instance.PlaySFX("hover");
        OptionButton.MouseEntered += () => AudioManager.Instance.PlaySFX("hover");
        QuitButton.MouseEntered += () => AudioManager.Instance.PlaySFX("hover");

        await FrameDelay();
        BindHoverFeedback(StartButton);
        BindHoverFeedback(OptionButton);
        BindHoverFeedback(QuitButton);
    }

    private void BindHoverFeedback(TextureRect button)
    {
        _buttonTweens[button] = null;
        _isHovered[button] = false;
        button.PivotOffset = button.Size / 2;
        button.MouseEntered += () =>
        {
            TweenButtonScale(button, HoverScale);
            _isHovered[button] = true;
        };
        button.MouseExited += () =>
        {
            TweenButtonScale(button, NormalScale);
            _isHovered[button] = false;
        };
        // button.ButtonDown += () => TweenButtonScale(button, PressedScale);
        // button.ButtonUp += () => TweenButtonScale(button, _isHovered[button] ? HoverScale : NormalScale);
    }

    private void TweenButtonScale(TextureRect button, float targetScale)
    {
        Tween tween = _buttonTweens[button];
        tween?.Kill();
        tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "scale", Vector2.One * targetScale, ScaleTweenDuration);
    }

    private void TweenPressedScale(TextureRect button)
    {
        Tween tween = _buttonTweens[button];
        tween?.Kill();
        tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "scale", Vector2.One * PressedScale, ScaleTweenDuration / 2);
        tween.TweenProperty(button, "scale", Vector2.One * (_isHovered[button] ? HoverScale : NormalScale), ScaleTweenDuration / 2);
    }
    private async void OnStartPressed(InputEvent inputEvent = null)
    {
        if (inputEvent is not InputEventMouseButton mouseEvent || mouseEvent.ButtonIndex != MouseButton.Left || !mouseEvent.Pressed)
        {
            return;
        }

        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        AudioManager.Instance.PlaySFX("click_button");
        TriggerBigRippleAtMouse();
        TweenPressedScale(StartButton);
        await DelaySeconds(0.35);
        await SceneManager.Instance.ChangeSceneToFileAsync(GameScenePath);
    }

    private void OnOptionPressed(InputEvent inputEvent = null)
    {
        if (inputEvent is not InputEventMouseButton mouseEvent || mouseEvent.ButtonIndex != MouseButton.Left || !mouseEvent.Pressed)
        {
            return;
        }

        if (_isStarting)
        {
            return;
        }
        AudioManager.Instance.PlaySFX("click_button");
        TriggerClickRippleAtMouse();
        TweenPressedScale(OptionButton);
        OptionMenu.Open();
    }
    private void OnQuitPressed(InputEvent inputEvent = null)
    {
        if (inputEvent is not InputEventMouseButton mouseEvent || mouseEvent.ButtonIndex != MouseButton.Left || !mouseEvent.Pressed)
        {
            return;
        }

        if (_isStarting)
        {
            return;
        }
        AudioManager.Instance.PlaySFX("click_button");
        TriggerClickRippleAtMouse();
        TweenPressedScale(QuitButton);
        GetTree().Quit();
    }
    private void TriggerRippleAt(Vector2I cell)
    {
        Background.AddRipple(cell, false);
    }
    private void TriggerClickRippleAtMouse()
    {
        Vector2I? cell = GetMouseGridCell();
        
        if (cell.HasValue)
        {
            Background.AddRipple(cell.Value);
        }
    }

    private void TriggerBigRippleAtMouse()
    {
        Vector2I? cell = GetMouseGridCell();
        if (cell.HasValue)
        {
            Background.AddBugDeathRipple(cell.Value);
        }
    }

    private Vector2I? GetMouseGridCell()
    {
        Vector2 localMouse = Background.GetLocalMousePosition();
        if (!new Rect2(Vector2.Zero, Background.Size).HasPoint(localMouse))
        {
            return null;
        }
        Vector2I result = Grid.WorldToGrid(Background.ScreenToWorld(localMouse));
        return result;
    }

    private async Task DelaySeconds(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
