using System;
namespace CloudCafe {
 public enum Phase { Arrival, Dialogue, Order, Grind, Extract, Milk, Ready, Reaction, Done }
 [Serializable] public class SaveData { public int version=1; public int day=1; public int coins=120; }
 public sealed class CafeState {
  public Phase Stage=Phase.Arrival;
  public int Day=1, Coins=120, Line, Score, Reward;
  public float Arrival, Ground, ExtractTime, Extraction, Remaining=240, Milk, Spill, InFlight;
  public bool ApplyPhysicalMilk(float remaining,float flight,float collected,float spilled){
   if(Stage!=Phase.Milk)return false;
   foreach(float v in new[]{remaining,flight,collected,spilled})if(float.IsNaN(v)||float.IsInfinity(v)||v<0)return false;
   if(Math.Abs(remaining+flight+collected+spilled-240)>.05f)return false;
   Remaining=remaining;InFlight=flight;Milk=Math.Min(150,collected);Spill=spilled+Math.Max(0,collected-150);return true;
  }
  public bool Paid;
  static float Clamp(float n,float min,float max){return Math.Max(min,Math.Min(max,n));}
  public CafeState(SaveData saved=null){if(saved!=null){Day=saved.day>0&&saved.day<100000?saved.day:1;Coins=saved.coins>=0&&saved.coins<1000000000?saved.coins:120;}}
  public void Tick(float dt,bool held=false,bool hit=true){
   if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
   if(Stage==Phase.Arrival){Arrival+=dt;if(Arrival>=1.8f)Stage=Phase.Dialogue;}
   else if(Stage==Phase.Grind&&held){Ground=Clamp(Ground+dt/2,0,1);if(Ground>=1)Stage=Phase.Extract;}
   else if(Stage==Phase.Extract&&held){ExtractTime=Math.Min(6,ExtractTime+dt);if(ExtractTime>=6)Release();}
   else if(Stage==Phase.Milk&&held){float flow=Math.Min(Remaining,dt*30);Remaining-=flow;float accepted=hit?Math.Min(flow,150-Milk):0;Milk+=accepted;Spill+=flow-accepted;if(Remaining<=0)Finish();}
  }
  public void Advance(){if(Stage==Phase.Dialogue){Line++;if(Line>=4)Stage=Phase.Order;}else if(Stage==Phase.Reaction)Stage=Phase.Done;}
  public void Start(){if(Stage==Phase.Order)Stage=Phase.Grind;}
  public void Release(){if(Stage==Phase.Extract&&ExtractTime>0){Extraction=Clamp(1-Math.Abs(ExtractTime-3)/3,0,1);Stage=Phase.Milk;}}
  public void Finish(){if(Stage==Phase.Milk&&Milk+Spill>0&&InFlight<.5f)Stage=Phase.Ready;}
  public void Retry(){if(Stage==Phase.Grind||Stage==Phase.Extract||Stage==Phase.Milk||Stage==Phase.Ready){Ground=ExtractTime=Extraction=Milk=Spill=InFlight=0;Score=Reward=0;Remaining=240;Stage=Phase.Grind;}}
  public bool Serve(){if(Stage!=Phase.Ready||Paid)return false;Score=(int)Math.Round(Clamp(40*Extraction+60*(1-Math.Abs(Milk-150)/150)-Math.Min(30,Spill/3),0,100),MidpointRounding.AwayFromZero);Reward=4+(int)Math.Round(Score*.16,MidpointRounding.AwayFromZero);Coins+=Reward;Paid=true;Stage=Phase.Reaction;return true;}
  public SaveData Save(){return new SaveData{day=Day+(Paid?1:0),coins=Coins};}
 }
}
