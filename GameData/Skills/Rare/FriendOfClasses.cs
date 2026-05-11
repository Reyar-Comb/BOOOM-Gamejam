using Godot;
using System;

public class FriendOfClasses : Skill
{
    public override string Name => "friend-of-classes";
    public override string Description => "Vars gain fixed positive stats when entering a new region for the first time.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data, int stack = 1)
    {
        data.SkillManager.AddRuntime(GetSkillRuntime());
    }

    public override ISkillRuntime GetSkillRuntime() => new FriendOfClassesRuntime(Resource);
}
