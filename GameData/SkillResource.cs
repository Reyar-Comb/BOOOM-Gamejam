using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SkillResource : Resource
{
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
