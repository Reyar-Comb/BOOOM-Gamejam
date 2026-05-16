using Godot;
using System.Threading.Tasks;

public partial class SceneManager : CanvasLayer
{
	public static SceneManager Instance { get; private set; } = null!;

	private const string GlassBreakShaderPath = "res://Shaders/GlassBreakReveal.gdshader";
	private const double DefaultDuration = 0.85;

	private ColorRect _overlay = null!;
	private ShaderMaterial _transitionMaterial = null!;
	private Tween _activeTween;
	private bool _isTransitioning;

	public override void _Ready()
	{
		Instance = this;
		Layer = 128;
		ProcessMode = ProcessModeEnum.Always;
		BuildOverlay();
	}

	public async Task ChangeSceneToFileAsync(string scenePath, double fadeDuration = DefaultDuration, double revealDuration = DefaultDuration)
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;
		await FadeToBlackInternalAsync(fadeDuration);
		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PushError($"SceneManager failed to change scene to '{scenePath}': {error}");
			await RevealInternalAsync(revealDuration);
			_isTransitioning = false;
			return;
		}

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await RevealInternalAsync(revealDuration);
		_isTransitioning = false;
	}

	public async Task FadeToBlackAsync(double duration = DefaultDuration)
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;
		await FadeToBlackInternalAsync(duration);
		_isTransitioning = false;
	}

	public async Task RevealAsync(double duration = DefaultDuration)
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;
		await RevealInternalAsync(duration);
		_isTransitioning = false;
	}

	public async Task RevealCanvasItemAsync(CanvasItem canvasItem, double duration = DefaultDuration)
	{
		if (_isTransitioning || canvasItem == null)
		{
			return;
		}

		_isTransitioning = true;
		ShaderMaterial material = CreateTransitionMaterial(false);
		material.SetShaderParameter("cover_from_edges", false);
		material.SetShaderParameter("progress", 0.0f);
		canvasItem.Material = material;

		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(material, "shader_parameter/progress", 1.0f, duration);
		await ToSignal(tween, Tween.SignalName.Finished);
		_isTransitioning = false;
	}

	private void BuildOverlay()
	{
		_transitionMaterial = CreateTransitionMaterial(true);
		_transitionMaterial.SetShaderParameter("overlay_mode", true);
		_transitionMaterial.SetShaderParameter("overlay_color", Colors.Black);
		_transitionMaterial.SetShaderParameter("progress", 0.0f);

		_overlay = new ColorRect
		{
			Name = "GlassBreakTransitionOverlay",
			Color = Colors.White,
			Material = _transitionMaterial,
			MouseFilter = Control.MouseFilterEnum.Stop,
			Visible = false
		};
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_overlay);
	}

	private static ShaderMaterial CreateTransitionMaterial(bool overlayMode)
	{
		Shader shader = ResourceLoader.Load<Shader>(GlassBreakShaderPath);
		ShaderMaterial material = new() { Shader = shader };
		material.SetShaderParameter("overlay_mode", overlayMode);
		material.SetShaderParameter("overlay_color", Colors.Black);
		return material;
	}

	private async Task FadeToBlackInternalAsync(double duration)
	{
		PrepareOverlay(0.0f, true);
		await TweenProgressAsync(1.0f, duration);
	}

	private async Task RevealInternalAsync(double duration)
	{
		PrepareOverlay(0.0f, false);
		await TweenProgressAsync(1.0f, duration);
		_overlay.Hide();
	}

	private void PrepareOverlay(float progress, bool coverFromEdges)
	{
		_activeTween?.Kill();
		_transitionMaterial.SetShaderParameter("overlay_mode", true);
		_transitionMaterial.SetShaderParameter("cover_from_edges", coverFromEdges);
		_transitionMaterial.SetShaderParameter("progress", progress);
		_overlay.Show();
	}

	private async Task TweenProgressAsync(float targetProgress, double duration)
	{
		_activeTween = CreateTween();
		_activeTween.SetEase(Tween.EaseType.Out);
		_activeTween.SetTrans(Tween.TransitionType.Cubic);
		_activeTween.TweenProperty(_transitionMaterial, "shader_parameter/progress", targetProgress, duration);
		await ToSignal(_activeTween, Tween.SignalName.Finished);
		_activeTween = null;
	}
}
