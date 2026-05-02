#nullable enable
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

namespace Booooom.Editor.RangeEditor;

[Tool]
public partial class RangeEditorPanel : VBoxContainer
{
    private const string DefaultFileName = "VarAttackRange.tres";

    private enum PendingAction
    {
        None,
        NewResource,
        OpenResource
    }

    public RangeEditorPlugin Plugin { get; set; } = null!;

    private AttackRangeGridControl _grid = null!;
    private Label _resourceLabel = null!;
    private Label _facingLabel = null!;
    private Label _hoverOverlayLabel = null!;
    private FileDialog _openDialog = null!;
    private FileDialog _saveDialog = null!;
    private ConfirmationDialog _discardChangesDialog = null!;
    private AcceptDialog _messageDialog = null!;

    private VarAttackRange _currentResource = null!;
    private string _currentPath = string.Empty;
    private bool _hasUnsavedChanges;
    private PendingAction _pendingAction = PendingAction.None;

    public override void _Ready()
    {
        Name = "RangeEditorPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        BuildInterface();
        CreateNewResource();
    }

    private void BuildInterface()
    {
        AddChild(BuildToolbar());
        AddChild(BuildGridArea());
        AddChild(BuildDialogs());

        _grid.SelectionChanged += OnGridSelectionChanged;
        _grid.HoverCellChanged += OnGridHoverCellChanged;
        _grid.ViewRotationChanged += _ => UpdateFacingLabel();
    }

    private Control BuildToolbar()
    {
        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 6);

        HBoxContainer toolbar = new();
        margin.AddChild(toolbar);

        toolbar.AddChild(CreateButton("New", "Create a new empty VarAttackRange resource.", OnNewPressed));
        toolbar.AddChild(CreateButton("Open", "Open an existing VarAttackRange resource from the project.", OnOpenPressed));
        toolbar.AddChild(CreateButton("Save", "Save the current VarAttackRange resource.", OnSavePressed));
        toolbar.AddChild(CreateButton("Save As", "Save the current VarAttackRange resource under a new path.", OnSaveAsPressed));

        toolbar.AddChild(new VSeparator());

        toolbar.AddChild(CreateButton("Rotate 90 Left", "Preview the range with the facing rotated 90 degrees counter-clockwise.", () => _grid.RotateLeft()));
        toolbar.AddChild(CreateButton("Rotate 180", "Preview the range with the facing flipped 180 degrees.", () => _grid.RotateHalfTurn()));
        toolbar.AddChild(CreateButton("Rotate 90 Right", "Preview the range with the facing rotated 90 degrees clockwise.", () => _grid.RotateRight()));
        toolbar.AddChild(CreateButton("Reset View", "Center the grid and restore the default zoom level.", () => _grid.ResetView()));

        toolbar.AddChild(new VSeparator());

        _resourceLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.AddChild(_resourceLabel);

