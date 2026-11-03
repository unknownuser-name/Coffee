using System;
using System.IO;
using CloudCafe;
using UnityEngine;
public static class CafeVerification {
 static int assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception("CAFE TEST FAILED: "+message);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.02f;}
 static CafeState AtMilk(){var s=new CafeState();s.Tick(2);for(int i=0;i<4;i++)s.Advance();s.Start();s.Tick(2,true);s.Tick(3,true);s.Release();return s;}
 public static void Run(){
  assertions=0;
  var s=new CafeState();s.Start();Check(s.Stage==Phase.Arrival,"cannot skip arrival");s.Tick(2);Check(s.Stage==Phase.Dialogue,"arrival enters dialogue");
  for(int i=0;i<4;i++)s.Advance();Check(s.Stage==Phase.Order,"four dialogue lines lead to order");s.Start();s.Tick(2,true);Check(s.Stage==Phase.Extract,"grind leads to extraction");s.Tick(3,true);s.Release();Check(s.Stage==Phase.Milk&&Near(s.Extraction,1),"three-second extraction");
  Check(!s.Serve(),"unfinished drink cannot be served");s.Tick(5,true);s.Finish();Check(s.Serve(),"finished drink can be served");Check(s.Score==100&&s.Coins==140,"perfect drink scores and pays");Check(!s.Serve()&&s.Coins==140,"cannot pay twice");s.Advance();Check(s.Stage==Phase.Done,"reaction leads to closing");
  var next=new CafeState(s.Save());Check(next.Day==2&&next.Coins==140&&next.Stage==Phase.Arrival,"save advances exactly one day");
  s=AtMilk();s.Tick(1,true,false);Check(Near(s.Spill,30),"missed flow spills");s.Tick(20,true,true);Check(Near(s.Milk,150)&&Near(s.Remaining,0),"capacity and empty pitcher");Check(Near(s.Milk+s.Remaining+s.Spill,240),"milk volume conservation");Check(s.Stage==Phase.Ready,"empty pitcher ends pouring");
  s=AtMilk();s.Tick(1,true,false);s.Retry();Check(s.Stage==Phase.Grind&&Near(s.Spill,0)&&Near(s.Remaining,240),"retry resets drink");s.Tick(2,true);s.Tick(8,true);Check(s.Stage==Phase.Milk&&Near(s.Extraction,0),"overextraction auto stops");s.Tick(3,true,false);s.Tick(2,true,true);s.Finish();s.Serve();Check(s.Score<50,"poor preparation lowers score");
  s=new CafeState(new SaveData{day=-5,coins=-10});Check(s.Day==1&&s.Coins==120,"corrupt save values rejected");s.Tick(float.NaN,true);Check(s.Stage==Phase.Arrival,"invalid delta ignored");
  s=new CafeState();s.Tick(2);var ui=CafeUiRules.For(s);Check(ui.Dialogue&&!ui.Order&&!ui.Hold&&!ui.Steps,"conversation hides crafting actions");
  for(int i=0;i<4;i++)s.Advance();ui=CafeUiRules.For(s);Check(ui.Order&&ui.Recipe&&!ui.Hold,"order exposes recipe and make action");
  s.Start();ui=CafeUiRules.For(s);Check(ui.Steps&&ui.Hold&&ui.ActiveStep==0&&!ui.Finish,"grinding exposes current step only");s.Tick(2,true);ui=CafeUiRules.For(s);Check(ui.ActiveStep==1&&ui.Hold&&!ui.Serve,"extraction step");
  s.Tick(3,true);s.Release();ui=CafeUiRules.For(s);Check(ui.ActiveStep==2&&ui.Finish&&!ui.CanFinish,"finish is disabled before pouring");s.Tick(.5f,true);Check(CafeUiRules.For(s).CanFinish,"finish enabled after pouring");s.Finish();ui=CafeUiRules.For(s);Check(ui.Serve&&ui.Retry&&!ui.Hold&&!ui.Finish,"finished drink exposes serve and retry");s.Serve();ui=CafeUiRules.For(s);Check(ui.Dialogue&&!ui.Serve&&!ui.Hold,"reaction returns to conversation");
  s=AtMilk();Check(s.ApplyPhysicalMilk(120,20,80,20),"accept physical particle snapshot");Check(Near(s.Remaining,120)&&Near(s.InFlight,20)&&Near(s.Milk,80)&&Near(s.Spill,20),"physical snapshot values");s.Finish();Check(s.Stage==Phase.Milk,"cannot finish with milk in flight");
  Check(!s.ApplyPhysicalMilk(float.NaN,0,0,0)&&Near(s.Remaining,120),"invalid physical snapshot ignored");Check(!s.ApplyPhysicalMilk(240,0,50,0),"cannot create physical milk");Check(!s.ApplyPhysicalMilk(-1,0,200,41),"negative physical quantities rejected");
  Check(s.ApplyPhysicalMilk(90,0,150,0),"settled physical snapshot");s.Finish();Check(s.Stage==Phase.Ready,"settled milk can finish");Check(!s.ApplyPhysicalMilk(240,0,0,0),"physical snapshot cannot mutate finished drink");Check(s.Serve()&&s.Score==100,"physical milk connects to reward");
  s=AtMilk();Check(s.ApplyPhysicalMilk(20,0,180,40)&&Near(s.Milk,150)&&Near(s.Spill,70),"physical cup excess accounted");s.Retry();Check(s.InFlight==0&&s.Remaining==240,"retry clears physical milk");
  Directory.CreateDirectory("../Review");File.WriteAllText("../Review/model-tests.json","{\"passed\":true,\"assertions\":"+assertions+"}");Debug.Log("CAFE_MODEL_TESTS_PASSED "+assertions);
 }
}
