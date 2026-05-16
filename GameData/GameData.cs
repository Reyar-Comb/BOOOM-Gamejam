using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameData
{
    private const int SkillChoiceCount = 3;
    private static readonly VarStats.VarType[] UnlockableVarTypes =
    [
        VarStats.VarType.Int,
        VarStats.VarType.Float,
        VarStats.VarType.Long,
        VarStats.VarType.Double,
        VarStats.VarType.LongDouble,
        VarStats.VarType.Char,
        VarStats.VarType.Bool,
    ];
    private static readonly List<SkillData> AllSkillData = LoadAllSkillData();

    public NumericData NumericData;
    public SkillManager SkillManager;
    private WaveConfigProvider _waveConfigProvider;
    public HashSet<VarStats.VarType> UnlockedVarTypes { get; private set; } = new()
    {
        VarStats.VarType.Int,
        VarStats.VarType.Float,
        VarStats.VarType.Char
    };
    public void SetWaveConfigProvider(WaveConfigProvider waveConfigProvider)
    {
        _waveConfigProvider = waveConfigProvider;
    }
    public GameData()
    {
        NumericData = new NumericData();
        SkillManager = new SkillManager();
    }
    public void Reset()
    {
        NumericData.Reset();
        SkillManager.Reset();
    }

    public List<Upgrade> GetRandomSkillChoices(int wave)
    {
        List<SkillData> availableSkillData = AllSkillData.ToList();
        WaveConfig waveConfig = _waveConfigProvider?.GetConfig(wave) ?? new WaveConfig { Wave = wave };

        List<Upgrade> choices = new List<Upgrade>();
        for (int i = 0; i < SkillChoiceCount && availableSkillData.Count > 0; i++)
        {
            Skill skill = CreateWeightedRandomSkill(availableSkillData, waveConfig);
            choices.Add(new SkillUpgrade(skill));
            availableSkillData.RemoveAll(skillData => skillData.Type == skill.GetType());
        }

        return choices;
    }

    public List<Upgrade> GetRandomUpgradeChoices(int wave)
    {
        List<Upgrade> choices = GetRandomSkillChoices(wave);

        VarStats.VarType? varType = GetRandomLockedVarType();
        if (varType != null)
        {
            choices.Add(new VarTypeUnlockUpgrade(varType.Value));
        }

        return choices;
    }

    public void UnlockVarType(VarStats.VarType varType)
    {
        UnlockedVarTypes.Add(varType);
    }

    public bool IsVarTypeUnlocked(VarStats.VarType varType)
    {
        return UnlockedVarTypes.Contains(varType);
    }

    private VarStats.VarType? GetRandomLockedVarType()
    {
        List<VarStats.VarType> lockedVarTypes = UnlockableVarTypes
            .Where(varType => !UnlockedVarTypes.Contains(varType))
            .ToList();

        if (lockedVarTypes.Count == 0)
        {
            return null;
        }

        return lockedVarTypes[Random.Shared.Next(lockedVarTypes.Count)];
    }

    private static Skill CreateWeightedRandomSkill(List<SkillData> availableSkillData, WaveConfig waveConfig)
    {
        Dictionary<Skill.RarityLevel, List<SkillData>> dataByRarity = availableSkillData
            .GroupBy(skillData => skillData.Rarity)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<Skill.RarityLevel, float> rarityWeights = waveConfig.GetSkillRarityWeights();
        float totalWeight = dataByRarity.Keys.Sum(rarity => rarityWeights.GetValueOrDefault(rarity, 0.0f));
        if (totalWeight <= 0.0f)
        {
            return CreateSkill(availableSkillData[Random.Shared.Next(availableSkillData.Count)].Type);
        }

        float selectedWeight = (float)Random.Shared.NextDouble() * totalWeight;
        float currentWeight = 0.0f;

        foreach (Skill.RarityLevel rarity in rarityWeights.Keys)
        {
            if (!dataByRarity.TryGetValue(rarity, out List<SkillData> skillDataList))
            {
                continue;
            }

            float rarityWeight = rarityWeights[rarity];
            if (rarityWeight <= 0.0f)
            {
                continue;
            }

            currentWeight += rarityWeight;
            if (selectedWeight < currentWeight)
            {
                return CreateSkill(skillDataList[Random.Shared.Next(skillDataList.Count)].Type);
            }
        }

        return CreateSkill(availableSkillData[Random.Shared.Next(availableSkillData.Count)].Type);
    }

    private static List<SkillData> LoadAllSkillData()
    {
        return typeof(Skill).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Skill)) && !type.IsAbstract)
            .Select(type =>
            {
                Skill skill = CreateSkill(type);
                return new SkillData(type, skill.Rarity);
            })
            .ToList();
    }

    private static Skill CreateSkill(Type skillType)
    {
        return (Skill)Activator.CreateInstance(skillType);
    }

    private readonly struct SkillData
    {
        public readonly Type Type;
        public readonly Skill.RarityLevel Rarity;

        public SkillData(Type type, Skill.RarityLevel rarity)
        {
            Type = type;
            Rarity = rarity;
        }
    }
}
