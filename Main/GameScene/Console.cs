using Godot;
using System;

public partial class Console : RichTextLabel
{
	[Export]
	public ConsoleManager ConsoleManager { get; set; }
	public override void _Ready()
	{
		ConsoleManager.LogAdded += OnLogUpdated;
	}

	public void OnLogUpdated(string logText)
	{
		this.Text += logText + "\n";
	}
}
