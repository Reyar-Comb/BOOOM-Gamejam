using Godot;
using System;
using System.Linq;

public class DroneBoolRuntime : ISkillRuntime
{
    public void OnWaveStarted() {}
    public void OnVarCreated(VarCreationInfo info) {}
    public void OnBeforeAttack(AttackInfo info) {}
    public void OnDetected(DetectInfo info)
    {
        if (info.DetectedVar.Stats.Type == VarStats.VarType.Bool)
        {
            info.ShouldRender = true;
        }
    }
    public void OnTokenOperation() {}
}
