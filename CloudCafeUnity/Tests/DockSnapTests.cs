using System;
using CloudCafe.Crafting;
class DockSnapTests {
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static void Main(){Check(DockSnapModel.Near(.38f,-.59f),"visible approach catches forward offset");Check(DockSnapModel.Near(0,0),"center");Check(!DockSnapModel.Near(.9f,0),"other station not caught");Check(!DockSnapModel.Near(0,-1.2f),"too far toward player");Check(!DockSnapModel.Near(float.NaN,0),"invalid coordinate");Check(DockSnapModel.HoverSeconds(.1f,true,.05f)>=.15f,"stable approach dwell");Check(DockSnapModel.HoverSeconds(.1f,false,.05f)==0,"leave clears dwell");Console.WriteLine("DOCK_SNAP_TESTS_PASSED "+n);}
}
