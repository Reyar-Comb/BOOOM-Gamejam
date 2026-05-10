using Godot;
using System;
using System.Collections.Generic;

public class GameData
{
    public NumericData NumericData;
    public SkillManager SkillManager;
    public GameData()
    {
        NumericData = new NumericData();
        SkillManager = new SkillManager();
    }
    public void Reset()
    {
        NumericData.Reset();
        SkillManager.Reset();
    }
}
