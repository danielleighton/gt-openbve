using Godot;
using System;

public struct Form
{
    internal int PrimaryRail;
    internal int SecondaryRail;
    internal int FormType;
    internal int RoofType;
    internal const int SecondaryRailStub = 0;
    internal const int SecondaryRailL = -1;
    internal const int SecondaryRailR = -2;
}