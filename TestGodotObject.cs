using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class TestGodotObject : MeshInstance
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    static int LOG_LEVEL = 1;

    static float SIZE = 1.0f;
    static float HEIGHT = (float)(Math.Sqrt((double)3.0f) * SIZE);
    static float HALF_HEIGHT = HEIGHT * 0.5f;
    static float KITE_HEIGHT = (2.0f / 3.0f) * (HALF_HEIGHT);

    static float KITE_ORTHOGONAL_HEIGHT = (0.75f) * KITE_HEIGHT;
    static float KITE_SIDE_LONG = (float)SIZE / 2.0f;
    static float KITE_SIDE_SHORT = (2.0f / 3.0f) * KITE_ORTHOGONAL_HEIGHT;

    // onready var mesh_instance = $MeshInstance

    Vector3 pt1 = new Vector3(0.0f, 0.0f, 0.0f);
    Vector3 pt2 = new Vector3(KITE_ORTHOGONAL_HEIGHT, 0.0f, 0.25f);
    Vector3 pt3 = new Vector3(KITE_SIDE_SHORT, 0.0f, 0.5f);
    Vector3 pt4 = new Vector3(0.0f, 0.0f, 0.5f);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ArrayMesh am = new ArrayMesh();

        Vector3[] first_tri_first_kite_vertices = new Vector3[] { pt1, pt2, pt3, pt4 };

        Vector3[] rolling_vertices = first_tri_first_kite_vertices;

        for (int i = 0; i < 6; i++)
        {
            am = draw_hex_tri(rolling_vertices, ref am);
            for (int i2 = 0; i2 < rolling_vertices.Length; i2++)
            {
                rolling_vertices[i2] = point_rotation(rolling_vertices[i2], rolling_vertices[0], 60.0f);
            }
        }

        apply_color(am);

        this.Mesh = am;
    }



    private ArrayMesh draw_hex_tri(Vector3[] base_verts, ref ArrayMesh local_array_mesh)
    {
        Mesh a = new ArrayMesh();
        
        Godot.Collections.Array mesh_arrays = new Godot.Collections.Array();
        
        int[] indices = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh_arrays.Resize((int)Mesh.ArrayType.Max);

        for (int i = 0; i < 3; i++)
        {
            List<Vector3> verts = new List<Vector3>();
            for (int i2 = 0; i2 < base_verts.Length; i2++)
            {
                verts.Add(point_rotation(base_verts[i2], base_verts[2], 120f * i));
            }

            //mesh_arrays[Godot.Mesh] = verts;
            mesh_arrays[(int)ArrayMesh.ArrayType.Vertex] = verts.ToArray();
            mesh_arrays[(int)ArrayMesh.ArrayType.Index] = indices;
        }

        local_array_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh_arrays);
        return local_array_mesh;
    }

    Vector3 point_rotation(Vector3 point_to_rotate, Vector3 rotation_origin, float angle)
    {
        float rads = Mathf.Deg2Rad(angle);
        float sin_val = Mathf.Sin(rads);
        float cos_val = Mathf.Cos(rads);

        point_to_rotate.x -= rotation_origin.x;
        point_to_rotate.z -= rotation_origin.z;

        float xnew = point_to_rotate.x * cos_val - point_to_rotate.z * sin_val;
        float znew = point_to_rotate.x * sin_val + point_to_rotate.z * cos_val;

        return new Vector3(xnew + rotation_origin.x, 0.0f, znew + rotation_origin.z);
    }

    SpatialMaterial[] make_materials(int n)
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

    void apply_color(ArrayMesh am)
    {
        int surface_count = am.GetSurfaceCount();
        SpatialMaterial[] mats = make_materials(surface_count);
        for (int i = 0; i < surface_count; i++)
        {
            am.SurfaceSetMaterial(i, mats[i]);
        }

    }

}
