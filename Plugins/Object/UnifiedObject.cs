using System;
using System.Text;
using Godot;

/// <summary>A unified object is the abstract class encompassing all object types within the sim</summary>

public abstract class UnifiedObject
{
    private static Node m_routeRootNode;
    public static Node RouteRootNode {get { return m_routeRootNode; } set { m_routeRootNode = value; } }

    /// <summary>Creates the object within the worldspace without using track based transforms</summary>
    /// <param name="Position">The world position</param>
    /// <param name="StartingDistance">The track distance at which this is displayed by the renderer</param>
    /// <param name="EndingDistance">The track distance at which this hidden by the renderer</param>
    /// <param name="TrackPosition">The absolute track position at which this object is placed</param>
    public void CreateObject(Vector3 Position, double StartingDistance, double EndingDistance, double TrackPosition)
    {
        CreateObject(Position, Godot.Transform.Identity, Godot.Transform.Identity, -1, StartingDistance, EndingDistance, TrackPosition, 1.0);
    }

    /// <summary>Creates the object within the worldspace using a single track based transforms</summary>
    /// <param name="Position">The world position</param>
    /// <param name="WorldTransformation">The world transformation to apply (e.g. ground, rail)</param>
    /// <param name="StartingDistance">The track distance at which this is displayed by the renderer</param>
    /// <param name="EndingDistance">The track distance at which this hidden by the renderer</param>
    /// <param name="TrackPosition">The absolute track position at which this object is placed</param>
    public void CreateObject(Vector3 Position, Godot.Transform WorldTransformation, double StartingDistance, double EndingDistance, double TrackPosition)
    {
        CreateObject(Position, WorldTransformation, Godot.Transform.Identity, -1, StartingDistance, EndingDistance, TrackPosition, 1.0);
    }

    /// <summary>Creates the object within the world</summary>
    /// <param name="Position">The world position</param>
    /// <param name="WorldTransformation">The world transformation to apply (e.g. ground, rail)</param>
    /// <param name="LocalTransformation">The local transformation to apply in order to rotate the model</param>
    /// <param name="StartingDistance">The track distance at which this is displayed by the renderer</param>
    /// <param name="EndingDistance">The track distance at which this hidden by the renderer</param>
    /// <param name="TrackPosition">The absolute track position at which this object is placed</param>
    public void CreateObject(Vector3 Position, Godot.Transform WorldTransformation, Godot.Transform LocalTransformation, double StartingDistance, double EndingDistance, double TrackPosition)
    {
        CreateObject(Position, WorldTransformation, LocalTransformation, -1, StartingDistance, EndingDistance, TrackPosition, 1.0);
    }

    /// <summary>Creates the object within the world</summary>
    /// <param name="Position">The world position</param>
    /// <param name="WorldTransformation">The world transformation to apply (e.g. ground, rail)</param>
    /// <param name="LocalTransformation">The local transformation to apply in order to rotate the model</param>
    /// <param name="SectionIndex">The section index (If placed via Track.SigF)</param>
    /// <param name="StartingDistance">The track distance at which this is displayed by the renderer</param>
    /// <param name="EndingDistance">The track distance at which this hidden by the renderer</param>
    /// <param name="TrackPosition">The absolute track position at which this object is placed</param>
    /// <param name="Brightness">The brightness value of this object</param>
    /// <param name="DuplicateMaterials">Whether the materials are to be duplicated (Not set when creating BVE4 signals)</param>
    public abstract void CreateObject(Vector3 Position, Godot.Transform WorldTransformation, Godot.Transform LocalTransformation, int SectionIndex, double StartingDistance, double EndingDistance, double TrackPosition, double Brightness, bool DuplicateMaterials = false);

    /// <summary>Call this method to optimize the object</summary>
    /// <param name="PreserveVerticies">Whether duplicate verticies are to be preserved (Takes less time)</param>
    /// <param name="Threshold">The face size threshold for optimization</param>
    /// <param name="VertexCulling">Whether vertex culling is performed</param>
    public abstract void OptimizeObject(bool PreserveVerticies, int Threshold, bool VertexCulling);

    /// <summary>Creates a clone of this object</summary>
    /// <returns>The cloned object</returns>
    public abstract UnifiedObject Clone();

    /// <summary>Creates a mirrored clone of this object</summary>
    /// <returns>The mirrored clone</returns>
    public abstract UnifiedObject Mirror();

