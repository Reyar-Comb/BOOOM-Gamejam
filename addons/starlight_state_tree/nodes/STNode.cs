using Godot;
using System.Collections.Generic;
using StarlightBT.Data;
namespace StarlightStateTree;

[GlobalClass]
public partial class STNode : RefCounted
{
	[Signal] public delegate void TransitionRequestedEventHandler(string targetStateName);

	public virtual string Name => "STNode";
	public STNode PreviousState { get; internal set; } = null;
	public STNode ParentState { get; private set; } = null;
	public List<STNode> Children { get; private set; } = new();
	protected bool _isActive { get; private set; } = false;
	protected Blackboard _blackboard { get; private set; } = new();
	public void AddChild(STNode child)
	{
		if (child.ParentState != null)
		{
			child.ParentState.RemoveChild(child);
		}
		child.ParentState = this;
		Children.Add(child);
		child.OnEnterTree();
	}

	public void RemoveChild(STNode child)
	{
		if (Children.Remove(child))
		{
			child.ParentState = null;
			child.OnExitTree();
		}
	}

	public void Initialize(Blackboard blackboard = null)
	{
		if (blackboard != null)
		{
			_blackboard = blackboard;
		}
		OnInit();
		foreach (var child in Children)
		{
			child.Initialize(blackboard);
		}
	}

	public void PhysicsUpdate(double delta)
	{
		if (!_isActive) return;
		OnPhysicsUpdate(delta);
		foreach (var child in Children)
		{
			child.PhysicsUpdate(delta);
		}
	}

	public void FrameUpdate(double delta)
	{
		if (!_isActive) return;
		OnFrameUpdate(delta);
		foreach (var child in Children)
		{
			child.FrameUpdate(delta);
		}
	}

	protected void RequestTransition(string targetStateName) => EmitSignal(SignalName.TransitionRequested, targetStateName);

	public void Enter()
	{
		_isActive = true;
		OnEnter();
	}

	protected virtual void OnEnter() { }

	public void Exit()
	{
		OnExit();
		_isActive = false;
	}

	protected virtual void OnExit() { }
	protected virtual void OnPhysicsUpdate(double delta) { }
	protected virtual void OnFrameUpdate(double delta) { }
	protected virtual void OnInit() { }
	protected virtual void OnEnterTree() { }
	protected virtual void OnExitTree() { }
}
