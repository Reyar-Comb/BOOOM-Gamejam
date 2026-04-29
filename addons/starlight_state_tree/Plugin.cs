#if TOOLS
using Godot;
using System;

[Tool]
public partial class Plugin : EditorPlugin
{
	public override void _EnterTree()
	{
		AddCustomType("STNode", "RefCounted",
		ResourceLoader.Load<Script>("res://addons/starlight_state_tree/nodes/STNode.cs"),
		ResourceLoader.Load<Texture2D>("res://addons/starlight_state_tree/icons/STNode.svg"));

		AddCustomType("STRoot", "STNode",
		ResourceLoader.Load<Script>("res://addons/starlight_state_tree/nodes/STRoot.cs"),
		ResourceLoader.Load<Texture2D>("res://addons/starlight_state_tree/icons/STRoot.svg"));
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
	}
}
#endif
