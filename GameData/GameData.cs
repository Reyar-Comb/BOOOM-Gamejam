using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameData
{
    private const int SkillChoiceCount = 3;
    private static readonly Dictionary<Skill.RarityLevel, int> SkillRarityWeights = new()
    {
        { Skill.RarityLevel.Common, 60 },
        { Skill.RarityLevel.Uncommon, 30 },
        { Skill.RarityLevel.Rare, 10 },
    };
    private static readonly List<SkillData> AllSkillData = LoadAllSkillData();

    public NumericData NumericData;
    public SkillManager SkillManager;
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

    public List<Skill> GetRandomSkillChoices()
    {
        List<SkillData> availableSkillData = AllSkillData;

        List<Skill> choices = new List<Skill>();
        for (int i = 0; i < SkillChoiceCount && availableSkillData.Count > 0; i++)
        {
            Skill skill = CreateWeightedRandomSkill(availableSkillData);
            choices.Add(skill);
            availableSkillData.RemoveAll(skillData => skillData.Type == skill.GetType());
        }

        return choices;
    }

    private static Skill CreateWeightedRandomSkill(List<SkillData> availableSkillData)
    {
        Dictionary<Skill.RarityLevel, List<SkillData>> dataByRarity = availableSkillData
            .GroupBy(skillData => skillData.Rarity)
            .ToDictionary(group => group.Key, group => group.ToList());

        int totalWeight = dataByRarity.Keys.Sum(rarity => SkillRarityWeights.GetValueOrDefault(rarity, 0));
        int selectedWeight = Random.Shared.Next(totalWeight);
        int currentWeight = 0;

        foreach (Skill.RarityLevel rarity in SkillRarityWeights.Keys)
        {
            if (!dataByRarity.TryGetValue(rarity, out List<SkillData> skillDataList))
            {
                continue;
            }

            currentWeight += SkillRarityWeights[rarity];
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
