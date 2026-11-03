using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public sealed class BrewModel {
  public sealed class Parcel {public float Age,Amount;}
  public readonly List<Parcel> Parcels=new List<Parcel>();
  public const float Target=36,Travel=.22f;
  public bool Inserted{get;private set;} public bool Locked{get{return Inserted&&Angle>=39.9f;}}
  public float Angle{get;private set;} public bool CupDocked,Running;
  public float Pumped{get;private set;}public float InFlight{get;private set;}public float InCup{get;private set;}public float Spilled{get;private set;}
  float elapsed;
  public bool Complete{get{return InCup>=35.999f&&InFlight<.001f&&!Running;}}
  public bool Insert(bool prepared){if(!prepared||Inserted)return false;Inserted=true;return true;}
  public void Turn(float degrees){if(!Inserted||Running||Locked||float.IsNaN(degrees)||float.IsInfinity(degrees))return;Angle=Math.Max(0,Math.Min(40,Angle+degrees));}
  public bool TogglePump(){if(Running){Running=false;return true;}if(!Locked||!CupDocked||Pumped>=Target-.001f)return false;Running=true;return true;}
  public void Step(float dt){if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;dt=Math.Min(.05f,dt);
   for(int i=Parcels.Count-1;i>=0;i--){var p=Parcels[i];p.Age+=dt;if(p.Age<Travel)continue;InFlight-=p.Amount;if(CupDocked)InCup+=p.Amount;else Spilled+=p.Amount;Parcels.RemoveAt(i);}if(Parcels.Count==0)InFlight=0;
   if(!Running)return;if(!Locked||!CupDocked){Running=false;return;}elapsed+=dt;if(elapsed<1.2f)return;float amount=Math.Min(dt*4.5f,Math.Max(0,Target-Pumped));Pumped+=amount;InFlight+=amount;if(amount>0)Parcels.Add(new Parcel{Amount=amount});if(Pumped>=Target-.0001f)Running=false;
  }
  public void Reset(){Inserted=CupDocked=Running=false;Angle=Pumped=InFlight=InCup=Spilled=elapsed=0;Parcels.Clear();}
 }
}