    /// <summary>Creates a transformed clone of this object</summary>
    /// <param name="NearDistance">The object's width at the start of the block</param>
    /// <param name="FarDistance">The object's width at the end of the block</param>
    /// <returns>The transformed clone</returns>
    public abstract UnifiedObject Transform(double NearDistance, double FarDistance);

    
    public static UnifiedObject LoadObject(Node parent, string fileName, Encoding fileEncoding, bool preserveVertices, bool forceTextureRepeatX, bool forceTextureRepeatY)
    {
        return LoadStaticObject(parent, fileName, fileEncoding, preserveVertices, forceTextureRepeatX, forceTextureRepeatY);
    }

    public static UnifiedObject InstantiateObject(Node parent, UnifiedObject templateObject, Vector3 position, Transform baseTransformation, Transform auxTransformation, bool accurateObjectDisposal, double startingDistance, double endingDistance, double blockLength, double trackPosition)
    {
        StaticObject instantiatedObject = (StaticObject)templateObject.Clone();

        MeshInstance instantiatedMesh = instantiatedObject.ObjectMeshInstance;

        Transform finalTrans = baseTransformation * auxTransformation;

        instantiatedMesh.GlobalTransform = new Transform(finalTrans.basis, Vector3.Zero);

        parent.AddChild(instantiatedMesh);

        instantiatedMesh.GlobalTranslate(position);

        return instantiatedObject;

        // UnifiedObject retObject = null;
        // if (templateObject is StaticObject)
        // {
        //     retObject = new StaticObject();
        //     retObject.gameObject = instantiatedMesh;
        // }
        // else if (templateObject is AnimatedObjectCollection)
        // {
        // }

        // return retObject;
    }

    public static int CreateStaticObject(StaticObject Prototype, Vector3 Position, Transform WorldTransformation, Transform LocalTransformation, float AccurateObjectDisposalZOffset, float StartingDistance, float EndingDistance, float TrackPosition, double Brightness)
    {
        // return CreateStaticObject(Prototype, Position, WorldTransformation, LocalTransformation, ObjectDisposalMode.Accurate, AccurateObjectDisposalZOffset, StartingDistance, EndingDistance, 25.0, TrackPosition, Brightness);
        return 0;
    }
    
    public static StaticObject LoadStaticObject(Node parent, string fileName, Encoding fileEncoding, bool preserveVertices, bool forceTextureRepeatX, bool forceTextureRepeatY) 
    {

        if (!System.IO.Path.HasExtension(fileName))
        {
            // Try to add extension
            while (true)
            {
                if (TryAddedExtensionLoad(ref fileName, ".x")) break;
                if (TryAddedExtensionLoad(ref fileName, ".csv")) break;
                if (TryAddedExtensionLoad(ref fileName, ".b3d")) break;
                if (TryAddedExtensionLoad(ref fileName, ".x")) break;
                break;
            }
        }

        StaticObject loadedObject = new StaticObject();
        loadedObject.SourceFile = fileName;
        
        switch (System.IO.Path.GetExtension(fileName).ToLowerInvariant())
        {
            case ".csv":
            case ".b3d":
                loadedObject.ObjectMeshInstance = CsvB3dObjectParser.LoadFromFile(fileName, fileEncoding, forceTextureRepeatX, forceTextureRepeatY);

                break;
            case ".x":
                //Result = XObjectParser.ReadObject(FileName, Encoding, LoadMode, ForceTextureRepeatX, ForceTextureRepeatY);
                //Debug.LogWarningFormat(".x files not yet supported {0}", fileName);
                break;
            case ".animated":
                //Debug.LogErrorFormat("Tried to load an animated object even though only static objects are allowed: {0}", fileName);
                return null;
            default:
                //Debug.LogErrorFormat("The file extension '{0}' is not supported: {1}", Path.GetExtension(fileName), fileName);
                return null;
        }

        // if (loadedObject != null)
        //     loadedObject.gameObject.SetActive(false);

        //To disable only the renderer
        //MeshRenderer render = loadedObject.GetComponentInChildren<MeshRenderer>();
        //render.enabled = false;
        return loadedObject;
    }



    /// <summary>
    /// Try to load the given filename with given extension
    /// </summary>
    /// <param name="fileName">Base file name - value is modified to the loaded path if successful</param>
    /// <param name="extension">Extension to attempt, including the period (e.g. .csv)</param>
    /// <returns>True if successfully loaded using the given extension</returns>
    private static bool TryAddedExtensionLoad(ref string fileName, string extension)
    {
        string pathWithExtn = System.IO.Path.ChangeExtension(fileName, extension);

        if (System.IO.File.Exists(pathWithExtn))
        {
            fileName = pathWithExtn;
            return true;
        }
        else
        {
            return false;
        }
    }

}
