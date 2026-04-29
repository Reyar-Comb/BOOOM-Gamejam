using Godot;
using System;
using StarlightBT.Data;
using StarlightStateTree;
public partial class Var_MoveState : STNode
{
    public override string Name => "Move";
    protected override void OnEnter()
    {
        GD.Print("Entered Move State");
    }
}
