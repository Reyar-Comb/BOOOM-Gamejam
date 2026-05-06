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
        Message = $"Successfully Created {targetVar}";
    }

    protected override string FormatMessage()
    {
        return $"[Info] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }

}

public class StatusAck : Log
{

    public StatusAck(Var targetVar) : base(LogType.Info, targetVar.ToString())
    {
        Message = $"HP={targetVar.Stats.CurrentHealth}/{targetVar.Stats.MaxHealth}, Pos={targetVar.Stats.Position}, Direction={targetVar.Stats.Direction}";
    }

    protected override string FormatMessage()
    {
        return $"[Info] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}

public class MoveAck : Log
{
    public MoveAck(Var targetVar, Vector2 newPosition) : base(LogType.Info, targetVar.ToString())
    {
        Message = $"Moving to {newPosition}";
    }

    protected override string FormatMessage()
    {
        return $"[Info] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}

public class AttackedWarning : Log
{
    public AttackedWarning(Var targetVar, AttackInfo atkInfo) : base(LogType.Warning, targetVar.ToString())
    {
        Message = $"Attacked from {atkInfo.GetFromDirection(targetVar.Stats.Position)}, Damage={atkInfo.Damage}";
    }

    protected override string FormatMessage()
    {
        return $"[Warning] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}

public class DetectedWarning : Log
{
    public DetectedWarning(Var targetVar, Var detectedVar) : base(LogType.Warning, targetVar.ToString())
    {
        Message = $"Detected {detectedVar}";
    }

    protected override string FormatMessage()
    {
        return $"[Warning] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}

public class OutOfDetectWarning : Log
{
    public OutOfDetectWarning(Var targetVar, Var lostTarget) : base(LogType.Warning, targetVar.ToString())
    {
        Message = $"Lost detection of {lostTarget}";
    }

    protected override string FormatMessage()
    {
        return $"[Warning] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}

public class DeathError : Log
{
    public DeathError(Var targetVar) : base(LogType.Error, targetVar.ToString())
    {
        Message = $"Died.";
    }

    protected override string FormatMessage()
    {
        return $"[Error] [{BattleManager.Instance.GetTimeString()}] {Actor}: {Message}";
    }
}