using Godot;

public partial class MainGame : Node2D
{
	private const string PauseMenuAction = "Pause";

	[Export] public double BreakDuration { get; private set; } = 1.35;

	private Control EndMenuRoot => field ??= GetNode<Control>("CanvasLayer/EndMenuRoot");
	private TextureRect GameOverSnapshot => field ??= GetNode<TextureRect>("CanvasLayer/GameOverSnapshot");
	private SubViewportContainer GameViewportContainer => field ??= GetNode<SubViewportContainer>("CanvasLayer/SubViewportContainer");
	private SubViewport GameViewport => field ??= GetNode<SubViewport>("CanvasLayer/SubViewportContainer/SubViewport");
	private GameScene GameScene => field ??= GetNode<GameScene>("CanvasLayer/SubViewportContainer/SubViewport/GameScene");
	private CanvasItem ScanningLine => field ??= GetNodeOrNull<CanvasItem>("CanvasLayer/ScanningLine");
	private CanvasItem AlertBorder => field ??= GetNodeOrNull<CanvasItem>("CanvasLayer/AlertBorder");
	private EndMenu EndMenu => field ??= GetNode<EndMenu>("CanvasLayer/EndMenuRoot/EndMenu");
	private bool _gameOverStarted = false;

	public override void _Ready()
	{
		CallDeferred(MethodName.ConnectBattleManager);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (_gameOverStarted || !inputEvent.IsActionPressed(PauseMenuAction))
		{
			return;
		}

		GameScene.TogglePauseMenu();
		GetViewport().SetInputAsHandled();
	}

	private void ConnectBattleManager()
	{
		if (BattleManager.Instance == null)
		{
			GD.PushWarning("MainGame could not connect game over effect because BattleManager.Instance is null.");
			return;
		}

		BattleManager.Instance.GameOver += OnGameOver;
	}

	public override void _ExitTree()
	{
		if (BattleManager.Instance != null)
		{
			BattleManager.Instance.GameOver -= OnGameOver;
		}
	}
	private async void OnGameOver()
	{
		if (_gameOverStarted)
		{
			return;
		}

		_gameOverStarted = true;
		CaptureGameOverSnapshot();
		HideGameplay();
		ShowEndMenuBehindGame();
		await SceneManager.Instance.RevealCanvasItemAsync(GameOverSnapshot, BreakDuration);
		GameOverSnapshot.Hide();
		GameOverSnapshot.Texture = null;
	}

	private void CaptureGameOverSnapshot()
	{
		Image image = GetViewport().GetTexture().GetImage();
		ImageTexture snapshotTexture = ImageTexture.CreateFromImage(image);
		GameOverSnapshot.Texture = snapshotTexture;
		GameOverSnapshot.Show();
	}

	private void ShowEndMenuBehindGame()
	{
		EndMenuRoot.Show();
		EndMenu.Show();
		BattleManager.Instance.ApplyStatsToEndMenu(EndMenu);
	}

	private void HideGameplay()
	{
		GameViewportContainer.Hide();
		if (ScanningLine != null)
		{
			ScanningLine.Hide();
		}
		if (AlertBorder != null)
		{
			AlertBorder.Hide();
		}
	}

}
