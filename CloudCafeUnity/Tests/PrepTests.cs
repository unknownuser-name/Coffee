using System;
using CloudCafe.Crafting;
class PrepTests {
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static void Main(){var p=new PuckPrepModel();p.Rotate(720);Check(!p.Leveled,"rotation away from basket does nothing");Check(!p.BeginTamp(),"tamp requires leveling");p.BeginLevel();p.Rotate(130);p.Rotate(-140);Check(p.Leveled,"leveling accepts either direction and accumulates arc");p.EndLevel();Check(p.BeginTamp(),"tamp starts after leveling");p.Press(1,.1f);p.Release();Check(!p.Complete,"short press insufficient");p.BeginTamp();for(int i=0;i<10;i++)p.Press(.4f,.05f);p.Release();Check(!p.Complete,"light press insufficient");p.BeginTamp();for(int i=0;i<7;i++)p.Press(1,.05f);Check(!p.Complete,"lift before completion");p.Release();Check(p.Complete,"full press then lift completes");p.Reset();Check(!p.Complete&&!p.Leveled&&p.Rotation==0,"reset clears workflow");Console.WriteLine("PUCK_PREP_TESTS_PASSED "+n);}
}
