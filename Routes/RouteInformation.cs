using Godot;
// using System.Drawing;

/// <summary>Holds the information for a route</summary>
public class RouteInformation
{
    /// <summary>The loading screen background</summary>
    public Texture LoadingScreenBackground;

    /// <summary>A bitmap storing the current route-map image</summary>
    public Image RouteMap;

    /// <summary>A bitmap storing the current route gradient profile </summary>
    public Image GradientProfile;

    /// <summary>A string storing the absolute on-disk path to a .RTF or .TXT document describing the briefing for the current scenario</summary>
#pragma warning disable 649
    public string RouteBriefing;
#pragma warning restore 649
    /// <summary>A string storing the absolute on-disk path to the current route file</summary>
    public string RouteFile;

    /// <summary>A string storing the absolute on-disk path to the current train folder</summary>
    public string TrainFolder;

    /// <summary>The number of files not found</summary>
    public string FilesNotFound;

    /// <summary>The number of errors and warnings</summary>
    public string ErrorsAndWarnings;

    /// <summary>Ranges of route info bitmaps</summary>
    public int GradientMinTrack, GradientMaxTrack;

    public string DefaultTimetableDescription = "";

    public int RouteMinX, RouteMaxX, RouteMinZ, RouteMaxZ;

    public void LoadInformation()
    {
        // lock (Illustrations.Locker)
        // {
        //     RouteMap = Illustrations.CreateRouteMap(500, 500, true);
        //     RouteMinX = Illustrations.LastRouteMinX;
        //     RouteMaxX = Illustrations.LastRouteMaxX;
        //     RouteMinZ = Illustrations.LastRouteMinZ;
        //     RouteMaxZ = Illustrations.LastRouteMaxZ;
        //     GradientProfile = Illustrations.CreateRouteGradientProfile(500, 500, true);
        //     GradientMinTrack = Illustrations.LastGradientMinTrack;
        //     GradientMaxTrack = Illustrations.LastGradientMaxTrack;
        // }
    }
}
