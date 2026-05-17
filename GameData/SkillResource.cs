using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SkillResource : Resource
{
    [Export] public string Name { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
    [Export] public Skill.RarityLevel Rarity { get; set; } = Skill.RarityLevel.Common;
    [Export] public Dictionary<string, int> Values { get; set; } = new();

    public int GetValue(string key, int fallback = 0)
    {
        if (Values.TryGetValue(key, out int value))
        {
            return value;
        }

        Debug.PushError($"SkillResource '{ResourcePath}': Value '{key}' not found.");
        return fallback;
    }
}

