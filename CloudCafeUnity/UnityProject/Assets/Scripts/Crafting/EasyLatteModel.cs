using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public sealed class EasyLatteModel {
  public sealed class Parcel {public float Age,Travel,Volume,X,Z;public bool White;}
  public sealed class Mark {public float X,Z,Volume;}
  public readonly List<Parcel> Parcels=new List<Parcel>();public readonly List<Mark> Marks=new List<Mark>();
  public float Remaining=180,Flying,Cup,Tilt,Height=.75f,Flow;
  public bool Complete{get{return Cup>=109.999f&&Flying<.001f;}}
  public static void AssistedAim(float x,float z,out float ax,out float az){ax=x;az=z;float r=(float)Math.Sqrt(ax*ax+az*az);if(r>.85f){ax*=.85f/r;az*=.85f/r;}}
  // Clamp the stored pointer too: reversing at the rim must respond immediately.
  public static void MoveAim(ref float x,ref float z,float dx,float dz){AssistedAim(x+dx,z+dz,out x,out z);}
  public void Step(float dt,bool pour,float x,float z,float manualHeight){
   if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||float.IsNaN(x)||float.IsNaN(z)||float.IsNaN(manualHeight))return;dt=Math.Min(.05f,dt);
   float desiredHeight=manualHeight<0?(Cup<25?.75f:.22f):Math.Max(.05f,Math.Min(1,manualHeight));Height+=Math.Max(-1.8f*dt,Math.Min(1.8f*dt,desiredHeight-Height));
   bool active=pour&&Cup+Flying<109.999f;float target=active?58:0;Tilt+=Math.Max(-260*dt,Math.Min(260*dt,target-Tilt));
   for(int i=Parcels.Count-1;i>=0;i--){var p=Parcels[i];p.Age+=dt;if(p.Age<p.Travel)continue;Flying-=p.Volume;Cup+=p.Volume;if(p.White)Marks.Add(new Mark{X=p.X,Z=p.Z,Volume=p.Volume});Parcels.RemoveAt(i);}if(Parcels.Count==0)Flying=0;
   float amount=active&&Tilt>=30?Math.Min(Remaining,Math.Min(30*dt,Math.Max(0,110-Cup-Flying))):0;Flow=amount/dt;
   if(amount>0){float ax,az;AssistedAim(x,z,out ax,out az);Remaining-=amount;Flying+=amount;Parcels.Add(new Parcel{Travel=.13f+.22f*Height,Volume=amount,X=ax,Z=az,White=Cup>=25&&Height<.43f});}
  }
  public void Reset(){Remaining=180;Flying=Cup=Tilt=Flow=0;Height=.75f;Parcels.Clear();Marks.Clear();}
 }
}
