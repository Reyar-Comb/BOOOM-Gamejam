using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class SkillManager
{
    public event Action CreationAvailabilityChanged;

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

    public void ApplyOwnedSkills(GameData data)
    {
        _activeSkillRuntimes.Clear();
        foreach (var skillGroup in OwnedSkills.GroupBy(skill => skill.GetType()))
        {
            skillGroup.First().Apply(data, skillGroup.Count());
        }
    }

    public void OnWaveStarted()
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnWaveStarted();
        }

        CreationAvailabilityChanged?.Invoke();
    }

    public bool CanCreateVar(VarStats.VarType type)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            if (!runtime.CanCreateVar(type))
            {
                return false;
            }
        }

        return true;
    }

    public void OnBeforeAttack(AttackInfo info)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnBeforeAttack(info);
        }
    }

    public void OnVarCreated(VarCreationInfo info)
    {
        Dictionary<VarStats.VarType, bool> previousAvailability = GetCreationAvailabilitySnapshot();

        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnVarCreated(info);
        }

        if (HasCreationAvailabilityChanged(previousAvailability))
        {
            CreationAvailabilityChanged?.Invoke();
        }
    }

    public void OnDetected(DetectInfo info)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnDetected(info);
        }
    }

    public void OnRegionEntered(RegionEnteredInfo info)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnRegionEntered(info);
        }
    }

    public IEnumerable<Vector2I> OnAttackRangeQuery(AttackRangeQueryInfo info, IEnumerable<Vector2I> rangeCells)
    {
        IEnumerable<Vector2I> result = rangeCells;
        foreach (var runtime in _activeSkillRuntimes)
        {
            result = runtime.OnAttackRangeQuery(info, result);
        }

        return result;
    }

    public void OnTokenOperation(TokenOperationInfo info)
    {
        foreach (var runtime in _activeSkillRuntimes)
        {
            runtime.OnTokenOperation(info);
        }
    }

    private Dictionary<VarStats.VarType, bool> GetCreationAvailabilitySnapshot()
    {
        Dictionary<VarStats.VarType, bool> snapshot = new();
        foreach (VarStats.VarType type in Enum.GetValues<VarStats.VarType>())
        {
            snapshot[type] = CanCreateVar(type);
        }

        return snapshot;
    }

    private bool HasCreationAvailabilityChanged(Dictionary<VarStats.VarType, bool> previousAvailability)
    {
        foreach (VarStats.VarType type in Enum.GetValues<VarStats.VarType>())
        {
            if (!previousAvailability.TryGetValue(type, out bool previous) || previous != CanCreateVar(type))
            {
                return true;
            }
        }

        return false;
    }
}
