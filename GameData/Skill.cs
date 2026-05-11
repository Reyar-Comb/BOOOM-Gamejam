using Godot;
using System;

public abstract class Skill
{
    public enum RarityLevel
    {
        Common,
        Uncommon,
        Rare
    }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Texture2D Icon { get; }
    public abstract RarityLevel Rarity { get;}
    protected SkillResource Resource => SkillResourceCache.Get(Name);
    public abstract void Apply(GameData data, int stack = 1);
    public virtual ISkillRuntime GetSkillRuntime() => new EmptySkillRuntime();

    protected float GetValue(string key, float fallback = 0f)
    {
        return Resource.GetValue(key, fallback);
    }
}
