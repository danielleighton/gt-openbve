using Godot;
using System;

public abstract class SignalObject
{
    public virtual void Create(Vector3 wpos, Transform RailTransformation, Transform LocalTransformation, int SectionIndex, double StartingDistance, double EndingDistance, double TrackPosition, double Brightness)
    { }
}