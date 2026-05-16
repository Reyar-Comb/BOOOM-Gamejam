using Godot;
using System;
using System.Collections.Generic;

public partial class EndMenu : Control
{
	[Export] public RichTextLabel LogDisplay { get; private set; } = null!;
	[Export] public float LogLineIntervalSeconds { get; private set; } = 0.35f;
	private const string StartMenuPath = "res://Main/StartMenu/StartMenu.tscn";
	private const float NormalScale = 1.0f;
	private const float HoverScale = 1.08f;
	private const float PressedScale = 0.94f;
	private const double ScaleTweenDuration = 0.08;
	public int CompletedWaveCount { get; set; } = 0;
	public int CreatedVarCount { get; set; } = 0;
	public int RepairedEnemyCount { get; set; } = 0;
	public int EnemyDeathCount
	{
		get => RepairedEnemyCount;
		set => RepairedEnemyCount = Math.Max(0, value);
	}
	public string TimeText { get; set; } = "00:00";

	private Color _infoColor = Colors.LimeGreen;
	private Color _errorColor = Colors.Red;
	private Color _timeColor = Colors.White;
	private Color _actorColor = new("#6d9bff");
	private PanelContainer RestartButton => field ??= GetNode<PanelContainer>("%RestartPanel");
	private bool _isHovered = false;
	private Tween _tween;
	private int _settlementDisplayRunId = 0;

	public override void _Ready()
	{
		LogDisplay ??= GetNodeOrNull<RichTextLabel>("MarginContainer/WindowPanel/MarginContainer/LogDisplay");
		InitializeColors(BattleManager.Instance?.ColorData);
		BindButton();
	}

	public void InitializeColors(ColorData colorData)
	{
		if (colorData == null)
		{
			return;
		}

		_infoColor = colorData.Get("LogInfoText");
		_errorColor = colorData.Get("LogErrorText");
		_timeColor = colorData.Get("LogTimeText");
		_actorColor = colorData.Get("LogActorText");
	}

	public void SetBattleStats(int completedWaveCount, int createdVarCount, int repairedEnemyCount, string timeText)
	{
		CompletedWaveCount = Math.Max(0, completedWaveCount);
		CreatedVarCount = Math.Max(0, createdVarCount);
		RepairedEnemyCount = Math.Max(0, repairedEnemyCount);
		TimeText = string.IsNullOrEmpty(timeText) ? "00:00" : timeText;
	}

	public void SetBattleStats(BattleManager battleManager)
	{
		if (battleManager == null)
		{
			return;
		}

		InitializeColors(battleManager.ColorData);
		SetBattleStats(
			battleManager.CompletedWaveCount,
			battleManager.CreatedFriendlyVarCount,
			battleManager.RepairedEnemyCount,
			battleManager.GetTimeString());
	}

	public void StartSettlementDisplay(TokenManager.EndReason reason)
	{
		ShowSettlementLogs(reason);
	}

	public async void ShowSettlementLogs(TokenManager.EndReason reason)
	{
		if (LogDisplay == null)
		{
			return;
		}

		int runId = ++_settlementDisplayRunId;
		List<string> logs = new(CreateSettlementLogs(reason));
		LogDisplay.Clear();
		for (int i = 0; i < logs.Count; i++)
		{
			if (runId != _settlementDisplayRunId)
			{
				return;
			}

			LogDisplay.AppendText(logs[i] + "\n");
			if (i < logs.Count - 1 && LogLineIntervalSeconds > 0.0f)
			{
				await ToSignal(GetTree().CreateTimer(LogLineIntervalSeconds), SceneTreeTimer.SignalName.Timeout);
			}
		}
	}

	public void ClearLogs()
	{
		_settlementDisplayRunId++;
		LogDisplay?.Clear();
	}
	private string GetReasonText(TokenManager.EndReason reason)
	{
		return reason switch
		{
			TokenManager.EndReason.Token => FormatLog("ERROR", _errorColor, "System", "Token 耗尽，运行终止。", _errorColor),
			TokenManager.EndReason.Patience => FormatLog("ERROR", _errorColor, "System", "用户耐心值耗尽，运行终止。", _errorColor),
			TokenManager.EndReason.Victory => FormatLog("INFO", _infoColor, "System", "成功修复所有 Bug！", _infoColor),
			_ => "未知原因"
		};
	}
	private IEnumerable<string> CreateSettlementLogs(TokenManager.EndReason reason)
	{
		yield return GetReasonText(reason);
		yield return FormatLog("INFO", _infoColor, "System", $"本次运行时长：{TimeText}", _infoColor);
		yield return FormatLog("INFO", _infoColor, "System", $"已修复 Bug 数量：{CompletedWaveCount}", _infoColor);
		yield return FormatLog("INFO", _infoColor, "System", $"创建过的变量个数：{CreatedVarCount}", _infoColor);
		yield return FormatLog("INFO", _infoColor, "System", $"修复的异常变量个数：{RepairedEnemyCount}", _infoColor);
	}

	private string FormatLog(string typeText, Color typeColor, string actor, string message, Color messageColor)
	{
		return $"[b]{ColoredText(typeText, typeColor)}[/b] {ColoredText($"[{TimeText}]", _timeColor)} {ColoredText(actor, _actorColor)}: {ColoredText(message, messageColor)}";
	}

	private static string ColoredText(string text, Color color)
	{
		return $"[color={color.ToHtml()}]{text}[/color]";
	}

	private void BindButton()
	{
		RestartButton.MouseDefaultCursorShape = CursorShape.PointingHand;
		RestartButton.PivotOffset = RestartButton.Size / 2;
		RestartButton.MouseEntered += () =>
		{
			_isHovered = true;
			AudioManager.Instance.PlaySFX("hover");
			TweenButtonScale(HoverScale);
		};
		RestartButton.MouseExited += () =>
		{
			_isHovered = false;
			TweenButtonScale(NormalScale);
		};
		RestartButton.GuiInput += inputEvent =>
		{
			if (inputEvent is not InputEventMouseButton mouseEvent ||
				mouseEvent.ButtonIndex != MouseButton.Left ||
				!mouseEvent.Pressed)
			{
				return;
			}
			AudioManager.Instance.PlaySFX("click_button");
			_ = AudioManager.Instance.UnfilterBGM(3);
			_ = SceneManager.Instance.ChangeSceneToFileAsync(StartMenuPath);
			TweenPressedScale();
		};
	}
	private void TweenButtonScale(float targetScale)
	{
		_tween?.Kill();

		_tween = CreateTween();
		_tween.SetTrans(Tween.TransitionType.Sine);
		_tween.SetEase(Tween.EaseType.Out);
		_tween.TweenProperty(RestartButton, "scale", Vector2.One * targetScale, ScaleTweenDuration);
	}

	private void TweenPressedScale()
	{
		_tween?.Kill();

		_tween = CreateTween();
		_tween.SetTrans(Tween.TransitionType.Sine);
		_tween.SetEase(Tween.EaseType.Out);
		_tween.TweenProperty(RestartButton, "scale", Vector2.One * PressedScale, ScaleTweenDuration / 2.0);
		_tween.TweenProperty(RestartButton, "scale", Vector2.One * (_isHovered ? HoverScale : NormalScale), ScaleTweenDuration / 2.0);
	}
}
