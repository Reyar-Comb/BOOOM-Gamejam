using Godot;
using System;
using System.Collections.Generic;

public interface ISkillRuntime
{
    void OnWaveStarted();
    void OnVarCreated(VarCreationInfo info);
    void OnBeforeAttack(AttackInfo info);
    void OnDetected(DetectInfo info);
    IEnumerable<Vector2I> OnAttackRangeQuery(AttackRangeQueryInfo info, IEnumerable<Vector2I> rangeCells);
    void OnTokenOperation();
}
