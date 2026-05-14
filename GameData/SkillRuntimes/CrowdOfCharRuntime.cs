using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CrowdOfCharRuntime : ISkillRuntime
{
    private readonly SkillResource _resource;

    public CrowdOfCharRuntime(SkillResource resource)
    {
        _resource = resource;
    }

    public void OnBeforeAttack(AttackInfo info)
    {
        if (info == null || info.Attackers == null || info.Attackers.Count == 0 || info.Target == null || info.Target.IsDead)
        {
            return;
        }
        int charAttackerCount = info.Attackers.Count(a => a.Stats.Type == VarStats.VarType.Char);
        info.Damage += charAttackerCount * _resource.GetValue("DamageBonusPerCharAttacker");
    }
}
