using Godot;
using System;

public class EmptySkillRuntime : ISkillRuntime
{
    public void OnWaveStarted() {}
    public void OnVarCreated(VarCreationInfo info) {}
    public void OnBeforeAttack(AttackInfo info) {}
    public void OnDetected(DetectInfo info) {}
    public void OnTokenOperation() {}
}
