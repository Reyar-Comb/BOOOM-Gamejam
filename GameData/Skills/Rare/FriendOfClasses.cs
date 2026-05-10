using Godot;
using System;

public class FriendOfClasses : Skill
{
    public override string Name => "friend-of-classes";
    public override string Description => "";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new FriendOfClassesRuntime();
}
