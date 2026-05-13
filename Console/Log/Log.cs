using System;
using Godot;

public abstract class Log
{
	public string Time { get; set; }
	public LogType Type { get; set; }
	public string Actor { get; set; }
	public string Message { get; set; }


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
		Message = $"成功创建 {targetVar.Stats.Type} 于 {Grid.WorldToGrid(targetVar.Stats.Position)} ！";
	}

	protected override string FormatMessage()
	{
		return $"{_infoText} {_timeText} {_actorText}: {_messageText}";
	}

}


public class LocationAck : Log
{
	public LocationAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		Message = $"Current Location: {Grid.WorldToGrid(targetVar.Stats.Position)}";
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
		Message = $"正在移动至 {Grid.WorldToGrid(newPosition)}";
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
		Message = $"受到异常变量干扰于 {Grid.WorldToGrid(targetVar.Stats.Position)}";
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
		Message = $"发现 {detectInfo.DetectedVar.Stats.Type} 类敌人于 {Grid.WorldToGrid(detectInfo.DetectedVar.Stats.Position)} 处！";
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
		Message = $"Lost detection of {lostTarget.Stats.Name}";
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
		Message = $"变量于 {Grid.WorldToGrid(targetVar.Stats.Position)} 失效！";
	}

	protected override string FormatMessage()
	{
		return $"{_errorText} {_timeText} {_actorText}: {_messageText}";
	}
    
}

