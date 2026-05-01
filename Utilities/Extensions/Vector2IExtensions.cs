using Godot;
using System;
public static class Vector2IExtensions
{
    public static Vector2 ToVector2(this Vector2I vec)
    {
        return new Vector2(vec.X, vec.Y);
    }
}
