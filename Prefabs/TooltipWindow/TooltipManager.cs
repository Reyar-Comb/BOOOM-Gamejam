using Godot;
using System;
using System.Collections.Generic;

public partial class TooltipManager : Node
{

	[Export] public PackedScene TooltipScene { get; private set; } = null!;
	[Export] public VarRenderer VarRenderer { get; private set; } = null!;


	public static TooltipManager Instance { get; private set; } = null!;
	public Dictionary<string, Func<string>> TooltipContents { get; private set; } = new Dictionary<string, Func<string>>();

	private TooltipWindow _tooltipWindow;
	private Func<string> _currentContentFunc;
	private bool _currentHideOnClick = true;

	public override void _Ready()
	{
		Instance = this;
		_tooltipWindow = TooltipScene.Instantiate<TooltipWindow>();

  
		var canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 100;
		AddChild(canvasLayer);
		canvasLayer.AddChild(_tooltipWindow);

		LoadTooltipContent();
		ManualSetTooltipContents();
	}

	public void ShowTooltip(string tooltipId, bool hideOnClick = true)
	{
		if (string.IsNullOrEmpty(tooltipId))
			return;

		if (!TooltipContents.TryGetValue(tooltipId, out var content))
		{
			GD.PrintErr($"Tooltip ID '{tooltipId}' not found!");
			return;
		}

		_currentContentFunc = content;
		_currentHideOnClick = hideOnClick;
		_tooltipWindow.Show();
	}

	public void HideTooltip()
	{
		_tooltipWindow.Hide();
		_currentContentFunc = null;
	}

	public override void _Process(double delta)
	{
		if (!_tooltipWindow.Visible || _currentContentFunc == null)
			return;

		var mousePos = GetViewport().GetMousePosition();
		_tooltipWindow.GlobalPosition = mousePos + new Vector2(10, 10);

		string text = _currentContentFunc();
		_tooltipWindow.SetContent(string.IsNullOrEmpty(text) ? "..." : text);
	}

	public override void _Input(InputEvent @event)
	{
		if (_currentHideOnClick && @event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			HideTooltip();
		}
	}



	public void ManualSetTooltipContents()
	{
		// Map tooltip
		TooltipContents["map"] = () =>
		{
			var cell = VarRenderer.HoveredGridCell;
			return cell.HasValue ? $"{cell.Value.X}, {cell.Value.Y}" : "";
		};
	}

	public void LoadTooltipContent()
	{
		string path = "res://GameData/TooltipContent/TooltipContent.json";

		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Tooltip content file not found: {path}");
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string jsonText = file.GetAsText();

		var json = new Json();
		var error = json.Parse(jsonText);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to parse tooltip JSON: {error}");
			return;
		}

		var data = json.Data;
		if (data.VariantType != Variant.Type.Array)
		{
			GD.PrintErr("Tooltip JSON root must be an array.");
			return;
		}

		TooltipContents.Clear();
		foreach (var item in data.AsGodotArray())
		{
			var dict = item.AsGodotDictionary();
			string id = dict["id"].AsString();
			var contentArray = dict["content"].AsGodotArray();

			var lines = new System.Collections.Generic.List<string>();
			foreach (var line in contentArray)
				lines.Add(line.AsString());

			TooltipContents[id] = () => {
				return string.Join("\n", lines);
			};
		}

		GD.Print($"Loaded {TooltipContents.Count} tooltip entries from JSON.");
	}
}
