using Godot;
using System;
using System.Linq;

public class CrowdOfCharRuntime : ISkillRuntime
{
    public void OnWaveStarted() {}
    public void OnVarCreated() {}
    public void OnBeforeAttack(AttackInfo info)
    {
        int charAttackerCount = info.Attackers.Count(a => a.Stats.Type == VarStats.VarType.Char);
        info.Damage += charAttackerCount;
    }
    public void OnDetected() {}
    public void OnTokenOperation() {}
}
