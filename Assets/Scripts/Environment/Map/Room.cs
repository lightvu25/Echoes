using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum ExitDirection { Up, Down, Left, Right }

[Flags]
public enum RoomExitsMask
{
    None  = 0,
    Up    = 1 << 0,
    Down  = 1 << 1,
    Left  = 1 << 2,
    Right = 1 << 3
}

[Serializable]
public struct RoomExit
{
    public ExitDirection direction;
    public Transform     exitPoint;
}

public class Room : MonoBehaviour
{
    [SerializeField] private List<RoomExit> exits = new List<RoomExit>();

    public bool isExplored = false;
    public static event Action<Room> OnRoomExplored;

    public string RoomId { get; private set; }
    public RoomExitsMask ExitsMask { get; private set; }

    private void Awake()
    {
        RoomId = Guid.NewGuid().ToString("N");
        CalculateExitsMask();
    }



    public Bounds GetBounds()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            return col.bounds;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);
            return combined;
        }

        return new Bounds(transform.position, new Vector3(20f, 12f, 1f));
    }

    public void CalculateExitsMask()
    {
        ExitsMask = RoomExitsMask.None;
        if (exits == null) return;
        foreach (RoomExit exit in exits)
            ExitsMask |= ExitDirectionToMask(exit.direction);
    }

    public static RoomExitsMask ExitDirectionToMask(ExitDirection dir) => dir switch
    {
        ExitDirection.Up    => RoomExitsMask.Up,
        ExitDirection.Down  => RoomExitsMask.Down,
        ExitDirection.Left  => RoomExitsMask.Left,
        ExitDirection.Right => RoomExitsMask.Right,
        _ => RoomExitsMask.None
    };

    public IReadOnlyList<RoomExit> Exits => exits;

    public RoomExit GetRandomExit()
    {
        if (exits == null || exits.Count == 0)
            throw new InvalidOperationException($"Room '{name}' has no available exits.");

        return exits[Random.Range(0, exits.Count)];
    }

    public void RemoveExit(RoomExit exit)
    {
        exits.Remove(exit);
    }

    public bool HasAvailableExits() => exits != null && exits.Count > 0;

    public bool TryGetExitInDirection(ExitDirection direction, out RoomExit result)
    {
        foreach (RoomExit exit in exits)
        {
            if (exit.direction == direction)
            {
                result = exit;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static ExitDirection GetOpposite(ExitDirection direction) => direction switch
    {
        ExitDirection.Up    => ExitDirection.Down,
        ExitDirection.Down  => ExitDirection.Up,
        ExitDirection.Left  => ExitDirection.Right,
        ExitDirection.Right => ExitDirection.Left,
        _                   => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (exits == null) return;

        foreach (RoomExit exit in exits)
        {
            if (exit.exitPoint == null) continue;

            Gizmos.color = exit.direction switch
            {
                ExitDirection.Up    => Color.green,
                ExitDirection.Down  => Color.red,
                ExitDirection.Left  => Color.blue,
                ExitDirection.Right => Color.yellow,
                _                   => Color.white,
            };

            Gizmos.DrawSphere(exit.exitPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, exit.exitPoint.position);
        }
    }
#endif
}
