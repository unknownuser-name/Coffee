using System;
using CloudCafe.Crafting;
class EasyLatteTests {
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 static void Main(){float ax,az;EasyLatteModel.AssistedAim(1.3f,1.3f,out ax,out az);Check(ax*ax+az*az<=.723f,"corner aim stays inside circular cup");var m=new EasyLatteModel();m.Step(.05f,false,0,0,-1);Check(m.Remaining==180,"released Space does not pour");for(int i=0;i<20;i++)m.Step(.05f,true,1.2f,.3f,-1);Check(m.Cup>0&&m.Flying>0,"pour and flight");float left=m.Remaining;m.Step(.05f,false,0,0,-1);for(int i=0;i<10;i++)m.Step(.05f,false,0,0,-1);Check(m.Remaining==left&&m.Flying==0,"release stops source and finishes drops");for(int i=0;i<180;i++){m.Step(.05f,true,.4f,0,-1);Check(Math.Abs(m.Remaining+m.Flying+m.Cup-180)<.003f,"milk conserved");}Check(m.Complete&&Math.Abs(m.Cup-110)<.003f,"one cup auto stops");Check(m.Marks.Count>0,"near pour paints foam");Check(m.Height<.4f,"easy mode lowers spout");m.Reset();Check(!m.Complete&&m.Cup==0&&m.Remaining==180,"reset");Console.WriteLine("EASY_LATTE_TESTS_PASSED "+n);}
}
