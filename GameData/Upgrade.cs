using Godot;

public abstract class Upgrade
{
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

    public override void Apply(GameData data)
    {
        data.SkillManager.OwnedSkills.Add(Skill);
    }
}

public class VarTypeUnlockUpgrade : Upgrade
{
    public VarStats.VarType VarType { get; }

    public VarTypeUnlockUpgrade(VarStats.VarType varType)
    {
        VarType = varType;
    }

    public override string Name => $"{VarType} Unlock";
    public override string Description => $"Unlock {VarType} permanently.";
    public override Texture2D Icon => null;

    public override void Apply(GameData data)
    {
        data.UnlockVarType(VarType);
    }
}
