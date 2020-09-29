using Godot;
using System.Text;

public class SceneLoader : Node {

	public override void _Ready() {
	
//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\Compatibility\Signals\signal_post.csv", System.Text.Encoding.ASCII,true, true);


//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\EDtozai\bridge\EdoBridge.csv", System.Text.Encoding.ASCII,true, true);

		//MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\Camden\hb.csv", System.Text.Encoding.ASCII,true, true);

	//	MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\Sjyou\People\Staff3.csv", System.Text.Encoding.ASCII,true, true);

//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\Sjyou\car\Bus_A.csv", System.Text.Encoding.ASCII,true, true);

		Node n1 = new Node();
		n1.Name = "Test Objects";
		AddChild(n1);

		MeshInstance mi = CsvB3dObjectParser.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\gaku\kitatono.csv", System.Text.Encoding.ASCII,true, true);
		mi.Name = "kitatono";
		n1.AddChild(mi);

		MeshInstance mi_dupe = (MeshInstance)mi.Duplicate((int)DuplicateFlags.UseInstancing);
		mi_dupe.Name = "kitatono dupe";
		n1.AddChild(mi_dupe);

		mi_dupe.Translate(new Vector3(10f,10f,10f));



		// MeshInstance mi2 = CsvB3dObjectParser.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\EDtozai\bridge\EdoBridge.csv", System.Text.Encoding.ASCII,true, true);
		// mi2.Name = "EdoBridge";
		// n1.AddChild(mi2);


		Node n2 = new Node();
		n2.Name = "Route Objects";
		AddChild(n2);
				
		string rootResourcesPath = @"C:\users\danie\desktop\test";
        string routeFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Routes"));
        string objectFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Objects"));
        string trainFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Trains"));
        string soundFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Sound"));

		//string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Routes\É╝×èÉ³üi21,38ö¡,ï}ìsüj[OpenBVE].csv");

		string routePath = System.IO.Path.Combine(routeFolder, @"C:\Users\danie\Desktop\Test\Routes\test.csv");

        CsvRwRouteParser.ParseRoute(n2, routePath, Encoding.Default, false, trainFolder, objectFolder, "", false);
   
	}


}
