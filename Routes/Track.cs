using System;
using Godot;

/// <summary>A track (route) is made up of an array of track elements (cells)</summary>
public class Track
{
    /// <summary>The elements array</summary>
    public TrackElement[] Elements;

    /// <summary>The rail gauge for this track</summary>
    public float RailGauge = 1.435f;

    /// <summary>Gets the innacuracy (Gauge spread and track bounce) for a given track position and routefile innacuracy value</summary>
    /// <param name="position">The track position</param>
    /// <param name="inaccuracy">The openBVE innacuaracy value</param>
    /// <param name="x">The X (horizontal) co-ordinate to update</param>
    /// <param name="y">The Y (vertical) co-ordinate to update</param>
    /// <param name="c">???</param>
    public void GetInaccuracies(float position, float inaccuracy, out float x, out float y, out float c)
    {
        if (inaccuracy <= 0.0)
        {
            x = 0.0f;
            y = 0.0f;
            c = 0.0f;
        }
        else
        {
            float z = Godot.Mathf.Pow(0.25f * inaccuracy, 1.2f) * position;
            x = 0.14f * Godot.Mathf.Sin(0.5843f * z) + 0.82f * Godot.Mathf.Sin(0.2246f * z) + 0.55f * Godot.Mathf.Sin(0.1974f * z);
            x *= 0.0035f * RailGauge * inaccuracy;
            y = 0.18f * Godot.Mathf.Sin(0.5172f * z) + 0.37f * Godot.Mathf.Sin(0.3251f * z) + 0.91f * Godot.Mathf.Sin(0.3773f * z);
            y *= 0.0020f * RailGauge * inaccuracy;
            c = 0.23f * Godot.Mathf.Sin(0.3131f * z) + 0.54f * Godot.Mathf.Sin(0.5807f * z) + 0.81f * Godot.Mathf.Sin(0.3621f * z);
            c *= 0.0025f * RailGauge * inaccuracy;
        }
    }

    /// <summary>Computes the cant tangents for all elements</summary>
    public void ComputeCantTangents()
    {
        if (Elements.Length == 1)
        {
            Elements[0].CurveCantTangent = 0.0f;
        }
        else if (Elements.Length != 0)
        {
            float[] deltas = new float[Elements.Length - 1];
            for (int j = 0; j < Elements.Length - 1; j++)
            {
                deltas[j] = Elements[j + 1].CurveCant - Elements[j].CurveCant;
            }

            float[] tangents = new float[Elements.Length];
            tangents[0] = deltas[0];
            tangents[Elements.Length - 1] = deltas[Elements.Length - 2];
            for (int j = 1; j < Elements.Length - 1; j++)
            {
                tangents[j] = 0.5f * (deltas[j - 1] + deltas[j]);
            }

            for (int j = 0; j < Elements.Length - 1; j++)
            {
                if (deltas[j] == 0.0)
                {
                    tangents[j] = 0.0f;
                    tangents[j + 1] = 0.0f;
                }
                else
                {
                    float a = tangents[j] / deltas[j];
                    float b = tangents[j + 1] / deltas[j];
                    if (a * a + b * b > 9.0)
                    {
                        float t = 3.0f / Godot.Mathf.Sqrt(a * a + b * b);
                        tangents[j] = t * a * deltas[j];
                        tangents[j + 1] = t * b * deltas[j];
                    }
                }
            }

            for (int j = 0; j < Elements.Length; j++)
            {
                Elements[j].CurveCantTangent = tangents[j];
            }
        }
    }

