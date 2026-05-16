using Godot;

public partial class OptionMenu : Control
{
    private const float MinVolumeDb = -80.0f;
    private const float MaxVolumeDb = 0.0f;

    [Signal]
    public delegate void ClosedEventHandler();

    private Control WindowPanel => field ??= GetNode<Control>("CenterContainer/WindowPanel");
    private HSlider BgmSlider => field ??= GetNode<HSlider>("%BgmSlider");
    private HSlider SfxSlider => field ??= GetNode<HSlider>("%SfxSlider");
    private Label BgmValueLabel => field ??= GetNode<Label>("%BgmValueLabel");
    private Label SfxValueLabel => field ??= GetNode<Label>("%SfxValueLabel");

    public override void _Ready()
    {
        BgmSlider.ValueChanged += OnBgmVolumeChanged;
        SfxSlider.ValueChanged += OnSfxVolumeChanged;

        SetSliderWithoutSignal(BgmSlider, VolumeDbToPercent(GetBusVolumeDb("BGM")));
        SetSliderWithoutSignal(SfxSlider, VolumeDbToPercent(GetBusVolumeDb("SFX")));
        UpdateValueLabel(BgmValueLabel, BgmSlider.Value);
        UpdateValueLabel(SfxValueLabel, SfxSlider.Value);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!Visible)
        {
            return;
        }

        if (inputEvent.IsActionPressed("Pause"))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (inputEvent is not InputEventMouseButton mouseEvent ||
            mouseEvent.ButtonIndex != MouseButton.Left ||
            !mouseEvent.Pressed)
        {
            return;
        }

        if (WindowPanel.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
        {
            return;
        }

        Close();
        GetViewport().SetInputAsHandled();
    }

    public void Open()
    {
        Show();
    }

    public void Close()
    {
        if (!Visible)
        {
            return;
        }

        Hide();
        EmitSignal(SignalName.Closed);
    }

    private void OnBgmVolumeChanged(double value)
    {
        UpdateValueLabel(BgmValueLabel, value);
        float volumeDb = PercentToVolumeDb(value);
        AudioManager.Instance?.SetBGMVolume(volumeDb);
    }

    private void OnSfxVolumeChanged(double value)
    {
        UpdateValueLabel(SfxValueLabel, value);
        float volumeDb = PercentToVolumeDb(value);
        AudioManager.Instance?.SetSFXVolume(volumeDb);
    }

    private static void SetSliderWithoutSignal(HSlider slider, double value)
    {
        slider.SetValueNoSignal(Mathf.Clamp(value, slider.MinValue, slider.MaxValue));
    }

    private static void UpdateValueLabel(Label label, double value)
    {
        label.Text = $"{Mathf.RoundToInt(value)}";
    }

    private static float PercentToVolumeDb(double percent)
    {
        if (percent <= 0.0)
        {
            return MinVolumeDb;
        }

        return Mathf.LinearToDb((float)percent / 100.0f);
    }

    private static double VolumeDbToPercent(float volumeDb)
    {
        if (volumeDb <= MinVolumeDb)
        {
            return 0.0;
        }

        return Mathf.Clamp(Mathf.DbToLinear(Mathf.Clamp(volumeDb, MinVolumeDb, MaxVolumeDb)) * 100.0f, 0.0f, 100.0f);
    }

    private static float GetBusVolumeDb(string busName)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        return busIndex == -1 ? MaxVolumeDb : AudioServer.GetBusVolumeDb(busIndex);
    }
}
