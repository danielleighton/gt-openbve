using Godot;
using System;

public class Something : Node {

	public override void _Ready() {
	
		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\Compatibility\Signals\signal_4b_2.csv", System.Text.Encoding.ASCII,true, true);


		//MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\Camden\hb.csv", System.Text.Encoding.ASCII,true, true);

//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\Sjyou\People\Staff3.csv", System.Text.Encoding.ASCII,true, true);

//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\Sjyou\car\Bus_A.csv", System.Text.Encoding.ASCII,true, true);


//		MeshInstance mi = ObjectLoader.LoadFromFile(@"C:\Users\danie\Desktop\Test\Objects\reren\Sjyou\car\Estima.csv", System.Text.Encoding.ASCII,true, true);

		
		// Hello this is a program :) 
		AddChild(mi);
	}


}
