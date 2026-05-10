using Godot;
using System;

public class EmptySkillRuntime : ISkillRuntime
{
    public void OnWaveStarted() {}
    public void OnVarCreated() {}
    public void OnBeforeAttack(AttackInfo info) {}
    public void OnDetected() {}
    public void OnTokenOperation() {}
}
