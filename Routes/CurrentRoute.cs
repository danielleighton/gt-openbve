using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
// using LibRender2;
// using OpenBveApi.Colors;
// using OpenBveApi.Hosts;
// using OpenBveApi.Routes;
// using OpenBveApi.Runtime;
// using OpenBveApi.Trains;
// using RouteManager2.Climate;
// using RouteManager2.SignalManager;
// using RouteManager2.SignalManager.PreTrain;
// using RouteManager2.Stations;

public class CurrentRoute
{
    // private readonly HostInterface currentHost;

    // private readonly BaseRenderer renderer;

    /// <summary>Holds the information properties of the route</summary>
    public RouteInformation Information;
    /// <summary>The route's comment (For display in the main menu)</summary>
    public string Comment = "";
    /// <summary>The route's image file (For display in the main menu)</summary>
    public string Image = "";

    /// <summary>The list of tracks available in the simulation.</summary>
    public Dictionary<int, Track> Tracks;

    /// <summary>Holds all signal sections within the current route</summary>
    public Section[] Sections;

    /// <summary>Holds all stations within the current route</summary>
    // public RouteStation[] Stations;

    /// <summary>The name of the initial station on game startup, if set via command-line arguments</summary>
    public string InitialStationName;
    /// <summary>The start time at the initial station, if set via command-line arguments</summary>

    public double InitialStationTime = -1;

    /// <summary>Holds all .PreTrain instructions for the current route</summary>
    /// <remarks>Must be in distance and time ascending order</remarks>
    // public BogusPreTrainInstruction[] BogusPreTrainInstructions;

    public double[] PrecedingTrainTimeDeltas = new double[] { };

    /// <summary>Holds all points of interest within the game world</summary>
    public PointOfInterest[] PointsOfInterest;

    /// <summary>The currently displayed background texture</summary>
    // public BackgroundHandle CurrentBackground;

    /// <summary>The new background texture (Currently fading in)</summary>
    // public BackgroundHandle TargetBackground;

    /// <summary>The list of dynamic light definitions</summary>
    // public LightDefinition[] LightDefinitions;

    /// <summary>Whether dynamic lighting is currently enabled</summary>
    public bool DynamicLighting = false;

    /// <summary>The start of a region without fog</summary>
    /// <remarks>Must not be below the viewing distance (e.g. 600m)</remarks>
    public float NoFogStart;

    /// <summary>The end of a region without fog</summary>
    public float NoFogEnd;

    /// <summary>Holds the previous fog</summary>
    // public Fog PreviousFog;

    /// <summary>Holds the current fog</summary>
    // public Fog CurrentFog;

    /// <summary>Holds the next fog</summary>
    // public Fog NextFog;

    // public Atmosphere Atmosphere;

    public double[] BufferTrackPositions = new double[] { };

    /// <summary>The current in game time, expressed as the number of seconds since midnight on the first day</summary>
    public double SecondsSinceMidnight;

    /// <summary>Holds the length conversion units</summary>
    public double[] UnitOfLength = new double[] { 1.0 };

    /// <summary>The length of a block in meters</summary>
    public double BlockLength = 25.0;

    /// <summary>Controls the object disposal mode</summary>
    // public ObjectDisposalMode AccurateObjectDisposal;

    public CurrentRoute(/*HostInterface host, BaseRenderer renderer*/)
    {
        // currentHost = host;
        // this.renderer = renderer;

        Tracks = new Dictionary<int, Track>();
        Track t = new Track()
        {
            Elements = new TrackElement[0]
        };
        Tracks.Add(0, t);
        Sections = new Section[0];
        // Stations = new RouteStation[0];
        // BogusPreTrainInstructions = new BogusPreTrainInstruction[0];
        PointsOfInterest = new PointOfInterest[0];
        // CurrentBackground = new StaticBackground(null, 6, false);
        // TargetBackground = new StaticBackground(null, 6, false);
        NoFogStart = 800.0f;
        NoFogEnd = 1600.0f;
        // PreviousFog = new Fog(NoFogStart, NoFogEnd, Color24.Grey, 0.0);
        // CurrentFog = new Fog(NoFogStart, NoFogEnd, Color24.Grey, 0.5);
        // NextFog = new Fog(NoFogStart, NoFogEnd, Color24.Grey, 1.0);
        // Atmosphere = new Atmosphere();
        SecondsSinceMidnight = 0.0;
        Information = new RouteInformation();
        // Illustrations.CurrentRoute = this;
    }

    /// <summary>Updates all sections within the route</summary>
    public void UpdateAllSections()
    {
        /*
         * When there are an insane amount of sections, updating via a reference chain
         * may trigger a StackOverflowException
         *
         * Instead, pull out the reference to the next section in an out variable
         * and use a while loop
         * https://github.com/leezer3/OpenBVE/issues/557
         */
        Section nextSectionToUpdate;
        UpdateSection(Sections.LastOrDefault(), out nextSectionToUpdate);
        while (nextSectionToUpdate != null)
        {
            UpdateSection(nextSectionToUpdate, out nextSectionToUpdate);
        }
    }

    /// <summary>Updates the specified signal section</summary>
    /// <param name="SectionIndex"></param>
    public void UpdateSection(int SectionIndex)
    {
        Section nextSectionToUpdate;
        UpdateSection(Sections[SectionIndex], out nextSectionToUpdate);
        while (nextSectionToUpdate != null)
        {
            UpdateSection(nextSectionToUpdate, out nextSectionToUpdate);
        }
    }

    /// <summary>Updates the specified signal section</summary>
    /// <param name="Section"></param>
    /// <param name="PreviousSection"></param>
    public void UpdateSection(Section Section, out Section PreviousSection)
    {
        PreviousSection = null; // todo
        if (Section == null)
        {
            PreviousSection = null;
            return;
        }

        // double timeElapsed = SecondsSinceMidnight - Section.LastUpdate;
        // Section.LastUpdate = SecondsSinceMidnight;

        // preparations
        int zeroAspect = 0;
        bool setToRed = false;

        /*
                if (Section.Type == SectionType.ValueBased)
                {
                    // value-based
                    zeroAspect = 0;

                    for (int i = 1; i < Section.Aspects.Count(); i++)
                    {
                        if (Section.Aspects[i].Number < Section.Aspects[zeroAspect].Number)
                        {
                            zeroAspect = i;
                        }
                    }
                }

                // hold station departure signal at red
                int d = Section.StationIndex;

                if (d >= 0)
                {
                    // look for train in previous blocks
                    Section l = Section.PreviousSection;
                    AbstractTrain train = null;

                    while (true)
                    {
                        if (l != null)
                        {
                            train = l.GetFirstTrain(false);

                            if (train != null)
                            {
                                break;
                            }

                            l = l.PreviousSection;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (train == null)
                    {
                        double b = -Double.MaxValue;

                        foreach (AbstractTrain t in currentHost.Trains)
                        {
                            if (t.State == TrainState.Available)
                            {
                                if (t.TimetableDelta > b)
                                {
                                    b = t.TimetableDelta;
                                    train = t;
                                }
                            }
                        }
                    }

                    // set to red where applicable
                    if (train != null)
                    {
                        if (!Section.TrainReachedStopPoint)
                        {
                            if (train.Station == d)
                            {
                                int c = Stations[d].GetStopIndex(train.NumberOfCars);

                                if (c >= 0)
                                {
                                    double p0 = train.FrontCarTrackPosition();
                                    double p1 = Stations[d].Stops[c].TrackPosition - Stations[d].Stops[c].BackwardTolerance;

                                    if (p0 >= p1)
                                    {
                                        Section.TrainReachedStopPoint = true;
                                    }
                                }
                                else
                                {
                                    Section.TrainReachedStopPoint = true;
                                }
                            }
                        }

                        double t = -15.0;

                        if (Stations[d].DepartureTime >= 0.0)
                        {
                            t = Stations[d].DepartureTime - 15.0;
                        }
                        else if (Stations[d].ArrivalTime >= 0.0)
                        {
                            t = Stations[d].ArrivalTime;
                        }

                        if (AccurateObjectDisposal == ObjectDisposalMode.Mechanik)
                        {
                            if (train.LastStation == d - 1 || train.Station == d)
                            {
                                if (Section.RedTimer == -1)
                                {
                                    Section.RedTimer = 30;
                                }
                                else
                                {
                                    Section.RedTimer -= timeElapsed;
                                }

                                setToRed = !(Section.RedTimer <= 0);
                            }
                            else
                            {
                                Section.RedTimer = -1;
                            }
                        }

                        if (train.IsPlayerTrain & Stations[d].Type != StationType.Normal & Stations[d].DepartureTime < 0.0)
                        {
                            setToRed = true;
                        }
                        else if (t >= 0.0 & SecondsSinceMidnight < t - train.TimetableDelta)
                        {
                            setToRed = true;
                        }
                        else if (!Section.TrainReachedStopPoint)
                        {
                            setToRed = true;
                        }
                    }
                    else if (Stations[d].Type != StationType.Normal)
                    {
                        setToRed = true;
                    }
                }

                // train in block
                if (!Section.IsFree())
                {
                    setToRed = true;
                }

                // free sections
                int newAspect = -1;

                if (setToRed)
                {
                    Section.FreeSections = 0;
                    newAspect = zeroAspect;
                }
                else
                {
                    Section n = Section.NextSection;

                    if (n != null)
                    {
                        if (n.FreeSections == -1)
                        {
                            Section.FreeSections = -1;
                        }
                        else
                        {
                            Section.FreeSections = n.FreeSections + 1;
                        }
                    }
                    else
                    {
                        Section.FreeSections = -1;
                    }
                }

                // change aspect
                if (newAspect == -1)
                {
                    if (Section.Type == SectionType.ValueBased)
                    {
                        // value-based
                        Section n = Section.NextSection;
                        int a = Section.Aspects.Last().Number;

                        if (n != null && n.CurrentAspect >= 0)
                        {

                            a = n.Aspects[n.CurrentAspect].Number;
                        }

                        for (int i = Section.Aspects.Count() - 1; i >= 0; i--)
                        {
                            if (Section.Aspects[i].Number > a)
                            {
                                newAspect = i;
                            }
                        }

                        if (newAspect == -1)
                        {
                            newAspect = Section.Aspects.Count() - 1;
                        }
                    }
                    else
                    {
                        // index-based
                        if (Section.FreeSections >= 0 & Section.FreeSections < Section.Aspects.Count())
                        {
                            newAspect = Section.FreeSections;
                        }
                        else
                        {
                            newAspect = Section.Aspects.Count() - 1;
                        }
                    }
                }

                // apply new aspect
                Section.CurrentAspect = newAspect;
        */
        // update previous section
        // PreviousSection = Section.PreviousSection;
    }

