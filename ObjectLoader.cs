using Godot;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;


public class ObjectLoader : Node
{

    #region Helper Classes

    private class MeshFaceVertex
    {
        /// <summary>A reference to an element in the Vertex array of the containing Mesh structure</summary>
        /// <remarks>Note the actual coordinates of the vertex are stored with the Mesh - NOT within this struct</remarks>
        public ushort indexToMeshVertices;

        /// <summary>The normal to be used at the vertex</summary>
        public Vector3 normal;

        public MeshFaceVertex(int index)
        {
            this.indexToMeshVertices = (ushort)index;
            this.normal = new Vector3(0.0f, 0.0f, 0.0f);
        }

        public MeshFaceVertex(int index, Vector3 normal)
        {
            this.indexToMeshVertices = (ushort)index;
            this.normal = normal;
        }

        // operators
        public static bool operator ==(MeshFaceVertex a, MeshFaceVertex b)
        {
            if (a.indexToMeshVertices != b.indexToMeshVertices) return false;
            if (a.normal.x != b.normal.x) return false;
            if (a.normal.y != b.normal.y) return false;
            if (a.normal.z != b.normal.z) return false;
            return true;
        }
        public static bool operator !=(MeshFaceVertex a, MeshFaceVertex b)
        {
            if (a.indexToMeshVertices != b.indexToMeshVertices) return true;
            if (a.normal.x != b.normal.z) return true;
            if (a.normal.y != b.normal.y) return true;
            if (a.normal.z != b.normal.z) return true;
            return false;
        }
    }

    /// <summary>
    /// The face of an object mesh - i.e. list of vertices it is made up of, and information about materials
    /// </summary>
    private class MeshFace
    {
        #region Constants

        public static readonly int FACE_TYPE_MASK = 7;
        public static readonly int FACE_TYPE_POLYGON = 0;
        public static readonly int FACE_TYPE_TRIANGLES = 1;
        public static readonly int FACE_TYPE_TRIANGLE_STRIP = 2;
        public static readonly int FACE_TYPE_QUADS = 3;
        public static readonly int FACE_TYPE_QUAD_STRIP = 4;
        public static readonly int FACE_2_MASK = 8;

        #endregion

        /// <summary>
        /// List of vertices, made up of vertex information (reference only) and normal
        /// - note these only reference actual coordinates from parent Mesh coordinates
        /// </summary>
        public List<MeshFaceVertex> verticeIndexes;

        /// <summary>
        /// A reference to an element in the Material array of the containing Mesh structure.
        /// - note this is a reference to the actual material from parent material list
        /// </summary>
        //public int materialIndex;

        /// <summary>A bit mask combining constants of the MeshFace structure.</summary>
        public byte flags;

        public MeshFace()
        {
            this.verticeIndexes = new List<MeshFaceVertex>();
            // materialIndex = 0;
        }

        public MeshFace(MeshFaceVertex[] vertices) :
            this()
        {

            this.verticeIndexes.AddRange(vertices);

            // this.materialIndex = 0;
            this.flags = 0;
        }
        internal void Flip()
        {
            if ((this.flags & FACE_TYPE_MASK) == FACE_TYPE_QUAD_STRIP)
            {
                for (int i = 0; i < this.verticeIndexes.Count; i += 2)
                {
                    MeshFaceVertex x = this.verticeIndexes[i];
                    this.verticeIndexes[i] = this.verticeIndexes[i + 1];
                    this.verticeIndexes[i + 1] = x;
                }
            }
            else
            {
                int n = this.verticeIndexes.Count;
                for (int i = 0; i < (n >> 1); i++)
                {
                    MeshFaceVertex x = this.verticeIndexes[i];
                    this.verticeIndexes[i] = this.verticeIndexes[n - i - 1];
                    this.verticeIndexes[n - i - 1] = x;
                }
            }
        }

    }

    /// <summary>
    /// Material associated with a mesh (texture, color, etc)
    /// </summary>
    private class MeshMaterial
    {
        public Color color;

        public Color emissiveColor;
        public bool emissiveColorUsed;

        public Color transparentColor;
        public bool transparentColorUsed;

        //public World.MeshMaterialBlendMode BlendMode;
        public ushort glowAttenuationData;

        public string dayTexture;
        public string nightTexture;

        public MeshMaterial()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source"></param>
        public MeshMaterial(MeshMaterial source)
        {
            color = source.color;
            emissiveColor = source.emissiveColor;
            emissiveColorUsed = source.emissiveColorUsed;
            transparentColor = source.transparentColor;
            transparentColorUsed = source.transparentColorUsed;
            glowAttenuationData = source.glowAttenuationData;
            dayTexture = source.dayTexture;
            nightTexture = source.nightTexture;
        }
    }


    /// <summary>
    /// Collates and builds information about a mesh/submesh 
    /// </summary>
    /// <remarks>
    /// Typically corresponds to a createmeshbuilder or [meshbuilder] command in the object file
    /// </remarks>
    private class MeshBuilder
    {
        public List<MeshFace> faces;    // faces will reference the verticies list below, by index
        public List<Vector3> vertices;  // actual vertices (coordinates thereof)
        public List<Vector2> uvs;
        public MeshMaterial material;

        public MeshBuilder()
        {
            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            faces = new List<MeshFace>();
            //material = new List<MeshMaterial>();
        }




        private static SpatialMaterial[] make_materials(int n)
        {
            List<SpatialMaterial> matlist = new List<SpatialMaterial>();
            RandomNumberGenerator rng = new RandomNumberGenerator();
            rng.Randomize();
            for (int i = 0; i < n; i++)
            {
                SpatialMaterial m = new SpatialMaterial();
                Color c = new Color(rng.RandfRange(0.0f, 1.0f), rng.RandfRange(0.0f, 1.0f), rng.RandfRange(0.0f, 1.0f));
                m.AlbedoColor = c;
                matlist.Add(m);
            }
            return matlist.ToArray();
        }




