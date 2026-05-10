using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CrowdOfCharRuntime : ISkillRuntime
{
    public void OnBeforeAttack(AttackInfo info)
    {
        int charAttackerCount = info.Attackers.Count(a => a.Stats.Type == VarStats.VarType.Char);
        info.Damage += charAttackerCount;
    }
}