    /// <summary>Smooths all curves / turns</summary>
    public void SmoothTurns(int subdivisions) //, HostInterface currentHost)
    {
        if (subdivisions < 2)
        {
            throw new InvalidOperationException();
        }

        // subdivide track
        int length = Elements.Length;
        int newLength = (length - 1) * subdivisions + 1;
        float[] midpointsTrackPositions = new float[newLength];
        Vector3[] midpointsWorldPositions = new Vector3[newLength];
        Vector3[] midpointsWorldDirections = new Vector3[newLength];
        Vector3[] midpointsWorldUps = new Vector3[newLength];
        Vector3[] midpointsWorldSides = new Vector3[newLength];
        float[] midpointsCant = new float[newLength];
        TrackFollower follower = new TrackFollower(/*currentHost*/);

        for (int i = 0; i < newLength; i++)
        {
            int m = i % subdivisions;
            if (m != 0)
            {
                int q = i / subdivisions;

                float r = (float)m / (float)subdivisions;
                float p = (1.0f - r) * Elements[q].StartingTrackPosition + r * Elements[q + 1].StartingTrackPosition;
                follower.UpdateAbsolute(-1.0f, true, false);
                follower.UpdateAbsolute((float)p, true, false);
                midpointsTrackPositions[i] = p;
                midpointsWorldPositions[i] = follower.WorldPosition;
                midpointsWorldDirections[i] = follower.WorldDirection;
                midpointsWorldUps[i] = follower.WorldUp;
                midpointsWorldSides[i] = follower.WorldSide;
                midpointsCant[i] = follower.CurveCant;
            }
        }

        Array.Resize(ref Elements, newLength);
        for (int i = length - 1; i >= 1; i--)
        {
            Elements[subdivisions * i] = Elements[i];
        }

        for (int i = 0; i < Elements.Length; i++)
        {
            int m = i % subdivisions;
            if (m != 0)
            {
                int q = i / subdivisions;
                int j = q * subdivisions;
                Elements[i] = Elements[j];
                // Elements[i].Events = new GeneralEvent[] { };
                Elements[i].StartingTrackPosition = midpointsTrackPositions[i];
                Elements[i].WorldPosition = midpointsWorldPositions[i];
                Elements[i].WorldDirection = midpointsWorldDirections[i];
                Elements[i].WorldUp = midpointsWorldUps[i];
                Elements[i].WorldSide = midpointsWorldSides[i];
                Elements[i].CurveCant = midpointsCant[i];
                Elements[i].CurveCantTangent = 0.0f;
            }
        }

        // find turns
        bool[] isTurn = new bool[Elements.Length];
        {
            for (int i = 1; i < Elements.Length - 1; i++)
            {
                int m = i % subdivisions;
                if (m == 0)
                {
                    double p = 0.00000001 * Elements[i - 1].StartingTrackPosition + 0.99999999 * Elements[i].StartingTrackPosition;
                    follower.UpdateAbsolute((float)p, true, false);
                    Vector3 d1 = Elements[i].WorldDirection;
                    Vector3 d2 = follower.WorldDirection;
                    Vector3 d = d1 - d2;
                    double t = d.x * d.x + d.z * d.z;
                    const double e = 0.0001;
                    if (t > e)
                    {
                        isTurn[i] = true;
                    }
                }
            }
        }
        // replace turns by curves
        for (int i = 0; i < Elements.Length; i++)
        {
            if (isTurn[i])
            {
                // estimate radius
                Vector3 AP = Elements[i - 1].WorldPosition;
                Vector3 BP = Elements[i + 1].WorldPosition;
                Vector3 S = Elements[i - 1].WorldSide - Elements[i + 1].WorldSide;
                float rx;
                if (S.x * S.x > 0.000001)
                {
                    rx = (BP.x - AP.x) / S.x;
                }
                else
                {
                    rx = 0.0f;
                }

                float rz;
                if (S.z * S.z > 0.000001)
                {
                    rz = (BP.z - AP.z) / S.z;
                }
                else
                {
                    rz = 0.0f;
                }

                if (rx != 0.0 | rz != 0.0)
                {
                    float r;
                    if (rx != 0.0 & rz != 0.0)
                    {
                        if (Godot.Mathf.Sign(rx) == Godot.Mathf.Sign(rz))
                        {
                            float f = rx / rz;
                            if (f > -1.1 & f < -0.9 | f > 0.9 & f < 1.1)
                            {
                                r = Godot.Mathf.Sqrt(Godot.Mathf.Abs(rx * rz)) * Godot.Mathf.Sign(rx);
                            }
                            else
                            {
                                r = 0.0f;
                            }
                        }
                        else
                        {
                            r = 0.0f;
                        }
                    }
                    else if (rx != 0.0)
                    {
                        r = rx;
                    }
                    else
                    {
                        r = rz;
                    }

                    if (r * r > 1.0)
                    {
                        // apply radius
                        Elements[i - 1].CurveRadius = r;
                        double p = 0.00000001 * Elements[i - 1].StartingTrackPosition + 0.99999999 * Elements[i].StartingTrackPosition;
                        follower.UpdateAbsolute((float)p - 1.0f, true, false);
                        follower.UpdateAbsolute((float)p, true, false);
                        Elements[i].CurveRadius = r;
                        Elements[i].WorldPosition = follower.WorldPosition;
                        Elements[i].WorldDirection = follower.WorldDirection;
                        Elements[i].WorldUp = follower.WorldUp;
                        Elements[i].WorldSide = follower.WorldSide;
                        // iterate to shorten track element length
                        p = 0.00000001 * Elements[i].StartingTrackPosition + 0.99999999 * Elements[i + 1].StartingTrackPosition;
                        follower.UpdateAbsolute((float)p - 1.0f, true, false);
                        follower.UpdateAbsolute((float)p, true, false);
                        Vector3 d = Elements[i + 1].WorldPosition - follower.WorldPosition;
                        float bestT = (float)Calc.NormSquared(d);
                        int bestJ = 0;
                        int n = 1000;
                        float a = 1.0f / (float)n * (Elements[i + 1].StartingTrackPosition - Elements[i].StartingTrackPosition);
                        for (int j = 1; j < n - 1; j++)
                        {
                            follower.UpdateAbsolute((float)(Elements[i + 1].StartingTrackPosition - (double)j * a), true, false);
                            d = Elements[i + 1].WorldPosition - follower.WorldPosition;
                            float t = (float)Calc.NormSquared(d);
                            if (t < bestT)
                            {
                                bestT = t;
                                bestJ = j;
                            }
                            else
                            {
                                break;
                            }
                        }

                        float s = (float)bestJ * a;
                        for (int j = i + 1; j < Elements.Length; j++)
                        {
                            Elements[j].StartingTrackPosition -= s;
                        }

                        // introduce turn to compensate for curve
                        p = 0.00000001 * Elements[i].StartingTrackPosition + 0.99999999 * Elements[i + 1].StartingTrackPosition;
                        follower.UpdateAbsolute((float)p - 1.0f, true, false);
                        follower.UpdateAbsolute((float)p, true, false);
                        Vector3 AB = Elements[i + 1].WorldPosition - follower.WorldPosition;
                        Vector3 AC = Elements[i + 1].WorldPosition - Elements[i].WorldPosition;
                        Vector3 BC = follower.WorldPosition - Elements[i].WorldPosition;
                        float sa = Godot.Mathf.Sqrt(BC.x * BC.x + BC.z * BC.z);
                        float sb = Godot.Mathf.Sqrt(AC.x * AC.x + AC.z * AC.z);
                        float sc = Godot.Mathf.Sqrt(AB.x * AB.x + AB.z * AB.z);
                        float denominator = 2.0f * sa * sb;
                        if (denominator != 0.0)
                        {
                            float originalAngle;
                            {
                                float value = (sa * sa + sb * sb - sc * sc) / denominator;
                                if (value < -1.0)
                                {
                                    originalAngle = Godot.Mathf.Pi;
                                }
                                else if (value > 1.0)
                                {
                                    originalAngle = 0;
                                }
                                else
                                {
                                    originalAngle = Godot.Mathf.Acos(value);
                                }
                            }
                            TrackElement originalTrackElement = Elements[i];
                            bestT = float.MaxValue;
                            bestJ = 0;
                            for (int j = -1; j <= 1; j++)
                            {
                                float g = (float)(j * originalAngle);
                                Elements[i] = originalTrackElement;
                                Elements[i].WorldDirection = Elements[i].WorldDirection.Rotated(Vector3.Down, g);
                                Elements[i].WorldUp = Elements[i].WorldUp.Rotated(Vector3.Down, g);
                                Elements[i].WorldSide = Elements[i].WorldSide.Rotated(Vector3.Down, g);
                                p = 0.00000001 * Elements[i].StartingTrackPosition + 0.99999999 * Elements[i + 1].StartingTrackPosition;
                                follower.UpdateAbsolute((float)p - 1.0f, true, false);
                                follower.UpdateAbsolute((float)p, true, false);
                                d = Elements[i + 1].WorldPosition - follower.WorldPosition;
                                float t = (float)Calc.NormSquared(d);
                                if (t < bestT)
                                {
                                    bestT = t;
                                    bestJ = j;
                                }
                            }

                            {
                                float newAngle = (float)(bestJ * originalAngle);
                                Elements[i] = originalTrackElement;
                                Elements[i].WorldDirection = Elements[i].WorldDirection.Rotated(Vector3.Down, newAngle);
                                Elements[i].WorldUp = Elements[i].WorldUp.Rotated(Vector3.Down, newAngle);
                                Elements[i].WorldSide = Elements[i].WorldSide.Rotated(Vector3.Down, newAngle);
                            }

                            // iterate again to further shorten track element length
                            p = 0.00000001 * Elements[i].StartingTrackPosition + 0.99999999 * Elements[i + 1].StartingTrackPosition;
                            follower.UpdateAbsolute((float)p - 1.0f, true, false);
                            follower.UpdateAbsolute((float)p, true, false);
                            d = Elements[i + 1].WorldPosition - follower.WorldPosition;
                            bestT = (float)Calc.NormSquared(d);
                            bestJ = 0;
                            n = 1000;
                            a = 1.0f / (float)n * (Elements[i + 1].StartingTrackPosition - Elements[i].StartingTrackPosition);
                            for (int j = 1; j < n - 1; j++)
                            {
                                follower.UpdateAbsolute((float)(Elements[i + 1].StartingTrackPosition - (double)j * a), true, false);
                                d = Elements[i + 1].WorldPosition - follower.WorldPosition;
                                float t = (float)Calc.NormSquared(d);
                                if (t < bestT)
                                {
                                    bestT = t;
                                    bestJ = j;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            s = (float)bestJ * a;
                            for (int j = i + 1; j < Elements.Length; j++)
                            {
                                Elements[j].StartingTrackPosition -= s;
                            }
                        }

                        // compensate for height difference
                        p = 0.00000001 * Elements[i].StartingTrackPosition + 0.99999999 * Elements[i + 1].StartingTrackPosition;
                        follower.UpdateAbsolute((float)p - 1.0f, true, false);
                        follower.UpdateAbsolute((float)p, true, false);
                        Vector3 d1 = Elements[i + 1].WorldPosition - Elements[i].WorldPosition;
                        double a1 = Godot.Mathf.Atan(d1.y / Godot.Mathf.Sqrt(d1.x * d1.x + d1.z * d1.z));
                        Vector3 d2 = follower.WorldPosition - Elements[i].WorldPosition;
                        double a2 = Godot.Mathf.Atan(d2.y / Godot.Mathf.Sqrt(d2.x * d2.x + d2.z * d2.z));
                        double b = a2 - a1;
                        if (b * b > 0.00000001)
                        {
                            Elements[i].WorldDirection = Elements[i].WorldDirection.Rotated(Elements[i].WorldSide, (float)b);
                            Elements[i].WorldUp = Elements[i].WorldUp.Rotated(Elements[i].WorldSide, (float)b);
                        }
                    }
                }
            }
        }

        // correct events
        /*
        for (int i = 0; i < Elements.Length - 1; i++)
        {
            double startingTrackPosition = Elements[i].StartingTrackPosition;
            double endingTrackPosition = Elements[i + 1].StartingTrackPosition;
            for (int j = 0; j < Elements[i].Events.Length; j++)
            {
                GeneralEvent e = Elements[i].Events[j];
                double p = startingTrackPosition + e.TrackPositionDelta;
                if (p >= endingTrackPosition)
                {
                    int len = Elements[i + 1].Events.Length;
                    Array.Resize(ref Elements[i + 1].Events, len + 1);
                    Elements[i + 1].Events[len] = Elements[i].Events[j];
                    e = Elements[i + 1].Events[len];
                    e.TrackPositionDelta += startingTrackPosition - endingTrackPosition;
                    for (int k = j; k < Elements[i].Events.Length - 1; k++)
                    {
                        Elements[i].Events[k] = Elements[i].Events[k + 1];
                    }

                    len = Elements[i].Events.Length;
                    Array.Resize(ref Elements[i].Events, len - 1);
                    j--;
                }
            }
        }
        */
    }
}
