using Godot;
using System;

public class EfficientMarketing : Skill
{
    public override string Name => "efficient-marketing";
    public override string Description => "Increases tokens gained when requesting them from users.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Uncommon;
    public override void Apply(GameData data, int stack = 1)
    {
        data.NumericData.Set("TokenRequestGainBonus", GetValue("TokenRequestGainBonus") * stack);
    }
}
