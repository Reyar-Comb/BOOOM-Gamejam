using Godot;
using System;

public class GarbageCollector : Skill
{
    public override string Name => "garbage-collector";
    public override string Description => "Refunds a small amount of tokens when a var dies.";
    public override Texture2D Icon => null;
    public override RarityLevel Rarity => RarityLevel.Rare;
    public override void Apply(GameData data)
    {
        data.NumericData.Set("DeathTokenRefundMultiplier", 0.2f);
    }
}
