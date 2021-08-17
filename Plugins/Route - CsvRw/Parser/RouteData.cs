using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class RouteData
{
    internal float TrackPosition;
    internal float BlockInterval;
    /// <summary>OpenBVE runs internally in meters per second
    /// This value is used to convert between the speed set by Options.UnitsOfSpeed and m/s
    /// </summary>
    internal float UnitOfSpeed;
    internal bool AccurateObjectDisposal;
    internal bool SignedCant;
    internal bool FogTransitionMode;
    internal StructureData Structure = new StructureData();

    // TODO: proper compatibility signals
    // internal SignalDictionary Signals;
    //  internal CompatibilitySignalData[] CompatibilitySignals;
    internal SignalData[] CompatibilitySignals;

    internal Texture[] TimetableDaytime;
    internal Texture[] TimetableNighttime;
    // internal BackgroundDictionary Backgrounds;
    internal double[] SignalSpeeds;
    internal List<Block> Blocks;
    internal Marker[] Markers;
    internal StopRequest[] RequestStops;
    internal int FirstUsedBlock;
    internal bool IgnorePitchRoll;
    internal bool LineEndingFix;
    internal bool ValueBasedSections = false;
    internal bool TurnUsed = false;


    public  void InterpolateHeight()
    {
        int z = 0;
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (!double.IsNaN(Blocks[i].Height))
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (!double.IsNaN(Blocks[j].Height))
                    {
                        float a = Blocks[j].Height;
                        float b = Blocks[i].Height;
                        float d = (b - a) / (i - j);
                        for (int k = j + 1; k < i; k++)
                        {
                            a += d;
                            Blocks[k].Height = a;
                        }
                        break;
                    }
                }
                z = i;
            }
        }
        for (int i = z + 1; i < Blocks.Count; i++)
        {
            Blocks[i].Height = Blocks[z].Height;
        }
    }

    public void CreateMissingBlocks(int ToIndex, bool PreviewOnly)
    {
        if (ToIndex >= Blocks.Count)
        {
            for (int i = Blocks.Count; i <= ToIndex; i++)
            {
                Blocks.Add(new Block(PreviewOnly));
                if (!PreviewOnly)
                {
                    Blocks[i].Background = -1;
                    // Blocks[i].Fog = Blocks[i - 1].Fog;
                    Blocks[i].FogDefined = false;
                    Blocks[i].GroundCycles = Blocks[i - 1].GroundCycles;
                    Blocks[i].RailCycles = Blocks[i - 1].RailCycles;
                    Blocks[i].Height = float.NaN;
                    Blocks[i].SnowIntensity = Blocks[i - 1].SnowIntensity;
                    Blocks[i].RainIntensity = Blocks[i - 1].RainIntensity;
                    Blocks[i].WeatherObject = Blocks[i - 1].WeatherObject;
                }
                Blocks[i].RailType = new int[Blocks[i - 1].RailType.Length];
                if (!PreviewOnly)
                {
                    for (int j = 0; j < Blocks[i].RailType.Length; j++)
                    {
                        int rc = -1;
                        if (Blocks[i].RailCycles.Length > j)
                        {
                            rc = Blocks[i].RailCycles[j].RailCycleIndex;
                        }
                        if (rc != -1 && Structure.RailCycles.Length > rc && Structure.RailCycles[rc].Length > 1)
                        {
                            int cc = Blocks[i].RailCycles[j].CurrentCycle;
                            if (cc == Structure.RailCycles[rc].Length - 1)
                            {
                                Blocks[i].RailType[j] = Structure.RailCycles[rc][0];
                                Blocks[i].RailCycles[j].CurrentCycle = 0;
                            }
                            else
                            {
                                cc++;
                                Blocks[i].RailType[j] = Structure.RailCycles[rc][cc];
                                Blocks[i].RailCycles[j].CurrentCycle++;
                            }
                        }
                        else
                        {
                            Blocks[i].RailType[j] = Blocks[i - 1].RailType[j];
                        }
                    }
                }

                for (int j = 0; j < Blocks[i - 1].Rails.Count; j++)
                {
                    int key = Blocks[i - 1].Rails.ElementAt(j).Key;
                    Rail rail = new Rail
                    {
                        RailStarted = Blocks[i - 1].Rails[key].RailStarted,
                        RailStart = new Vector2(Blocks[i - 1].Rails[key].RailStart),
                        RailStartRefreshed = false,
                        RailEnded = false,
                        RailEnd = new Vector2(Blocks[i - 1].Rails[key].RailStart)
                    };
                    Blocks[i].Rails.Add(key, rail);
                }
                if (!PreviewOnly)
                {
                    Blocks[i].RailWall = new Dictionary<int, WallDike>();
                    for (int j = 0; j < Blocks[i - 1].RailWall.Count; j++)
                    {
                        int key = Blocks[i - 1].RailWall.ElementAt(j).Key;
                        if (Blocks[i - 1].RailWall[key] == null || !Blocks[i - 1].RailWall[key].Exists)
                        {
                            continue;
                        }
                        Blocks[i].RailWall.Add(key, Blocks[i - 1].RailWall[key].Clone());
                    }
                    Blocks[i].RailDike = new Dictionary<int, WallDike>();
                    for (int j = 0; j < Blocks[i - 1].RailDike.Count; j++)
                    {
                        int key = Blocks[i - 1].RailDike.ElementAt(j).Key;
                        if (Blocks[i - 1].RailDike[key] == null || !Blocks[i - 1].RailDike[key].Exists)
                        {
                            continue;
                        }
                        Blocks[i].RailDike.Add(key, Blocks[i - 1].RailDike[key].Clone());
                    }
                    Blocks[i].RailPole = new Pole[Blocks[i - 1].RailPole.Length];
                    for (int j = 0; j < Blocks[i].RailPole.Length; j++)
                    {
                        Blocks[i].RailPole[j] = new Pole(Blocks[i - 1].RailPole[j]);
                    }
                }
                Blocks[i].Pitch = Blocks[i - 1].Pitch;
                Blocks[i].CurrentTrackState = Blocks[i - 1].CurrentTrackState;
                Blocks[i].Turn = 0.0f;
                Blocks[i].Accuracy = Blocks[i - 1].Accuracy;
                Blocks[i].AdhesionMultiplier = Blocks[i - 1].AdhesionMultiplier;
            }
        }
    }
}