        /// <summary>
        /// Generate unity mesh from list of object submesh builders 
        /// </summary>
        /// <param name="submeshBuilders"></param>
        /// <returns></returns>
        /// <remarks>https://godotengine.org/qa/42501/use-of-arraymesh-for-surfacing-meshes</remarks>
        public static MeshInstance GenerateGodotMesh(MeshBuilder[] submeshBuilders)
        {
            MeshInstance gdMeshInstance = new MeshInstance();

            // Aggregate vertices and set
            List<Vector3> joinedVertices = new List<Vector3>();
            Array.ForEach(submeshBuilders, subMeshElement => joinedVertices.AddRange(subMeshElement.vertices));

            ArrayMesh gdMesh = null;

            //List<Texture> tex = BuildTextureList(submeshBuilders.ToList<MeshBuilder>());

            // Prepare submesh per meshbuilder section
            int offset = 0;

            for (int i = 0; i < submeshBuilders.Length; i++)
            {
                List<Vector2> joinedUVs = new List<Vector2>();

                // Calculate UVs
                if (submeshBuilders[i].uvs.Count > 0 && submeshBuilders[i].uvs.Count == submeshBuilders[i].vertices.Count)
                {
                    // We have UVs for this CreateMeshBuilder section
                    joinedUVs.AddRange(submeshBuilders[i].uvs);
                }
                else
                {
                    //if (submeshBuilders[i].uvs.Count > 0)
                    //GD.Print("UV count {0} vs vertex count {1} - group {2} ", submeshBuilders[i].uvs.Count, submeshBuilders[i].vertices.Count, i);

                    // Don't have UVs for this section or they're out of sync with vertices, so add dummy ones to maintain count
                    for (int k = 0; k < submeshBuilders[i].vertices.Count; k++)
                        joinedUVs.Add(new Vector2(0, 0));
                }

                // Calculate faces 
                offset += (i == 0 ? 0 : submeshBuilders[i - 1].vertices.Count);
                List<int> currFaceTriangles = new List<int>();


                foreach (MeshFace face in submeshBuilders[i].faces)
                {
                    offset = 0; // TODO: confused about offset now           

                    // TODO: support more than quads and triangles
                    if (face.verticeIndexes.Count == 4 || face.verticeIndexes.Count == 3)
                    {
                        gdMesh = AddFace(gdMesh, submeshBuilders[i].vertices.ToArray(), face.verticeIndexes.ToArray(), joinedUVs.ToArray());
                    }
                    else
                    {
                        //  GD.Print("Polygon vertex count {0} not supported", face.verticeIndexes.Count);
                        continue;
                    }


                    // Apply texture
                    if (submeshBuilders[i].material != null)
                    {
                        MeshMaterial mat = submeshBuilders[i].material;

                    
                        if (!string.IsNullOrEmpty(mat.dayTexture) && System.IO.File.Exists(mat.dayTexture))
                        {
                            ShaderMaterial matTexture = new ShaderMaterial();

                            // TODO don't know if these have to be unloaded somehow later
                            string extn = System.IO.Path.GetExtension(mat.dayTexture).ToLower();

                            // TODO be safer about checking the file type / extension / validity etc
                            //if (extn == ".bmp" || extn == ".png")
                            ImageTexture tex = new ImageTexture();
                            Error e = tex.Load(mat.dayTexture);

                            //https://godotforums.org/discussion/19348/new-detail-texture-shader 
                            //https://godotengine.org/qa/43789/texture-fragment-shader-is-different-from-original-texture

                            // tex_1_side
                            // tex_2_side
                            // tex_1_side_trans
                            // tex_2_side_trans

                            matTexture.Shader = ResourceLoader.Load<Shader>(String.Format("res://{0}{1}.shader", 
                                                                     (face.flags & MeshFace.FACE_2_MASK) == MeshFace.FACE_2_MASK ? "tex_2_side" : "tex_1_side",
                                                                     mat.transparentColorUsed ? "_trans" : String.Empty));

                            matTexture.SetShaderParam("day_texture", tex);

                            if (mat.transparentColorUsed) matTexture.SetShaderParam("trans_color", mat.transparentColor);

                            // Set submesh to use to textured shader/material
                            gdMesh.SurfaceSetMaterial(gdMesh.GetSurfaceCount() - 1, matTexture);

                        }
                        else
                        {
                            // Set submesh to use to simple color shader/material
                            SpatialMaterial matColor = new SpatialMaterial(); 
                            matColor.AlbedoColor = mat.color;

                            gdMesh.SurfaceSetMaterial(gdMesh.GetSurfaceCount() - 1, matColor);
                        }

                    }

                }

            }


            gdMeshInstance.Mesh = gdMesh;
            return gdMeshInstance;

        }
    }


