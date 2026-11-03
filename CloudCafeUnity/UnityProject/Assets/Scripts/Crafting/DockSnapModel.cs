using System;
namespace CloudCafe.Crafting {
 public static class DockSnapModel {
  public static bool Near(float dx,float dz){return !float.IsNaN(dx)&&!float.IsNaN(dz)&&Math.Abs(dx)<=.5f&&dz>=-.75f&&dz<=.32f;}
  public static float HoverSeconds(float current,bool near,float dt){return near?Math.Min(.2f,current+Math.Max(0,Math.Min(.05f,dt))):0;}
 }
}
