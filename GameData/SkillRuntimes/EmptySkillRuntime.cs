using Godot;
using System;
using System.Collections.Generic;

public class EmptySkillRuntime : ISkillRuntime
{
    public void OnWaveStarted() {}
    public void OnVarCreated(VarCreationInfo info) {}
    public void OnBeforeAttack(AttackInfo info) {}
    public void OnDetected(DetectInfo info) {}
    public IEnumerable<Vector2I> OnAttackRangeQuery(AttackRangeQueryInfo info, IEnumerable<Vector2I> rangeCells) => rangeCells;
    public void OnTokenOperation() {}
}
