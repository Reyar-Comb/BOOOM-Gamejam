using Godot;
using System;
using System.Threading.Tasks;

public partial class PPTScene : Control
{
	private const string GameScenePath = "res://Main/MainGame/MainGame.tscn";

	// ========== Exported ==========

	/// <summary>PPT 幻灯片纹理数组，按顺序放入每张 PPT 图片</summary>
	[Export] public Texture2D[] Slides { get; set; } = Array.Empty<Texture2D>();

	/// <summary>翻页过渡时长（秒）</summary>
	[Export] public float TransitionDuration { get; set; } = 0.5f;

	/// <summary>是否可以循环翻页（最后一页翻到第一页）</summary>
	[Export] public bool Loop { get; set; } = false;

	// ========== Glitch 效果参数 ==========

	[ExportGroup("Glitch 效果")]
	[Export(PropertyHint.Range, "0,1")]
	public float GlitchIntensity { get; set; } = 0.12f;

	[Export(PropertyHint.Range, "0,0.02")]
	public float GlitchRgbSplit { get; set; } = 0.005f;

	[Export(PropertyHint.Range, "0,0.06")]
	public float GlitchTearAmount { get; set; } = 0.014f;

	[Export(PropertyHint.Range, "0.7,1")]
	public float GlitchTearThreshold { get; set; } = 0.94f;

	[Export(PropertyHint.Range, "0.001,0.04")]
	public float GlitchTearWidth { get; set; } = 0.008f;

	[Export(PropertyHint.Range, "0,0.04")]
	public float GlitchBlockAmount { get; set; } = 0.007f;

	[Export(PropertyHint.Range, "2,30")]
	public float GlitchBlockSize { get; set; } = 12f;

	[Export(PropertyHint.Range, "0.7,1")]
	public float GlitchBlockThreshold { get; set; } = 0.95f;

	[Export(PropertyHint.Range, "0.1,5")]
	public float GlitchSpeed { get; set; } = 1f;

	[Export(PropertyHint.Range, "1,60")]
	public float GlitchNoiseScale { get; set; } = 25f;

	[Export(PropertyHint.Range, "0,0.15")]
	public float GlitchColorShift { get; set; } = 0.03f;

	// ========== Signals ==========

	/// <summary>用户请求退出 PPT 场景（按 Esc 或点击退出按钮时触发）</summary>
	[Signal] public delegate void ExitRequestedEventHandler();

	/// <summary>幻灯片切换时触发，参数为新页索引</summary>
	[Signal] public delegate void SlideChangedEventHandler(int slideIndex);

	/// <summary>到达最后一页并尝试继续时触发</summary>
	[Signal] public delegate void ReachedEndEventHandler();

	// ========== Node References ==========

	private TextureRect _slideDisplay = null!;
	private ColorRect _transitionOverlay = null!;
	private ShaderMaterial _transitionMaterial = null!;
	private ShaderMaterial _glitchMaterial = null!;
	private Label _pageIndicator = null!;
	private Label _hintLabel = null!;
	private Control _uiLayer = null!;

	// ========== State ==========

	private int _currentSlideIndex = -1;
	private bool _isTransitioning = false;

	// Shader uniform names
	private static readonly StringName ShaderProgress = "progress";

	// ========== Lifecycle ==========

