using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public static class SkillResourceCache
{
    private const string ResourceDirectory = "res://GameData/SkillResources";

    private static readonly Dictionary<string, SkillResource> Resources = new();

    public static SkillResource Get(Type skillType)
    {
        return Get(ToKebabCase(skillType.Name));
    }

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
            resource = new SkillResource { Name = skillName };
        }

        Resources[skillName] = resource;
        return resource;
    }

    private static string ToKebabCase(string value)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsUpper(current) && i > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
