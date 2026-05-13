using Godot;
using System;

[GlobalClass]
public partial class VarButton : Control
{
	// ========== 静态选中管理（全局同一时间只有一个 VarButton 被选中） ==========

	private static VarButton _selected;

	/// <summary>
	/// 闪烁效果：modulate.a 在 MinAlpha 和 MaxAlpha 之间往复。
	/// 设为 0 则选中时不闪烁，仅保持静止高亮。
	/// </summary>
	[Export] public float BlinkDuration { get; set; } = 0.35f;

	[Export] public float BlinkMinAlpha { get; set; } = 0.4f;
	[Export] public float BlinkMaxAlpha { get; set; } = 1.0f;

	private Tween _blinkTween;

	public bool IsSelected => _selected == this;

	// ========== Hover 回调（每次进入/离开触发一次）==========

	public Action OnHoverEnter { get; set; }
	public Action OnHoverLeave { get; set; }

	// ========== 导出字段 ==========

	[Export]
	public string Text {
		get => richTextLabel.Text;
		set => richTextLabel.Text = value;
	}

	public RichTextLabel richTextLabel;
	public ColorRect colorRect;
	public Button button;

	// ========== 生命周期 ==========

	public override void _Ready()
	{
		richTextLabel = GetNode<RichTextLabel>("RichTextLabel");
		colorRect = GetNode<ColorRect>("ColorRect");
		button = GetNode<Button>("Button");

		button.Pressed += onPressed;

		// Hover 回调
		button.MouseEntered += () => OnHoverEnter?.Invoke();
		button.MouseExited  += () => OnHoverLeave?.Invoke();

		this.SizeFlagsVertical = (int)SizeFlags.ShrinkBegin;
	}

	public override void _ExitTree()
	{
		KillBlink();
		if (_selected == this)
			_selected = null;
	}

	// ========== 选中 / 闪烁 ==========

	public void Select()
	{
		if (_selected == this) return;

		_selected?.Deselect();
		_selected = this;

		StartBlink();
	}

	public void Deselect()
	{
		if (_selected != this) return;
		_selected = null;
		KillBlink();
	}

	private void StartBlink()
	{
		KillBlink();

		if (BlinkDuration <= 0)
		{
			Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, BlinkMinAlpha);
			return;
		}

		// 以当前 modulate.a 为起点，摆动到 MinAlpha 再摆回来，循环往复
		_blinkTween = CreateTween().SetLoops();
		_blinkTween.TweenProperty(this, "modulate:a", BlinkMinAlpha, BlinkDuration)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Sine);
		_blinkTween.TweenProperty(this, "modulate:a", BlinkMaxAlpha, BlinkDuration)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private void KillBlink()
	{
		_blinkTween?.Kill();
		_blinkTween = null;
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, BlinkMaxAlpha);
	}

	// ========== 事件 ==========

	public void onPressed()
	{
		Select();
	}

	

	// ========== 工具方法 ==========

	public void SetText(string text)
	{
		richTextLabel.Text = "[b]" + text + "[/b]";
	}

	public void SetStyle(VarStats.VarType varType)
	{
		Color color = varType switch
		{
			VarStats.VarType.Int => BattleManager.Instance.ColorData.Get("ButtonIntColor"),
			VarStats.VarType.Float => BattleManager.Instance.ColorData.Get("ButtonFloatColor"),
			VarStats.VarType.Double => BattleManager.Instance.ColorData.Get("ButtonDoubleColor"),
			VarStats.VarType.LongDouble => BattleManager.Instance.ColorData.Get("ButtonLongDoubleColor"),
			VarStats.VarType.Char => BattleManager.Instance.ColorData.Get("ButtonCharColor"),
			VarStats.VarType.Bool => BattleManager.Instance.ColorData.Get("ButtonBoolColor"),
			VarStats.VarType.Long => BattleManager.Instance.ColorData.Get("ButtonLongColor"),
			_ => Colors.White
		};
		colorRect.Color = color;
	}


}
