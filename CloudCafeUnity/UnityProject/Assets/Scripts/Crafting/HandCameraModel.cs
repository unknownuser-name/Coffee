using System;
namespace CloudCafe.Crafting {
 public static class HandCameraModel {
  public static float Next(float cameraX,float handX,float viewWidth,float dt){
   if(float.IsNaN(handX)||float.IsInfinity(handX)||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||viewWidth<=0)return cameraX;
   float belt=viewWidth*.18f,delta=handX-cameraX;
   if(Math.Abs(delta)<=belt)return cameraX;
   float target=Math.Max(-.25f,Math.Min(2.15f,handX-Math.Sign(delta)*belt));
   float step=3*Math.Min(.05f,dt);return cameraX+Math.Max(-step,Math.Min(step,target-cameraX));
  }
 }
}
