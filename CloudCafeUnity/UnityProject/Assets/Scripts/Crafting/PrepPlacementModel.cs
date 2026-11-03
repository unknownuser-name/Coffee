using System;
namespace CloudCafe.Crafting {
 public static class PrepPlacementModel {
  public static bool Near(float dx,float dz,float screenX,float screenY){
   if(float.IsNaN(dx)||float.IsNaN(dz)||float.IsNaN(screenX)||float.IsNaN(screenY))return false;
   return (Math.Abs(dx)<=.6f&&Math.Abs(dz)<=.9f)||(Math.Abs(screenX)<=.095f&&Math.Abs(screenY)<=.145f);
  }
 }
}
