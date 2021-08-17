using Godot;
using System;
internal class FreeObj
{
    /// <summary>The track position of the object</summary>
    private readonly float TrackPosition;
    /// <summary>The routefile index of the object</summary>
    private readonly int Type;
    /// <summary>The position of the object</summary>
    private readonly Vector2 Position;
    /// <summary>The yaw of the object (radians)</summary>
    private readonly float Yaw;
    /// <summary>The pitch of the object (radians)</summary>
    private readonly float Pitch;
    /// <summary>The roll of the object (radians)</summary>
    private readonly float Roll;

    internal FreeObj(float trackPosition, int type, Vector2 position, float yaw, float pitch = 0, float roll = 0)
    {
        TrackPosition = trackPosition;
        Type = type;
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
        Roll = roll;
    }

    public void CreateRailAligned(ObjectDictionary FreeObjects, Vector3 WorldPosition, Transform RailTransformation, double StartingDistance, double EndingDistance)
    {
        double dz = TrackPosition - StartingDistance;
        // WorldPosition += Position.x * RailTransformation.basis.x + Position.y * RailTransformation.basis.y + dz * RailTransformation.basis.z;
        UnifiedObject obj;
        FreeObjects.TryGetValue(Type, out obj);
        if (obj != null)
        {
            Transform t = new Transform();
            t = t.Rotated(Vector3.Right, Yaw);
            t = t.Rotated(Vector3.Up, Pitch);
            t = t.Rotated(Vector3.Back, Roll);

            obj.CreateObject(WorldPosition, RailTransformation, t, StartingDistance, EndingDistance, TrackPosition);
        }
        
    }

    public void CreateGroundAligned(ObjectDictionary FreeObjects, Vector3 WorldPosition, Transform GroundTransformation, Vector2 Direction, double Height, double StartingDistance, double EndingDistance)
    {
        double d = TrackPosition - StartingDistance;
        Vector3 wpos = WorldPosition;// + new Vector3(Direction.x * d + Direction.y * Position.x, Position.y - Height, Direction.y * d - Direction.x * Position.x);
        UnifiedObject obj;
        FreeObjects.TryGetValue(Type, out obj);
        if (obj != null)
        {
            Transform t = new Transform();
            t = t.Rotated(Vector3.Right, Yaw);
            t = t.Rotated(Vector3.Up, Pitch);
            t = t.Rotated(Vector3.Back, Roll);

            obj.CreateObject(wpos, GroundTransformation, t, StartingDistance, EndingDistance, TrackPosition);
        }

    }
}
