using Godot;
using System;

public abstract class SignalData { }

public class Bve4SignalData : SignalData
{
    internal StaticObject BaseObject;
    internal StaticObject GlowObject;
    //internal Textures.Texture[] SignalTextures;
    //internal Textures.Texture[] GlowTextures;
}
public class CompatibilitySignalData : SignalData
{
    internal int[] Numbers;
    internal UnifiedObject[] Objects; // was staticobjects
    internal CompatibilitySignalData(int[] Numbers, UnifiedObject[] Objects)
    {
        this.Numbers = Numbers;
        this.Objects = Objects;
    }
}
public class AnimatedObjectSignalData : SignalData
{
    internal AnimatedObjectCollection Objects;
}