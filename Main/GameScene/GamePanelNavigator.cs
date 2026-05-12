using Godot;
using System;
using System.Collections.Generic;

public partial class GamePanelNavigator : PanelNavigator
{
	[Export]
	public Button BackButton { get; set; }
	[Export]
	public Godot.Collections.Array<RichTextLabel> SelectedVarLabels { get; set; }
	[Export]
	public Godot.Collections.Array<RichTextLabel> LocationLabels { get; set; }
	[Export]
	public VarManager VarManager { get; private set; } = null!;

	private List<PackedScene> _varButtons = new List<PackedScene>();

	private PackedScene _buttonScene;

	public override string CurrentVarName { get;
		set
		{
			field = value;
			foreach (var label in SelectedVarLabels)
			{
				label.Text = $"[b]{value}[/b]";
			}
		}
	} = "";

	[Export]
	private VBoxContainer _varListContainer;

	public override Vector2I LastClickedGrid { get; 
		set
		{
			field = value;
			foreach (var label in LocationLabels)
			{
				label.Text = $"[b][i]X: {value.X}\nY: {value.Y}[/i][/b]";
			}
		}
	} = Vector2I.Zero;

	public override void _Ready()
	{
		base._Ready();

		BackButton.Pressed += GoBack;

		_buttonScene = ResourceLoader.Load<PackedScene>("res://Prefabs/VarButton/VarButton.tscn");
		
		// ————— VarAddUnit 按钮绑定 —————

		// Int 类型变量
		InitAddButton();
		// Place 操作
		
		var placeBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/PanelContainer/PlaceButton");
		if (placeBtn == null)
		{
			GD.PrintErr("PlaceButton not found in VarAddUnit panel!");
		}
		RegisterButton(placeBtn, "",
			onPressed: () =>
			{
				GD.Print($"Placing var at {LastClickedGrid}");
				if (CurrentVarType == null)
				{
					GD.PrintErr("No var type selected for placement!");
					return;
				}
				BattleManager.Instance.RegisterVar(CurrentVarType.Value, LastClickedGrid);
				GoToRoot();
			},
			onHoverEnter: () =>
			{
				if (CurrentVarType == null) return;
				GD.Print($"Hovering");
				BattleManager.Instance.RegisterVar(CurrentVarType.Value, LastClickedGrid, isHovering: true);
			},
			onHoverLeave: () =>
			{
				GD.Print($"Hover leave");
				BattleManager.Instance.ClearCostRef();
			}
		);
		var mvBtn = FindInPanel<VarButton>("VarMoveUnit", "VBoxContainer/PanelContainer2/MoveButton");
		RegisterButton(mvBtn, "",
			onPressed: () =>
			{
				BattleManager.Instance.MoveVar(VarManager.GetVarByName(CurrentVarName), LastClickedGrid);
				GoToRoot();
			},
			onHoverEnter: () =>
			{
				BattleManager.Instance.MoveVar(VarManager.GetVarByName(CurrentVarName), LastClickedGrid, isHovering: true);
				//BattleManager.Instance.ExchangeToken(isHovering: true);
			},
			onHoverLeave: () =>
			{
				BattleManager.Instance.ClearCostRef();
			}
		);
		var locationBtn = FindInPanel<VarButton>("VarStatusUnit", "VBoxContainer/ScrollContainer/VBoxContainer/OpLocationButton");
		RegisterButton(locationBtn, "",
			onPressed: () =>
			{
				BattleManager.Instance.QueryVarLocation(VarManager.GetVarByName(CurrentVarName));
				GoToRoot();
			},
			onHoverEnter: () =>
			{
				BattleManager.Instance.QueryVarLocation(VarManager.GetVarByName(CurrentVarName), isHovering: true);
			},
			onHoverLeave: () =>
			{
				BattleManager.Instance.ClearCostRef();
			}
		);
		var healthBtn = FindInPanel<VarButton>("VarStatusUnit", "VBoxContainer/ScrollContainer/VBoxContainer/OpHealthButton");
		RegisterButton(healthBtn, "",
			onPressed: () =>
			{				
				BattleManager.Instance.QueryVarHealth(VarManager.GetVarByName(CurrentVarName));
				GoToRoot();
			},
			onHoverEnter: () =>
			{
				BattleManager.Instance.QueryVarHealth(VarManager.GetVarByName(CurrentVarName), isHovering: true);
			},
			onHoverLeave: () =>
			{
				BattleManager.Instance.ClearCostRef();
			}
		);
	}
	public void RefreshVarList()
	{
		var children = _varListContainer.GetChildren();
		foreach (Node child in children)		{
			child.QueueFree();
		}

		foreach (Var var in VarManager.Vars)
		{
			if (var.Stats.VarTeam != VarStats.Team.Friendly) continue;
			
			VarButton varButton = _buttonScene.Instantiate<VarButton>();
			CallDeferred("InstantiateVarButton", varButton, var);
		}
	}
	public void InitAddButton()
	{
		var addIntBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddIntButton");
		RegisterButton(addIntBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Int;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);

		var addFloatBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddFloatButton");
		RegisterButton(addFloatBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Float;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);

		var addDoubleBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddDoubleButton");
		RegisterButton(addDoubleBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Double;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);	

		var addLongBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddLongButton");
		RegisterButton(addLongBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Long;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);

		var addLongDoubleBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddLongDoubleButton");
		RegisterButton(addLongDoubleBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.LongDouble;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);

		var addBoolBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddBoolButton");
		RegisterButton(addBoolBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Bool;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);

		var addCharBtn = FindInPanel<VarButton>("VarAddUnit", "VBoxContainer/ScrollContainer/VBoxContainer/AddCharButton");
		RegisterButton(addCharBtn, "",
			onPressed: () =>
			{
				CurrentVarType = VarStats.VarType.Char;
				GD.Print($"Selected var type: {CurrentVarType}");
			}
		);
	}

	private void InstantiateVarButton(VarButton varButton, Var var)
	{
		_varListContainer.AddChild(varButton);
		varButton.SetText(var.Stats.Name);
		varButton.SetStyle(var.Stats.Type);
		RegisterButton(varButton, "VarOpUnit",
			onPressed: () =>
			{
				CurrentVarName = var.Stats.Name;
				CurrentVarType = var.Stats.Type;
				GD.Print($"Selected var: {CurrentVarName} of type {CurrentVarType}");
			}
		);
	}
}
