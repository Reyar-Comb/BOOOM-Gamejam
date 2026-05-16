using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class PauseMenu : Control
{
    private const string StartMenuPath = "res://Main/StartMenu/StartMenu.tscn";
    private const float NormalScale = 1.0f;
    private const float HoverScale = 1.08f;
    private const float PressedScale = 0.94f;
    private const double ScaleTweenDuration = 0.08;

    private Control ContinuePanel => field ??= GetNode<Control>("CenterContainer/VBoxContainer/ContinuePanel");
    private Control OptionPanel => field ??= GetNode<Control>("CenterContainer/VBoxContainer/OptionPanel");
    private Control QuitPanel => field ??= GetNode<Control>("CenterContainer/VBoxContainer/QuitPanel");
    private Control ButtonContainer => field ??= GetNode<Control>("CenterContainer");
    private OptionMenu OptionMenu => field ??= GetNode<OptionMenu>("OptionMenu");

    private readonly Dictionary<Control, bool> _isHovered = new();
    private readonly Dictionary<Control, Tween> _buttonTweens = new();

    public override void _Ready()
    {
        Hide();
        OptionMenu.Closed += OnOptionMenuClosed;
        BindButton(ContinuePanel, OnContinuePressed);
        BindButton(OptionPanel, OnOptionPressed);
        BindButton(QuitPanel, OnQuitPressed);
        CallDeferred(MethodName.RefreshButtonPivots);
    }

    public void Toggle()
    {
        if (Visible)
        {
            Close();
            return;
        }

        Open();
    }

    public void Close()
    {
        Hide();
        CloseOptionMenu();
        ResetButton(ContinuePanel);
        ResetButton(OptionPanel);
        ResetButton(QuitPanel);
    }

    private void Open()
    {
        if (BattleManager.Instance is { CanOpenPauseMenu: false })
        {
            Close();
            return;
        }

        Show();
        CallDeferred(MethodName.RefreshButtonPivots);
    }

    private void BindButton(Control button, System.Action action)
    {
        _isHovered[button] = false;
        _buttonTweens[button] = null;
        button.MouseDefaultCursorShape = CursorShape.PointingHand;
        button.MouseEntered += () =>
        {
            _isHovered[button] = true;
            TweenButtonScale(button, HoverScale);
        };
        button.MouseExited += () =>
        {
            _isHovered[button] = false;
            TweenButtonScale(button, NormalScale);
        };
        button.GuiInput += inputEvent =>
        {
            if (inputEvent is not InputEventMouseButton mouseEvent ||
                mouseEvent.ButtonIndex != MouseButton.Left ||
                !mouseEvent.Pressed)
            {
                return;
            }

            TweenPressedScale(button);
            action();
            AcceptEvent();
        };
    }

    private void RefreshButtonPivots()
    {
        ContinuePanel.PivotOffset = ContinuePanel.Size / 2.0f;
        OptionPanel.PivotOffset = OptionPanel.Size / 2.0f;
        QuitPanel.PivotOffset = QuitPanel.Size / 2.0f;
    }

    private void TweenButtonScale(Control button, float targetScale)
    {
        Tween tween = _buttonTweens[button];
        tween?.Kill();

        tween = CreateTween();
        _buttonTweens[button] = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "scale", Vector2.One * targetScale, ScaleTweenDuration);
    }

    private void TweenPressedScale(Control button)
    {
        Tween tween = _buttonTweens[button];
        tween?.Kill();

        tween = CreateTween();
        _buttonTweens[button] = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "scale", Vector2.One * PressedScale, ScaleTweenDuration / 2.0);
        tween.TweenProperty(button, "scale", Vector2.One * (_isHovered[button] ? HoverScale : NormalScale), ScaleTweenDuration / 2.0);
    }

    private void ResetButton(Control button)
    {
        _isHovered[button] = false;
        _buttonTweens[button]?.Kill();
        _buttonTweens[button] = null;
        button.Scale = Vector2.One;
    }

    private async void OnContinuePressed()
    {
        await DelaySeconds(ScaleTweenDuration);
        BattleManager.Instance?.TogglePause();
        Close();
    }

    private async void OnOptionPressed()
    {
        await DelaySeconds(ScaleTweenDuration);
        if (!Visible)
        {
            return;
        }

        ButtonContainer.Hide();
        OptionMenu.Open();
    }

    private async void OnQuitPressed()
    {
        await DelaySeconds(ScaleTweenDuration);
        _ = AudioManager.Instance.UnfilterBGM();
        GetTree().ChangeSceneToFile(StartMenuPath);
    }

    private async Task DelaySeconds(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private void CloseOptionMenu()
    {
        OptionMenu.Close();
    }

    private void OnOptionMenuClosed()
    {
        ButtonContainer.Show();
    }
}
