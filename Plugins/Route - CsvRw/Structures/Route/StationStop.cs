using Godot;
using System;
public struct Stop
{
    internal double TrackPosition;
    internal int Station;
    internal int Direction;
    internal double ForwardTolerance;
    internal double BackwardTolerance;
    internal int Cars;
}



public struct StopRequest
{
    internal int StationIndex;
    internal int MaxNumberOfCars;
    internal double TrackPosition;
    internal RequestStop Early;
    internal RequestStop OnTime;
    internal RequestStop Late;
    internal bool FullSpeed;

    internal void CreateEvent(double StartingDistance, double EndingDistance, ref TrackElement Element)
    {
        if (TrackPosition >= StartingDistance & TrackPosition < EndingDistance)
        {
            // int m = Element.Events.Length;
            // Array.Resize(ref Element.Events, m + 1);
            // Element.Events[m] = new RequestStopEvent(Plugin.CurrentRoute, StationIndex, MaxNumberOfCars, FullSpeed, OnTime, Early, Late);
        }
    }
}


public class RequestStop
{
    /// <summary>The index of the station to stop at</summary>
    public int StationIndex;
    /// <summary>The maximum number of cars that a train may have to stop</summary>
    public int MaxCars;
    /// <summary>The probability that this stop may be called</summary>
    public int Probability;
    /// <summary>The time at which this stop may be called</summary>
    public double Time = -1;
    /// <summary>The message displayed when the train is to stop</summary>
    public string StopMessage;
    /// <summary>The message displayed when the train is to pass</summary>
    public string PassMessage;
    /// <summary>Whether the stop request is to be passed at line speed</summary>
    public bool FullSpeed;
}
