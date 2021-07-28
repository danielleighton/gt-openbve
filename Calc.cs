using System;
using Godot;


/// <summary>
/// Various calculation helper methods 
/// </summary>

public static class Calc
{
    #region Cross Product 


    ///
    public static void Cross(double ax, double ay, double az, double bx, double by, double bz, out double cx, out double cy, out double cz)
    {
        cx = ay * bz - az * by;
        cy = az * bx - ax * bz;
        cz = ax * by - ay * bx;
    }

    public static void Cross(float ax, float ay, float az, float bx, float by, float bz, out float cx, out float cy, out float cz)
    {
        cx = ay * bz - az * by;
        cy = az * bx - ax * bz;
        cz = ax * by - ay * bx;
    }

    #endregion

    #region Rotation 

    public static void Rotate(ref Vector2 vector, double cosa, double sina)
    {
        double u = vector.x * cosa - vector.y * sina;
        double v = vector.x * sina + vector.y * cosa;
        vector.x = (float)u;
        vector.y = (float)v;
    }

    public static void Rotate(ref Vector2 vector, float cosa, float sina)
    {
        float u = vector.x * cosa - vector.y * sina;
        float v = vector.x * sina + vector.y * cosa;
        vector.x = u;
        vector.y = v;
    }

    // public static void Rotate(ref float px, ref float py, ref float pz, Transformation t)
    // {
    //     float x, y, z;
    //     x = t.X.x * px + t.Y.x * py + t.Z.x * pz;
    //     y = t.X.y * px + t.Y.y * py + t.Z.y * pz;
    //     z = t.X.z * px + t.Y.z * py + t.Z.z * pz;
    //     px = x; py = y; pz = z;
    // }

    public static void Rotate(ref double px, ref double py, ref double pz, double dx, double dy, double dz, double cosa, double sina)
    {
        double t = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
        dx *= t; dy *= t; dz *= t;
        double oc = 1.0 - cosa;
        double x = (cosa + oc * dx * dx) * px + (oc * dx * dy - sina * dz) * py + (oc * dx * dz + sina * dy) * pz;
        double y = (cosa + oc * dy * dy) * py + (oc * dx * dy + sina * dz) * px + (oc * dy * dz - sina * dx) * pz;
        double z = (cosa + oc * dz * dz) * pz + (oc * dx * dz - sina * dy) * px + (oc * dy * dz + sina * dx) * py;
        px = x; py = y; pz = z;
    }

    //public static void Rotate(ref float px, ref float py, ref float pz, double dx, double dy, double dz, double cosa, double sina)
    //{
    //    double t = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
    //    dx *= t; dy *= t; dz *= t;
    //    double oc = 1.0 - cosa;
    //    double x = (cosa + oc * dx * dx) * px + (oc * dx * dy - sina * dz) * py + (oc * dx * dz + sina * dy) * pz;
    //    double y = (cosa + oc * dy * dy) * py + (oc * dx * dy + sina * dz) * px + (oc * dy * dz - sina * dx) * pz;
    //    double z = (cosa + oc * dz * dz) * pz + (oc * dx * dz - sina * dy) * px + (oc * dy * dz + sina * dx) * py;
    //    px = (float)x; py = (float)y; pz = (float)z;
    //}

    public static void Rotate(ref float px, ref float py, ref float pz, double dx, double dy, double dz, double cosa, double sina)
    {
        float t = 1.0f / (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        dx *= t; dy *= t; dz *= t;
        float oc = (float)( 1.0f - cosa );
        float x = (float)( (cosa + oc * dx * dx) * px + (oc * dx * dy - sina * dz) * py + (oc * dx * dz + sina * dy) * pz);
        float y = (float)( (cosa + oc * dy * dy) * py + (oc * dx * dy + sina * dz) * px + (oc * dy * dz - sina * dx) * pz);
        float z = (float)( (cosa + oc * dz * dz) * pz + (oc * dx * dz - sina * dy) * px + (oc * dy * dz + sina * dx) * py);
        px = x; py = y; pz = z;
    }


    public static void Rotate(ref float px, ref float py, ref float pz, float dx, float dy, float dz, float cosa, float sina)
    {
        float t = (float)( 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz));
        dx *= t; dy *= t; dz *= t;
        float oc = 1.0f - cosa;
        float x = (cosa + oc * dx * dx) * px + (oc * dx * dy - sina * dz) * py + (oc * dx * dz + sina * dy) * pz;
        float y = (cosa + oc * dy * dy) * py + (oc * dx * dy + sina * dz) * px + (oc * dy * dz - sina * dx) * pz;
        float z = (cosa + oc * dz * dz) * pz + (oc * dx * dz - sina * dy) * px + (oc * dy * dz + sina * dx) * py;
        px = x; py = y; pz = z;
    }

