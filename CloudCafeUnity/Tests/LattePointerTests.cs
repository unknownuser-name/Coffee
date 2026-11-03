using System;
using CloudCafe.Crafting;
class LattePointerTests {
 static int n;
 static void Check(bool b,string why){n++;if(!b){Console.WriteLine("FAIL: "+why);Environment.Exit(1);}}
 static void Main(){
  float x,z;EasyLatteModel.AssistedAim(.5f,-.4f,out x,out z);
  Check(Math.Abs(x-.5f)<.0001f&&Math.Abs(z+.4f)<.0001f,"pointer aim must not shrink deliberate movement");
  EasyLatteModel.AssistedAim(100,100,out x,out z);
  Check(x*x+z*z<=.723f,"aim remains inside cup");
  x=z=0;for(int i=0;i<1000;i++)EasyLatteModel.MoveAim(ref x,ref z,1,0);
  Check(Math.Abs(x-.85f)<.0001f,"unlimited mouse travel stays at visible rim");
  EasyLatteModel.MoveAim(ref x,ref z,-.1f,0);
  Check(Math.Abs(x-.75f)<.0001f,"reverse at rim has no invisible overshoot");
  for(int i=0;i<100;i++)EasyLatteModel.MoveAim(ref x,ref z,-.1f,.04f);
  Check(x<0&&z>0&&x*x+z*z<=.723f,"full travel reaches opposite side and stays circular");
  Console.WriteLine("LATTE_POINTER_TESTS_PASSED "+n);
 }
}
