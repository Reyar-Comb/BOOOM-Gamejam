using Godot;
using System;

public abstract class Skill
{
    public enum RarityLevel
    {
        Common,
        Uncommon,
        Rare,
        Special
    }
    public string Name => Resource.Name;
    public string Description => Resource.Description;
    public Texture2D Icon => Resource.Icon;
    public RarityLevel Rarity => Resource.Rarity;
    protected SkillResource Resource => SkillResourceCache.Get(GetType());
    public abstract void Apply(GameData data, int stack = 1);
    public virtual ISkillRuntime GetSkillRuntime() => new EmptySkillRuntime();

    protected int GetValue(string key, int fallback = 0)
    {
        return Resource.GetValue(key, fallback);
    }
}
