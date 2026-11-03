using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public sealed class MilkCraftModel {
  public sealed class Drop {public float X,Y,StartX,StartY,EndX,Age,Travel,Volume;}
  public readonly List<Drop> Drops=new List<Drop>();
  public float Remaining=240,Flying,Cup,Spill,Flow,LastImpactX=320;
  float lastTilt;
  public bool CanFinish {get{return lastTilt<2&&Flying<.001f&&Cup+Spill>0;}}
  public void Step(float dt,float tilt,float x,float y,float landingX){
   if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||float.IsNaN(tilt)||float.IsNaN(x)||float.IsNaN(y)||float.IsNaN(landingX)||float.IsInfinity(tilt)||float.IsInfinity(x)||float.IsInfinity(y)||float.IsInfinity(landingX))return;
   dt=Math.Min(dt,.05f);lastTilt=Math.Max(0,Math.Min(75,tilt));
   for(int i=Drops.Count-1;i>=0;i--){var d=Drops[i];d.Age+=dt;float t=Math.Min(1,d.Age/d.Travel);d.X=d.StartX+(d.EndX-d.StartX)*t;d.Y=d.StartY+(252-d.StartY)*t*t;
    if(t>=1){Flying-=d.Volume;float inCup=Math.Abs(d.EndX-320)<48?Math.Min(d.Volume,Math.Max(0,150-Cup)):0;Cup+=inCup;Spill+=d.Volume-inCup;LastImpactX=d.EndX;Drops.RemoveAt(i);}
   }
   if(Drops.Count==0)Flying=0;
   // A lower liquid level needs a steeper forward pitch to reach the spout.
   float threshold=28+(1-Remaining/240)*18;
   float strength=Math.Max(0,Math.Min(1,(lastTilt-threshold)/28));
   float rate=44*strength;
   const float depletion=44f*18/(240*28);
   float amount=Math.Min(Remaining,strength>0&&strength<1?rate/depletion*(1-(float)Math.Exp(-depletion*dt)):dt*rate);Flow=dt>0?amount/dt:0;
   if(amount>0){Remaining-=amount;Flying+=amount;Drops.Add(new Drop{X=x,Y=y,StartX=x,StartY=y,EndX=landingX,Volume=amount,Travel=(float)Math.Sqrt(Math.Max(8,252-y)*2/850)});}
  }
  public void Reset(){Remaining=240;Flying=Cup=Spill=Flow=lastTilt=0;Drops.Clear();LastImpactX=320;}
 }
}