        _facingLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(110.0f, 0.0f)
        };
        toolbar.AddChild(_facingLabel);

        return margin;
    }

    private Control BuildGridArea()
    {
        MarginContainer margin = new();
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);

        Panel panel = new();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        margin.AddChild(panel);

        Control overlayRoot = new();
        overlayRoot.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        overlayRoot.SizeFlagsVertical = SizeFlags.ExpandFill;
        overlayRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(overlayRoot);

        _grid = new AttackRangeGridControl();
        _grid.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlayRoot.AddChild(_grid);

        PanelContainer hoverPanel = new()
        {
            AnchorLeft = 0.0f,
            AnchorTop = 0.0f,
            AnchorRight = 0.0f,
            AnchorBottom = 0.0f,
            OffsetLeft = 12.0f,
            OffsetTop = 12.0f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        overlayRoot.AddChild(hoverPanel);

        _hoverOverlayLabel = new Label
        {
            Text = "Offset: --"
        };
        hoverPanel.AddChild(_hoverOverlayLabel);

        return margin;
    }

    private Control BuildDialogs()
    {
        Control root = new();

        _openDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Resources,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Open VarAttackRange",
            UseNativeDialog = true
        };
        _openDialog.Filters = new[] { "*.tres;Godot Text Resource", "*.res;Godot Binary Resource" };
        _openDialog.FileSelected += OnOpenFileSelected;
        root.AddChild(_openDialog);

        _saveDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Resources,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = "Save VarAttackRange As",
            UseNativeDialog = true
        };
        _saveDialog.Filters = new[] { "*.tres;Godot Text Resource", "*.res;Godot Binary Resource" };
        _saveDialog.FileSelected += OnSaveFileSelected;
        root.AddChild(_saveDialog);

        _discardChangesDialog = new ConfirmationDialog
        {
            Title = "Discard Unsaved Changes",
            DialogText = "The current range has unsaved changes. Continue and discard them?"
        };
        _discardChangesDialog.Confirmed += OnDiscardChangesConfirmed;
        root.AddChild(_discardChangesDialog);

        _messageDialog = new AcceptDialog
        {
            Title = "Range Editor"
        };
        root.AddChild(_messageDialog);

        return root;
    }

    private Button CreateButton(string text, string tooltipText, Action action)
    {
        Button button = new()
        {
            Text = text,
            TooltipText = tooltipText
        };
        button.Pressed += action;
        return button;
    }

    private void OnNewPressed()
    {
        if (PromptForUnsavedChangesIfNeeded(PendingAction.NewResource))
        {
            return;
        }

        CreateNewResource();
    }

    private void OnOpenPressed()
    {
        if (PromptForUnsavedChangesIfNeeded(PendingAction.OpenResource))
        {
            return;
        }

        ShowOpenDialog();
    }

    private void OnSavePressed()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            ShowSaveDialog();
            return;
        }

        SaveToPath(_currentPath);
    }

    private void OnSaveAsPressed()
    {
        ShowSaveDialog();
    }

    private void OnGridSelectionChanged()
    {
        SyncGridToResource(markDirty: true);
    }

    private void OnGridHoverCellChanged(Vector2I? hoveredCell)
    {
        _hoverOverlayLabel.Text = hoveredCell == null
            ? "Offset: --"
            : $"Offset: ({hoveredCell.Value.X}, {hoveredCell.Value.Y})";
    }

    private void OnOpenFileSelected(string path)
    {
        Resource loadedResource = ResourceLoader.Load(path);
        if (loadedResource is not VarAttackRange attackRange)
        {
            ShowMessage($"Selected resource is not a VarAttackRange:\n{path}");
            return;
        }

        LoadResource(attackRange, path);
    }

    private void OnSaveFileSelected(string path)
    {
        if (!path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
            !path.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
        {
            path += ".tres";
        }

        SaveToPath(path);
    }

    private void OnDiscardChangesConfirmed()
    {
        switch (_pendingAction)
        {
            case PendingAction.NewResource:
                CreateNewResource();
                break;
            case PendingAction.OpenResource:
                ShowOpenDialog();
                break;
        }

        _pendingAction = PendingAction.None;
    }

    private bool PromptForUnsavedChangesIfNeeded(PendingAction pendingAction)
    {
        if (!_hasUnsavedChanges)
        {
            return false;
        }

        _pendingAction = pendingAction;
        _discardChangesDialog.PopupCentered(new Vector2I(440, 120));
        return true;
    }

    private void CreateNewResource()
    {
        _currentResource = new VarAttackRange
        {
            RelativeCells = new Godot.Collections.Array<Vector2I>()
        };
        _currentPath = string.Empty;
        _grid.LoadLocalCells(Array.Empty<Vector2I>());
        _grid.SetViewRotation(0);
        _grid.ResetView();
        SetDirty(false);
        UpdateFacingLabel();
        OnGridHoverCellChanged(null);
    }

    private void LoadResource(VarAttackRange resource, string path)
    {
        _currentResource = resource;
        _currentPath = path;
        _grid.LoadLocalCells(EnumerateResourceCells(resource));
        _grid.SetViewRotation(0);
        _grid.ResetView();
        SetDirty(false);
        UpdateFacingLabel();
    }

    private void ShowOpenDialog()
    {
        _openDialog.CurrentDir = GetDefaultDirectory();
        _openDialog.PopupFileDialog();
    }

    private void ShowSaveDialog()
    {
        _saveDialog.CurrentDir = GetDefaultDirectory();
        _saveDialog.CurrentFile = GetDefaultFileName();
        _saveDialog.PopupFileDialog();
    }

    private void SaveToPath(string path)
    {
        SyncGridToResource(markDirty: false);

        Error result = ResourceSaver.Save(_currentResource, path);
        if (result != Error.Ok)
        {
            ShowMessage($"Saving failed with error: {result}");
            return;
        }

        _currentPath = path;
        SetDirty(false);
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
    }

    private void SyncGridToResource(bool markDirty)
    {
        Godot.Collections.Array<Vector2I> serializedCells = new();
        foreach (Vector2I cell in _grid.GetLocalCellsOrdered())
        {
            serializedCells.Add(cell);
        }

        _currentResource.RelativeCells = serializedCells;
        _currentResource.EmitChanged();

        if (markDirty)
        {
            SetDirty(true);
        }
    }

    private void SetDirty(bool isDirty)
    {
        _hasUnsavedChanges = isDirty;
        UpdateResourceLabel();
    }

    private void UpdateResourceLabel()
    {
        string labelPath = string.IsNullOrEmpty(_currentPath) ? "[Unsaved] VarAttackRange" : _currentPath;
        if (_hasUnsavedChanges)
        {
            labelPath += " *";
        }

        _resourceLabel.Text = labelPath;
    }

    private void UpdateFacingLabel()
    {
        _facingLabel.Text = $"Facing: {_grid.GetFacingName()}";
    }

    private void ShowMessage(string message)
    {
        _messageDialog.DialogText = message;
        _messageDialog.PopupCentered(new Vector2I(420, 120));
    }

    private string GetDefaultDirectory()
    {
        if (!string.IsNullOrEmpty(_currentPath))
        {
            return GetDirectoryName(_currentPath);
        }

        return DirAccess.DirExistsAbsolute("res://Variable") ? "res://Variable" : "res://";
    }

    private string GetDefaultFileName()
    {
        return string.IsNullOrEmpty(_currentPath) ? DefaultFileName : GetFileName(_currentPath);
    }

    private static IEnumerable<Vector2I> EnumerateResourceCells(VarAttackRange resource)
    {
        if (resource.RelativeCells == null)
        {
            yield break;
        }

        foreach (Vector2I cell in resource.RelativeCells)
        {
            yield return cell;
        }
    }

    private static string GetDirectoryName(string resourcePath)
    {
        int separatorIndex = resourcePath.LastIndexOf('/');
        return separatorIndex >= 0 ? resourcePath[..separatorIndex] : "res://";
    }

    private static string GetFileName(string resourcePath)
    {
        int separatorIndex = resourcePath.LastIndexOf('/');
        return separatorIndex >= 0 ? resourcePath[(separatorIndex + 1)..] : resourcePath;
    }
}
#endif
