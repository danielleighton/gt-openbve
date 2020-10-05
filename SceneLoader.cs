using Godot;
using System.Text;

public class SceneLoader : Node {

	public override void _Ready() {
	
//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\Compatibility\Signals\signal_post.csv", System.Text.Encoding.ASCII,true, true);


//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\reren\EDtozai\bridge\EdoBridge.csv", System.Text.Encoding.ASCII,true, true);

		//MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\Camden\hb.csv", System.Text.Encoding.ASCII,true, true);

	//	MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\reren\Sjyou\People\Staff3.csv", System.Text.Encoding.ASCII,true, true);

//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\reren\Sjyou\car\Bus_A.csv", System.Text.Encoding.ASCII,true, true);
/*
		Node n1 = new Node();
		n1.Name = "Test Objects";
		AddChild(n1);

		MeshInstance mi = CsvB3dObjectParser.LoadFromFile(@"C:\Users\danie\Desktop\Test\Object\gaku\fumikiri1.csv", System.Text.Encoding.ASCII,true, true);
		mi.Name = "kitatono";
		n1.AddChild(mi);
		*/

		
/*

		MeshInstance mi_dupe = (MeshInstance)mi.Duplicate( (int)DuplicateFlags.UseInstancing);
		mi_dupe.Name = "kitatono dupe";
		n1.AddChild(mi_dupe);

	

		Transform t = new Transform(Basis.Identity, new Vector3(0,0,0));
		t = t.Rotated(Vector3.Up, 0.9996877f);
		t = t.Rotated(Vector3.Back, -0.02499219f);

		mi_dupe.Transform = t;

		// MeshInstance mi2 = CsvB3dObjectParser.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\EDtozai\bridge\EdoBridge.csv", System.Text.Encoding.ASCII,true, true);
		// mi2.Name = "EdoBridge";
		// n1.AddChild(mi2);
	
		mi_dupe.Translate(new Vector3(10f,10f,10f));

*/
		Node n2 = new Node();
		n2.Name = "Route Objects";
		AddChild(n2);
				
		string rootResourcesPath = @"C:\users\danie\desktop\test";
        string routeFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Route"));
        string objectFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Object"));
        string trainFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Train"));
        string soundFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Sound"));

		//string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Route\Taipei Rapid Transit  Zhonghe Line-Down.csv");

		string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Route\test.csv");

        CsvRwRouteParser.ParseRoute(n2, routePath, Encoding.Default, false, trainFolder, objectFolder, "", false);
   
	}


}
