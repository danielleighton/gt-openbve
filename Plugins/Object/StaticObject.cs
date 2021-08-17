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

        if (this.m_meshInstance != null)        // TODO : why is this happening sometimes
        {
            clone.m_meshInstance = (MeshInstance)this.m_meshInstance.Duplicate(0);  // TODO Godot.Node.DuplicateFlags.

            for (int i = 0; i < this.m_meshInstance.GetSurfaceMaterialCount(); i++)
            {
                clone.m_meshInstance.SetSurfaceMaterial(i, this.m_meshInstance.GetSurfaceMaterial(i));
            }
        }
        return clone;
    }

    public override void CreateObject(Vector3 position, Transform worldTransformation, Transform localTransformation,
			int SectionIndex, double StartingDistance, double EndingDistance,
			double TrackPosition, double Brightness, bool DuplicateMaterials = false)
    {

        StaticObject instantiatedObject = (StaticObject)this.Clone();

        MeshInstance instantiatedMesh = instantiatedObject.ObjectMeshInstance;

        if (instantiatedMesh != null)       // todo: why is this happening sometimes
        {
            Transform finalTrans = worldTransformation * localTransformation;

            instantiatedMesh.GlobalTransform = new Transform(finalTrans.basis, Vector3.Zero);

            UnifiedObject.RouteRootNode.AddChild(instantiatedMesh);

            instantiatedMesh.GlobalTranslate(position);
        }

        //      return instantiatedObject;

        // Protototype is passed as 'this' from staticobject
        //	currentHost.CreateStaticObject(this, Position, WorldTransformation, LocalTransformation, 0.0, StartingDistance, EndingDistance, TrackPosition, Brightness);




        // Matrix4D Translate = Matrix4D.CreateTranslation(Position.X, Position.Y, -Position.Z);
        // 		Matrix4D Rotate = (Matrix4D)new Transformation(LocalTransformation, WorldTransformation);
        // 		return CreateStaticObject(Prototype, LocalTransformation, Rotate, Translate, AccurateObjectDisposal, AccurateObjectDisposalZOffset, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness);


        // if (Prototype == null)
        // 		{
        // 			return -1;
        // 		}

        // 		if (Prototype.Mesh.Faces.Length == 0)
        // 		{
        // 			//Null object- Waste of time trying to calculate anything for these
        // 			return -1;
        // 		}

        // 		float startingDistance = float.MaxValue;
        // 		float endingDistance = float.MinValue;

        // 		if (AccurateObjectDisposal == ObjectDisposalMode.Accurate)
        // 		{
        // 			foreach (VertexTemplate vertex in Prototype.Mesh.Vertices)
        // 			{
        // 				Vector3 Coordinates = new Vector3(vertex.Coordinates);
        // 				Coordinates.Rotate(LocalTransformation);

        // 				if (Coordinates.Z < startingDistance)
        // 				{
        // 					startingDistance = (float)Coordinates.Z;
        // 				}

        // 				if (Coordinates.Z > endingDistance)
        // 				{
        // 					endingDistance = (float)Coordinates.Z;
        // 				}
        // 			}

        // 			startingDistance += (float)AccurateObjectDisposalZOffset;
        // 			endingDistance += (float)AccurateObjectDisposalZOffset;
        // 		}

        // 		const double minBlockLength = 20.0;

        // 		if (BlockLength < minBlockLength)
        // 		{
        // 			BlockLength *= Math.Ceiling(minBlockLength / BlockLength);
        // 		}

        // 		switch (AccurateObjectDisposal)
        // 		{
        // 			case ObjectDisposalMode.Accurate:
        // 				startingDistance += (float)TrackPosition;
        // 				endingDistance += (float)TrackPosition;
        // 				double z = BlockLength * Math.Floor(TrackPosition / BlockLength);
        // 				StartingDistance = Math.Min(z - BlockLength, startingDistance);
        // 				EndingDistance = Math.Max(z + 2.0 * BlockLength, endingDistance);
        // 				startingDistance = (float)(BlockLength * Math.Floor(StartingDistance / BlockLength));
        // 				endingDistance = (float)(BlockLength * Math.Ceiling(EndingDistance / BlockLength));
        // 				break;
        // 			case ObjectDisposalMode.Legacy:
        // 				startingDistance = (float)StartingDistance;
        // 				endingDistance = (float)EndingDistance;
        // 				break;
        // 			case ObjectDisposalMode.Mechanik:
        // 				startingDistance = (float) StartingDistance;
        // 				endingDistance = (float) EndingDistance + 1500;
        // 				if (startingDistance < 0)
        // 				{
        // 					startingDistance = 0;
        // 				}
        // 				break;
        // 		}
        // 		StaticObjectStates.Add(new ObjectState
        // 		{
        // 			Prototype = Prototype,
        // 			Translation = Translate,
        // 			Rotate = Rotate,
        // 			Brightness = Brightness,
        // 			StartingDistance = startingDistance,
        // 			EndingDistance = endingDistance
        // 		});

        // 		foreach (MeshFace face in Prototype.Mesh.Faces)
        // 		{
        // 			switch (face.Flags & FaceFlags.FaceTypeMask)
        // 			{
        // 				case FaceFlags.Triangles:
        // 					InfoTotalTriangles++;
        // 					break;
        // 				case FaceFlags.TriangleStrip:
        // 					InfoTotalTriangleStrip++;
        // 					break;
        // 				case FaceFlags.Quads:
        // 					InfoTotalQuads++;
        // 					break;
        // 				case FaceFlags.QuadStrip:
        // 					InfoTotalQuadStrip++;
        // 					break;
        // 				case FaceFlags.Polygon:
        // 					InfoTotalPolygon++;
        // 					break;
        // 			}
        // 		}

        // 		return StaticObjectStates.Count - 1;
    }

    public override void OptimizeObject(bool PreserveVerticies, int Threshold, bool VertexCulling)
    {

    }


    public override UnifiedObject Mirror()
    {
        StaticObject clone = (StaticObject) this.Clone();

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

        clone.ObjectMeshInstance.Mesh = newMesh;

        return clone;
    }

    /// <inheritdoc/>
    public override UnifiedObject Transform(double NearDistance, double FarDistance)
    {
        // StaticObject cloneObj = (StaticObject)this.Clone(
        // int n = 0;
        // double x2 = 0.0, x3 = 0.0, x6 = 0.0, x7 = 0.0;
        // for (int i = 0; i < cloneObj.ObjectMeshInstance.Mesh.
        // {
        //     if (n == 2)
        //     {
        //         x2 = cloneObj.Mesh.Vertices[i].Coordinates.
        //     }
        //     else if (n == 3)
        //     {
        //         x3 = cloneObj.Mesh.Vertices[i].Coordinates.
        //     }
        //     else if (n == 6)
        //     {
        //         x6 = cloneObj.Mesh.Vertices[i].Coordinates.
        //     }
        //     else if (n == 7)
        //     {
        //         x7 = cloneObj.Mesh.Vertices[i].Coordinates.
        //     }
        //     n++;
        //     if (n == 8)
        //     {
        //         break;
        //     }
        // }
        // if (n >= 4)
        // {
        //     int m = 0;
        //     for (int i = 0; i < cloneObj.Mesh.Vertices.Length; i+
        //     {
        //         if (m == 0)
        //         {
        //             cloneObj.Mesh.Vertices[i].Coordinates.X = NearDistance - x
        //         }
        //         else if (m == 1)
        //         {
        //             cloneObj.Mesh.Vertices[i].Coordinates.X = FarDistance - x
        //             if (n < 8)
        //             {
        //                 break;
        //             }
        //         }
        //         else if (m == 4)
        //         {
        //             cloneObj.Mesh.Vertices[i].Coordinates.X = NearDistance - x
        //         }
        //         else if (m == 5)
        //         {
        //             cloneObj.Mesh.Vertices[i].Coordinates.X = NearDistance - x
        //             break;
        //         }
        //         m++;
        //         if (m == 8)
        //         {
        //             break;
        //         }
        //     }
        // }
        // return cloneOb
        return null;
    }
}