    /// <summary>Gets the next section from the specified track position</summary>
    /// <param name="trackPosition">The track position</param>
    public Section NextSection(double trackPosition)
    {
        if (Sections == null || Sections.Length < 2)
        {
            return null;
        }
        // for (int i = 1; i < Sections.Length; i++)
        // {
        //     if (Sections[i].TrackPosition > trackPosition && Sections[i - 1].TrackPosition <= trackPosition)
        //     {
        //         return Sections[i];
        //     }
        // }
        return null;
    }

    /// <summary>Gets the next station from the specified track position</summary>
    /// <param name="trackPosition">The track position</param>
    // public RouteStation NextStation(double trackPosition)
    // {
    //     if (Stations == null || Stations.Length == 0)
    //     {
    //         return null;
    //     }
    //     for (int i = 1; i < Stations.Length; i++)
    //     {
    //         if (Stations[i].DefaultTrackPosition > trackPosition && Stations[i - 1].DefaultTrackPosition <= trackPosition)
    //         {
    //             return Stations[i];
    //         }
    //     }
    //     return null;
    // }

    /// <summary>Updates the currently displayed background</summary>
    /// <param name="timeElapsed">The time elapsed since the previous call to this function</param>
    /// <param name="gamePaused">Whether the game is currently paused</param>
    public void UpdateBackground(double timeElapsed, bool gamePaused)
    {
        /*
        if (gamePaused)
        {
            //Don't update the transition whilst paused
            timeElapsed = 0.0;
        }

        const float scale = 0.5f;

        // fog
        const float fogDistance = 600.0f;

        if (CurrentFog.Start < CurrentFog.End & CurrentFog.Start < fogDistance)
        {
            float ratio = (float)CurrentBackground.BackgroundImageDistance / fogDistance;

            renderer.OptionFog = true;
            renderer.Fog.Start = CurrentFog.Start * ratio * scale;
            renderer.Fog.End = CurrentFog.End * ratio * scale;
            renderer.Fog.Color = CurrentFog.Color;
            renderer.Fog.Density = CurrentFog.Density;
            renderer.Fog.IsLinear = CurrentFog.IsLinear;
            renderer.Fog.SetForImmediateMode();
        }
        else
        {
            renderer.OptionFog = false;
        }

        //Update the currently displayed background
        CurrentBackground.UpdateBackground(SecondsSinceMidnight, timeElapsed, false);

        if (TargetBackground == null || TargetBackground == CurrentBackground)
        {
            //No target background, so call the render function
            renderer.Background.Render(CurrentBackground, scale);
            return;
        }

        //Update the target background
        if (TargetBackground is StaticBackground)
        {
            TargetBackground.Countdown += timeElapsed;
        }

        TargetBackground.UpdateBackground(SecondsSinceMidnight, timeElapsed, true);

        switch (TargetBackground.Mode)
        {
            //Render, switching on the transition mode
            case BackgroundTransitionMode.FadeIn:
                renderer.Background.Render(CurrentBackground, 1.0f, scale);
                renderer.Background.Render(TargetBackground, TargetBackground.CurrentAlpha, scale);
                break;
            case BackgroundTransitionMode.FadeOut:
                renderer.Background.Render(TargetBackground, 1.0f, scale);
                renderer.Background.Render(CurrentBackground, TargetBackground.CurrentAlpha, scale);
                break;
        }

        //If our target alpha is greater than or equal to 1.0f, the background is fully displayed
        if (TargetBackground.CurrentAlpha >= 1.0f)
        {
            //Set the current background to the target & reset target to null
            CurrentBackground = TargetBackground;
            TargetBackground = null;
        }
        */
    }




