using System;
using System.Runtime.InteropServices.ObjectiveC;
using Godot;

public abstract class Log
{
	public string Time { get; set; }
	public LogType Type { get; set; }
	public string Actor { get; set; }
	public string Message { get; set; }
	public Vector2I? ReportedCell { get; protected set; }
	public string Objective { get; set; } = "";


	protected static Color InfoColor;
	protected static Color WarningColor;
	protected static Color ErrorColor;
	protected static Color TimeColor;
	protected static Color ActorColor;

	protected Color GetColorByType()
	{
		return Type switch
		{
			LogType.Info => InfoColor,
			LogType.Warning => WarningColor,
			LogType.Error => ErrorColor,
			_ => Colors.White
		};
	}

	protected string _timeText => ColoredText($"[{BattleManager.Instance.GetTimeString()}]", TimeColor);
	protected string _infoText = $"[b]{ColoredText("INFO", InfoColor)}[/b]";
	protected string _warningText = $"[b]{ColoredText("WARNING", WarningColor)}[/b]";
	protected string _errorText = $"[b]{ColoredText("ERROR", ErrorColor)}[/b]";
	protected string _actorText => ColoredText(Actor, ActorColor);
	protected string _messageText => ColoredText(Message, GetColorByType());

	protected Log(LogType type, string actor)
	{
		Time = DateTime.Now.ToString("HH:mm:ss.fff");
		Type = type;
		Actor = actor ?? "System";
	}

	protected Log(LogType type, string actor, string objective)
	{
		Time = DateTime.Now.ToString("HH:mm:ss.fff");
		Type = type;
		Actor = actor ?? "System";
		Objective = objective;
	}

	protected void SetReportedWorldPosition(Vector2 worldPosition)
	{
		ReportedCell = Grid.WorldToGrid(worldPosition);
	}

	protected void SetReportedCell(Vector2I cell)
	{
		ReportedCell = cell;
	}

	protected abstract string FormatMessage();

	public override string ToString()
	{
		return FormatMessage();
	}

	protected static string ColoredText(string text, Color color)
	{
		string hexColor = color.ToHtml();
		return $"[color={hexColor}]{text}[/color]";
	}

	public static void Initialize(ColorData colorData)
	{
		Log.InfoColor = colorData.Get("LogInfoText");
		Log.WarningColor = colorData.Get("LogWarningText");
		Log.ErrorColor = colorData.Get("LogErrorText");
		Log.TimeColor = colorData.Get("LogTimeText");
		Log.ActorColor = colorData.Get("LogActorText");
	}
}

public enum LogType
{
	Info,
	Warning,
	Error
}

public class CreateAck : Log
{
	public CreateAck(Var targetVar) : base(LogType.Info, "System")
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Objective = targetVar.Stats.Name;
		Message = $"成功创建 {targetVar.Stats.Type} 于 {ReportedCell} ！";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}

}

public class CreateBlockedError : Log
{
	public CreateBlockedError(Vector2I cell) : base(LogType.Error, "System")
	{
		SetReportedCell(cell);
		Message = $"无法在 {ReportedCell} 处创建变量：此 Class 内存在异常变量";
	}

	protected override string FormatMessage()
	{
		return $"{_errorText} {_timeText} {_actorText}: {_messageText}";
	}
}


public class LocationAck : Log
{
	public LocationAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Message = $"当前坐标：{ReportedCell}";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class HealthAck : Log
{
	public HealthAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		int currentHealth = targetVar.Stats.CurrentHealth;
		int maxHealth = targetVar.Stats.MaxHealth;
		if (currentHealth <= 0.3 * maxHealth)
		{
			Message = $"当前Health状态较差";
		}
		else if (currentHealth <= 0.7 * maxHealth)
		{
			Message = $"当前Health状态一般";
		}
		else
		{
			Message = $"当前Health状态良好";
		}
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}
public class MoveAck : Log
{
	public MoveAck(Var targetVar, Vector2 newPosition) : base(LogType.Info, targetVar.Stats.Name)
	{
		SetReportedWorldPosition(newPosition);
		Message = $"正在移动至 {ReportedCell}";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class MoveCompletedAck : Log
{
	public MoveCompletedAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Message = $"已到达 {ReportedCell}";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class AttackedWarning : Log
{
	public AttackedWarning(Var targetVar, AttackInfo atkInfo) : base(LogType.Warning, targetVar.Stats.Name)
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Objective = targetVar.Stats.Name;
		Message = $"受到异常变量干扰于 {ReportedCell}";
	}

	protected override string FormatMessage()
	{
		return $"{_warningText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class DetectedWarning : Log
{
	public DetectedWarning(DetectInfo detectInfo) : base(LogType.Warning, detectInfo.Detector.Stats.Name)
	{
		SetReportedWorldPosition(detectInfo.DetectedVar.Stats.Position);
		Objective = detectInfo.DetectedVar.Stats.Name;
		Message = $"发现 {detectInfo.DetectedVar.Stats.Type} 类型异常变量于 {ReportedCell} 处！";
	}

	protected override string FormatMessage()
	{
		return $"{_warningText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class OutOfDetectWarning : Log
{
	public OutOfDetectWarning(Var targetVar, Var lostTarget) : base(LogType.Warning, targetVar.Stats.Name)
	{
		Message = $"{lostTarget.Stats.Name} 已离开检测范围！";
	}

	protected override string FormatMessage()
	{
		return $"{_warningText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class DeathError : Log
{
	public DeathError(Var targetVar) : base(LogType.Error, targetVar.Stats.Name)
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Message = $"变量于 {ReportedCell} 失效！";
	}

	protected override string FormatMessage()
	{
		return $"{_errorText} {_timeText} {_actorText}: {_messageText}";
	}
	
}

public class EnemyRepairedInfo : Log
{
	public EnemyRepairedInfo(Var targetVar, Var repairerVar) : base(LogType.Info, repairerVar?.Stats?.Name ?? "System")
	{
		SetReportedWorldPosition(targetVar.Stats.Position);
		Message = $"已修复异常变量于 {ReportedCell}，正在待命中";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}

public class WaveStartAck : Log
{
	public WaveStartAck(int waveNumber) : base(LogType.Info, "System")
	{
		Message = $"======== 正在处理 15 个 Bug 中的第 {waveNumber} 个 ========";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}
}
