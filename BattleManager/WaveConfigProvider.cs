using Godot;
using System;
using System.Collections.Generic;

public class WaveConfigProvider
{
	private const string WaveConfigPath = "res://BattleManager/WaveConfig.json";

	private readonly List<WaveConfig> _configs = new();

	public void Load()
	{
		_configs.Clear();
		if (!FileAccess.FileExists(WaveConfigPath))
		{
			GD.PushWarning($"Wave config file not found: {WaveConfigPath}");
			return;
		}

		using var file = FileAccess.Open(WaveConfigPath, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();

		var json = new Json();
		Error error = json.Parse(jsonText);
		if (error != Error.Ok)
		{
			Debug.PushError($"Failed to parse wave config JSON: {error}");
			return;
		}

		if (json.Data.VariantType != Variant.Type.Array)
		{
			Debug.PushError("Wave config JSON root must be an array.");
			return;
		}

		foreach (Variant item in json.Data.AsGodotArray())
		{
			if (item.VariantType != Variant.Type.Dictionary)
			{
				GD.PushWarning("Skipped wave config entry because it is not an object.");
				continue;
			}

			if (TryParseConfig(item.AsGodotDictionary(), out WaveConfig config))
			{
				_configs.Add(config);
			}
		}

		_configs.Sort((a, b) => a.Wave.CompareTo(b.Wave));
		Debug.Print($"Loaded {_configs.Count} wave configs.");
	}

	public WaveConfig GetConfig(int wave)
	{
		if (_configs.Count == 0)
		{
			return new WaveConfig { Wave = wave };
		}

		WaveConfig selected = null!;
		foreach (WaveConfig config in _configs)
		{
			if (config.Wave > wave)
			{
				break;
			}

			selected = config;
			if (config.Wave == wave)
			{
				break;
			}
		}

		return selected ?? new WaveConfig { Wave = wave };
	}

	private bool TryParseConfig(Godot.Collections.Dictionary dict, out WaveConfig config)
	{
		config = null!;
		int wave = GetInt(dict, "wave", _configs.Count + 1);
		if (wave <= 0)
		{
			GD.PushWarning("Skipped wave config entry because wave must be greater than 0.");
			return false;
		}

		config = new WaveConfig
		{
			Wave = wave,
			RegionCount = Math.Max(GetInt(dict, "regionCount", WaveConfig.DefaultRegionCount), 2),
			EnemyCount = Math.Max(GetInt(dict, "enemyCount", WaveConfig.DefaultEnemyCount), 1),
			EnemyBaseSpawnProbability = Mathf.Clamp(
				GetFloat(dict, "enemyBaseSpawnProbability", WaveConfig.DefaultEnemyBaseSpawnProbability),
				0.0f,
				1.0f),
			CommonSkillChance = Math.Max(GetFloat(dict, "commonSkillChance", WaveConfig.DefaultCommonSkillChance), 0.0f),
			UncommonSkillChance = Math.Max(GetFloat(dict, "uncommonSkillChance", WaveConfig.DefaultUncommonSkillChance), 0.0f),
			RareSkillChance = Math.Max(GetFloat(dict, "rareSkillChance", WaveConfig.DefaultRareSkillChance), 0.0f),
			EnemyTypeProbabilities = ParseEnemyTypeProbabilities(dict)
		};
		return true;
	}

	private static int GetInt(Godot.Collections.Dictionary dict, string key, int fallback)
	{
		return dict.ContainsKey(key) ? dict[key].AsInt32() : fallback;
	}

	private static float GetFloat(Godot.Collections.Dictionary dict, string key, float fallback)
	{
		return dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
	}

	private static Dictionary<VarStats.VarType, float> ParseEnemyTypeProbabilities(Godot.Collections.Dictionary dict)
	{
		Dictionary<VarStats.VarType, float> probabilities = new();
		if (!dict.ContainsKey("enemyTypeProbabilities"))
		{
			return probabilities;
		}

		Variant probabilityVariant = dict["enemyTypeProbabilities"];
		if (probabilityVariant.VariantType != Variant.Type.Dictionary)
		{
			GD.PushWarning("Skipped enemyTypeProbabilities because it is not an object.");
			return probabilities;
		}

		foreach (KeyValuePair<Variant, Variant> pair in probabilityVariant.AsGodotDictionary())
		{
			string typeName = pair.Key.AsString();
			if (!Enum.TryParse(typeName, ignoreCase: true, out VarStats.VarType type))
			{
				GD.PushWarning($"Skipped unknown enemy type in wave config: {typeName}");
				continue;
			}

			if (type == VarStats.VarType.Bug)
			{
				continue;
			}

			float probability = Math.Max(0.0f, (float)pair.Value.AsDouble());
			probabilities[type] = probability;
		}

		return probabilities;
	}
}

