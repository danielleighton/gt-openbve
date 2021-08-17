using Godot;
using System;

public enum BackgroundTransitionMode
{
    /// <summary>No transition is performed</summary>
    None = 0,
    /// <summary>The new background fades in</summary>
    FadeIn = 1,
    /// <summary>The old background fades out</summary>
    FadeOut = 2
}

public abstract class BackgroundHandle
{
    /// <summary>The user-selected viewing distance.</summary>
    public double BackgroundImageDistance = 600;

    /// <summary>The current transition mode between backgrounds</summary>
    public BackgroundTransitionMode Mode;

    /// <summary>The current transition countdown</summary>
    public double Countdown = -1;

    /// <summary>The current transition alpha level</summary>
    public float CurrentAlpha = 1.0f;

    /// <summary>Called once a frame to update the current state of the background</summary>
    /// <param name="SecondsSinceMidnight">The current in game time, expressed as the number of seconds since midnight on the first day</param>
    /// <param name="ElapsedTime">The total elapsed frame time</param>
    /// <param name="Target">Whether this is the target background during a transition (Affects alpha rendering)</param>
    public abstract void UpdateBackground(double SecondsSinceMidnight, double ElapsedTime, bool Target);
}