    public static void Rotate(ref float px, ref float py, ref float pz, float dx, float dy, float dz, double cosa, double sina)
    {
        float t = (float)(1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz));
        dx *= t; dy *= t; dz *= t;
        float oc = 1.0f - (float)cosa;
        float x = ((float)cosa + oc * dx * dx) * px + (oc * dx * dy - (float)sina * dz) * py + (oc * dx * dz + (float)sina * dy) * pz;
        float y = ((float)cosa + oc * dy * dy) * py + (oc * dx * dy + (float)sina * dz) * px + (oc * dy * dz - (float)sina * dx) * pz;
        float z = ((float)cosa + oc * dz * dz) * pz + (oc * dx * dz - (float)sina * dy) * px + (oc * dy * dz + (float)sina * dx) * py;
        px = x; py = y; pz = z;
    }


    #endregion

    #region Normalization 


    public static void Normalize(ref float x, ref float y)
    {
        float t = x * x + y * y;
        if (t != 0.0)
        {
            t = (float)(1.0f / Math.Sqrt(t));
            x *= t;
            y *= t;
        }
    }


    public static void Normalize(ref double x, ref double y, ref double z)
    {
        double t = x * x + y * y + z * z;
        if (t != 0.0)
        {
            t = 1.0 / Math.Sqrt(t);
            x *= t;
            y *= t;
            z *= t;
        }
    }


    public static void Normalize(ref float x, ref float y, ref float z)
    {
        float t = x * x + y * y + z * z;
        if (t != 0.0)
        {
            t =  1.0f / (float)Math.Sqrt(t);
            x *= t;
            y *= t;
            z *= t;
        }
    }


    // normalize
    public static void Normalize(ref double x, ref double y)
    {
        double t = x * x + y * y;
        if (t != 0.0)
        {
            t = 1.0 / Math.Sqrt(t);
            x *= t;
            y *= t;
        }
    }

    /// <summary>Returns a Calc.Normalized vector based on a 2D vector in the XZ plane and an additional Y-coordinate.</summary>
    /// <param name="Vector">The vector in the XZ-plane. The X and Y components in Vector represent the X- and Z-coordinates, respectively.</param>
    /// <param name="Y">The Y-coordinate.</param>
    public static Vector3 GetNormalizedVector3(Vector2 vector, double y)
    {
        double t = 1.0 / System.Math.Sqrt(vector.x * vector.x + vector.y * vector.y + y * y);
        return new Vector3((float)t * vector.x, (float)(t * y), (float)t * vector.y);
    }

  

    #endregion

    #region Old methods moved from Transformation.cs

    //public static void Rotate(ref float px, ref float py, ref float pz, double dx, double dy, double dz, double cosa, double sina)
    //{
    //    double t = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
    //    dx *= t; dy *= t; dz *= t;
    //    double oc = 1.0 - cosa;
    //    double x = (cosa + oc * dx * dx) * (double)px + (oc * dx * dy - sina * dz) * (double)py + (oc * dx * dz + sina * dy) * (double)pz;
    //    double y = (cosa + oc * dy * dy) * (double)py + (oc * dx * dy + sina * dz) * (double)px + (oc * dy * dz - sina * dx) * (double)pz;
    //    double z = (cosa + oc * dz * dz) * (double)pz + (oc * dx * dz - sina * dy) * (double)px + (oc * dy * dz + sina * dx) * (double)py;
    //    px = (float)x; py = (float)y; pz = (float)z;
    //}

    //public static void Rotate(ref double px, ref double py, ref double pz, double dx, double dy, double dz, double ux, double uy, double uz, double sx, double sy, double sz)
    //{
    //    double x, y, z;
    //    x = sx * px + ux * py + dx * pz;
    //    y = sy * px + uy * py + dy * pz;
    //    z = sz * px + uz * py + dz * pz;
    //    px = x; py = y; pz = z;
    //}
    //public static void Rotate(ref float px, ref float py, ref float pz, Transformation t)
    //{
    //    double x, y, z;
    //    x = t.X.x * (double)px + t.Y.x * (double)py + t.Z.x * (double)pz;
    //    y = t.X.y * (double)px + t.Y.y * (double)py + t.Z.y * (double)pz;
    //    z = t.X.z * (double)px + t.Y.z * (double)py + t.Z.z * (double)pz;
    //    px = (float)x; py = (float)y; pz = (float)z;
    //}
    //public static void Rotate(ref double px, ref double py, ref double pz, Transformation t)
    //{
    //    double x, y, z;
    //    x = t.X.x * px + t.Y.x * py + t.Z.x * pz;
    //    y = t.X.y * px + t.Y.y * py + t.Z.y * pz;
    //    z = t.X.z * px + t.Y.z * py + t.Z.z * pz;
    //    px = x; py = y; pz = z;
    //}


    #endregion
}
