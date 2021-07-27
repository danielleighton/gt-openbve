using Godot;
using System;

public struct RouteData
{
    internal double TrackPosition;
    internal double BlockInterval;
    internal double UnitOfSpeed;
    internal bool AccurateObjectDisposal;
    internal bool SignedCant;
    internal bool FogTransitionMode;
    internal StructureData Structure;
    internal SignalData[] SignalData;
    internal CompatibilitySignalData[] CompatibilitySignalData;
    //internal Textures.Texture[] TimetableDaytime;
    //internal Textures.Texture[] TimetableNighttime;
    internal ImageTexture[] Backgrounds;
    internal double[] SignalSpeeds;
    internal Block[] Blocks;
    internal Marker[] Markers;
    internal int FirstUsedBlock;
}