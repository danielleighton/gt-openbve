using Godot;
using System;

public class Pole
{
    /// <summary>Whether the pole exists in the current block</summary>
    public bool Exists;

    /// <summary>The pole mode</summary>
    public int Mode;

    /// <summary>The location within the block of the .Pole command</summary>
    public float Location;

    /// <summary>The repetition interval</summary>
    public float Interval;

    /// <summary>The structure type</summary>
    public int Type;

	public Pole ()
	{

	}
	
    public Pole(Pole p)
    {
        Exists = p.Exists;
        Mode = p.Mode;
        Location = p.Location;
        Interval = p.Interval;
        Type = p.Type;
    }

    public void Create(PoleDictionary poles, Vector3 worldPos, Transform railTransform, Vector2 direction, float planar, float updown, float startingDistance, float endingDistance)
    {
        if (!Exists)
        {
            return;
        }

        float dz = startingDistance / Interval;
        dz -= Godot.Mathf.Floor(dz + 0.5f);
        if (dz >= -0.01 & dz <= 0.01)
        {
            if (Mode == 0)
            {
                if (Location <= 0.0)
                {
                    poles[0][Type].CreateObject(worldPos, railTransform, startingDistance, endingDistance, startingDistance);
                }
                else
                {
                    UnifiedObject Pole = poles[0][Type].Mirror();
                    Pole.CreateObject(worldPos, railTransform, startingDistance, endingDistance, startingDistance);
                }
            }
            else
            {
                int m = Mode;
                float dx = -Location * 3.8f;
                float wa = Godot.Mathf.Atan2(direction.y, direction.x) - planar;
                Vector3 w = new Vector3(Godot.Mathf.Cos(wa), Godot.Mathf.Tan(updown), Godot.Mathf.Sin(wa));
                w = w.Normalized();
                float sx = direction.y;
                float sy = 0.0f;
                float sz = -direction.x;
                Vector3 wpos = worldPos + new Vector3(sx * dx + w.x * dz, sy * dx + w.y * dz, sz * dx + w.z * dz);
                int type = Type;
                poles[m][type].CreateObject(wpos, railTransform, startingDistance, endingDistance, startingDistance);
            }
        }
    }
}