using Godot;

public partial class Slider : HSlider
{
	[Export] public BattleManager BattleManager { get; set; } = null!;

	private Label _valueLabel = null!;

	public override void _Ready()
	{
		MinValue = 0.1;
		MaxValue = 4.0;
		Step = 0.1;

		EnsureValueLabel();
		ValueChanged += OnValueChanged;

		if (BattleManager == null)
		{
			GD.PushWarning($"{nameof(Slider)} on {GetPath()} has no BattleManager assigned.");
			Value = 1.0;
			UpdateValueLabel((float)Value, false);
			return;
		}

		Value = BattleManager.TickScale;
		UpdateValueLabel(BattleManager.TickScale, true);
	}

	private void EnsureValueLabel()
	{
		_valueLabel = GetNodeOrNull<Label>("ValueLabel");
		if (_valueLabel != null)
		{
			return;
		}

		_valueLabel = new Label
		{
			Name = "ValueLabel",
			Position = new Vector2(0.0f, -28.0f),
			MouseFilter = MouseFilterEnum.Ignore
		};

		AddChild(_valueLabel);
	}

	private void OnValueChanged(double value)
	{
		float tickScale = Mathf.Max((float)value, 0.01f);
		bool hasBattleManager = BattleManager != null;

		if (hasBattleManager)
		{
			BattleManager.TickScale = tickScale;
		}

		UpdateValueLabel(tickScale, hasBattleManager);
	}

	private void UpdateValueLabel(float tickScale, bool hasBattleManager)
	{
		if (_valueLabel == null)
		{
			return;
		}

		string suffix = hasBattleManager ? string.Empty : " (Unbound)";
		_valueLabel.Text = $"Time Scale: {tickScale:0.0}x{suffix}";
	}
}
