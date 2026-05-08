using Godot;
using System;
using System.Collections.Generic;

public class GameData
{
    public NumericData NumericData;
    public List<Skill> OwnedSkills { get; private set; } = new List<Skill>();
    public GameData()
    {
        NumericData = new NumericData();
    }
    public void Reset()
    {
        NumericData.Reset();
    }
}
