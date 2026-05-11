using Godot;
using System.Collections.Generic;

public static class SkillResourceCache
{
    private const string ResourceDirectory = "res://GameData/SkillResources";

    private static readonly Dictionary<string, SkillResource> Resources = new();

    public static SkillResource Get(string skillName)
    {
        if (Resources.TryGetValue(skillName, out SkillResource resource))
        {
            return resource;
        }

        string path = $"{ResourceDirectory}/{skillName}.tres";
        resource = ResourceLoader.Load<SkillResource>(path);
        if (resource == null)
        {
            GD.PushError($"SkillResourceCache: Resource '{path}' not found.");
            resource = new SkillResource();
        }

        Resources[skillName] = resource;
        return resource;
    }
}
