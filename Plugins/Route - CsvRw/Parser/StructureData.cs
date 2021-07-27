using Godot;
using System;
public struct StructureData
{
    internal UnifiedObject[] Rail;
    internal UnifiedObject[] Ground;
    internal UnifiedObject[] WallL;
    internal UnifiedObject[] WallR;
    internal UnifiedObject[] DikeL;
    internal UnifiedObject[] DikeR;
    internal UnifiedObject[] FormL;
    internal UnifiedObject[][] Poles;
    internal UnifiedObject[] FormR;
    internal UnifiedObject[] FormCL;
    internal UnifiedObject[] FormCR;
    internal UnifiedObject[] RoofL;
    internal UnifiedObject[] RoofR;
    internal UnifiedObject[] RoofCL;
    internal UnifiedObject[] RoofCR;
    internal UnifiedObject[] CrackL;
    internal UnifiedObject[] CrackR;
    internal UnifiedObject[] FreeObj;
    internal UnifiedObject[] Beacon;
    internal int[][] Cycle;
    internal int[] Run;
    internal int[] Flange;
}