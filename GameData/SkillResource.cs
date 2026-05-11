using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SkillResource : Resource
{
    [Export] public string Name { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
    [Export] public Skill.RarityLevel Rarity { get; set; } = Skill.RarityLevel.Common;
    [Export] public Dictionary<string, float> Values { get; set; } = new();

    public float GetValue(string key, float fallback = 0f)
    {
        if (Values.TryGetValue(key, out float value))
        {
            return value;
        }

        GD.PushError($"SkillResource '{ResourcePath}': Value '{key}' not found.");
        return fallback;
    }
}
