using System;
using System.IO;
using Godot;


// unified objects
public abstract class UnifiedObject
{
    internal Node gameObject;
};

// static objects
public class StaticObject : UnifiedObject
{
    internal string SourceFile;

    internal MeshInstance Mesh;

    /// <summary>The index to the Renderer.Object array, plus 1. The value of zero represents that the object is not currently shown by the renderer.</summary>
    internal int RendererIndex;
    /// <summary>The starting track position, for static objects only.</summary>
    internal float StartingDistance;
    /// <summary>The ending track position, for static objects only.</summary>
    internal float EndingDistance;
    /// <summary>The block mod group, for static objects only.</summary>
    internal short GroupIndex;
    /// <summary>Whether the object is dynamic, i.e. not static.</summary>
    public bool Dynamic;
}