using Godot;
using System.Text;

public class SceneLoader : Node {

	public override void _Ready() {
		
		// Parent of test objects
		Node n1 = new Node();
		n1.Name = "Test Objects";
		AddChild(n1);

		// Test object 
		// MeshInstance mi = CsvB3dObjectParser.LoadFromFile(@"D:\LegacyContent\Railway\Object\Compatibility\Poles\pole_1.csv", System.Text.Encoding.ASCII, true, true);
		// mi.Name = "kitatono";
		// n1.AddChild(mi);
				
		string rootResourcesPath = @"D:\LegacyContent\Railway";
		string routeFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Route"));
		string objectFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Object"));
		string trainFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Train"));
		string soundFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootResourcesPath, "Sound"));

		//string routePath = System.IO.Path.Combine(routeFolder, @"Birmingham_Cross-City_South_BVE4\LowDetail\Day\LowDetail_158_Spring_2005_1200_Dry_Overcast.csv");

		string routePath = System.IO.Path.Combine(routeFolder, @"Animated Object Demonstration Route.csv");

		// string[] args = OS.GetCmdlineArgs();
		// if (args.Length > 0)
		// {
		// 	routePath = args[0];
		// }

		GetNode<Label>("/root/Scene1/lblInformation").SetText(routePath);

		Node n2 = new Node();
		n2.Name = "Route Objects";
		AddChild(n2);

		CsvRwRouteParser.ParseRoute(n2, routePath, Encoding.Default, false, trainFolder, objectFolder, "", false);

		
	}


}
