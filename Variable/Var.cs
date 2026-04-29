using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

public partial class Var : RefCounted
{
    public VarStats Stats { get; set; }
    private bool _isWalking = false;
    private int _currentPathIndex = 0;
    private static readonly float REACHED_THRESHOLD = 0.1f;
    private List<Vector2I> _currentPath = null;
    public void SetPath(List<Vector2I> path)
    {
        if (path == null || path.Count == 0) return;

        _currentPath = path;
        _currentPathIndex = 0;
        _isWalking = true;
    }
    public bool Step(double delta)
    {
        if (!_isWalking || _currentPath == null || _currentPathIndex >= _currentPath.Count) return false;

        Vector2I nextPos = _currentPath[_currentPathIndex];
        Stats.Direction = (nextPos - Stats.Position).Normalized();
        Stats.Position += Stats.Direction * Stats.MoveSpeed * (float)delta;
        if (Stats.Position.DistanceTo(nextPos) > REACHED_THRESHOLD) return true;

        _currentPathIndex++;
        if (_currentPathIndex >= _currentPath.Count)
        {
            _isWalking = false;
            return false;
        }
        return true;
    }
}
