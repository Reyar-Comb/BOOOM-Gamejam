using Godot;
using System;

public interface ISkillRuntime
{
    void OnWaveStarted();
    void OnVarCreated();
    void OnBeforeAttack(AttackInfo info);
    void OnDetected();
    void OnTokenOperation();
}
