using System;
using CloudCafe.Crafting;
class HandCursorTests {
 static void Main(){if(HandCursorModel.Resolve(false,false,false,false)!=HandPose.Open)throw new Exception("free hand");if(HandCursorModel.Resolve(false,false,true,false)!=HandPose.Hover)throw new Exception("grabbable hover");if(HandCursorModel.Resolve(true,false,true,false)!=HandPose.Grip)throw new Exception("successful grab closes hand");if(HandCursorModel.Resolve(false,true,true,false)!=HandPose.Press)throw new Exception("pressing pose");if(HandCursorModel.Resolve(true,true,true,true)!=HandPose.Open)throw new Exception("pause releases cursor pose");Console.WriteLine("HAND_CURSOR_TESTS_PASSED 5");}
}
