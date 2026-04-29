using Godot;
using StarlightStateTree;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

public partial class Var : RefCounted
{
    public VarStats Stats { get; set; }
    private bool _isWalking = false;
    private int _currentPathIndex = 0;
    private List<Vector2I> _currentPath = null;

    public void SetPath(List<Vector2I> path)
    {
        if (path == null || path.Count == 0) return;

        _currentPath = path;
        _currentPathIndex = 0;
        _isWalking = true;
    }
    public bool Update(double delta)
    {
        if (!_isWalking || _currentPath == null || _currentPathIndex >= _currentPath.Count) return false;

        Vector2I nextPos = _currentPath[_currentPathIndex] * 50;
        Stats.Direction = (nextPos - Stats.Position).Normalized();
        float stepLength = Stats.MoveSpeed * (float)delta;
        Stats.Position += Stats.Direction * stepLength;
        if (Stats.Position.DistanceSquaredTo(nextPos) > stepLength * stepLength) return true;

        _currentPathIndex++;
        if (_currentPathIndex >= _currentPath.Count)
        {
            _isWalking = false;
            return false;
        }
        return true;
    }
}
