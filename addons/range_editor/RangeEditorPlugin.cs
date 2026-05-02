#nullable enable
#if TOOLS
using Godot;

namespace Booooom.Editor.RangeEditor;

[Tool]
public partial class RangeEditorPlugin : EditorPlugin
{
    private EditorDock? _dock;
    private RangeEditorPanel? _panel;

    public override void _EnterTree()
    {
        _panel = new RangeEditorPanel
        {
            Plugin = this
        };

        _dock = new EditorDock
        {
            Title = "Range Editor",
            Name = "RangeEditorDock",
            DefaultSlot = EditorDock.DockSlot.Bottom,
            Closable = true
        };
        _dock.AddChild(_panel);

        AddDock(_dock);
        _dock.MakeVisible();
    }

    public override void _ExitTree()
    {
        if (_dock == null)
        {
            return;
        }

        RemoveDock(_dock);
        _dock.QueueFree();
        _dock = null;
        _panel = null;
    }
}
#endif
