using Godot;
using System;

public interface ISkillRuntime
{
    void OnWaveStarted();
    void OnVarCreated(VarCreationInfo info);
    void OnBeforeAttack(AttackInfo info);
    void OnDetected(DetectInfo info);
    void OnTokenOperation();
}