    public static void ApplyRouteData(Node routeRootNode, string fileName, string compatibilityFolder, Encoding fileEncoding, ref RouteData routeData, bool previewOnly)
    {
        string signalPath, limitPath, limitGraphicsPath, transponderPath;
        UnifiedObject signalPost, limitPostStraight, limitPostLeft, limitPostRight, limitPostInfinite;
        UnifiedObject limitOneDigit, limitTwoDigits, limitThreeDigits, stopPost;
        UnifiedObject transponderS, transponderSN, transponderFalseStart, transponderPOrigin, transponderPStop;

        if (!previewOnly)
        {
            // load compatibility objects
            Node rootForCompatObjects = new Node();//("Compatibility");
            rootForCompatObjects.Name = "Compatibility";

            signalPath = System.IO.Path.Combine(compatibilityFolder, @"Signals\Japanese");
            signalPost = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(signalPath, "signal_post.csv"), fileEncoding, false, false, false);
            limitPath = System.IO.Path.Combine(compatibilityFolder, "Limits");
            limitGraphicsPath = System.IO.Path.Combine(limitPath, "Graphics");
            limitPostStraight = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_straight.csv"), fileEncoding, false, false, false);
            limitPostLeft = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_left.csv"), fileEncoding, false, false, false);
            limitPostRight = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_right.csv"), fileEncoding, false, false, false);
            limitPostInfinite = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_infinite.csv"), fileEncoding, false, false, false);
            limitOneDigit = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_1_digit.csv"), fileEncoding, false, false, false);
            limitTwoDigits = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_2_digits.csv"), fileEncoding, false, false, false);
            limitThreeDigits = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_3_digits.csv"), fileEncoding, false, false, false);
            stopPost = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(compatibilityFolder, "stop.csv"), fileEncoding, false, false, false);
            transponderPath = System.IO.Path.Combine(compatibilityFolder, "Transponders");
            transponderS = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "s.csv"), fileEncoding, false, false, false);
            transponderSN = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "sn.csv"), fileEncoding, false, false, false);
            transponderFalseStart = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "falsestart.csv"), fileEncoding, false, false, false);
            transponderPOrigin = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "porigin.csv"), fileEncoding, false, false, false);
            transponderPStop = UnifiedObject.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "pstop.csv"), fileEncoding, false, false, false);
        }
        else
        {
            signalPath = null;
            limitPath = null;
            limitGraphicsPath = null;
            transponderPath = null;
            signalPost = null;
            limitPostStraight = null;
            limitPostLeft = null;
            limitPostRight = null;
            limitPostInfinite = null;
            limitOneDigit = null;
            limitTwoDigits = null;
            limitThreeDigits = null;
            stopPost = null;
            transponderS = null;
            transponderSN = null;
            transponderFalseStart = null;
            transponderPOrigin = null;
            transponderPStop = null;
        }

        // initialize
        System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
        int lastBlock = (int)Math.Floor((routeData.TrackPosition + 600.0) / routeData.BlockInterval + 0.001) + 1;
        int blocksUsed = routeData.Blocks.Count;
        routeData.CreateMissingBlocks(lastBlock, previewOnly);
        // Array.Resize<Block>(ref routeData.Blocks, blocksUsed);


        if (!previewOnly)
        {
            // interpolate height
            routeData.InterpolateHeight();
        }

        // background
        if (!previewOnly)
        {
            // if (routeData.Blocks[0].Background >= 0 & routeData.Blocks[0].Background < routeData.Backgrounds.Length)
            // {
            //     //RenderSettings.skybox.mainTexture = routeData.Backgrounds[routeData.Blocks[0].Background];

            //     // Material material = CreateSkyboxMaterial(routeData.Backgrounds[routeData.Blocks[0].Background]);
            //     // Node camera = Camera.main.Node;
            //     // Skybox skybox = camera.GetComponent<Skybox>();
            //     // if (skybox == null) skybox = camera.AddComponent<Skybox>();
            //     // skybox.material = material;
            // }
            // else
            // {
            //     //World.CurrentBackground = new World.Background(null, 6, false);
            // }
            // //World.TargetBackground = World.CurrentBackground;
        }

        // brightness
        int currentBrightnessElement = -1;
        int currentBrightnessEvent = -1;
        float currentBrightnessValue = 1.0f;
        double currentBrightnessTrackPosition = (double)routeData.FirstUsedBlock * routeData.BlockInterval;
        if (!previewOnly)
        {
            for (int i = routeData.FirstUsedBlock; i < routeData.Blocks.Count(); i++)
            {
                // if (routeData.Blocks[i].Brightness != null && routeData.Blocks[i].Brightness.Length != 0)
                // {
                //     currentBrightnessValue = routeData.Blocks[i].Brightness[0].Value;
                //     currentBrightnessTrackPosition = routeData.Blocks[i].Brightness[0].Value;
                //     break;
                // }
            }
        }


        // create objects and track
        Vector3 playerRailPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector2 playerRailDir = new Vector2(0.0f, 1.0f);

        CurrentRoute currRoute = new CurrentRoute();

        double currentSpeedLimit = double.PositiveInfinity;
        int currentRunIndex = 0;
        int currentFlangeIndex = 0;
        if (routeData.FirstUsedBlock < 0) routeData.FirstUsedBlock = 0;
        int currentTrackLength = 0;
        int previousFogElement = -1;
        int previousFogEvent = -1;
 	    double lastRainIntensity = 0.0;
			// Fog PreviousFog = new Fog(CurrentRoute.NoFogStart, CurrentRoute.NoFogEnd, Color24.Grey, -Data.BlockInterval);
			// Fog CurrentFog = new Fog(CurrentRoute.NoFogStart, CurrentRoute.NoFogEnd, Color24.Grey, 0.0);

        for (int i = routeData.FirstUsedBlock; i < routeData.Blocks.Count; i++)
        {
            if (routeData.Blocks[i].Rails.Count > currRoute.Tracks.Count)
            {
                for (int d = 0; d < routeData.Blocks[i].Rails.Count; d++)
                {
                    var item = routeData.Blocks[i].Rails.ElementAt(d);
                    if (!currRoute.Tracks.ContainsKey(item.Key))
                    {
                        currRoute.Tracks.Add(item.Key, new Track());
                        currRoute.Tracks[item.Key].Elements = new TrackElement[256];
                    }
                }
            }
        }

        // process blocks
        float progressFactor = routeData.Blocks.Count() - routeData.FirstUsedBlock == 0 ? 0.5f : 0.5f / (float)(routeData.Blocks.Count() - routeData.FirstUsedBlock);
        for (int blockIdx = routeData.FirstUsedBlock; blockIdx < routeData.Blocks.Count(); blockIdx++)
        {
            float startingDistance = (float)blockIdx * routeData.BlockInterval;
            float endingDistance = startingDistance + routeData.BlockInterval;

            playerRailDir = playerRailDir.Normalized();

            // track
            if (!previewOnly)
            {
                if (routeData.Blocks[blockIdx].GroundCycles.Length == 1 && routeData.Blocks[blockIdx].GroundCycles[0] == -1)
                {
                    if (routeData.Structure.GroundCycles.Length == 0 || routeData.Structure.GroundCycles[0] == null)
                    {
                        routeData.Blocks[blockIdx].GroundCycles = new int[] { 0 };
                    }
                    else
                    {
                        routeData.Blocks[blockIdx].GroundCycles = routeData.Structure.GroundCycles[0];
                    }
                }
            }

            TrackElement worldTrackElement = routeData.Blocks[blockIdx].CurrentTrackState;
            int n = currentTrackLength;
            for (int j = 0; j < currRoute.Tracks.Count; j++)
            {
                if (previewOnly && j != 0)
                {
                    break;
                }
                var key = currRoute.Tracks.ElementAt(j).Key;
                if (currRoute.Tracks[key].Elements == null || currRoute.Tracks[key].Elements.Length == 0)
                {
                    currRoute.Tracks[key].Elements = new TrackElement[256];
                }
                if (n >= currRoute.Tracks[key].Elements.Length)
                {
                    Array.Resize(ref currRoute.Tracks[key].Elements, currRoute.Tracks[key].Elements.Length << 1);
                }
            }

            currentTrackLength++;
            currRoute.Tracks[0].Elements[n] = worldTrackElement;
            currRoute.Tracks[0].Elements[n].WorldPosition = playerRailPos;
            currRoute.Tracks[0].Elements[n].WorldDirection = Calc.GetNormalizedVector3(playerRailDir, routeData.Blocks[blockIdx].Pitch);
            currRoute.Tracks[0].Elements[n].WorldSide = new Vector3(playerRailDir.y, 0.0f, -playerRailDir.x);
            // World.Cross( this.Tracks[0].Elements[n].WorldDirection.X, 
            //              this.Tracks[0].Elements[n].WorldDirection.Y, 
            //              this.Tracks[0].Elements[n].WorldDirection.Z, 
            //              this.Tracks[0].Elements[n].WorldSide.X, 
            //              this.Tracks[0].Elements[n].WorldSide.Y, 
            //              this.Tracks[0].Elements[n].WorldSide.Z, 
            //             out  this.Tracks[0].Elements[n].WorldUp.X, 
            //             out  this.Tracks[0].Elements[n].WorldUp.Y, 
            //             out  this.Tracks[0].Elements[n].WorldUp.Z);
            currRoute.Tracks[0].Elements[n].StartingTrackPosition = startingDistance;
            // this.Tracks[0].Elements[n].Events = new TrackManager.GeneralEvent[] { };
            currRoute.Tracks[0].Elements[n].AdhesionMultiplier = routeData.Blocks[blockIdx].AdhesionMultiplier;
            currRoute.Tracks[0].Elements[n].CsvRwAccuracyLevel = routeData.Blocks[blockIdx].Accuracy;



            // background
            //if (!PreviewOnly)
            //{
            //    if (Data.Blocks[i].Background >= 0)
            //    {
            //        int typ;
            //        if (i == Data.FirstUsedBlock)
            //        {
            //            typ = Data.Blocks[i].Background;
            //        }
            //        else
            //        {
            //            typ = Data.Backgrounds.Length > 0 ? 0 : -1;
            //            for (int j = i - 1; j >= Data.FirstUsedBlock; j--)
            //            {
            //                if (Data.Blocks[j].Background >= 0)
            //                {
            //                    typ = Data.Blocks[j].Background;
            //                    break;
            //                }
            //            }
            //        }
            //        if (typ >= 0 & typ < Data.Backgrounds.Length)
            //        {
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //             this.Tracks[0].Elements[n].Events[m] = new TrackManager.BackgroundChangeEvent(0.0, Data.Backgrounds[typ], Data.Backgrounds[Data.Blocks[i].Background]);
            //        }
            //    }
            //}

            // brightness
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Blocks[i].Brightness.Length; j++)
            //    {
            //        int m =  this.Tracks[0].Elements[n].Events.Length;
            //        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //        double d = Data.Blocks[i].Brightness[j].TrackPosition - startingDistance;
            //         this.Tracks[0].Elements[n].Events[m] = new TrackManager.BrightnessChangeEvent(d, Data.Blocks[i].Brightness[j].Value, CurrentBrightnessValue, Data.Blocks[i].Brightness[j].TrackPosition - CurrentBrightnessTrackPosition, Data.Blocks[i].Brightness[j].Value, 0.0);
            //        if (CurrentBrightnessElement >= 0 & CurrentBrightnessEvent >= 0)
            //        {
            //            TrackManager.BrightnessChangeEvent bce = (TrackManager.BrightnessChangeEvent) this.Tracks[0].Elements[CurrentBrightnessElement].Events[CurrentBrightnessEvent];
            //            bce.NextBrightness = Data.Blocks[i].Brightness[j].Value;
            //            bce.NextDistance = Data.Blocks[i].Brightness[j].TrackPosition - CurrentBrightnessTrackPosition;
            //        }
            //        CurrentBrightnessElement = n;
            //        CurrentBrightnessEvent = m;
            //        CurrentBrightnessValue = Data.Blocks[i].Brightness[j].Value;
            //        CurrentBrightnessTrackPosition = Data.Blocks[i].Brightness[j].TrackPosition;
            //    }
            //}

            // fog
            //if (!PreviewOnly)
            //{
            //    if (Data.FogTransitionMode)
            //    {
            //        if (Data.Blocks[i].FogDefined)
            //        {
            //            Data.Blocks[i].Fog.TrackPosition = startingDistance;
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //             this.Tracks[0].Elements[n].Events[m] = new TrackManager.FogChangeEvent(0.0, PreviousFog, Data.Blocks[i].Fog, Data.Blocks[i].Fog);
            //            if (PreviousFogElement >= 0 & PreviousFogEvent >= 0)
            //            {
            //                TrackManager.FogChangeEvent e = (TrackManager.FogChangeEvent) this.Tracks[0].Elements[PreviousFogElement].Events[PreviousFogEvent];
            //                e.NextFog = Data.Blocks[i].Fog;
            //            }
            //            else
            //            {
            //                Game.PreviousFog = PreviousFog;
            //                Game.CurrentFog = PreviousFog;
            //                Game.NextFog = Data.Blocks[i].Fog;
            //            }
            //            PreviousFog = Data.Blocks[i].Fog;
            //            PreviousFogElement = n;
            //            PreviousFogEvent = m;
            //        }
            //    }
            //    else
            //    {
            //        Data.Blocks[i].Fog.TrackPosition = startingDistance + Data.BlockInterval;
            //        int m =  this.Tracks[0].Elements[n].Events.Length;
            //        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //         this.Tracks[0].Elements[n].Events[m] = new TrackManager.FogChangeEvent(0.0, PreviousFog, CurrentFog, Data.Blocks[i].Fog);
            //        PreviousFog = CurrentFog;
            //        CurrentFog = Data.Blocks[i].Fog;
            //    }
            //}

            // rail sounds
            //if (!PreviewOnly)
            //{
            //    int j = Data.Blocks[i].RailType[0];
            //    int r = j < Data.Structure.Run.Length ? Data.Structure.Run[j] : 0;
            //    int f = j < Data.Structure.Flange.Length ? Data.Structure.Flange[j] : 0;
            //    int m =  this.Tracks[0].Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //     this.Tracks[0].Elements[n].Events[m] = new TrackManager.RailSoundsChangeEvent(0.0, CurrentRunIndex, CurrentFlangeIndex, r, f);
            //    CurrentRunIndex = r;
            //    CurrentFlangeIndex = f;
            //}

            // point sound
            //if (!PreviewOnly)
            //{
            //    if (i < Data.Blocks.Length - 1)
            //    {
            //        bool q = false;
            //        for (int j = 0; j < Data.Blocks[i].Rail.Length; j++)
            //        {
            //            if (Data.Blocks[i].Rail[j].RailStart & Data.Blocks[i + 1].Rail.Length > j)
            //            {
            //                bool qx = Math.Sign(Data.Blocks[i].Rail[j].RailStart.x) != Math.Sign(Data.Blocks[i + 1].Rail[j].RailEnd.x);
            //                bool qy = Data.Blocks[i].Rail[j].RailStartY * Data.Blocks[i + 1].Rail[j].RailEndY <= 0.0;
            //                if (qx & qy)
            //                {
            //                    q = true;
            //                    break;
            //                }
            //            }
            //        }
            //        if (q)
            //        {
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //             this.Tracks[0].Elements[n].Events[m] = new TrackManager.SoundEvent(0.0, null, false, false, true, new Vector3D(0.0, 0.0, 0.0), 12.5);
            //        }
            //    }
            //}

            // station
            //if (Data.Blocks[i].Station >= 0)
            //{
            //    // station
            //    int s = Data.Blocks[i].Station;
            //    int m =  this.Tracks[0].Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //     this.Tracks[0].Elements[n].Events[m] = new TrackManager.StationStartEvent(0.0, s);
            //    double dx, dy = 3.0;
            //    if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = -5.0;
            //    }
            //    else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = 5.0;
            //    }
            //    else
            //    {
            //        dx = 0.0;
            //    }
            //    Game.Stations[s].SoundOrigin.X = Position.X + dx *  this.Tracks[0].Elements[n].WorldSide.X + dy *  this.Tracks[0].Elements[n].WorldUp.X;
            //    Game.Stations[s].SoundOrigin.Y = Position.Y + dx *  this.Tracks[0].Elements[n].WorldSide.Y + dy *  this.Tracks[0].Elements[n].WorldUp.Y;
            //    Game.Stations[s].SoundOrigin.Z = Position.Z + dx *  this.Tracks[0].Elements[n].WorldSide.Z + dy *  this.Tracks[0].Elements[n].WorldUp.Z;
            //    // passalarm
            //    if (!PreviewOnly)
            //    {
            //        if (Data.Blocks[i].StationPassAlarm)
            //        {
            //            int b = i - 6;
            //            if (b >= 0)
            //            {
            //                int j = b - Data.FirstUsedBlock;
            //                if (j >= 0)
            //                {
            //                    m =  this.Tracks[0].Elements[j].Events.Length;
            //                    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[j].Events, m + 1);
            //                     this.Tracks[0].Elements[j].Events[m] = new TrackManager.StationPassAlarmEvent(0.0);
            //                }
            //            }
            //        }
            //    }
            //}

            // stop
            //for (int j = 0; j < Data.Blocks[i].Stop.Length; j++)
            //{
            //    int s = Data.Blocks[i].Stop[j].Station;
            //    int t = Game.Stations[s].Stops.Length;
            //    Array.Resize<Game.StationStop>(ref Game.Stations[s].Stops, t + 1);
            //    Game.Stations[s].Stops[t].TrackPosition = Data.Blocks[i].Stop[j].TrackPosition;
            //    Game.Stations[s].Stops[t].ForwardTolerance = Data.Blocks[i].Stop[j].ForwardTolerance;
            //    Game.Stations[s].Stops[t].BackwardTolerance = Data.Blocks[i].Stop[j].BackwardTolerance;
            //    Game.Stations[s].Stops[t].Cars = Data.Blocks[i].Stop[j].Cars;
            //    double dx, dy = 2.0;
            //    if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = -5.0;
            //    }
            //    else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = 5.0;
            //    }
            //    else
            //    {
            //        dx = 0.0;
            //    }
            //    Game.Stations[s].SoundOrigin.X = Position.X + dx *  this.Tracks[0].Elements[n].WorldSide.X + dy *  this.Tracks[0].Elements[n].WorldUp.X;
            //    Game.Stations[s].SoundOrigin.Y = Position.Y + dx *  this.Tracks[0].Elements[n].WorldSide.Y + dy *  this.Tracks[0].Elements[n].WorldUp.Y;
            //    Game.Stations[s].SoundOrigin.Z = Position.Z + dx *  this.Tracks[0].Elements[n].WorldSide.Z + dy *  this.Tracks[0].Elements[n].WorldUp.Z;
            //}

            // limit
            //for (int j = 0; j < Data.Blocks[i].Limit.Length; j++)
            //{
            //    int m =  this.Tracks[0].Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //    double d = Data.Blocks[i].Limit[j].TrackPosition - startingDistance;
            //     this.Tracks[0].Elements[n].Events[m] = new TrackManager.LimitChangeEvent(d, CurrentSpeedLimit, Data.Blocks[i].Limit[j].Speed);
            //    CurrentSpeedLimit = Data.Blocks[i].Limit[j].Speed;
            //}

            // marker
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Markers.Length; j++)
            //    {
            //        if (Data.Markers[j].StartingPosition >= startingDistance & Data.Markers[j].StartingPosition < endingDistance)
            //        {
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //            double d = Data.Markers[j].StartingPosition - startingDistance;
            //             this.Tracks[0].Elements[n].Events[m] = new TrackManager.MarkerStartEvent(d, Data.Markers[j].Texture);
            //        }
            //        if (Data.Markers[j].EndingPosition >= startingDistance & Data.Markers[j].EndingPosition < endingDistance)
            //        {
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //            double d = Data.Markers[j].EndingPosition - startingDistance;
            //             this.Tracks[0].Elements[n].Events[m] = new TrackManager.MarkerEndEvent(d, Data.Markers[j].Texture);
            //        }
            //    }
            //}

            // sound
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Blocks[i].Sound.Length; j++)
            //    {
            //        if (Data.Blocks[i].Sound[j].Type == SoundType.TrainStatic | Data.Blocks[i].Sound[j].Type == SoundType.TrainDynamic)
            //        {
            //            int m =  this.Tracks[0].Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
            //            double d = Data.Blocks[i].Sound[j].TrackPosition - startingDistance;
            //            switch (Data.Blocks[i].Sound[j].Type)
            //            {
            //                case SoundType.TrainStatic:
            //                     this.Tracks[0].Elements[n].Events[m] = new TrackManager.SoundEvent(d, Data.Blocks[i].Sound[j].SoundBuffer, true, true, false, new Vector3D(0.0, 0.0, 0.0), 0.0);
            //                    break;
            //                case SoundType.TrainDynamic:
            //                     this.Tracks[0].Elements[n].Events[m] = new TrackManager.SoundEvent(d, Data.Blocks[i].Sound[j].SoundBuffer, false, false, true, new Vector3D(0.0, 0.0, 0.0), Data.Blocks[i].Sound[j].Speed);
            //                    break;
            //            }
            //        }
            //    }
            //}

            // Turn
            if (routeData.Blocks[blockIdx].Turn != 0.0)
            {
                float ag = (float)Math.Atan(routeData.Blocks[blockIdx].Turn);
                playerRailDir = playerRailDir.Rotated(-ag);

                // World.RotatePlane(ref  this.Tracks[0].Elements[n].WorldDirection, cosag, sinag);
                // World.RotatePlane(ref  this.Tracks[0].Elements[n].WorldSide, cosag, sinag);
                // World.Cross( this.Tracks[0].Elements[n].WorldDirection.X,  this.Tracks[0].Elements[n].WorldDirection.Y,  this.Tracks[0].Elements[n].WorldDirection.Z,  this.Tracks[0].Elements[n].WorldSide.X,  this.Tracks[0].Elements[n].WorldSide.Y,  this.Tracks[0].Elements[n].WorldSide.Z, out  this.Tracks[0].Elements[n].WorldUp.X, out  this.Tracks[0].Elements[n].WorldUp.Y, out  this.Tracks[0].Elements[n].WorldUp.Z);

                currRoute.Tracks[0].Elements[n].WorldDirection = currRoute.Tracks[0].Elements[n].WorldDirection.Rotated(Vector3.Left, -ag);
                currRoute.Tracks[0].Elements[n].WorldSide = currRoute.Tracks[0].Elements[n].WorldSide.Rotated(Vector3.Left, -ag);
                currRoute.Tracks[0].Elements[n].WorldUp = currRoute.Tracks[0].Elements[n].WorldDirection.Cross(currRoute.Tracks[0].Elements[n].WorldSide);
            }

            // Pitch
            if (routeData.Blocks[blockIdx].Pitch != 0.0)
            {
                currRoute.Tracks[0].Elements[n].Pitch = routeData.Blocks[blockIdx].Pitch;
            }
            else
            {
                currRoute.Tracks[0].Elements[n].Pitch = 0.0f;
            }

            // Curves
            float a = 0.0f;
            float c = routeData.BlockInterval;
            float h = 0.0f;
            if (worldTrackElement.CurveRadius != 0.0f & routeData.Blocks[blockIdx].Pitch != 0.0f)
            {
                float d = routeData.BlockInterval;
                float p = routeData.Blocks[blockIdx].Pitch;
                float r = worldTrackElement.CurveRadius;
                float s = d / Godot.Mathf.Sqrt(1.0f + p * p);
                h = s * p;
                float b = s / Godot.Mathf.Abs(r);
                c = Godot.Mathf.Sqrt(2.0f * r * r * (1.0f - Godot.Mathf.Cos(b)));
                a = 0.5f * Godot.Mathf.Sign(r) * (float)b;
                playerRailDir = playerRailDir.Rotated((float)-a);
            }
            else if (worldTrackElement.CurveRadius != 0.0)
            {
                float d = routeData.BlockInterval;
                float r = worldTrackElement.CurveRadius;
                float b = d / Math.Abs(r);
                c = Godot.Mathf.Sqrt(2.0f * r * r * (1.0f - Godot.Mathf.Cos((float)b)));
                a = 0.5f * Godot.Mathf.Sign(r) * b;
                playerRailDir = playerRailDir.Rotated((float)-a);
            }
            else if (routeData.Blocks[blockIdx].Pitch != 0.0)
            {
                float p = routeData.Blocks[blockIdx].Pitch;
                float d = routeData.BlockInterval;
                c = d / Godot.Mathf.Sqrt(1.0f + p * p);
                h = c * p;
            }

            float trackYaw = (float)Math.Atan2(playerRailDir.x, playerRailDir.y);
            float trackPitch = (float)Math.Atan(routeData.Blocks[blockIdx].Pitch);

            Transform groundTransformation = new Transform(Basis.Identity, Vector3.Zero);
            groundTransformation = groundTransformation.Rotated(Vector3.Up, -(float)trackYaw);

            Transform trackTransformation = new Transform(Basis.Identity, Vector3.Zero);
            trackTransformation = trackTransformation.Rotated(Vector3.Up, -(float)trackYaw);
            trackTransformation = trackTransformation.Rotated(Vector3.Right, (float)trackPitch);

            Transform nullTransformation = new Transform(Basis.Identity, Vector3.Zero);

            // ground
            if (!previewOnly)
            {
                int cb = (int)Math.Floor(blockIdx + 0.001);
                int ci = (cb % routeData.Blocks[blockIdx].GroundCycles.Length + routeData.Blocks[blockIdx].GroundCycles.Length) % routeData.Blocks[blockIdx].GroundCycles.Length;
                int gi = routeData.Blocks[blockIdx].GroundCycles[ci];
                if (routeData.Structure.Ground.ContainsKey(gi))
                {
                    routeData.Structure.Ground[routeData.Blocks[blockIdx].GroundCycles[ci]].CreateObject(playerRailPos + new Vector3(0.0f, -routeData.Blocks[blockIdx].Height, 0.0f), groundTransformation, startingDistance, endingDistance, startingDistance);
                }
            }

            // ground-aligned free objects
            if (!previewOnly)
            {
                for (int j = 0; j < routeData.Blocks[blockIdx].GroundFreeObj.Count; j++)
                {
                    routeData.Blocks[blockIdx].GroundFreeObj[j].CreateGroundAligned(routeData.Structure.FreeObjects, playerRailPos, groundTransformation, playerRailDir, routeData.Blocks[blockIdx].Height, startingDistance, endingDistance);
                }
            }
            if (!previewOnly && routeData.Structure.WeatherObjects.ContainsKey(routeData.Blocks[blockIdx].WeatherObject))
            {
                UnifiedObject obj = routeData.Structure.WeatherObjects[routeData.Blocks[blockIdx].WeatherObject];
                obj.CreateObject(playerRailPos, groundTransformation, routeData.Blocks[blockIdx].Height, startingDistance, endingDistance);
            }


            // Rail-aligned objects
            if (!previewOnly)
            {

                // Rail
                // RailIdx == 0 - player rail
                // RailIdx > 0  - auxiliary rails

                for (int jj = 0; jj < routeData.Blocks[blockIdx].Rails.Count; jj++)
                {
                    int j = routeData.Blocks[blockIdx].Rails.ElementAt(jj).Key;
                    if (j > 0 && !routeData.Blocks[blockIdx].Rails[j].RailStarted)
                    {
                        currRoute.Tracks[j].Elements[n].InvalidElement = true;
                        continue;
                    }
                    // rail
                    Vector3 pos;
                    Transform railTransformation = new Transform();
                    float planar, updown;
                    if (j == 0)
                    {
                        // rail 0
                        planar = 0.0f;
                        updown = 0.0f;
                        railTransformation = new Transform(trackTransformation.basis, Vector3.Zero);
                        railTransformation = railTransformation.Rotated(Vector3.Up, -(float)planar);            // TODO: seems a waste, always rotating by 0.0 planar / updown ???
                        railTransformation = railTransformation.Rotated(Vector3.Right, -(float)updown);
                        pos = playerRailPos;
                    }
                    else
                    {
                        // rails 1-infinity
                        float x = routeData.Blocks[blockIdx].Rails[j].RailStart.x;
                        float y = routeData.Blocks[blockIdx].Rails[j].RailStart.y;
                        Vector3 offset = new Vector3(playerRailDir.y * x, y, -playerRailDir.x * x);
                        pos = playerRailPos + offset;
                        if (blockIdx < routeData.Blocks.Count - 1 && routeData.Blocks[blockIdx + 1].Rails.ContainsKey(j))
                        {
                            // take orientation of upcoming block into account
                            Vector2 playerRailDir2 = playerRailDir;
                            Vector3 Position2 = playerRailPos;
                            Position2.x += (float)(playerRailDir.x * c);
                            Position2.y += (float)h;
                            Position2.z += (float)(playerRailDir.y * c);
                            if (a != 0.0)
                            {
                                playerRailDir2 = playerRailDir2.Rotated(-(float)a);
                            }
                            if (routeData.Blocks[blockIdx + 1].Turn != 0.0)
                            {
                                double ag = -Math.Atan(routeData.Blocks[blockIdx + 1].Turn);
                                playerRailDir2 = playerRailDir2.Rotated((float)ag);
                            }
                            double a2;
                            // double c2 = routeData.BlockInterval;
                            // double h2 = 0.0;
                            if (routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius != 0.0 && routeData.Blocks[blockIdx + 1].Pitch != 0.0)
                            {
                                double d2 = routeData.BlockInterval;
                                double p2 = routeData.Blocks[blockIdx + 1].Pitch;
                                double r2 = routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius;
                                double s2 = d2 / Math.Sqrt(1.0 + p2 * p2);
                                // h2 = s2 * p2;
                                double b2 = s2 / Math.Abs(r2);
                                // c2 = Math.Sqrt(2.0 * r2 * r2 * (1.0 - Math.Cos(b2)));
                                a2 = 0.5 * Math.Sign(r2) * b2;
                                playerRailDir2 = playerRailDir2.Rotated(-(float)a2);
                            }
                            else if (routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius != 0.0)
                            {
                                double d2 = routeData.BlockInterval;
                                double r2 = routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius;
                                double b2 = d2 / Math.Abs(r2);
                                // c2 = Math.Sqrt(2.0 * r2 * r2 * (1.0 - Math.Cos(b2)));
                                a2 = 0.5 * Math.Sign(r2) * b2;
                                playerRailDir2 = playerRailDir2.Rotated(-(float)a2);
                            }
                            // else if (routeData.Blocks[i + 1].Pitch != 0.0) {
                            // double p2 = routeData.Blocks[i + 1].Pitch;
                            // double d2 = routeData.BlockInterval;
                            // c2 = d2 / Math.Sqrt(1.0 + p2 * p2);
                            // h2 = c2 * p2;
                            // }

                            //These generate a compiler warning, as secondary tracks do not generate yaw, as they have no
                            //concept of a curve, but rather are a straight line between two points
                            //TODO: Revist the handling of secondary tracks ==> !!BACKWARDS INCOMPATIBLE!!
                            /*
                            double TrackYaw2 = Math.Atan2(playerRailDir2.x, playerRailDir2.y);
                            double TrackPitch2 = Math.Atan(routeData.Blocks[i + 1].Pitch);
                            Transformation GroundTransformation2 = new Transformation(TrackYaw2, 0.0, 0.0);
                            Transformation TrackTransformation2 = new Transformation(TrackYaw2, TrackPitch2, 0.0);
                             */
                            float x2 = routeData.Blocks[blockIdx + 1].Rails[j].RailEnd.x;
                            float y2 = routeData.Blocks[blockIdx + 1].Rails[j].RailEnd.y;
                            Vector3 offset2 = new Vector3(playerRailDir2.y * x2, y2, -playerRailDir2.x * x2);
                            Vector3 pos2 = Position2 + offset2;
                            Vector3 r = new Vector3(pos2.x - pos.x, pos2.y - pos.y, pos2.z - pos.z);
                            r = r.Normalized();

                            // railTransformation.Z = r;
                            // railTransformation.X = new Vector3(r.Z, 0.0, -r.X);
                            // Normalize(ref railTransformation.X.X, ref railTransformation.X.Z);
                            // railTransformation.Y = Vector3.Cross(railTransformation.Z, railTransformation.X);

                            Vector3 newY = railTransformation.basis.x.Cross(railTransformation.basis.y);

                            railTransformation = new Transform(new Vector3(r.z, 0.0f, -r.x), newY, r, railTransformation.origin);

                            planar = Godot.Mathf.Atan(routeData.Blocks[blockIdx + 1].Rails[j].MidPoint.x / c);
                            updown = Godot.Mathf.Atan(routeData.Blocks[blockIdx + 1].Rails[j].MidPoint.y / c);
                        }
                        else
                        {
                            planar = 0.0f;
                            updown = 0.0f;
                            railTransformation = new Transform(trackTransformation.basis, Vector3.Zero);
                        }

                        currRoute.Tracks[j].Elements[n].StartingTrackPosition = startingDistance;
                        currRoute.Tracks[j].Elements[n].WorldPosition = pos;
                        currRoute.Tracks[j].Elements[n].WorldDirection = railTransformation.basis.z;
                        currRoute.Tracks[j].Elements[n].WorldSide = railTransformation.basis.x;
                        currRoute.Tracks[j].Elements[n].WorldUp = railTransformation.basis.y;
                        currRoute.Tracks[j].Elements[n].CurveCant = routeData.Blocks[blockIdx].Rails[j].CurveCant;
                        currRoute.Tracks[j].Elements[n].AdhesionMultiplier = routeData.Blocks[blockIdx].AdhesionMultiplier;
                    }
                    if (routeData.Structure.RailObjects.ContainsKey(routeData.Blocks[blockIdx].RailType[j]))
                    {
                        if (routeData.Structure.RailObjects[routeData.Blocks[blockIdx].RailType[j]] != null)
                        {
                            routeData.Structure.RailObjects[routeData.Blocks[blockIdx].RailType[j]].CreateObject(pos, railTransformation, startingDistance, endingDistance, startingDistance);
                        }
                    }

                    // points of interest
                    for (int k = 0; k < routeData.Blocks[blockIdx].PointsOfInterest.Length; k++)
                    {
                        if (routeData.Blocks[blockIdx].PointsOfInterest[k].RailIndex == j)
                        {
                            float d = routeData.Blocks[blockIdx].PointsOfInterest[k].TrackPosition - startingDistance;
                            float x = routeData.Blocks[blockIdx].PointsOfInterest[k].Position.x;
                            float y = routeData.Blocks[blockIdx].PointsOfInterest[k].Position.y;
                            int m = currRoute.PointsOfInterest.Length;
                            Array.Resize(ref currRoute.PointsOfInterest, m + 1);
                            currRoute.PointsOfInterest[m].TrackPosition = routeData.Blocks[blockIdx].PointsOfInterest[k].TrackPosition;
                            if (blockIdx < routeData.Blocks.Count - 1 && routeData.Blocks[blockIdx + 1].Rails.ContainsKey(j))
                            {
                                Vector2 trackOffset = routeData.Blocks[blockIdx].Rails[j].MidPoint;
                                trackOffset.x = routeData.Blocks[blockIdx].Rails[j].RailStart.x + d / routeData.BlockInterval * trackOffset.x;
                                trackOffset.y = routeData.Blocks[blockIdx].Rails[j].RailStart.y + d / routeData.BlockInterval * trackOffset.y;
                                currRoute.PointsOfInterest[m].TrackOffset = new Vector3(x + trackOffset.x, y + trackOffset.y, 0.0f);
                            }
                            else
                            {
                                float dx = routeData.Blocks[blockIdx].Rails[j].RailStart.x;
                                float dy = routeData.Blocks[blockIdx].Rails[j].RailStart.y;
                                currRoute.PointsOfInterest[m].TrackOffset = new Vector3(x + dx, y + dy, 0.0f);
                            }
                            currRoute.PointsOfInterest[m].TrackYaw = routeData.Blocks[blockIdx].PointsOfInterest[k].Yaw + planar;
                            currRoute.PointsOfInterest[m].TrackPitch = routeData.Blocks[blockIdx].PointsOfInterest[k].Pitch + updown;
                            currRoute.PointsOfInterest[m].TrackRoll = routeData.Blocks[blockIdx].PointsOfInterest[k].Roll;
                            currRoute.PointsOfInterest[m].Text = routeData.Blocks[blockIdx].PointsOfInterest[k].Text;
                        }
                    }

                    // poles
                    if (routeData.Blocks[blockIdx].RailPole.Length > j)
                    {
                        routeData.Blocks[blockIdx].RailPole[j].Create(routeData.Structure.Poles, pos, railTransformation, playerRailDir, planar, updown, startingDistance, endingDistance);
                    }

                    // walls
                    if (routeData.Blocks[blockIdx].RailWall.ContainsKey(j))
                    {
                        routeData.Blocks[blockIdx].RailWall[j].Create(pos, railTransformation, startingDistance, endingDistance);
                    }

                    // dikes
                    if (routeData.Blocks[blockIdx].RailDike.ContainsKey(j))
                    {
                        routeData.Blocks[blockIdx].RailDike[j].Create(pos, railTransformation, startingDistance, endingDistance);
                    }
                    
                    // sounds
                    if (j == 0)
                    {
                        // for (int k = 0; k < routeData.Blocks[blockIdx].SoundEvents.Length; k++)
                        // {
                        //     routeData.Blocks[blockIdx].SoundEvents[k].Create(pos, startingDistance, playerRailDir, planar, updown);
                        // }
                    }
                    // forms
                    for (int k = 0; k < routeData.Blocks[blockIdx].Forms.Length; k++)
                    {
                        // primary rail
                        if (routeData.Blocks[blockIdx].Forms[k].PrimaryRail == j)
                        {
                            routeData.Blocks[blockIdx].Forms[k].CreatePrimaryRail(routeRootNode, routeData.Blocks[blockIdx], routeData.Blocks[blockIdx + 1], pos, railTransformation, startingDistance, endingDistance, fileName);
                        }
                        // secondary rail
                        if (routeData.Blocks[blockIdx].Forms[k].SecondaryRail == j)
                        {
                            routeData.Blocks[blockIdx].Forms[k].CreateSecondaryRail(routeData.Blocks[blockIdx], pos, railTransformation, startingDistance, endingDistance, fileName);
                        }
                    }

                    // cracks
                    for (int k = 0; k < routeData.Blocks[blockIdx].Cracks.Length; k++)
                    {
                        // routeData.Blocks[blockIdx].Cracks[k].Create(j, railTransformation, pos, routeData.Blocks[blockIdx], routeData.Blocks[blockIdx + 1], routeData.Structure, startingDistance, endingDistance, fileName);
                    }

                    // free objects
                    if (routeData.Blocks[blockIdx].RailFreeObj.ContainsKey(j))
                    {
                        for (int k = 0; k < routeData.Blocks[blockIdx].RailFreeObj[j].Count; k++)
                        {
                            routeData.Blocks[blockIdx].RailFreeObj[j][k].CreateRailAligned(routeData.Structure.FreeObjects, new Vector3(pos), railTransformation, startingDistance, endingDistance);
                        }
                    }

                    // transponder objects
                    if (j == 0)
                    {
                        // for (int k = 0; k < routeData.Blocks[blockIdx].Transponders.Length; k++)
                        // {
                        //     double b = 0.25 + 0.75 * GetBrightness(ref routeData, routeData.Blocks[blockIdx].Transponders[k].TrackPosition);
                        //     routeData.Blocks[blockIdx].Transponders[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, b, routeData.Structure.Beacon);
                        // }
                        // for (int k = 0; k < routeData.Blocks[blockIdx].DestinationChanges.Length; k++)
                        // {
                        //     routeData.Blocks[blockIdx].DestinationChanges[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, routeData.Structure.Beacon);
                        // }
                        // for (int k = 0; k < routeData.Blocks[blockIdx].HornBlows.Length; k++)
                        // {
                        //     routeData.Blocks[blockIdx].HornBlows[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, routeData.Structure.Beacon);
                        // }
                    }

                    // sections/signals/transponders
                    if (j == 0)
                    {
                        // signals
                        // for (int k = 0; k < routeData.Blocks[blockIdx].Signals.Length; k++)
                        // {
                        //     routeData.Blocks[blockIdx].Signals[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, 0.27 + 0.75 * GetBrightness(ref Data, routeData.Blocks[blockIdx].Signals[k].TrackPosition));
                        // }
                        // // sections
                        // for (int k = 0; k < routeData.Blocks[blockIdx].Sections.Length; k++)
                        // {
                        //     routeData.Blocks[blockIdx].Sections[k].Create(CurrentRoute, routeData.Blocks, i, n, routeData.SignalSpeeds, startingDistance, routeData.BlockInterval);
                        // }
                        // // transponders introduced after corresponding sections
                        // for (int l = 0; l < routeData.Blocks[blockIdx].Transponders.Length; l++)
                        // {
                        //     routeData.Blocks[blockIdx].Transponders[l].CreateEvent(ref CurrentRoute.Tracks[0].Elements[n], startingDistance);
                        // }
                    }

                    // limit
                    if (j == 0)
                    {
                        // for (int k = 0; k < routeData.Blocks[blockIdx].Limits.Length; k++)
                        // {
                        //     double b = 0.25 + 0.75 * GetBrightness(ref Data, routeData.Blocks[blockIdx].Limits[k].TrackPosition);
                        //     routeData.Blocks[blockIdx].Limits[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, b, routeData.UnitOfSpeed);
                        // }
                    }

                    // stop
                    if (j == 0)
                    {
                        // for (int k = 0; k < routeData.Blocks[blockIdx].StopPositions.Length; k++)
                        // {
                        //     double b = 0.25 + 0.75 * GetBrightness(ref Data, routeData.Blocks[blockIdx].StopPositions[k].TrackPosition);
                        //     routeData.Blocks[blockIdx].StopPositions[k].Create(new Vector3(pos), railTransformation, startingDistance, endingDistance, b);
                        // }
                    }
                }
            }

            // finalize block
            playerRailPos.x += (float)(playerRailDir.x * c);
            playerRailPos.y += (float)h;
            playerRailPos.z -= (float)(playerRailDir.y * c);
            if (a != 0.0)
            {
                // Calc.Rotate(ref direction, Math.Cos(-a), Math.Sin(-a));
                playerRailDir = playerRailDir.Rotated(-(float)a);
            }
        }

        // orphaned transponders
        //if (!PreviewOnly)
        //{
        //    for (int i = Data.FirstUsedBlock; i < Data.Blocks.Count(); i++)
        //    {
        //        for (int j = 0; j < Data.Blocks[i].Transponder.Count(); j++)
        //        {
        //            if (Data.Blocks[i].Transponder[j].Type != -1)
        //            {
        //                int n = i - Data.FirstUsedBlock;
        //                int m =  this.Tracks[0].Elements[n].Events.Count();
        //                Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
        //                double d = Data.Blocks[i].Transponder[j].TrackPosition -  this.Tracks[0].Elements[n].StartingTrackPosition;
        //                int s = Data.Blocks[i].Transponder[j].Section;
        //                if (s >= 0) s = -1;
        //                 this.Tracks[0].Elements[n].Events[m] = new TrackManager.TransponderEvent(d, Data.Blocks[i].Transponder[j].Type, Data.Blocks[i].Transponder[j].Data, s, Data.Blocks[i].Transponder[j].ClipToFirstRedSection);
        //                Data.Blocks[i].Transponder[j].Type = -1;
        //            }
        //        }
        //    }
        //}

        // insert station end events
        //for (int i = 0; i < Game.Stations.Count(); i++)
        //{
        //    int j = Game.Stations[i].Stops.Count() - 1;
        //    if (j >= 0)
        //    {
        //        double p = Game.Stations[i].Stops[j].TrackPosition + Game.Stations[i].Stops[j].ForwardTolerance + Data.BlockInterval;
        //        int k = (int)Math.Floor(p / (double)Data.BlockInterval) - Data.FirstUsedBlock;
        //        if (k >= 0 & k < Data.Blocks.Count())
        //        {
        //            double d = p - (double)(k + Data.FirstUsedBlock) * (double)Data.BlockInterval;
        //            int m =  this.Tracks[0].Elements[k].Events.Count();
        //            Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[k].Events, m + 1);
        //             this.Tracks[0].Elements[k].Events[m] = new TrackManager.StationEndEvent(d, i);
        //        }
        //    }
        //}

        // create default point of interests
        //if (Game.PointsOfInterest.Count() == 0)
        //{
        //    Game.PointsOfInterest = new OpenBve.Game.PointOfInterest[Game.Stations.Count()];
        //    int n = 0;
        //    for (int i = 0; i < Game.Stations.Count(); i++)
        //    {
        //        if (Game.Stations[i].Stops.Count() != 0)
        //        {
        //            Game.PointsOfInterest[n].Text = Game.Stations[i].Name;
        //            Game.PointsOfInterest[n].TrackPosition = Game.Stations[i].Stops[0].TrackPosition;
        //            Game.PointsOfInterest[n].TrackOffset = new Vector3D(0.0, 2.8, 0.0);
        //            if (Game.Stations[i].OpenLeftDoors & !Game.Stations[i].OpenRightDoors)
        //            {
        //                Game.PointsOfInterest[n].TrackOffset.X = -2.5;
        //            }
        //            else if (!Game.Stations[i].OpenLeftDoors & Game.Stations[i].OpenRightDoors)
        //            {
        //                Game.PointsOfInterest[n].TrackOffset.X = 2.5;
        //            }
        //            n++;
        //        }
        //    }
        //    Array.Resize<Game.PointOfInterest>(ref Game.PointsOfInterest, n);
        //}

        // convert block-based cant into point-based cant
        //for (int i = CurrentTrackLength - 1; i >= 1; i--)
        //{
        //    if ( this.Tracks[0].Elements[i].CurveCant == 0.0)
        //    {
        //         this.Tracks[0].Elements[i].CurveCant =  this.Tracks[0].Elements[i - 1].CurveCant;
        //    }
        //    else if ( this.Tracks[0].Elements[i - 1].CurveCant != 0.0)
        //    {
        //        if (Math.Sign( this.Tracks[0].Elements[i - 1].CurveCant) == Math.Sign( this.Tracks[0].Elements[i].CurveCant))
        //        {
        //            if (Math.Abs( this.Tracks[0].Elements[i - 1].CurveCant) > Math.Abs( this.Tracks[0].Elements[i].CurveCant))
        //            {
        //                 this.Tracks[0].Elements[i].CurveCant =  this.Tracks[0].Elements[i - 1].CurveCant;
        //            }
        //        }
        //        else
        //        {
        //             this.Tracks[0].Elements[i].CurveCant = 0.5 * ( this.Tracks[0].Elements[i].CurveCant +  this.Tracks[0].Elements[i - 1].CurveCant);
        //        }
        //    }
        //}

        // finalize
        //Array.Resize<TrackManager.TrackElement>(ref  this.Tracks[0].Elements, CurrentTrackLength);
        //for (int i = 0; i < Game.Stations.Count(); i++)
        //{
        //    if (Game.Stations[i].Stops.Count() == 0 & Game.Stations[i].StopMode != Game.StationStopMode.AllPass)
        //    {
        //        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Station " + Game.Stations[i].Name + " expects trains to stop but does not define stop points at track position " + Game.Stations[i].DefaultTrackPosition.ToString(Culture) + " in file " + FileName);
        //        Game.Stations[i].StopMode = Game.StationStopMode.AllPass;
        //    }
        //    if (Game.Stations[i].StationType == Game.StationType.ChangeEnds)
        //    {
        //        if (i < Game.Stations.Count() - 1)
        //        {
        //            if (Game.Stations[i + 1].StopMode != Game.StationStopMode.AllStop)
        //            {
        //                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Station " + Game.Stations[i].Name + " is marked as \"change ends\" but the subsequent station does not expect all trains to stop in file " + FileName);
        //                Game.Stations[i + 1].StopMode = Game.StationStopMode.AllStop;
        //            }
        //        }
        //        else
        //        {
        //            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Station " + Game.Stations[i].Name + " is marked as \"change ends\" but there is no subsequent station defined in file " + FileName);
        //            Game.Stations[i].StationType = Game.StationType.Terminal;
        //        }
        //    }
        //}
        //if (Game.Stations.Count() != 0)
        //{
        //    Game.Stations[Game.Stations.Count() - 1].StationType = Game.StationType.Terminal;
        //}
        //if ( this.Tracks[0].Elements.Count() != 0)
        //{
        //    int n =  this.Tracks[0].Elements.Count() - 1;
        //    int m =  this.Tracks[0].Elements[n].Events.Count();
        //    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[n].Events, m + 1);
        //     this.Tracks[0].Elements[n].Events[m] = new TrackManager.TrackEndEvent(Data.BlockInterval);
        //}

        // insert compatibility beacons
        //if (!PreviewOnly)
        //{
        //    List<TrackManager.TransponderEvent> transponders = new List<TrackManager.TransponderEvent>();
        //    bool atc = false;
        //    for (int i = 0; i <  this.Tracks[0].Elements.Count(); i++)
        //    {
        //        for (int j = 0; j <  this.Tracks[0].Elements[i].Events.Count(); j++)
        //        {
        //            if (!atc)
        //            {
        //                if ( this.Tracks[0].Elements[i].Events[j] is TrackManager.StationStartEvent)
        //                {
        //                    TrackManager.StationStartEvent station = (TrackManager.StationStartEvent) this.Tracks[0].Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Atc)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[i].Events,  this.Tracks[0].Elements[i].Events.Count() + 2);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 0, 0, false);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 1, 0, false);
        //                        atc = true;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if ( this.Tracks[0].Elements[i].Events[j] is TrackManager.StationStartEvent)
        //                {
        //                    TrackManager.StationStartEvent station = (TrackManager.StationStartEvent) this.Tracks[0].Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Ats)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[i].Events,  this.Tracks[0].Elements[i].Events.Count() + 2);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 2, 0, false);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 3, 0, false);
        //                    }
        //                }
        //                else if ( this.Tracks[0].Elements[i].Events[j] is TrackManager.StationEndEvent)
        //                {
        //                    TrackManager.StationEndEvent station = (TrackManager.StationEndEvent) this.Tracks[0].Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Atc)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[i].Events,  this.Tracks[0].Elements[i].Events.Count() + 2);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 1, 0, false);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 2, 0, false);
        //                    }
        //                    else if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Ats)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[i].Events,  this.Tracks[0].Elements[i].Events.Count() + 2);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 3, 0, false);
        //                         this.Tracks[0].Elements[i].Events[ this.Tracks[0].Elements[i].Events.Count() - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 0, 0, false);
        //                        atc = false;
        //                    }
        //                }
        //                else if ( this.Tracks[0].Elements[i].Events[j] is TrackManager.LimitChangeEvent)
        //                {
        //                    TrackManager.LimitChangeEvent limit = (TrackManager.LimitChangeEvent) this.Tracks[0].Elements[i].Events[j];
        //                    int speed = (int)Math.Round(Math.Min(4095.0, 3.6 * limit.NextSpeedLimit));
        //                    int distance = Math.Min(1048575, (int)Math.Round( this.Tracks[0].Elements[i].StartingTrackPosition + limit.TrackPositionDelta));
        //                    unchecked
        //                    {
        //                        int value = (int)((uint)speed | ((uint)distance << 12));
        //                        transponders.Add(new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcSpeedLimit, value, 0, false));
        //                    }
        //                }
        //            }
        //            if ( this.Tracks[0].Elements[i].Events[j] is TrackManager.TransponderEvent)
        //            {
        //                TrackManager.TransponderEvent transponder =  this.Tracks[0].Elements[i].Events[j] as TrackManager.TransponderEvent;
        //                if (transponder.Type == TrackManager.SpecialTransponderTypes.InternalAtsPTemporarySpeedLimit)
        //                {
        //                    int speed = Math.Min(4095, transponder.Data);
        //                    int distance = Math.Min(1048575, (int)Math.Round( this.Tracks[0].Elements[i].StartingTrackPosition + transponder.TrackPositionDelta));
        //                    unchecked
        //                    {
        //                        int value = (int)((uint)speed | ((uint)distance << 12));
        //                        transponder.DontTriggerAnymore = true;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    int n =  this.Tracks[0].Elements[0].Events.Count();
        //    Array.Resize<TrackManager.GeneralEvent>(ref  this.Tracks[0].Elements[0].Events, n + transponders.Count);
        //    for (int i = 0; i < transponders.Count; i++)
        //    {
        //         this.Tracks[0].Elements[0].Events[n + i] = transponders[i];
        //    }
        //}

        // cant
        if (!previewOnly)
        {
            ComputeCantTangents(currRoute);
            int subdivisions = (int)Godot.Mathf.Floor(routeData.BlockInterval / 5.0f);
            if (subdivisions >= 2)
            {
                if (routeData.TurnUsed)
                {
                    currRoute.Tracks[0].SmoothTurns(subdivisions);
                }
                ComputeCantTangents(currRoute);
            }
        }
    }

    private static void ComputeCantTangents(CurrentRoute r)
    {
        for (int ii = 0; ii < r.Tracks.Count; ii++)
        {
            int i = r.Tracks.ElementAt(ii).Key;
            r.Tracks[i].ComputeCantTangents();
        }
    }

}
