using System;
using System.IO;
using Godot;


// unified objects
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

// static objects
public class StaticObject : UnifiedObject
{
    private string m_sourceFile;

    public String SourceFile { get { return m_sourceFile; } set { m_sourceFile = value; } }

    private MeshInstance m_meshInstance;

    public MeshInstance ObjectMeshInstance { get { return m_meshInstance; } set {m_meshInstance = value;} }


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

    /// <summary>Creates a clone of this object.</summary>
    public override UnifiedObject Clone()
    {
        StaticObject clone = new StaticObject();

        clone.StartingDistance = this.StartingDistance;
        clone.EndingDistance = this.EndingDistance;
        clone.Dynamic = this.Dynamic;
        clone.m_meshInstance = (MeshInstance)this.m_meshInstance.Duplicate(0);  // TODO Godot.Node.DuplicateFlags.

        for (int i = 0; i < this.m_meshInstance.GetSurfaceMaterialCount(); i++)
        {
            clone.m_meshInstance.SetSurfaceMaterial(i, this.m_meshInstance.GetSurfaceMaterial(i));
        }
        return clone;
    }

    /// <summary>Creates a mirrored clone of this object</summary>
    public override void Mirror()
    {
        // StaticObject mirroredClone = (StaticObject)this.Clone();

        ArrayMesh originalMesh = (ArrayMesh)m_meshInstance.Mesh;

        ArrayMesh newMesh = new ArrayMesh();

        int originalSurfaceCount = originalMesh.GetSurfaceCount();

        for (int i = 0; i < originalSurfaceCount; i++)
        {
            MeshDataTool mdt = new MeshDataTool();
            mdt.CreateFromSurface(originalMesh, i);

            for (int j = 0; j < mdt.GetVertexCount(); j++)
            {
                Vector3 vert = mdt.GetVertex(j);
                vert.x *= -1;
                mdt.SetVertex(j, vert);
            }
            mdt.CommitToSurface(newMesh);
        }

        this.m_meshInstance.Mesh = newMesh;

        // return mirroredClone;
    }
}