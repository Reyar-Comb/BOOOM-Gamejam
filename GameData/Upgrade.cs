using System.Collections.Generic;
using Godot;

public abstract class Upgrade
{
    public abstract Skill.RarityLevel Rarity { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Texture2D Icon { get; }
    public abstract void Apply(GameData data);
}

public class SkillUpgrade : Upgrade
{
    public Skill Skill { get; }

    public SkillUpgrade(Skill skill)
    {
        Skill = skill;
    }

    public override string Name => Skill.Name;
    public override string Description => Skill.Description;
    public override Texture2D Icon => Skill.Icon;
    public override Skill.RarityLevel Rarity => Skill.Rarity;
    public override void Apply(GameData data)
    {
        data.SkillManager.OwnedSkills.Add(Skill);
    }
}

public class VarTypeUnlockUpgrade : Upgrade
{
    public VarStats.VarType VarType { get; }
    public override Skill.RarityLevel Rarity => Skill.RarityLevel.Special;
    private static readonly Dictionary<VarStats.VarType, Texture2D> VarTypeIcons = new Dictionary<VarStats.VarType, Texture2D>
    {
        [VarStats.VarType.Bool] = ResourceLoader.Load<Texture2D>("res://assets/Image/Bol.png"),
        [VarStats.VarType.Long] = ResourceLoader.Load<Texture2D>("res://assets/Image/Lon.png"),
        [VarStats.VarType.LongDouble] = ResourceLoader.Load<Texture2D>("res://assets/Image/LDb.png"),
        [VarStats.VarType.Double] = ResourceLoader.Load<Texture2D>("res://assets/Image/Dob.png")
    };
    public VarTypeUnlockUpgrade(VarStats.VarType varType)
    {
        VarType = varType;
    }

    public override string Name => $"{VarType} Unlock";
    public override string Description => $"Unlock {VarType} permanently.";
    public override Texture2D Icon => VarTypeIcons.TryGetValue(VarType, out var icon) ? icon : null;

    public override void Apply(GameData data)
    {
        data.UnlockVarType(VarType);
    }
}
