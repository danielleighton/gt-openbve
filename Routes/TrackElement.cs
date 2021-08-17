using Godot;

/// <summary>Defines a single track element (cell)</summary>
public struct TrackElement
{
    /// <summary>Whether the element is invalid</summary>
    public bool InvalidElement;
    /// <summary>The starting linear track position of this element</summary>
    public float StartingTrackPosition;
    /// <summary>The curve radius applying to this element</summary>
    public float CurveRadius;
    /// <summary>The curve cant applying to this element</summary>
    public float CurveCant;
    /// <summary>The tangent value applied to the curve due to cant</summary>
    public float CurveCantTangent;
    /// <summary>The adhesion multiplier applying to this element</summary>
    public float AdhesionMultiplier;
    /// <summary>The rain intensity applying to this element</summary>
    public int RainIntensity;
    /// <summary>The snow intensity applying to this element</summary>
    public int SnowIntensity;
    /// <summary>The accuracy level of this element (Affects cab sway etc) </summary>
    public float CsvRwAccuracyLevel;
    /// <summary>The pitch of this element</summary>
    public float Pitch;
    /// <summary>The absolute world position</summary>
    public Vector3 WorldPosition;
    /// <summary>The direction vector</summary>
    public Vector3 WorldDirection;
    /// <summary>The up vector</summary>
    public Vector3 WorldUp;
    /// <summary>The side vector</summary>
    public Vector3 WorldSide;
    /// <summary>An array containing all events attached to this element</summary>
    // public GeneralEvent[] Events;

    /// <summary>Creates a new track element</summary>
    /// <param name="StartingTrackPosition">The starting position (relative to zero)</param>
    public TrackElement(float StartingTrackPosition)
    {
        this.InvalidElement = false;
        this.StartingTrackPosition = StartingTrackPosition;
        this.Pitch = 0.0f;
        this.CurveRadius = 0.0f;
        this.CurveCant = 0.0f;
        this.CurveCantTangent = 0.0f;
        this.AdhesionMultiplier = 1.0f;
        this.RainIntensity = 0;
        this.SnowIntensity = 0;
        this.CsvRwAccuracyLevel = 2.0f;
        this.WorldPosition = Vector3.Zero;
        this.WorldDirection = Vector3.Forward;
        this.WorldUp = Vector3.Down;
        this.WorldSide = Vector3.Right;
        // this.Events = new GeneralEvent[] { };
    }
}
