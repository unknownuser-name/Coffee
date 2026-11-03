using System;
using CloudCafe.Crafting;
class PrepPlacementTests {
 static void Main(){if(!PrepPlacementModel.Near(.45f,-.45f,.08f,.12f))throw new Exception("near basket should catch");if(!PrepPlacementModel.Near(.65f,-.9f,.06f,.03f))throw new Exception("visual overlap should catch despite depth");if(PrepPlacementModel.Near(1.2f,0,.2f,0))throw new Exception("far tool should return");if(PrepPlacementModel.Near(float.NaN,0,0,0))throw new Exception("invalid coordinates");Console.WriteLine("PREP_PLACEMENT_TESTS_PASSED 4");}
}
