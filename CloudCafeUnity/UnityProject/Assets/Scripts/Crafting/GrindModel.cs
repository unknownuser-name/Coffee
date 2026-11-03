using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public enum CraftSlot {Invalid,Mat,Grinder,Held,Machine}
 public sealed class GrindModel {
  public sealed class Parcel {public float Age,Amount;}
  public readonly List<Parcel> Parcels=new List<Parcel>();
  public const float Travel=.22f;
  public float Beans=60,Flying,Grounds,Waste;
  public CraftSlot Slot=CraftSlot.Mat;
  CraftSlot origin=CraftSlot.Mat;
  public bool Running;
  public bool Ready {get{return Grounds>=17.99f&&Flying<.001f;}}
  public bool Complete {get{return Ready&&Slot==CraftSlot.Mat;}}
  public bool PickUp(){if(Slot==CraftSlot.Held)return false;if(Running){Running=false;return false;}if(Flying>.001f)return false;origin=Slot;Slot=CraftSlot.Held;return true;}
  public void Drop(CraftSlot slot){if(Slot!=CraftSlot.Held)return;Slot=slot==CraftSlot.Grinder||slot==CraftSlot.Mat||slot==CraftSlot.Machine?slot:origin;}
  public bool Toggle(){if(Running){Running=false;return true;}if(Slot!=CraftSlot.Grinder||Beans<=0||Grounds+Flying>=17.99f)return false;Running=true;return true;}
  public void Step(float dt){
   if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;dt=Math.Min(.05f,dt);
   for(int i=Parcels.Count-1;i>=0;i--){var p=Parcels[i];p.Age+=dt;if(p.Age<Travel)continue;Flying-=p.Amount;if(Slot==CraftSlot.Grinder)Grounds+=p.Amount;else Waste+=p.Amount;Parcels.RemoveAt(i);}
   if(Parcels.Count==0)Flying=0;
   if(Running){if(Slot!=CraftSlot.Grinder){Running=false;return;}float amount=Math.Min(Beans,Math.Min(dt*4.5f,Math.Max(0,18-Grounds-Flying)));Beans-=amount;Flying+=amount;if(amount>0)Parcels.Add(new Parcel{Amount=amount});if(Beans<=.0001f||Grounds+Flying>=17.999f)Running=false;}
  }
  public void Reset(){Beans=60;Flying=Grounds=Waste=0;Running=false;Slot=origin=CraftSlot.Mat;Parcels.Clear();}
 }
}
