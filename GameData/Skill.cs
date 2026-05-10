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
    public abstract void Apply(GameData data, int stack = 1);
    public virtual ISkillRuntime GetSkillRuntime() => new EmptySkillRuntime();
}
