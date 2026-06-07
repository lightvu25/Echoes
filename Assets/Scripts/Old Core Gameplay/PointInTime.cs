using UnityEngine;
using System;

public class PointInTime
{
    public Vector3 position;
    public Quaternion rotation;
    public float gameTime;

    public PointInTime(Vector3 _position, Quaternion _rotation, float _gameTime)
    {
        position = _position;
        rotation = _rotation;
        gameTime = _gameTime;
    }
}