    private static ArrayMesh AddFace(ArrayMesh mesh, Vector3[] verts, MeshFaceVertex[] fv, Vector2[] uvs)
    {
        if (mesh == null) mesh = new ArrayMesh();

        // TODO: Assumes rectangular face
        int[] indices;

        if (fv.Length == 4)
            indices = new int[] { fv[0].indexToMeshVertices, fv[2].indexToMeshVertices, fv[3].indexToMeshVertices,
                                  fv[0].indexToMeshVertices, fv[1].indexToMeshVertices, fv[2].indexToMeshVertices};
        else
            indices = new int[] { fv[0].indexToMeshVertices, fv[1].indexToMeshVertices, fv[2].indexToMeshVertices };


        // Vector2 [] uv11 = new Vector2[] {new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)};

        Vector3[] no = new Vector3[verts.Count()];
        for (int i = 0; i < verts.Length; i++)
        {
            no[i] = verts[i].Normalized();
        }


        Godot.Collections.Array mesh_arrays = new Godot.Collections.Array();
        mesh_arrays.Resize((int)Mesh.ArrayType.Max);
        mesh_arrays[(int)ArrayMesh.ArrayType.Vertex] = verts;
        //mesh_arrays[(int)ArrayMesh.ArrayType.Normal] = no;
        mesh_arrays[(int)ArrayMesh.ArrayType.Index] = indices;
        mesh_arrays[(int)ArrayMesh.ArrayType.TexUv] = uvs;

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh_arrays);
        return mesh;
    }





    #endregion
    public static MeshInstance LoadFromFile(string fileName, Encoding fileEncoding, bool forceTextureRepeatX, bool forceTextureRepeatY)
    {
        // TODO: Not sure if safe to assume textures are always in the object path
        string fileExtension = System.IO.Path.GetExtension(fileName);
        string containingFolder = System.IO.Path.GetDirectoryName(fileName);

        string[] lines = System.IO.File.ReadAllLines(fileName, System.Text.Encoding.Default);

        bool isB3D = String.Equals(fileExtension, ".b3d", StringComparison.OrdinalIgnoreCase);

        GD.Print("Processing {0} lines from {1}", lines.Length, fileName);

        // Overall object mesh is made up of one or more submesh builders (i.e. "CreateMeshBuilder" command)
        MeshBuilder currentSubMesh = null;
        List<MeshBuilder> entireObjectMesh = new List<MeshBuilder>();

        Vector3[] normals = new Vector3[4];

        // Loop through object file and process each command
        for (int i = 0; i < lines.Length; i++)
        {
            // Strip away comments
            int commentStart = lines[i].IndexOf(';');
            lines[i] = commentStart >= 0 ? lines[i].Substring(0, commentStart) : lines[i];

            // Collect arguments
            string[] arguments = ParseArguments(lines[i]);

            // Determine command
            string command = null;
            if (arguments.Length > 0)
            {
                if (isB3D)
                {
                    // b3d
                    int space = arguments[0].IndexOf(' ');
                    if (space >= 0)
                    {
                        command = arguments[0].Substring(0, space).TrimEnd();
                        arguments[0] = arguments[0].Substring(space + 1).TrimStart();
                    }
                    else
                    {
                        command = arguments[0];
                        if (arguments.Length != 1)
                        {
                            GD.Print("Invalid syntax at line {0} in file {1}", i + 1, fileName);
                        }
                        arguments = new string[] { };
                    }
                }
                else
                {
                    // csv
                    command = arguments[0];
                    arguments = arguments.Skip(1).ToArray();
                }
            }

            // Parse terms
            if (command != null)
            {
                string cmd = command.ToLowerInvariant();
                switch (cmd)
                {
                    case "createmeshbuilder":
                    case "[meshbuilder]":
                        {
                            if (cmd == "createmeshbuilder" & isB3D)
                            {
                                GD.Print("CreateMeshBuilder is not a supported command - did you mean [MeshBuilder]? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            else if (cmd == "[meshbuilder]" & !isB3D)
                            {
                                GD.Print("[MeshBuilder] is not a supported command - did you mean CreateMeshBuilder? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            if (arguments.Length > 0)
                            {
                                GD.Print("0 arguments are expected in {0} - at line {1} in file {2} ", command, i, fileName);
                            }

                            if (currentSubMesh != null)
                            {
                                // Completed previous submesh, add to list before moving to next
                                entireObjectMesh.Add(currentSubMesh);
                            }

                            // Prepare next submesh
                            currentSubMesh = new MeshBuilder();
                            normals = new Vector3[4];
                        }

                        break;

                    case "addvertex":
                    case "vertex":
                        {
                            if (cmd == "addvertex" & isB3D)
                            {
                                GD.Print("AddVertex is not a supported command - did you mean Vertex? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            else if (cmd == "vertex" & !isB3D)
                            {
                                GD.Print("Vertex is not a supported command - did you mean AddVertex? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            if (arguments.Length > 6)
                            {
                                GD.Print("At most 6 arguments are expected in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                            }
                            double vx = 0.0, vy = 0.0, vz = 0.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out vx))
                            {
                                GD.Print("Invalid argument vX in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                vx = 0.0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out vy))
                            {
                                GD.Print("Invalid argument vY in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                vy = 0.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out vz))
                            {
                                GD.Print("Invalid argument vZ in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                vz = 0.0;
                            }
                            double nx = 0.0, ny = 0.0, nz = 0.0;
                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out nx))
                            {
                                GD.Print("Invalid argument nX in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                nx = 0.0;
                            }
                            if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out ny))
                            {
                                GD.Print("Invalid argument nY in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                ny = 0.0;
                            }
                            if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out nz))
                            {
                                GD.Print("Invalid argument nZ in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                                nz = 0.0;
                            }

                            vz *= -1;

                            // normals
                            // todo: float/double conversions
                            Vector3 coords = new Vector3((float)nx, (float)ny, (float)nz);

                            while (currentSubMesh.vertices.Count >= normals.Length)
                            {
                                Array.Resize<Vector3>(ref normals, normals.Length << 1);
                            }
                            normals[currentSubMesh.vertices.Count] = coords.Normalized();

                            // vertices
                            currentSubMesh.vertices.Add(new Vector3((float)vx, (float)vy, (float)vz));
                        }

                        break;

                    case "addface":
                    case "addface2":
                    case "face":
                    case "face2":
                        {
                            // The index corresponds to the order in which the vertices have been created by the Vertex command, thus the Face command needs to be stated after the 
                            // corresponding Vertex commands. The first Vertex command used creates index 0, and subsequent Vertex commands create indices 1, 2, 3 and so on.
                            // The order in which the vertex indices appear is important. They need to be given in clockwise order when looking at the front of the face. 
                            // The back of the face will not be visible. However, the Face2 command can be used to create a face which is visible from both sides. Only convex polygons are supported.

                            if (isB3D)
                            {
                                if (cmd == "addface")
                                {
                                    GD.Print("AddFace is not a supported command - did you mean Face? - at line {0} in file {1} ", i + 1, fileName);
                                }
                                else if (cmd == "addface2")
                                {
                                    GD.Print("AddFace2 is not a supported command - did you mean Face2? - at line {0} in file {1} ", i + 1, fileName);
                                }
                            }
                            else
                            {
                                if (cmd == "face")
                                {
                                    GD.Print("Face is not a supported command - did you mean AddFace? - at line {0} in file {1} ", i + 1, fileName);
                                }
                                else if (cmd == "face2")
                                {
                                    GD.Print("Face2 is not a supported command - did you mean AddFace2? - at line {0} in file {1} ", i + 1, fileName);
                                }
                            }

                            if (arguments.Length < 3)
                            {
                                GD.Print("At least 3 arguments are required in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                            }
                            else
                            {
                                bool valid = true;
                                int[] faceVertexIndices = new int[arguments.Length];
                                for (int j = 0; j < arguments.Length; j++)
                                {
                                    if (!Conversions.TryParseIntVb6(arguments[j], out faceVertexIndices[j]))
                                    {
                                        GD.Print("v{0} is invalid in {1} - at line {2} in file {3} ", j, command, i + 1, fileName);
                                        valid = false;
                                        break;
                                    }
                                    else if (faceVertexIndices[j] < 0 | faceVertexIndices[j] >= currentSubMesh.vertices.Count)
                                    {
                                        GD.Print("v{0} references a non-existing vertex in {1} - at line {2} in file {3} ", j, command, i + 1, fileName);
                                        valid = false;
                                        break;
                                    }
                                    else if (faceVertexIndices[j] > 65535)
                                    {
                                        GD.Print("v{0} indexes a vertex above 65535 which is not currently supported in {1} - at line {2} in file {3} ", j, command, i + 1, fileName);
                                        valid = false;
                                        break;
                                    }
                                }

                                if (valid)
                                {
                                    MeshFace f = new MeshFace();

                                    while (currentSubMesh.vertices.Count > normals.Length)
                                    {
                                        Array.Resize<Vector3>(ref normals, normals.Length << 1);
                                    }

                                    for (int j = 0; j < arguments.Length; j++)
                                    {
                                        MeshFaceVertex v = new MeshFaceVertex((ushort)faceVertexIndices[j]);

                                        v.indexToMeshVertices = (ushort)faceVertexIndices[j];
                                        v.normal = normals[faceVertexIndices[j]];
                                        f.verticeIndexes.Add(v);
                                    }

                                    if (cmd == "addface2" | cmd == "face2")
                                    {
                                        f.flags = (byte)MeshFace.FACE_2_MASK;
                                    }

                                    currentSubMesh.faces.Add(f);
                                }
                            }
                        }
                        break;

                    case "loadtexture":
                    case "load":
                        {
                            if (cmd == "loadtexture" & isB3D)
                            {
                                GD.Print("LoadTexture is not a supported B3D file command - did you mean Load? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            else if (cmd == "load" & !isB3D)
                            {
                                GD.Print("Load is not a supported CSV file command - did you mean LoadTexture? - at line {0} in file {1} ", i + 1, fileName);

                            }
                            if (arguments.Length > 2)
                            {
                                GD.Print("At most 2 arguments are expected in {0} - at line {1} in file {2} ", cmd, i + 1, fileName);
                            }

                            // todo: far more error handling
                            string tday = null, tnight = null;
                            if (arguments.Length >= 1 && arguments[0].Length != 0)
                            {
                                tday = System.IO.Path.Combine(containingFolder, arguments[0]);
                            }

                            if (arguments.Length >= 2 && arguments[1].Length != 0)
                            {
                                tnight = System.IO.Path.Combine(containingFolder, arguments[1]);
                            }

                            MeshMaterial mm = new MeshMaterial()
                            {
                                dayTexture = !String.IsNullOrEmpty(tday) ? System.IO.Path.Combine(containingFolder, tday) : null,
                                nightTexture = !String.IsNullOrEmpty(tnight) ? System.IO.Path.Combine(containingFolder, tnight) : null
                            };

                            currentSubMesh.material = mm;

                            //  currentSubMesh.faces[currentSubMesh.faces.Count-1].materialIndex = currentSubMesh.materials.Count-1;
                        }
                        break;

                    case "settexturecoordinates":
                    case "coordinates":
                        {
                            if (cmd == "settexturecoordinates" & isB3D)
                            {
                                GD.Print("SetTextureCoordinates is not a supported B3D file command - did you mean Coordinates? - at line {0} in file {1} ", i + 1, fileName);
                                //    Debug.AddMessage(Debug.MessageType.Warning, false, "SetTextureCoordinates is not a supported command - did you mean Coordinates? - at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            else if (cmd == "coordinates" & !isB3D)
                            {
                                GD.Print("SetTextureCoordinates is not a supported CSV file command - did you mean Coordinates? - at line {0} in file {1} ", i + 1, fileName);
                                //    Debug.AddMessage(Debug.MessageType.Warning, false, "Coordinates is not a supported command - did you mean SetTextureCoordinates? - at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            if (arguments.Length > 3)
                            {
                                GD.Print("At most 3 arguments are expected in {0} - at line {1} in file {2} ", cmd, i + 1, fileName);
                                //    Debug.AddMessage(Debug.MessageType.Warning, false, "At most 3 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }

                            int k = 0; float x = 0.0f, y = 0.0f;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out k))
                            {
                                GD.Print("Invalid argument VertexIndex in {0} - at line {1} in file {2} ", cmd, i + 1, fileName);
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument VertexIndex in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                k = 0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], out x))
                            {
                                GD.Print("Invalid argument X in {0} - at line {1} in file {2} ", cmd, i + 1, fileName);
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument X in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                x = 0.0f;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], out y))
                            {
                                GD.Print("Invalid argument Y in {0} - at line {1} in file {2} ", cmd, i + 1, fileName);
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Y in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                y = 0.0f;
                            }

                            if (k >= 0 & k < currentSubMesh.vertices.Count)
                            {
                                currentSubMesh.uvs.Add(new Vector2(x, y));
                            }
                            else
                            {
                                GD.Print("VertexIndex references a non-existing vertex in {0} - at line {1} in file {2} ", command, i + 1, fileName);
                            }
                        }
                        break;

                    case "setcolor":
                    case "color":
                        {

                            if (cmd == "setcolor" & isB3D)
                            {
                                GD.Print("SetColor is not a supported B3D file command - did you mean Color? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            else if (cmd == "color" & !isB3D)
                            {
                                GD.Print("Color is not a supported CSV file command - did you mean SetColor? - at line {0} in file {1} ", i + 1, fileName);
                            }
                            if (arguments.Length > 4)
                            {
                                GD.Print("At most 4 arguments are expected in {0} - at line {1} in file {2} ", command, i + 1, fileName);

                            }
                            int r = 0, g = 0, b = 0, a = 255;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out r))
                            {
                                r = 0;
                            }
                            else if (r < 0 | r > 255)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Red is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                r = r < 0 ? 0 : 255;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Green in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                g = 0;
                            }
                            else if (g < 0 | g > 255)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Green is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                g = g < 0 ? 0 : 255;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Blue in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                b = 0;
                            }
                            else if (b < 0 | b > 255)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Blue is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                b = b < 0 ? 0 : 255;
                            }
                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out a))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Alpha in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                a = 255;
                            }
                            else if (a < 0 | a > 255)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Alpha is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                a = a < 0 ? 0 : 255;
                            }


                            MeshMaterial mmcolor = new MeshMaterial();
                            mmcolor.color = Color.Color8((byte)r, (byte)g, (byte)b, (byte)a);
                            currentSubMesh.material = mmcolor;

                            //currentSubMesh.faces[currentSubMesh.faces.Count-1].materialIndex = currentSubMesh.materials.Count-1;
                        }
                        break;

                    case "setdecaltransparentcolor":
                    case "transparent":
                        {
                            if (cmd == "setdecaltransparentcolor" & isB3D)
                            {
                                //currentHost.AddMessage(MessageType.Warning, false, "SetDecalTransparentColor is not a supported command - did you mean Transparent? - at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            else if (cmd == "transparent" & !isB3D)
                            {
                                //currentHost.AddMessage(MessageType.Warning, false, "Transparent is not a supported command - did you mean SetDecalTransparentColor? - at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            if (arguments.Length > 3)
                            {
                                // currentHost.AddMessage(MessageType.Warning, false, "At most 3 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            int r = 0, g = 0, b = 0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out r))
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Invalid argument Red in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                r = 0;
                            }
                            else if (r < 0 | r > 255)
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Red is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                r = r < 0 ? 0 : 255;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Invalid argument Green in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                g = 0;
                            }
                            else if (g < 0 | g > 255)
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Green is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                g = g < 0 ? 0 : 255;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Invalid argument Blue in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                b = 0;
                            }
                            else if (b < 0 | b > 255)
                            {
                                // currentHost.AddMessage(MessageType.Error, false, "Blue is required to be within the range from 0 to 255 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                b = b < 0 ? 0 : 255;
                            }

                            currentSubMesh.material.transparentColor = Color.Color8((byte)r, (byte)g, (byte)b);
                            currentSubMesh.material.transparentColorUsed = true;

                            // for (int j = 0; j < Builder.Materials.Length; j++)
                            // {
                            //     Builder.Materials[j].TransparentColor = new Color24((byte)r, (byte)g, (byte)b);
                            //     Builder.Materials[j].TransparentColorUsed = true;
                            // }
                        }
                        break;

                    case "translate":
                    case "translateall":
                        {
                            if (arguments.Length > 3)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 3 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            double xt = 0.0, yt = 0.0, zt = 0.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out xt))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument X in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                xt = 0.0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out yt))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Y in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                yt = 0.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out zt))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Z in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                zt = 0.0;
                            }

                            ApplyTranslation(currentSubMesh, xt, yt, zt);

                            // todo: translateall
                            if (cmd == "translateall")
                            {
                                //    ApplyTranslation(Object, xt, yt, zt);
                            }
                        }
                        break;

                    case "scale":
                    case "scaleall":
                        {
                            if (arguments.Length > 3)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 3 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            double sx = 1.0, sy = 1.0, sz = 1.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out sx))
                            {
                                //    Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument X in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sx = 1.0;
                            }
                            else if (sx == 0.0)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "X is required to be different from zero in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sx = 1.0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out sy))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Y in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sy = 1.0;
                            }
                            else if (sy == 0.0)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Y is required to be different from zero in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sy = 1.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out sz))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Z in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sz = 1.0;
                            }
                            else if (sz == 0.0)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Z is required to be different from zero in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                sz = 1.0;
                            }

                            ApplyScale(currentSubMesh, sx, sy, sz);

                            if (cmd == "scaleall")
                            {
                                // ApplyScale(Object, x, y, z);
                            }
                        }
                        break;

                    case "rotate":
                    case "rotateall":
                        {
                            if (arguments.Length > 4)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 4 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            double rx = 0.0, ry = 0.0, rz = 0.0, ra = 0.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out rx))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument X in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                rx = 0.0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out ry))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Y in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                ry = 0.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out rz))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Z in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                rz = 0.0;
                            }
                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out ra))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Angle in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                ra = 0.0;
                            }
                            double rt = rx * rx + ry * ry + rz * rz;
                            if (rt == 0.0)
                            {
                                rx = 1.0;
                                ry = 0.0;
                                rz = 0.0;
                                rt = 1.0;
                            }

                            if (ra != 0.0)
                            {
                                rt = 1.0 / Math.Sqrt(rt);
                                rx *= rt;
                                ry *= rt;
                                rz *= rt;
                                ra *= 0.0174532925199433;

                                ApplyRotation(ref currentSubMesh, rx, ry, rz, ra);

                                if (cmd == "rotateall")
                                {
                                    //TODO
                                    //ApplyRotation(Object, x, y, z, a);
                                }
                            }
                        }
                        break;

                    case "shear":
                    case "shearall":
                        {
                            if (arguments.Length > 7)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 7 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            double shdx = 0.0, shdy = 0.0, shdz = 0.0;
                            double shsx = 0.0, shsy = 0.0, shsz = 0.0;
                            double shr = 0.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out shdx))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument dX in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shdx = 0.0;
                            }
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out shdy))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument dY in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shdy = 0.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out shdz))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument dZ in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shdz = 0.0;
                            }
                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out shsx))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument sX in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shsx = 0.0;
                            }
                            if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out shsy))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument sY in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shsy = 0.0;
                            }
                            if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out shsz))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument sZ in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shsz = 0.0;
                            }
                            if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out shr))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Ratio in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                shr = 0.0;
                            }


                            //TODO
                            // Calc.Normalize(ref shdx, ref shdy, ref shdz);
                            // Calc.Normalize(ref shsx, ref shsy, ref shsz);

                            // ApplyShear(meshBuilder, shdx, shdy, shdz, shsx, shsy, shsz, shr);

                            if (cmd == "shearall")
                            {
                                //TODO
                                //ApplyShear(Object, dx, dy, dz, sx, sy, sz, r);
                            }
                        }
                        break;

                    case "cylinder":
                        {
                            if (arguments.Length > 4)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 4 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            int cyl_n = 8;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out cyl_n))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument n in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cyl_n = 8;
                            }
                            if (cyl_n < 2)
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "n is expected to be at least 2 in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cyl_n = 8;
                            }
                            double cyl_r1 = 0.0, cyl_r2 = 0.0, cyl_h = 1.0;
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out cyl_r1))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument UpperRadius in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cyl_r1 = 1.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out cyl_r2))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument LowerRadius in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cyl_r2 = 1.0;
                            }
                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out cyl_h))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument Height in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cyl_h = 1.0;
                            }
                            CreateCylinder(ref currentSubMesh, cyl_n, cyl_r1, cyl_r2, cyl_h);
                        }
                        break;

                    case "cube":
                        {
                            if (arguments.Length > 3)
                            {
                                //Debug.AddMessage(Debug.MessageType.Warning, false, "At most 3 arguments are expected in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                            }
                            double cube_x = 0.0;
                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out cube_x))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument HalfWidth in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cube_x = 1.0;
                            }
                            double cube_y = cube_x, cube_z = cube_x;
                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out cube_y))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument HalfHeight in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cube_y = 1.0;
                            }
                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out cube_z))
                            {
                                //Debug.AddMessage(Debug.MessageType.Error, false, "Invalid argument HalfDepth in " + Command + " at line " + (i + 1).ToString(Culture) + " in file " + FileName);
                                cube_z = 1.0;
                            }
                            CreateCube(ref currentSubMesh, cube_x, cube_y, cube_z);
                        }
                        break;

                    default:

                        GD.Print("Unknown command '{0}' - at line {1} in file {2}", command, i + 1, fileName);
                        break;

                }
            }
        }

        // Add the last meshbuilder
        if (entireObjectMesh.Count == 0 || currentSubMesh != entireObjectMesh.Last())
            entireObjectMesh.Add(currentSubMesh);

        // Finally, create the godot engine mesh based on the consolidated MeshBuilder instances for the overall object     	
        return MeshBuilder.GenerateGodotMesh(entireObjectMesh.ToArray());

    }

    // create cube
    private static void CreateCube(ref MeshBuilder builder, double sx, double sy, double sz)
    {
        int v = builder.vertices.Count;

        builder.vertices.Add(new Vector3((float)sx, (float)-sy, (float)-sz));
        builder.vertices.Add(new Vector3((float)sx, (float)sy, (float)-sz));
        builder.vertices.Add(new Vector3((float)-sx, (float)-sy, (float)-sz));
        builder.vertices.Add(new Vector3((float)-sx, (float)sy, (float)-sz));
        builder.vertices.Add(new Vector3((float)sx, (float)sy, (float)sz));
        builder.vertices.Add(new Vector3((float)sx, (float)-sy, (float)sz));
        builder.vertices.Add(new Vector3((float)-sx, (float)-sy, (float)sz));
        builder.vertices.Add(new Vector3((float)-sx, (float)sy, (float)sz));

        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(1) }));

        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 0), new MeshFaceVertex(v + 1), new MeshFaceVertex(v + 2), new MeshFaceVertex(v + 3) }));
        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 0), new MeshFaceVertex(v + 4), new MeshFaceVertex(v + 5), new MeshFaceVertex(v + 1) }));
        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 0), new MeshFaceVertex(v + 3), new MeshFaceVertex(v + 7), new MeshFaceVertex(v + 4) }));
        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 6), new MeshFaceVertex(v + 5), new MeshFaceVertex(v + 4), new MeshFaceVertex(v + 7) }));
        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 6), new MeshFaceVertex(v + 7), new MeshFaceVertex(v + 3), new MeshFaceVertex(v + 2) }));
        builder.faces.Add(new MeshFace(new MeshFaceVertex[] { new MeshFaceVertex(v + 6), new MeshFaceVertex(v + 2), new MeshFaceVertex(v + 1), new MeshFaceVertex(v + 5) }));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="n"></param>
    /// <param name="r1"></param>
    /// <param name="r2"></param>
    /// <param name="h"></param>
    /// <remarks>
    /// n: An integer representing the number of vertices to be used for the base of the frustum.
    /// UpperRadius: A floating-point number representing the radius for the upper base of the frustum in meters.Can be negative to indicate that the top cap is to be omitted.
    /// LowerRadius: A floating-point number representing the radius for the lower base of the frustum in meters.Can be negative to indicate that the bottom cap is to be omitted.
    /// Height: A floating-point number representing the height of the prism in meters.Can be negative, which will flip the frustum vertically and display it inside-out.
    /// </emarks>
    /// 
    private static void CreateCylinder(ref MeshBuilder builder, int n, double r1, double r2, double h)
    {
        // parameters
        bool uppercap = r1 > 0.0;
        bool lowercap = r2 > 0.0;
        int m = (uppercap ? 1 : 0) + (lowercap ? 1 : 0);
        r1 = Math.Abs(r1);
        r2 = Math.Abs(r2);
        double ns = h >= 0.0 ? 1.0 : -1.0;

        // initialization
        int v = builder.vertices.Count;
        //Array.Resize<World.Vertex>(ref builder.Vertices, v + 2 * n);

        Vector3[] normals = new Vector3[2 * n];
        double d = 2.0 * Math.PI / (double)n;
        double g = 0.5 * h;
        double t = 0.0;
        double a = h != 0.0 ? Math.Atan((r2 - r1) / h) : 0.0;
        double cosa = Math.Cos(a);
        double sina = Math.Sin(a);

        // vertices and normals
        for (int i = 0; i < n; i++)
        {
            double dx = Math.Cos(t);
            double dz = Math.Sin(t);
            double lx = dx * r2;
            double lz = dz * r2;
            double ux = dx * r1;
            double uz = dz * r1;
            builder.vertices.Add(new Vector3((float)ux, (float)g, (float)uz));
            builder.vertices.Add(new Vector3((float)lx, (float)-g, (float)lz));
            double nx = dx * ns, ny = 0.0, nz = dz * ns;
            double sx, sy, sz;
            Calc.Cross(nx, ny, nz, 0.0, 1.0, 0.0, out sx, out sy, out sz);
            Calc.Rotate(ref nx, ref ny, ref nz, sx, sy, sz, cosa, sina);
            normals[2 * i + 0] = new Vector3((float)nx, (float)ny, (float)nz);
            normals[2 * i + 1] = new Vector3((float)nx, (float)ny, (float)nz);
            t += d;
        }

        // faces
        int f = builder.faces.Count;
        //Array.Resize<World.MeshFace>(ref builder.Faces, f + n + m);
        for (int i = 0; i < n; i++)
        {
            //            builder.faces[f + i].flags = 0;
            int i0 = (2 * i + 2) % (2 * n);
            int i1 = (2 * i + 3) % (2 * n);
            int i2 = 2 * i + 1;
            int i3 = 2 * i;
            //builder.Faces[f + i].Vertices = new World.MeshFaceVertex[] { new World.MeshFaceVertex(v + i0, normals[i0]), new World.MeshFaceVertex(v + i1, normals[i1]), new World.MeshFaceVertex(v + i2, normals[i2]), new World.MeshFaceVertex(v + i3, normals[i3]) };
            MeshFace f1 = new MeshFace();
            f1.verticeIndexes.Add(new MeshFaceVertex(v + i0, normals[i0]));
            f1.verticeIndexes.Add(new MeshFaceVertex(v + i1, normals[i1]));
            f1.verticeIndexes.Add(new MeshFaceVertex(v + i2, normals[i2]));
            f1.verticeIndexes.Add(new MeshFaceVertex(v + i3, normals[i3]));
            builder.faces.Add(f1);
        }


        //for (int i = 0; i < m; i++)
        //{
        //    builder.faces[f + n + i].verticeIndexes = new List<MeshFaceVertex>();
        //    for (int j = 0; j < n; j++)
        //    {
        //        if (i == 0 & lowercap)
        //        {
        //            // lower cap
        //            builder.faces[f + n + i].verticeIndexes[j] = new MeshFaceVertex(v + 2 * j + 1);
        //        }
        //        else
        //        {
        //            // upper cap
        //            builder.faces[f + n + i].verticeIndexes[j] = new MeshFaceVertex(v + 2 * (n - j - 1));
        //        }
        //    }
        //}
    }




    private static void ApplyShear(MeshBuilder builder, double dx, double dy, double dz, double sx, double sy, double sz, double r)
    {
        Vector3[] vertexArray = builder.vertices.ToArray();

        for (int j = 0; j < vertexArray.Length; j++)
        {
            double n = r * (dx * builder.vertices[j].x + dy * builder.vertices[j].y + dz * builder.vertices[j].z);
            vertexArray[j].x += (float)(sx * n);
            vertexArray[j].y += (float)(sy * n);
            vertexArray[j].z += (float)(sz * n);
        }

        foreach (MeshFace f in builder.faces)
        {
            for (int k = 0; k < f.verticeIndexes.Count; k++)
            {
                if (f.verticeIndexes[k].normal.x != 0.0f | f.verticeIndexes[k].normal.y != 0.0f | f.verticeIndexes[k].normal.z != 0.0f)
                {
                    double nx = (double)f.verticeIndexes[k].normal.x;
                    double ny = (double)f.verticeIndexes[k].normal.y;
                    double nz = (double)f.verticeIndexes[k].normal.z;
                    double n = r * (sx * nx + sy * ny + sz * nz);
                    nx -= dx * n;
                    ny -= dy * n;
                    nz -= dz * n;
                    Calc.Normalize(ref nx, ref ny, ref nz);
                    f.verticeIndexes[k].normal.x = (float)nx;
                    f.verticeIndexes[k].normal.y = (float)ny;
                    f.verticeIndexes[k].normal.z = (float)nz;
                }
            }
        }
    }
    //private static void ApplyShear(ObjectManager.StaticObject Object, double dx, double dy, double dz, double sx, double sy, double sz, double r)
    //{
    //    for (int j = 0; j < Object.Mesh.Vertices.Length; j++)
    //    {
    //        double n = r * (dx * Object.Mesh.Vertices[j].Coordinates.X + dy * Object.Mesh.Vertices[j].Coordinates.Y + dz * Object.Mesh.Vertices[j].Coordinates.Z);
    //        Object.Mesh.Vertices[j].Coordinates.X += sx * n;
    //        Object.Mesh.Vertices[j].Coordinates.Y += sy * n;
    //        Object.Mesh.Vertices[j].Coordinates.Z += sz * n;
    //    }
    //    double ux, uy, uz;
    //    World.Cross(sx, sy, sz, dx, dy, dz, out ux, out uy, out uz);
    //    for (int j = 0; j < Object.Mesh.Faces.Length; j++)
    //    {
    //        for (int k = 0; k < Object.Mesh.Faces[j].Vertices.Length; k++)
    //        {
    //            if (Object.Mesh.Faces[j].Vertices[k].Normal.X != 0.0f | Object.Mesh.Faces[j].Vertices[k].Normal.Y != 0.0f | Object.Mesh.Faces[j].Vertices[k].Normal.Z != 0.0f)
    //            {
    //                double nx = (double)Object.Mesh.Faces[j].Vertices[k].Normal.X;
    //                double ny = (double)Object.Mesh.Faces[j].Vertices[k].Normal.Y;
    //                double nz = (double)Object.Mesh.Faces[j].Vertices[k].Normal.Z;
    //                double n = r * (sx * nx + sy * ny + sz * nz);
    //                nx -= dx * n;
    //                ny -= dy * n;
    //                nz -= dz * n;
    //                World.Normalize(ref nx, ref ny, ref nz);
    //                Object.Mesh.Faces[j].Vertices[k].Normal.X = (float)nx;
    //                Object.Mesh.Faces[j].Vertices[k].Normal.Y = (float)ny;
    //                Object.Mesh.Faces[j].Vertices[k].Normal.Z = (float)nz;
    //            }
    //        }
    //    }
    //}


    private static void ApplyScale(MeshBuilder builder, double x, double y, double z)
    {
        float rx = (float)(1.0 / x);
        float ry = (float)(1.0 / y);
        float rz = (float)(1.0 / z);
        float rx2 = rx * rx;
        float ry2 = ry * ry;
        float rz2 = rz * rz;

        for (int i = 0; i < builder.vertices.Count; i++)
        {
            Vector3 v = builder.vertices[i];        // need a copy because struct is value type

            v.z *= (float)x;
            v.y *= (float)y;
            v.z *= (float)z;

            builder.vertices[i] = v;
        }

        foreach (MeshFace f in builder.faces)
        {
            for (int j = 0; j < f.verticeIndexes.Count; j++)
            {
                float nx2 = f.verticeIndexes[j].normal.x * f.verticeIndexes[j].normal.x;
                float ny2 = f.verticeIndexes[j].normal.y * f.verticeIndexes[j].normal.y;
                float nz2 = f.verticeIndexes[j].normal.z * f.verticeIndexes[j].normal.z;
                float u = nx2 * rx2 + ny2 * ry2 + nz2 * rz2;
                if (u != 0.0)
                {
                    u = (float)Math.Sqrt((double)((nx2 + ny2 + nz2) / u));
                    Vector3 n = f.verticeIndexes[j].normal;     // need a copy because struct is value type
                    n.x *= rx * u;
                    n.y *= ry * u;
                    n.z *= rz * u;
                    f.verticeIndexes[j].normal = n;
                }
            }
        }

        if (x * y * z < 0.0)
        {
            for (int i = 0; i < builder.faces.Count; i++)
            {
                builder.faces[i].Flip();
            }
        }
    }

    private static void ApplyTranslation(MeshBuilder builder, double x, double y, double z)
    {
        for (int i = 0; i < builder.vertices.Count; i++)
        {
            Vector3 v = builder.vertices[i];    // careful - need a copy because struct is value type
            v.x += (float)x;
            v.y += (float)y;
            v.z += (float)z;
            builder.vertices[i] = v;

        }
    }

    // apply rotation
    private static void ApplyRotation(ref MeshBuilder builder, double x, double y, double z, double a)
    {
        double cosa = Math.Cos(a);
        double sina = Math.Sin(a);

        for (int i = 0; i < builder.vertices.Count; i++)
        {
            Vector3 v = builder.vertices[i];    // careful - need a copy because struct is value type
            Calc.Rotate(ref v.x, ref v.y, ref v.z, x, y, z, cosa, sina);
            builder.vertices[i] = v;
        }

        foreach (MeshFace f in builder.faces)
        {
            for (int j = 0; j < f.verticeIndexes.Count; j++)
            {
                Vector3 n = f.verticeIndexes[j].normal;  // careful - need a copy because struct is value type
                Calc.Rotate(ref n.x, ref n.y, ref n.z, x, y, z, cosa, sina);
                f.verticeIndexes[j].normal = n;
            }
        }
    }

    /// <summary>
    /// Parse arguments from given object file command
    /// </summary>
    /// <param name="line">Object file command to parse (e.g. AddVertex,-1.2,3.7,0,|) per BVE B3d object format</param>
    /// <returns>Array of arguments from the given object command</returns>
    private static string[] ParseArguments(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return new string[] { };
        }
        else
        {
            string[] arguments = line.Split(',');
            for (int j = 0; j < arguments.Length; j++)
            {
                arguments[j] = arguments[j].Trim();
            }

            // Remove unused arguments at the end of the chain
            int k;
            for (k = arguments.Length - 1; k >= 0; k--)
            {
                if (arguments[k].Length != 0) break;
            }

            if (k >= 0)
                Array.Resize<string>(ref arguments, k + 1);

            return arguments;
        }
    }



    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
