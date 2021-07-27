using System;
using Godot;

// static objects
public class StaticObject : UnifiedObject
{
    private string m_sourceFile;
    public String SourceFile { get { return m_sourceFile; } set { m_sourceFile = value; } }

    private MeshInstance m_meshInstance;
    public MeshInstance ObjectMeshInstance { get { return m_meshInstance; } set { m_meshInstance = value; } }

    /// <summary>The index to the Renderer.Object array, plus 1. The value of zero represents that the object is not currently shown by the renderer.</summary>
    private int m_rendererIndex;
    public int RendererIndex { get { return m_rendererIndex; } set { m_rendererIndex = value; } }

    /// <summary>The starting track position, for static objects only.</summary>
    private float m_startingDistance;
    public float StartingDistance { get { return m_startingDistance; } set { m_startingDistance = value; } }
    
    /// <summary>The ending track position, for static objects only.</summary>
    private float m_endingDistance { get { return m_endingDistance; } set { m_endingDistance = value; } }
    public float EndingDistance;

    /// <summary>The block mod group, for static objects only.</summary>
    private short GroupIndex;

    /// <summary>Whether the object is dynamic, i.e. not static.</summary>
    private bool m_isDynamic;

    public bool Dynamic { get { return m_isDynamic; } set { m_isDynamic = value; } }

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

    /// <summary>Modifies this object mesh to be mirror image of itself</summary>
    public override void Mirror()
    {
        ArrayMesh originalMesh = (ArrayMesh)this.m_meshInstance.Mesh;
        int originalSurfaceCount = originalMesh.GetSurfaceCount();

        ArrayMesh newMesh = new ArrayMesh();
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
    }
}