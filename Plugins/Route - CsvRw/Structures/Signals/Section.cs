using Godot;
using System;

public enum SectionType
{
    /// <summary>A section aspect may have any value</summary>
    ValueBased,

    /// <summary>Section aspect count upwards from zero (0,1,2,3....)</summary>
    IndexBased
}

public struct SectionAspect
{
    /// <summary>The aspect number</summary>
    public int Number;

    /// <summary>The speed limit associated with this aspect number</summary>
    public double Speed;

    /// <summary>Creates a new signalling aspect</summary>
    /// <param name="Number">The aspect number</param>
    /// <param name="Speed">The speed limit</param>
    public SectionAspect(int Number, double Speed)
    {
        this.Number = Number;
        this.Speed = Speed;
    }
}

public class Section
{
    private readonly double TrackPosition;
    private readonly int[] Aspects;
    private readonly int DepartureStationIndex;
    private readonly bool Invisible;
    private readonly SectionType Type;

    internal Section(double trackPosition, int[] aspects, int departureStationIndex, SectionType type, bool invisible = false)
    {
        TrackPosition = trackPosition;
        Aspects = aspects;
        DepartureStationIndex = departureStationIndex;
        Type = type;
        Invisible = invisible;
    }

    /// <summary>Called when a train enters the section</summary>
    /// <param name="Train">The train</param>
    // public void Enter(AbstractTrain Train)
    // {
    //     int n = Trains.Length;

    //     for (int i = 0; i < n; i++)
    //     {
    //         if (Trains[i] == Train)
    //         {
    //             return;
    //         }
    //     }

    //     Array.Resize(ref Trains, n + 1);
    //     Trains[n] = Train;
    // }

    // /// <summary>Called when a train leaves the section</summary>
    // /// <param name="Train">The train</param>
    // public void Leave(AbstractTrain Train)
    // {
    //     int n = Trains.Length;

    //     for (int i = 0; i < n; i++)
    //     {
    //         if (Trains[i] == Train)
    //         {
    //             for (int j = i; j < n - 1; j++)
    //             {
    //                 Trains[j] = Trains[j + 1];
    //             }

    //             Array.Resize(ref Trains, n - 1);
    //             return;
    //         }
    //     }
    // }

    // /// <summary>Checks whether a train is currently within the section</summary>
    // /// <param name="Train">The train</param>
    // /// <returns>True if the train is within the section, false otherwise</returns>
    // public bool Exists(AbstractTrain Train)
    // {
    //     return Trains.Any(t => t == Train);
    // }

    // /// <summary>Checks whether the section is free, disregarding the specified train.</summary>
    // /// <param name="train">The train to disregard.</param>
    // /// <returns>Whether the section is free, disregarding the specified train.</returns>
    // public bool IsFree(AbstractTrain train)
    // {
    //     return Trains.All(t => !(t != train & (t.State == TrainState.Available | t.State == TrainState.Bogus)));
    // }

    // /// <summary>Checks whether the section is free</summary>
    // /// <returns>Whether the section is free</returns>
    // public bool IsFree()
    // {
    //     return Trains.All(t => !(t.State == TrainState.Available | t.State == TrainState.Bogus));
    // }

    // /// <summary>Gets the first train within the section</summary>
    // /// <param name="AllowBogusTrain">Whether bogus trains are to be allowed</param>
    // /// <returns>The first train within the section, or null if no trains are found</returns>
    // public AbstractTrain GetFirstTrain(bool AllowBogusTrain)
    // {
    //     for (int i = 0; i < Trains.Length; i++)
    //     {
    //         if (Trains[i].State == TrainState.Available)
    //         {
    //             return Trains[i];
    //         }

    //         if (AllowBogusTrain & Trains[i].State == TrainState.Bogus)
    //         {
    //             return Trains[i];
    //         }
    //     }

    //     return null;
    // }

    // /// <summary>Gets the signal data for a plugin.</summary>
    // /// <param name="train">The train.</param>
    // /// <returns>The signal data.</returns>
    // public SignalData GetPluginSignal(AbstractTrain train)
    // {
    //     if (Exists(train))
    //     {
    //         int aspect;

    //         if (IsFree(train))
    //         {
    //             if (Type == SectionType.IndexBased)
    //             {
    //                 if (NextSection != null)
    //                 {
    //                     int value = NextSection.FreeSections;

    //                     if (value == -1)
    //                     {
    //                         value = Aspects.Length - 1;
    //                     }
    //                     else
    //                     {
    //                         value++;

    //                         if (value >= Aspects.Length)
    //                         {
    //                             value = Aspects.Length - 1;
    //                         }

    //                         if (value < 0)
    //                         {
    //                             value = 0;
    //                         }
    //                     }

    //                     aspect = Aspects[value].Number;
    //                 }
    //                 else
    //                 {
    //                     aspect = Aspects[Aspects.Length - 1].Number;
    //                 }
    //             }
    //             else
    //             {

    //                 aspect = Aspects[Aspects.Length - 1].Number;

    //                 if (NextSection != null)
    //                 {
    //                     int value = NextSection.Aspects[NextSection.CurrentAspect].Number;

    //                     for (int i = 0; i < Aspects.Length; i++)
    //                     {
    //                         if (Aspects[i].Number > value)
    //                         {
    //                             aspect = Aspects[i].Number;
    //                             break;
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             aspect = Aspects[CurrentAspect].Number;
    //         }

    //         double position = train.FrontCarTrackPosition();
    //         double distance = TrackPosition - position;
    //         return new SignalData(aspect, distance);
    //     }
    //     else
    //     {
    //         int aspect = Aspects[CurrentAspect].Number;
    //         double position = train.FrontCarTrackPosition();
    //         double distance = TrackPosition - position;
    //         return new SignalData(aspect, distance);
    //     }
    // }
}