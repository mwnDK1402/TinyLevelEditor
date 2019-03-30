using System;
using UnityEngine;

[Serializable]
public struct LevelPosition
{
    public LevelPosition(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    public float X, Y;

    public static explicit operator LevelPosition(Vector2 position)
    {
        return new LevelPosition(position.x, position.y);
    }

    public static explicit operator LevelPosition(Vector3 position)
    {
        return new LevelPosition(position.x, position.y);
    }

    public static implicit operator Vector2(LevelPosition position)
    {
        return new Vector2(position.X, position.Y);
    }

    public static implicit operator Vector3(LevelPosition position)
    {
        return new Vector3(position.X, position.Y);
    }
}
