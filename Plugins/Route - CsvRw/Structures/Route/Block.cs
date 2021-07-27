using Godot;
using System;

public class Block
{
    internal int Background;
    internal Brightness[] Brightness;
    //internal Game.Fog Fog;
    internal bool FogDefined;
    internal int[] Cycle;
    internal double Height;
    internal Rail[] Rail;
    internal int[] RailType;
    internal WallDike[] RailWall;
    internal WallDike[] RailDike;
    internal Pole[] RailPole;
    internal FreeObj[][] RailFreeObj;
    internal FreeObj[] GroundFreeObj;
    internal Form[] Form;
    internal Crack[] Crack;
    internal Signal[] Signal;
    internal Section[] Section;
    internal Limit[] Limit;
    internal Stop[] Stop;
    internal Sound[] Sound;
    internal Transponder[] Transponder;
    internal PointOfInterest[] PointsOfInterest;
    internal TrackManager.TrackElement CurrentTrackState;

    internal double Pitch;
    internal double Turn;
    internal int Station;
    internal bool StationPassAlarm;
    internal double Accuracy;
    internal double AdhesionMultiplier;
}