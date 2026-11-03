using System;
namespace CloudCafe.Crafting {
 public sealed class PuckPrepModel {
  public float Rotation{get;private set;} public float Depth{get;private set;}
  public bool Leveled{get{return Rotation>=270;}} public bool Complete{get;private set;}
  bool leveling,tamping;float firmTime;
  public void BeginLevel(){if(!Complete)leveling=true;}
  public void Rotate(float degrees){if(leveling&&!float.IsNaN(degrees)&&!float.IsInfinity(degrees))Rotation=Math.Min(270,Rotation+Math.Abs(degrees));}
  public void EndLevel(){leveling=false;}
  public bool BeginTamp(){if(!Leveled||Complete)return false;tamping=true;firmTime=0;return true;}
  public void Press(float depth,float dt){if(!tamping||float.IsNaN(depth)||float.IsInfinity(depth))return;Depth=Math.Max(0,Math.Min(1,depth));if(!float.IsNaN(dt)&&!float.IsInfinity(dt)&&dt>0){if(Depth>=.85f)firmTime+=Math.Min(.05f,dt);else firmTime=0;}}
  public void Release(){if(tamping&&firmTime>=.25f)Complete=true;tamping=false;Depth=firmTime=0;}
  public void Reset(){Rotation=Depth=firmTime=0;leveling=tamping=Complete=false;}
 }
}
