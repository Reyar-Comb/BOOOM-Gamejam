using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class InitialSkills : Resource
{
    [Export] public bool EffectiveFeedback { get; set; } = false;
    [Export] public bool FastIteration { get; set; } = false;
    [Export] public bool GreaterRepairing { get; set; } = false;
    [Export] public bool GreaterSafety { get; set; } = false;
    [Export] public bool GreaterStability { get; set; } = false;

    [Export] public bool EffectiveCreation { get; set; } = false;
    [Export] public bool EfficientMarketing { get; set; } = false;
    [Export] public bool PersonalizedRecommendation { get; set; } = false;
    [Export] public bool RepairerFloatingPoint { get; set; } = false;
    [Export] public bool SaferInteger { get; set; } = false;

    [Export] public bool AccurateFloat { get; set; } = false;
    [Export] public bool BerserkLong { get; set; } = false;
    [Export] public bool BiasedRecognition { get; set; } = false;
    [Export] public bool CrowdOfChar { get; set; } = false;
    [Export] public bool DroneBool { get; set; } = false;
    [Export] public bool FriendOfClasses { get; set; } = false;
    [Export] public bool GarbageCollector { get; set; } = false;
    [Export] public bool GrowingInt { get; set; } = false;
    [Export] public bool SingletonLongDouble { get; set; } = false;
    [Export] public bool SniperDouble { get; set; } = false;

    public IEnumerable<Skill> CreateSkills()
    {
        if (EffectiveFeedback) yield return new EffectiveFeedback();
        if (FastIteration) yield return new FastIteration();
        if (GreaterRepairing) yield return new GreaterRepairing();
        if (GreaterSafety) yield return new GreaterSafety();
        if (GreaterStability) yield return new GreaterStability();

        if (EffectiveCreation) yield return new EffectiveCreation();
        if (EfficientMarketing) yield return new EfficientMarketing();
        if (PersonalizedRecommendation) yield return new PersonalizedRecommendation();
        if (RepairerFloatingPoint) yield return new RepairerFloatingPoint();
        if (SaferInteger) yield return new SaferInteger();

        if (AccurateFloat) yield return new AccurateFloat();
        if (BerserkLong) yield return new BerserkLong();
        if (BiasedRecognition) yield return new BiasedRecognition();
        if (CrowdOfChar) yield return new CrowdOfChar();
        if (DroneBool) yield return new DroneBool();
        if (FriendOfClasses) yield return new FriendOfClasses();
        if (GarbageCollector) yield return new GarbageCollector();
        if (GrowingInt) yield return new GrowingInt();
        if (SingletonLongDouble) yield return new SingletonLongDouble();
        if (SniperDouble) yield return new SniperDouble();
    }
}