	public override void _Ready()
	{
		SetupNodes();
		SetupTransitionShader();
		SetupGlitchShader();

		// 显示第一张幻灯片
		if (Slides.Length > 0)
		{
			ShowSlide(0, instant: true);
		}
		else
		{
			GD.PushWarning("PPTScene: Slides 数组为空，请放入 PPT 纹理。");
			_slideDisplay.Texture = null;
			UpdateUI();
		}

		// 初始状态：隐藏过渡层
		_transitionOverlay.Visible = false;
		_transitionMaterial.SetShaderParameter(ShaderProgress, 0.0f);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isTransitioning) return;

		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				NextSlide();
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Right)
			{
				PreviousSlide();
			}
		}
		else if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Space:
				case Key.Enter:
				case Key.Right:
				case Key.D:
					NextSlide();
					break;
				case Key.Left:
				case Key.A:
					PreviousSlide();
					break;
				case Key.Escape:
					RequestExit();
					break;
			}
		}
	}

	// ========== Public API ==========

	/// <summary>跳转到下一页（带过渡动画）</summary>
	public async void NextSlide()
	{
		if (_isTransitioning || Slides.Length == 0) return;

		int nextIndex = _currentSlideIndex + 1;
		if (nextIndex >= Slides.Length)
		{
			if (Loop)
			{
				nextIndex = 0;
			}
			else
			{
				SceneManager.Instance.IsTutorialPlayed = true;
				_ = AudioManager.Instance.UnfilterBGM(3);
				await SceneManager.Instance.ChangeSceneToFileAsync(GameScenePath);
				return;
			}
		}

		await TransitionToSlide(nextIndex, direction: 1);
	}

	/// <summary>跳转到上一页（带过渡动画）</summary>
	public async void PreviousSlide()
	{
		if (_isTransitioning || Slides.Length == 0) return;

		int prevIndex = _currentSlideIndex - 1;
		if (prevIndex < 0)
		{
			if (Loop)
			{
				prevIndex = Slides.Length - 1;
			}
			else
			{
				return;
			}
		}

		await TransitionToSlide(prevIndex, direction: -1);
	}

	/// <summary>直接跳转到指定页（带过渡动画）</summary>
	public async void GoToSlide(int index)
	{
		if (_isTransitioning || Slides.Length == 0) return;
		if (index < 0 || index >= Slides.Length) return;
		int dir = index > _currentSlideIndex ? 1 : -1;
		await TransitionToSlide(index, dir);
	}

	/// <summary>请求退出 PPT 场景。发出 ExitRequested 信号，由外部处理。</summary>
	public void RequestExit()
	{
		EmitSignal(SignalName.ExitRequested);
	}

	/// <summary>获取当前幻灯片索引（0-based）</summary>
	public int GetCurrentSlideIndex() => _currentSlideIndex;

	/// <summary>获取幻灯片总数</summary>
	public int GetSlideCount() => Slides.Length;

	// ========== Internal ==========

	private void SetupNodes()
	{
		// —— SlideDisplay: 显示当前幻灯片 ——
		_slideDisplay = GetNode<TextureRect>("SlideDisplay");
		_slideDisplay.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_slideDisplay.StretchMode = TextureRect.StretchModeEnum.Scale;
		// 锚定全屏
		_slideDisplay.AnchorLeft = 0f;
		_slideDisplay.AnchorTop = 0f;
		_slideDisplay.AnchorRight = 1f;
		_slideDisplay.AnchorBottom = 1f;
		_slideDisplay.OffsetLeft = 0f;
		_slideDisplay.OffsetTop = 0f;
		_slideDisplay.OffsetRight = 0f;
		_slideDisplay.OffsetBottom = 0f;

		// —— TransitionOverlay: 翻页动画覆盖层 ——
		_transitionOverlay = GetNode<ColorRect>("TransitionOverlay");
		_transitionOverlay.AnchorLeft = 0f;
		_transitionOverlay.AnchorTop = 0f;
		_transitionOverlay.AnchorRight = 1f;
		_transitionOverlay.AnchorBottom = 1f;
		_transitionOverlay.OffsetLeft = 0f;
		_transitionOverlay.OffsetTop = 0f;
		_transitionOverlay.OffsetRight = 0f;
		_transitionOverlay.OffsetBottom = 0f;
		_transitionOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;

		// —— UI 层 ——
		_uiLayer = GetNode<Control>("UILayer");
		_pageIndicator = _uiLayer.GetNode<Label>("PageIndicator");
		_hintLabel = _uiLayer.GetNode<Label>("HintLabel");
	}

	private void SetupTransitionShader()
	{
		_transitionMaterial = _transitionOverlay.Material as ShaderMaterial;
		if (_transitionMaterial == null)
		{
			_transitionMaterial = new ShaderMaterial();
			_transitionOverlay.Material = _transitionMaterial;
		}

		// 默认 shader 路径 —— 如果未在场景中指定，则从这里加载
		if (_transitionMaterial.Shader == null)
		{
			var shader = GD.Load<Shader>("res://Shaders/PageFlip.gdshader");
			if (shader != null)
			{
				_transitionMaterial.Shader = shader;
			}
		}

		// 初始化 shader 参数
		_transitionMaterial.SetShaderParameter(ShaderProgress, 0.0f);
	}

	/// <summary>加载并应用 Glitch 覆盖 shader 到 SlideDisplay</summary>
	private void SetupGlitchShader()
	{
		var shader = GD.Load<Shader>("res://Shaders/GlitchOverlay.gdshader");
		if (shader == null)
		{
			GD.PushWarning("PPTScene: 找不到 GlitchOverlay.gdshader");
			return;
		}

		_glitchMaterial = new ShaderMaterial();
		_glitchMaterial.Shader = shader;
		_slideDisplay.Material = _glitchMaterial;

		ApplyGlitchParameters();
	}

	/// <summary>将 Inspector 中的 glitch 参数同步到 shader</summary>
	public void ApplyGlitchParameters()
	{
		if (_glitchMaterial == null) return;

		_glitchMaterial.SetShaderParameter("intensity", GlitchIntensity);
		_glitchMaterial.SetShaderParameter("rgb_split", GlitchRgbSplit);
		_glitchMaterial.SetShaderParameter("tear_amount", GlitchTearAmount);
		_glitchMaterial.SetShaderParameter("tear_threshold", GlitchTearThreshold);
		_glitchMaterial.SetShaderParameter("tear_width", GlitchTearWidth);
		_glitchMaterial.SetShaderParameter("block_amount", GlitchBlockAmount);
		_glitchMaterial.SetShaderParameter("block_size", GlitchBlockSize);
		_glitchMaterial.SetShaderParameter("block_threshold", GlitchBlockThreshold);
		_glitchMaterial.SetShaderParameter("speed", GlitchSpeed);
		_glitchMaterial.SetShaderParameter("noise_scale", GlitchNoiseScale);
		_glitchMaterial.SetShaderParameter("color_shift", GlitchColorShift);
	}

	/// <summary>立即显示某张幻灯片（无动画）</summary>
	private void ShowSlide(int index, bool instant)
	{
		_currentSlideIndex = index;
		_slideDisplay.Texture = Slides[index];
		UpdateUI();
	}

	/// <summary>过渡到指定幻灯片（黑屏淡入淡出）</summary>
	private async Task TransitionToSlide(int targetIndex, int direction)
	{
		if (_isTransitioning) return;
		_isTransitioning = true;

		float halfDuration = TransitionDuration / 2f;
		_transitionOverlay.Visible = true;
		_transitionMaterial.SetShaderParameter(ShaderProgress, 0.0f);

		// 阶段1：渐黑（progress 0 → 0.5）
		Tween fadeOut = CreateTween();
		fadeOut.TweenMethod(
			Callable.From<float>(p => _transitionMaterial.SetShaderParameter(ShaderProgress, p)),
			0.0f, 0.5f, halfDuration
		);
		fadeOut.SetEase(Tween.EaseType.InOut);
		fadeOut.SetTrans(Tween.TransitionType.Sine);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		// 黑屏时切换幻灯片
		ShowSlide(targetIndex, instant: true);

		// 阶段2：渐亮（progress 0.5 → 1.0）
		Tween fadeIn = CreateTween();
		fadeIn.TweenMethod(
			Callable.From<float>(p => _transitionMaterial.SetShaderParameter(ShaderProgress, p)),
			0.5f, 1.0f, halfDuration
		);
		fadeIn.SetEase(Tween.EaseType.InOut);
		fadeIn.SetTrans(Tween.TransitionType.Sine);
		await ToSignal(fadeIn, Tween.SignalName.Finished);

		_transitionOverlay.Visible = false;
		_transitionMaterial.SetShaderParameter(ShaderProgress, 0.0f);
		EmitSignal(SignalName.SlideChanged, targetIndex);
		_isTransitioning = false;
	}

	private void UpdateUI()
	{
		if (Slides.Length == 0)
		{
			_pageIndicator.Text = "0 / 0";
			_hintLabel.Text = "暂无幻灯片";
			return;
		}

		int displayIndex = _currentSlideIndex + 1; // 1-based for display
		_pageIndicator.Text = $"{displayIndex} / {Slides.Length}";

		// 最后一张时更换提示文字
		if (_currentSlideIndex >= Slides.Length - 1 && !Loop)
		{
			_hintLabel.Text = "已到最后一页 — 按 Esc 退出";
		}
		else
		{
			_hintLabel.Text = "单击 / Space / → 翻页   ← 上一页   Esc 退出";
		}
	}
}
