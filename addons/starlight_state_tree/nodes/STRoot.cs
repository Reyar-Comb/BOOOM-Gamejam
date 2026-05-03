using Godot;
using System.Collections.Generic;
using StarlightBT.Data;
namespace StarlightStateTree;

[GlobalClass]
public partial class STRoot : STNode
{
    public string InitialState = "";
    public override string Name => "STRoot";
    public bool AllowRepeatedEnterAndExit = false;
    protected Dictionary<string, STNode> _statesByName = new();
    public STNode CurrentState { get; set; } = null;
    internal bool HasPendingTransition => _hasPendingTransition;

    private bool _hasPendingTransition = false;
    private string _pendingTransitionTarget = "";

    public void Shutdown()
    {
        UnregisterAllStateHandlers();
        CurrentState = null;
    }

    protected override void OnInit()
    {
        UnregisterAllStateHandlers();
        RegisterStateRecursively(this);
        if (string.IsNullOrEmpty(InitialState))
        {
            GD.PushError($"InitialState is not set in {Name}.");
            return;
        }
        EnterInitialStateChain();
    }

    protected void RegisterStateRecursively(STNode state)
    {
        state.TransitionRequested += OnStateTransitionRequested;

        if (!_statesByName.TryAdd(state.Name, state))
        {
            GD.PushError($"Duplicate state name '{state.Name}' found. State names must be unique under root '{Name}'.");
            state.TransitionRequested -= OnStateTransitionRequested;
            return;
        }

        foreach (STNode child in state.Children)
        {
            RegisterStateRecursively(child);
        }
    }

    protected void OnStateTransitionRequested(string targetStateName)
    {
        _pendingTransitionTarget = targetStateName;
        _hasPendingTransition = true;
    }

    protected void UnregisterAllStateHandlers()
    {
        foreach (STNode registeredState in _statesByName.Values)
        {
            registeredState.TransitionRequested -= OnStateTransitionRequested;
        }
        _statesByName.Clear();
    }

    private (int, int) GetLastCommonStateIndices(List<STNode> firstPath, List<STNode> secondPath)
    {
        int firstPathIndex = firstPath.Count - 1;
        int secondPathIndex = secondPath.Count - 1;
        while (firstPathIndex >= 0 && secondPathIndex >= 0 && firstPath[firstPathIndex] == secondPath[secondPathIndex])
        {
            firstPathIndex--;
            secondPathIndex--;
        }
        return (firstPathIndex, secondPathIndex);
    }

    protected void TransitionTo(string targetStateName)
    {
        if (!_statesByName.TryGetValue(targetStateName, out STNode nextState))
        {
            GD.PushError($"State '{targetStateName}' not found in {Name}.");
            return;
        }

        List<STNode> currentPath = BuildPathToRoot(CurrentState);
        List<STNode> nextPath = BuildPathToRoot(nextState);

        (int currentPathExitIndex, int nextPathEnterIndex) = (currentPath.Count - 1, nextPath.Count - 1);

        if (!AllowRepeatedEnterAndExit)
        {
            (currentPathExitIndex, nextPathEnterIndex) = GetLastCommonStateIndices(currentPath, nextPath);
        }

        for (int index = 0; index <= currentPathExitIndex; index++)
        {
            currentPath[index]?.Exit();
        }

        STNode previousState = CurrentState;
        CurrentState = nextState;
        CurrentState.PreviousState = previousState;

        for (int index = nextPathEnterIndex; index >= 0; index--)
        {
            nextPath[index]?.Enter();
        }
    }

    protected List<STNode> BuildPathToRoot(STNode state)
    {
        List<STNode> statePath = new();
        while (state != null && state != this)
        {
            statePath.Add(state);
            state = state.ParentState;
        }
        return statePath;
    }

    protected void EnterInitialStateChain()
    {
        Stack<STNode> stateStack = new();
        if (!_statesByName.TryGetValue(InitialState, out STNode initialState))
        {
            GD.PushError($"Initial state '{InitialState}' not found in {Name}.");
            return;
        }

        CurrentState = initialState;

        while (initialState != null)
        {
            stateStack.Push(initialState);
            initialState = initialState.ParentState;
        }

        while (stateStack.Count > 0)
        {
            stateStack.Pop().Enter();
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        base.PhysicsUpdate(delta);
        FlushPendingTransition();
    }

    public override void FrameUpdate(double delta)
    {
        base.FrameUpdate(delta);
        FlushPendingTransition();
    }

    private void FlushPendingTransition()
    {
        if (!_hasPendingTransition)
        {
            return;
        }

        string targetStateName = _pendingTransitionTarget;
        _pendingTransitionTarget = "";
        _hasPendingTransition = false;
        TransitionTo(targetStateName);
    }
}
