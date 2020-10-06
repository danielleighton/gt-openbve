using Godot;
using System.Text;

public class SceneLoader : Node {

	public override void _Ready() {
	
		/* 		
		// Parent of test objects
		Node n1 = new Node();
		n1.Name = "Test Objects";
		AddChild(n1);

		// Original test object 
		MeshInstance mi = CsvB3dObjectParser.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\gaku\fumikiri1.csv", System.Text.Encoding.ASCII,true, true);
		mi.Name = "kitatono";
		n1.AddChild(mi);

		// Duplicated test object with rotation
		MeshInstance mi_dupe = (MeshInstance)mi.Duplicate( (int)DuplicateFlags.UseInstancing);
		mi_dupe.Name = "kitatono dupe";
		n1.AddChild(mi_dupe);
		Transform t = new Transform(Basis.Identity, new Vector3(0,0,0));
		t = t.Rotated(Vector3.Up, 0.9996877f);		// yaw by 57 degrees
		mi_dupe.Transform = t;
		mi_dupe.Translate(new Vector3(10f,10f,10f));
		*/
				
		string rootResourcesPath = @"C:\users\danie\desktop\test";
        string routeFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Route"));
        string objectFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Object"));
        string trainFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Train"));
        string soundFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Sound"));

		//string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Route\Taipei Rapid Transit  Zhonghe Line-Down.csv");

		string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Route\test.csv");

		Node n2 = new Node();
		n2.Name = "Route Objects";
		AddChild(n2);

        CsvRwRouteParser.ParseRoute(n2, routePath, Encoding.Default, false, trainFolder, objectFolder, "", false);
   
	}


}
