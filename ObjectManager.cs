using System.IO;
using System.Collections.Generic;
using System.Text;
using Godot;

internal class ObjectManager
{
    #region Singleton

    private static readonly ObjectManager instance = new ObjectManager();

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static ObjectManager()
    {
    }

    private ObjectManager()
    {
    }

    public static ObjectManager Instance
    {
        get
        {
            return instance;
        }
    }

    #endregion

    private List<StaticObject> staticObjects;

    /// <summary>
    /// Try to load the given filename with given extension
    /// </summary>
    /// <param name="fileName">Base file name - value is modified to the loaded path if successful</param>
    /// <param name="extension">Extension to attempt, including the period (e.g. .csv)</param>
    /// <returns>True if successfully loaded using the given extension</returns>
    private bool TryAddedExtensionLoad(ref string fileName, string extension)
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

    public UnifiedObject LoadObject(Node parent, string fileName, Encoding fileEncoding, bool preserveVertices, bool forceTextureRepeatX, bool forceTextureRepeatY)
    {
        return LoadStaticObject(parent, fileName, fileEncoding, preserveVertices, forceTextureRepeatX, forceTextureRepeatY);
    }

    //UnifiedObject Prototype, Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double BlockLength, double TrackPosition) {
    public UnifiedObject InstantiateObject(Node parent, UnifiedObject template, Vector3 position, Transform baseTransformation, Transform auxTransformation, bool accurateObjectDisposal, double startingDistance, double endingDistance, double blockLength, double trackPosition)
    {
        MeshInstance instantiatedObject = (MeshInstance)((MeshInstance)(((StaticObject)template).Mesh)).Duplicate((int)Node.DuplicateFlags.UseInstancing);
  
        Transform finalTrans = baseTransformation * auxTransformation;

        instantiatedObject.GlobalTransform = new Transform(finalTrans.basis, position);
        //instantiatedObject.Transform = new Transform(finalTrans.basis, position);
        
        parent.AddChild(instantiatedObject);
        
        // GameObject instantiatedObject = (GameObject)GameObject.Instantiate(template.gameObject, position, Quaternion.identity);
        // instantiatedObject.SetActive(true);

        // if (parent != null) instantiatedObject.transform.parent = parent.transform;

        // Mesh mesh = instantiatedObject.GetComponent<MeshFilter>().mesh;
        // Vector3[] vertices = mesh.vertices;

        // for (int i = 0; i < vertices.Length; i++)
        // {
        //     Calc.Rotate(ref vertices[i].x, ref vertices[i].y, ref vertices[i].z, auxTransformation);
        //     Calc.Rotate(ref vertices[i].x, ref vertices[i].y, ref vertices[i].z, baseTransformation);
        // }
        // mesh.vertices = vertices;



        // ------


        //Object.Mesh.Vertices = new World.Vertex[Prototype.Mesh.Vertices.Length];
        //for (int j = 0; j < Prototype.Mesh.Vertices.Length; j++)
        //{
        //    Object.Mesh.Vertices[j] = Prototype.Mesh.Vertices[j];
        //    if (AccurateObjectDisposal)
        //    {
        //        World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, AuxTransformation);
        //        if (Object.Mesh.Vertices[j].Coordinates.Z < Object.StartingDistance)
        //        {
        //            Object.StartingDistance = (float)Object.Mesh.Vertices[j].Coordinates.Z;
        //        }
        //        if (Object.Mesh.Vertices[j].Coordinates.Z > Object.EndingDistance)
        //        {
        //            Object.EndingDistance = (float)Object.Mesh.Vertices[j].Coordinates.Z;
        //        }
        //        Object.Mesh.Vertices[j].Coordinates = Prototype.Mesh.Vertices[j].Coordinates;
        //    }
        //    World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, AuxTransformation);
        //    World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, BaseTransformation);
        //    Object.Mesh.Vertices[j].Coordinates.X += Position.X;
        //    Object.Mesh.Vertices[j].Coordinates.Y += Position.Y;
        //    Object.Mesh.Vertices[j].Coordinates.Z += Position.Z;
        //}


        //instantiatedObject.transform.eulerAngles = auxTransformation + baseTransformation;
        //if (auxTransformation != Vector3.zero)
        //    instantiatedObject.transform.eulerAngles = auxTransformation;

        //World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, auxTransformation);
        //World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, BaseTransformation);

        UnifiedObject retObject = null;
        if (template is StaticObject)
        {
            //StaticObject s = (StaticObject)template;
            //CreateStaticObject(s, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness, DuplicateMaterials);
            retObject = new StaticObject();
            retObject.gameObject = instantiatedObject;
       
            
        }
        else if (template is AnimatedObjectCollection)
        {
            //AnimatedObjectCollection a = (AnimatedObjectCollection)template;
            //CreateAnimatedWorldObjects(a.Objects, Position, BaseTransformation, AuxTransformation, SectionIndex, AccurateObjectDisposal, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness, DuplicateMaterials);
            //retObject = new AnimatedObjectCollection();
            //retObject.gameObject = instantiatedObject;
        }

        return retObject;

    }

    public StaticObject LoadStaticObject(Node parent, string fileName, Encoding fileEncoding, bool preserveVertices, bool forceTextureRepeatX, bool forceTextureRepeatY) 
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
                loadedObject.Mesh = CsvB3dObjectParser.LoadFromFile(fileName, fileEncoding, forceTextureRepeatX, forceTextureRepeatY);

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

    internal StaticObject CloneObject(StaticObject prototype)
    {
        return prototype == null ? null : CloneObject(prototype, null, null);
    }


    /// <summary>Creates a clone of the specified object.</summary>
    /// <param name="Prototype">The prototype.</param>
    /// <param name="daytimeTexture">The replacement daytime texture, or a null reference to keep the texture of the prototype.</param>
    /// <param name="nighttimeTexture">The replacement nighttime texture, or a null reference to keep the texture of the prototype.</param>
    /// <returns></returns>
    internal static StaticObject CloneObject(StaticObject Prototype, ImageTexture daytimeTexture, ImageTexture nighttimeTexture)
    {
        if (Prototype == null) return null;
        StaticObject result = new StaticObject();
        result.StartingDistance = Prototype.StartingDistance;
        result.EndingDistance = Prototype.EndingDistance;
        result.Dynamic = Prototype.Dynamic;
        
        //Result.gameObject = GameObject.Instantiate(Prototype.gameObject);

        return result;
    }


}
