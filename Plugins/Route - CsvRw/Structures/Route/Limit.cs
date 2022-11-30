using Godot;
using System;

public class Limit
{
    /// <summary>The track position at which the limit is placed</summary>
    internal readonly float TrackPosition;
    /// <summary>The speed limit to be enforced</summary>
    /// <remarks>Stored in km/h, has been transformed by UnitOfSpeed if appropriate</remarks>
    internal readonly float Speed;
    /// <summary>The side of the auto-generated speed limit post</summary>
    internal readonly int Direction;
    /// <summary>The cource (little arrow) on the speed limit post denoting a diverging JA limit</summary>
    internal readonly int Cource;

    internal Limit(float trackPosition, float speed, int direction, int cource)
    {
        TrackPosition = trackPosition;
        Speed = speed;
        Direction = direction;
        Cource = cource;
    }

    internal void Create(Vector3 wpos, Transform RailTransformation, float StartingDistance, float EndingDistance, float b, float UnitOfSpeed)
    {
        if (Direction == 0)
        {
            return;
        }
        float dx = 2.2f * Direction;
        float dz = TrackPosition - StartingDistance;

        // wpos += dx * RailTransformation.basis.x + dz * RailTransformation.basis.z;
        RailTransformation = RailTransformation.Scaled(new Vector3(dx, dz, 0f));
        wpos += RailTransformation.basis.x + RailTransformation.basis.y;

        double tpos = TrackPosition;
        if (Speed <= 0.0 | Speed >= 1000.0)
        {
            if (CompatibilityObjects.LimitPostInfinite == null)
            {
                return;
            }
            CompatibilityObjects.LimitPostInfinite.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
        }
        else
        {
            if (Cource < 0)
            {
                if (CompatibilityObjects.LimitPostLeft == null)
                {
                    return;
                }
                CompatibilityObjects.LimitPostLeft.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
            }
            else if (Cource > 0)
            {
                if (CompatibilityObjects.LimitPostRight == null)
                {
                    return;
                }
                CompatibilityObjects.LimitPostRight.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
            }
            else
            {
                if (CompatibilityObjects.LimitPostStraight == null)
                {
                    return;
                }
                CompatibilityObjects.LimitPostStraight.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
            }

            double lim = Speed / UnitOfSpeed;
            if (lim < 10.0)
            {
                if (CompatibilityObjects.LimitOneDigit == null)
                {
                    return;
                }
                if (CompatibilityObjects.LimitOneDigit is StaticObject)
                {
                    int d0 = (int)Math.Round(lim);
                    StaticObject o = (StaticObject)CompatibilityObjects.LimitOneDigit.Clone();
                    // if (o.Mesh.Materials.Length >= 1)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
                    // }

                    o.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
                }
                else
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitOneDigit, where only static objects are allowed.");
                }

            }
            else if (lim < 100.0)
            {
                if (CompatibilityObjects.LimitTwoDigits == null)
                {
                    return;
                }
                if (CompatibilityObjects.LimitTwoDigits is StaticObject)
                {
                    int d1 = (int)Math.Round(lim);
                    int d0 = d1 % 10;
                    d1 /= 10;
                    StaticObject o = (StaticObject)CompatibilityObjects.LimitTwoDigits.Clone();
                    // if (o.Mesh.Materials.Length >= 1)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d1 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
                    // }

                    // if (o.Mesh.Materials.Length >= 2)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[1].DaytimeTexture);
                    // }

                    o.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
                }
                else
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitTwoDigits, where only static objects are allowed.");
                }
            }
            else
            {
                if (CompatibilityObjects.LimitThreeDigits == null)
                {
                    return;
                }
                if (CompatibilityObjects.LimitThreeDigits is StaticObject)
                {
                    int d2 = (int)Math.Round(lim);
                    int d0 = d2 % 10;
                    int d1 = (d2 / 10) % 10;
                    d2 /= 100;
                    StaticObject o = (StaticObject)CompatibilityObjects.LimitThreeDigits.Clone();
                    // if (o.Mesh.Materials.Length >= 1)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d2 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[0].DaytimeTexture);
                    // }

                    // if (o.Mesh.Materials.Length >= 2)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d1 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[1].DaytimeTexture);
                    // }

                    // if (o.Mesh.Materials.Length >= 3)
                    // {
                    //     Plugin.CurrentHost.RegisterTexture(OpenBveApi.Path.CombineFile(CompatibilityObjects.LimitGraphicsPath, "limit_" + d0 + ".png"), new TextureParameters(null, null), out o.Mesh.Materials[2].DaytimeTexture);
                    // }
                    o.CreateObject(wpos, RailTransformation, Transform.Identity, -1, StartingDistance, EndingDistance, tpos, b);
                }
                else
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Attempted to use an animated object for LimitThreeDigits, where only static objects are allowed.");
                }
            }

        }
    }

    internal void CreateEvent(double StartingDistance, ref double CurrentSpeedLimit, ref TrackElement Element)
    {
        // int m = Element.Events.Length;
        // Array.Resize(ref Element.Events, m + 1);
        // double d = TrackPosition - StartingDistance;
        // Element.Events[m] = new LimitChangeEvent(d, CurrentSpeedLimit, Speed);
        // CurrentSpeedLimit = Speed;
    }
}