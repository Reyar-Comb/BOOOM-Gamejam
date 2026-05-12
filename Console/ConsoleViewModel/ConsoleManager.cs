using Godot;
using System;
using System.Collections.Generic;

public partial class ConsoleManager : Node
{
    public static ConsoleManager Instance { get; private set; } = null!;

    [Signal] 
    public delegate void LogAddedEventHandler(String formattedLog);
    
    public List<Log> Logs { get; private set; } = new List<Log>();

    private readonly Dictionary<Var, List<Callable>> _logSources = new Dictionary<Var, List<Callable>>();


    public override void _Ready()
    {
        Instance = this;
    }


    
    public IReadOnlyList<String> FormattedLogs
    {
        get
        {
            List<string> formatted = new List<string>();
            foreach (var log in Logs)
            {
                formatted.Add(log.ToString());
            }
            return formatted;
        }
    }

    public void AddLog(Log log)
    {
        Logs.Add(log);
        GD.Print($"Added log: {log.ToString()}");
        EmitSignal(SignalName.LogAdded, log.ToString());
    }

    public void SubscribeVarEvents(Var v)
    {
        if (v == null) return;
        if (_logSources.ContainsKey(v)) return;

        var callables = new List<Callable>();

        var onDetectedCallable = Callable.From((DetectInfo detectInfo) => {
            AddLog(new DetectedWarning(detectInfo));
        });
        v.Connect(Var.SignalName.OnDetected, onDetectedCallable);
        callables.Add(onDetectedCallable);

        var onAttackedCallable = Callable.From((int dmg, Var srcVar) => {
            AddLog(new AttackedWarning(v, new AttackInfo { Source = srcVar, Damage = dmg }));
        });
        v.Connect(Var.SignalName.OnAttacked, onAttackedCallable);
        callables.Add(onAttackedCallable);

        var onDeathCallable = Callable.From(() => {
            AddLog(new DeathError(v));
        });
        v.Stats.Connect(VarStats.SignalName.OnDeath, onDeathCallable);
        callables.Add(onDeathCallable);

        _logSources[v] = callables;
    }

    public void UnsubscribeVarEvents(Var v)
    {
        if (v == null) return;
        if (!_logSources.ContainsKey(v)) return;

        var callables = _logSources[v];
        v.Disconnect(Var.SignalName.OnDetected, callables[0]);
        v.Disconnect(Var.SignalName.OnAttacked, callables[1]);
        v.Stats?.Disconnect(VarStats.SignalName.OnDeath, callables[2]);

        _logSources.Remove(v);
    }

    public void UnsubscribeAllVarEvents()
    {
        foreach (Var v in new List<Var>(_logSources.Keys))
        {
            UnsubscribeVarEvents(v);
        }
    }


    public void RegisterVar(Var v)
    {
        if (v == null) return;
        SubscribeVarEvents(v);
        AddLog(new CreateAck(v));
    }

    public void QueryVarStatus(Var v)
    {
        if (v == null) return;
        AddLog(new StatusAck(v));
    }

    public void MoveVar(Var v, Vector2 newPosition)
    {
        if (v == null) return;
        AddLog(new MoveAck(v, newPosition));
    }

    public void QueryLocation(Var v)
    {
        if (v == null) return;
        AddLog(new LocationAck(v));
    }

    public void QueryHealth(Var v)
    {
        if (v == null) return;
        AddLog(new HealthAck(v));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsPressed() && @event is InputEventKey keyEvent && keyEvent.Keycode == Key.W)
        {
            AddLog(new AttackedWarning(
                new Var { Stats = new VarStats { Name = "TestVar", CurrentHealth = 50, MaxHealth = 100, Position = Vector2.Zero, Direction = Vector2.Right } },
                new AttackInfo { Source = new Var { Stats = new VarStats { Name = "Attacker", CurrentHealth = 100, MaxHealth = 100, Position = Vector2.Zero, Direction = Vector2.Right } }, Damage = 20 }
            ));
        }
        else if (@event.IsPressed() && @event is InputEventKey keyEvent2 && keyEvent2.Keycode == Key.E)
        {
            AddLog(new DeathError(
                new Var { Stats = new VarStats { Name = "TestVar", CurrentHealth = 0, MaxHealth = 100, Position = Vector2.Zero, Direction = Vector2.Right } }
            ));
        }
    }

}
