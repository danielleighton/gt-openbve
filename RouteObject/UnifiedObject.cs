using System;
using System.IO;
using Godot;

public abstract class UnifiedObject
{
    internal Node gameObject;

    /// <summary>Creates a clone of this object</summary>
    /// <returns>The cloned object</returns>
    public abstract UnifiedObject Clone();

    /// <summary>Creates a mirrored clone of this object</summary>
    /// <returns>The mirrored clone</returns>
    public abstract void Mirror();
};
