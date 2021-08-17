using Godot;
using System;
using System.Collections.Generic;

internal class POI
{
    /// <summary>The track position at which the PointOfInterest is placed</summary>
    internal readonly float TrackPosition;
    /// <summary>The rail index to which the PointOfInterest is attached</summary>
    internal readonly int RailIndex;
    /// <summary>The relative position to the rail</summary>
    internal readonly Vector2 Position;
    /// <summary>The yaw value</summary>
    internal readonly float Yaw;
    /// <summary>The pitch value</summary>
    internal readonly float Pitch;
    /// <summary>The roll value</summary>
    internal readonly float Roll;
    /// <summary>The text to display when jumping to the PointOfInterest</summary>
    /// <remarks>Typically station name etc.</remarks>
    internal readonly string Text;

    internal POI(float trackPosition, int railIndex, string text, Vector2 position, float yaw, float pitch, float roll)
    {
        TrackPosition = trackPosition;
        RailIndex = railIndex;
        Text = text;
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
        Roll = roll;
    }
}


public class Block
{
    internal int Background;
    internal Brightness[] BrightnessChanges;
    // internal Fog Fog;
    internal bool FogDefined;
    internal int[] GroundCycles;
    internal RailCycle[] RailCycles;
    internal float Height;
    internal Dictionary<int, Rail> Rails;
    internal int[] RailType;
    internal Dictionary<int, WallDike> RailWall;
    internal Dictionary<int, WallDike> RailDike;
    internal Pole[] RailPole;
    internal Dictionary<int, List<FreeObj>> RailFreeObj;
    internal List<FreeObj> GroundFreeObj;
    internal Form[] Forms;
    internal Crack[] Cracks;
    internal Signal[] Signals;
    internal Section[] Sections;
    internal Limit[] Limits;
    internal Stop[] StopPositions;
    internal Sound[] SoundEvents;
    internal Transponder[] Transponders;
    // internal DestinationEvent[] DestinationChanges;
    internal POI[] PointsOfInterest;
    // internal HornBlowEvent[] HornBlows;
    internal TrackElement CurrentTrackState;
    internal float Pitch;
    internal float Turn;
    internal int Station;
    internal bool StationPassAlarm;
    internal float Accuracy;
    internal float AdhesionMultiplier;
    internal int SnowIntensity;
    internal int RainIntensity;
    internal int WeatherObject;

    internal Block(bool previewOnly)
    {
        Rails = new Dictionary<int, Rail>();
        Limits = new Limit[] { };
        StopPositions = new Stop[] { };
        Station = -1;
        StationPassAlarm = false;
        if (!previewOnly)
        {
            BrightnessChanges = new Brightness[] { };
            Forms = new Form[] { };
            Cracks = new Crack[] { };
            Signals = new Signal[] { };
            Sections = new Section[] { };
            SoundEvents = new Sound[] { };
            Transponders = new Transponder[] { };
            // DestinationChanges = new DestinationEvent[] { };
            // HornBlows = new HornBlowEvent[] { };
            RailFreeObj = new Dictionary<int, List<FreeObj>>();
            GroundFreeObj = new List<FreeObj>();
            PointsOfInterest = new POI[] { };
        }
    }
}