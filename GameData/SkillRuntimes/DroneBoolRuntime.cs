using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DroneBoolRuntime : ISkillRuntime
{
    public void OnDetected(DetectInfo info)
    {
        if (info.DetectedVar.Stats.Type == VarStats.VarType.Bool)
        {
            info.ShouldRender = true;
        }
    }
}
