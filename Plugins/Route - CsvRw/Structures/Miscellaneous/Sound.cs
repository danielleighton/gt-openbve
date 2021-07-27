using Godot;
using System;

public enum SoundType { World, TrainStatic, TrainDynamic }

public struct Sound
{
    internal double TrackPosition;
    //internal Sounds.SoundBuffer SoundBuffer;
    internal SoundType Type;
    internal double X;
    internal double Y;
    internal double Radius;
    internal double Speed;
}




