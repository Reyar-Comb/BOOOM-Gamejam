using Godot;
using System;
using System.Collections.Generic;

public class SkillManager
{
    public List<Skill> OwnedSkills { get; private set; } = new List<Skill>();
    private List<ISkillRuntime> _activeSkillRuntimes = new List<ISkillRuntime>();
    public void AddRuntime(ISkillRuntime runtime)
    {
        _activeSkillRuntimes.Add(runtime);
    }
    public void Reset()
    {
        _activeSkillRuntimes.Clear();
    }
    public void OnBeforeAttack(AttackInfo info)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnBeforeAttack(info);
        }
    }
}
