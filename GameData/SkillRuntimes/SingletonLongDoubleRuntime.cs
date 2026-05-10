using Godot;
using System.Collections.Generic;

public class SingletonLongDoubleRuntime : ISkillRuntime
{
    private const int HealthMultiplier = 10;
    private const int AttackDamageMultiplier = 10;

    private bool _isWaitingForFirstCreation;
    private bool _isFloatingPointCreationLocked;
    private bool _hasResolvedFirstCreation;

    public void OnWaveStarted()
    {
        _isWaitingForFirstCreation = true;
        _isFloatingPointCreationLocked = false;
        _hasResolvedFirstCreation = false;
    }

    public bool CanCreateVar(VarStats.VarType type)
    {
        return !_isFloatingPointCreationLocked || !IsFloatingPointType(type);
    }

    public void OnVarCreated(VarCreationInfo info)
    {
        if (!_isWaitingForFirstCreation || _hasResolvedFirstCreation)
        {
            return;
        }

        _hasResolvedFirstCreation = true;
        _isWaitingForFirstCreation = false;

        Var firstCreatedVar = info?.Var;
        if (firstCreatedVar?.Stats == null || firstCreatedVar.Stats.Type != VarStats.VarType.LongDouble)
        {
            return;
        }

        firstCreatedVar.Stats.MaxHealth *= HealthMultiplier;
        firstCreatedVar.Stats.CurrentHealth = firstCreatedVar.Stats.MaxHealth;
        firstCreatedVar.Stats.AttackDamage *= AttackDamageMultiplier;
        firstCreatedVar.Stats.Defense = 0;

        _isFloatingPointCreationLocked = true;
    }

    private static bool IsFloatingPointType(VarStats.VarType type)
    {
        return type == VarStats.VarType.Float
            || type == VarStats.VarType.Double
            || type == VarStats.VarType.LongDouble;
    }
}
