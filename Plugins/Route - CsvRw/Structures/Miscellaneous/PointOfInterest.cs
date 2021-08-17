using Godot;
using System;
public struct PointOfInterest
{
    /// <summary>The track position</summary>
    public float TrackPosition;

    /// <summary>The offset from Track 0's position</summary>
    public Vector3 TrackOffset;

    /// <summary>The yaw</summary>
    public float TrackYaw;

    /// <summary>The pitch</summary>
    public float TrackPitch;

    /// <summary>The roll</summary>
    public float TrackRoll;

    /// <summary>The textual message to be displayed when jumping to this point</summary>
    public string Text;
}