using Godot;
using System;
using System.Collections.Generic;

public class WaveConfig
{
	public const int DefaultRegionCount = 6;
	public const int DefaultEnemyCount = 8;
	public const float DefaultEnemyBaseSpawnProbability = 0.5f;
	public const float DefaultCommonSkillChance = 60.0f;
	public const float DefaultUncommonSkillChance = 30.0f;
	public const float DefaultRareSkillChance = 10.0f;

	public int Wave { get; init; }
	public int RegionCount { get; init; } = DefaultRegionCount;
	public int EnemyCount { get; init; } = DefaultEnemyCount;
	public float EnemyBaseSpawnProbability { get; init; } = DefaultEnemyBaseSpawnProbability;
	public float CommonSkillChance { get; init; } = DefaultCommonSkillChance;
	public float UncommonSkillChance { get; init; } = DefaultUncommonSkillChance;
	public float RareSkillChance { get; init; } = DefaultRareSkillChance;
	public Dictionary<VarStats.VarType, float> EnemyTypeProbabilities { get; init; } = new();

	public Dictionary<Skill.RarityLevel, float> GetSkillRarityWeights()
	{
		return new Dictionary<Skill.RarityLevel, float>
		{
			{ Skill.RarityLevel.Common, Math.Max(0.0f, CommonSkillChance) },
			{ Skill.RarityLevel.Uncommon, Math.Max(0.0f, UncommonSkillChance) },
			{ Skill.RarityLevel.Rare, Math.Max(0.0f, RareSkillChance) },
		};
	}

	public VarStats.VarType GetRandomEnemyType(RandomNumberGenerator random, IReadOnlyList<VarStats.VarType> fallbackTypes)
	{
		float totalWeight = 0.0f;
		foreach (VarStats.VarType type in fallbackTypes)
		{
			if (EnemyTypeProbabilities.TryGetValue(type, out float probability))
			{
				totalWeight += Math.Max(0.0f, probability);
			}
		}

		if (totalWeight <= 0.0f)
		{
			return fallbackTypes[random.RandiRange(0, fallbackTypes.Count - 1)];
		}

		float roll = (float)random.Randf() * totalWeight;
		VarStats.VarType lastWeightedType = fallbackTypes[0];
		foreach (VarStats.VarType type in fallbackTypes)
		{
			if (!EnemyTypeProbabilities.TryGetValue(type, out float probability))
			{
				continue;
			}

			float weight = Math.Max(0.0f, probability);
			if (weight <= 0.0f)
			{
				continue;
			}

			lastWeightedType = type;
			roll -= weight;
			if (roll <= 0.0f)
			{
				return type;
			}
		}

		return lastWeightedType;
	}
}
