using System;
using CloudCafe.Crafting;
class HandCameraTests {
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static void Main(){Check(HandCameraModel.Next(0,.65f,5.5f,.016f)==0,"center stays still");float x=0;for(int i=0;i<60;i++)x=HandCameraModel.Next(x,2.5f,5.5f,.016f);Check(x>1.3f&&x<1.6f,"hand pulls view right without prep gate");for(int i=0;i<60;i++)x=HandCameraModel.Next(x,-.64f,5.5f,.016f);Check(x<.4f,"hand pulls view left");for(int i=0;i<120;i++)x=HandCameraModel.Next(x,10,5.5f,.016f);Check(x<=2.15f,"right limit");for(int i=0;i<120;i++)x=HandCameraModel.Next(x,-10,5.5f,.016f);Check(x>=-.25f,"left limit");Check(HandCameraModel.Next(x,2,5.5f,0)==x,"paused dt holds");Check(HandCameraModel.Next(x,float.NaN,5.5f,.016f)==x,"invalid hand input holds");Console.WriteLine("HAND_CAMERA_TESTS_PASSED "+n);}
}
