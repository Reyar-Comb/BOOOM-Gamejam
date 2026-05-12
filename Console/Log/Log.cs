using System;
using Godot;

public abstract class Log
{
	public string Time { get; set; }
	public LogType Type { get; set; }
	public string Actor { get; set; }
	public string Message { get; set; }

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
		Message = $"Successfully Created {targetVar.Stats.Name}";
	}

	protected override string FormatMessage()
	{
		return $"[color=a0a9fe][b]INFO[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}

}

public class StatusAck : Log
{

	public StatusAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		Message = $"HP={targetVar.Stats.CurrentHealth}/{targetVar.Stats.MaxHealth}, Pos={targetVar.Stats.Position}, Direction={targetVar.Stats.Direction}";
	}

	protected override string FormatMessage()
	{
		return $"[color=a0a9fe][b]INFO[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
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
		return $"[color=a0a9fe][b]INFO[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}

public class HealthAck : Log
{
	public HealthAck(Var targetVar) : base(LogType.Info, targetVar.Stats.Name)
	{
		Message = $"Current Health: {targetVar.Stats.CurrentHealth}/{targetVar.Stats.MaxHealth}";
	}

	protected override string FormatMessage()
	{
		return $"[color=a0a9fe][b]INFO[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}
public class MoveAck : Log
{
	public MoveAck(Var targetVar, Vector2 newPosition) : base(LogType.Info, targetVar.Stats.Name)
	{
		Message = $"Moving to {Grid.WorldToGrid(newPosition)}";
	}

	protected override string FormatMessage()
	{
		return $"[color=a0a9fe][b]INFO[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}

public class AttackedWarning : Log
{
	public AttackedWarning(Var targetVar, AttackInfo atkInfo) : base(LogType.Warning, targetVar.Stats.Name)
	{
		Message = $"Attacked from {atkInfo.GetFromDirection(targetVar.Stats.Position)}, Damage={atkInfo.Damage}";
	}

	protected override string FormatMessage()
	{
		return $"[color=orange][b]WARNING[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}

public class DetectedWarning : Log
{
	public DetectedWarning(DetectInfo detectInfo) : base(LogType.Warning, detectInfo.Detector.Stats.Name)
	{
		Message = $"Detected {detectInfo.DetectedVar.Stats.Name}";
	}

	protected override string FormatMessage()
	{
		return $"[color=orange][b]WARNING[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
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
		return $"[color=orange][b]WARNING[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}

public class DeathError : Log
{
	public DeathError(Var targetVar) : base(LogType.Error, targetVar.Stats.Name)
	{
		Message = $"Died.";
	}

	protected override string FormatMessage()
	{
		return $"[color=red][b]ERROR[/b][/color] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
	}
}
