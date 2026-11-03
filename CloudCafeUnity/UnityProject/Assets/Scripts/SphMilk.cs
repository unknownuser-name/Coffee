using System;
using System.Collections.Generic;
using Seb.Helpers;
using UnityEngine;
namespace MilkStudy {
 // Uses SebLague Fluid-Sim SPH kernels, spatial hash and counting sort (MIT).
 // No particles are spawned at the outlet: all particles start inside the pitcher.
 public sealed class SphMilk:IDisposable {
  readonly ComputeShader cs;readonly SpatialHash hash;readonly List<ComputeBuffer> owned=new List<ComputeBuffer>();
  public readonly ComputeBuffer Positions;readonly ComputeBuffer velocities;
  public readonly Vector3[] Points;readonly Vector3[] initial;
  public int Count=>Points.Length;public int Steps;
  ComputeBuffer Buffer(int count,int stride){var b=new ComputeBuffer(count,stride);owned.Add(b);return b;}
  public SphMilk(Transform pitcher){
   if(!SystemInfo.supportsComputeShaders)throw new Exception("Compute shaders required for SPH fluid");
   cs=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("FluidCompute/FluidSim"));
   var points=new List<Vector3>();const float spacing=.075f;
   for(float y=.20f;y<1.00f;y+=spacing)for(float z=-.46f;z<.47f;z+=spacing)for(float x=-.46f;x<.47f;x+=spacing)if(x*x+z*z<.46f*.46f)points.Add(pitcher.TransformPoint(new Vector3(x,y,z)-VesselGeometry.Anchor));
   initial=points.ToArray();Points=new Vector3[initial.Length];Array.Copy(initial,Points,Count);
   hash=new SpatialHash(Count);Positions=Buffer(Count,12);velocities=Buffer(Count,12);var predicted=Buffer(Count,12);var density=Buffer(Count,8);var sp=Buffer(Count,12);var sv=Buffer(Count,12);var spr=Buffer(Count,12);var debug=Buffer(Count,12);var foam=Buffer(1,32);var counts=Buffer(4096,4);
   var bindings=new Dictionary<string,ComputeBuffer>{{"Positions",Positions},{"Velocities",velocities},{"PredictedPositions",predicted},{"Densities",density},{"SpatialKeys",hash.SpatialKeys},{"SpatialOffsets",hash.SpatialOffsets},{"SortedIndices",hash.SpatialIndices},{"SortTarget_Positions",sp},{"SortTarget_Velocities",sv},{"SortTarget_PredictedPositions",spr},{"Debug",debug},{"WhiteParticles",foam},{"WhiteParticleCounters",counts}};
   for(int k=0;k<8;k++)foreach(var pair in bindings)cs.SetBuffer(k,pair.Key,pair.Value);
   cs.SetInt("numParticles",Count);cs.SetInt("MaxWhiteParticleCount",1);cs.SetVector("trappedAirParams",Vector4.zero);cs.SetVector("kineticEnergyParams",new Vector4(1,2,0,0));
   float h=.16f;cs.SetFloat("smoothingRadius",h);cs.SetFloat("K_SpikyPow2",15/(2*Mathf.PI*Mathf.Pow(h,5)));cs.SetFloat("K_SpikyPow3",15/(Mathf.PI*Mathf.Pow(h,6)));cs.SetFloat("K_SpikyPow2Grad",15/(Mathf.PI*Mathf.Pow(h,5)));cs.SetFloat("K_SpikyPow3Grad",45/(Mathf.PI*Mathf.Pow(h,6)));
   cs.SetFloat("gravity",-9.81f);cs.SetFloat("targetDensity",2400);cs.SetFloat("pressureMultiplier",30);cs.SetFloat("nearPressureMultiplier",1.8f);cs.SetFloat("viscosityStrength",.003f);cs.SetFloat("collisionDamping",.05f);
   var bounds=Matrix4x4.TRS(new Vector3(0,2.5f,0),Quaternion.identity,new Vector3(9,5,8));cs.SetMatrix("localToWorld",bounds);cs.SetMatrix("worldToLocal",bounds.inverse);cs.SetVector("vesselAnchor",VesselGeometry.Anchor);cs.SetVector("cupCenter",new Vector3(0,0,.95f));Reset();
  }
  public void Reset(){Positions.SetData(initial);velocities.SetData(new Vector3[Count]);Array.Copy(initial,Points,Count);Steps=0;}
  void Dispatch(int kernel){cs.Dispatch(kernel,(Count+255)/256,1,1);}
  public void Step(float dt,Transform pitcher){
   cs.SetMatrix("pitcherLocalToWorld",pitcher.localToWorldMatrix);cs.SetMatrix("pitcherWorldToLocal",pitcher.worldToLocalMatrix);cs.SetFloat("deltaTime",dt/6);
   for(int i=0;i<6;i++){cs.SetFloat("simTime",Steps*dt+i*dt/6);Dispatch(0);Dispatch(1);hash.Run();Dispatch(2);Dispatch(3);Dispatch(4);Dispatch(5);Dispatch(6);Dispatch(7);}Steps++;Positions.GetData(Points);
  }
  public void Dispose(){foreach(var b in owned)b.Release();hash.Release();UnityEngine.Object.Destroy(cs);}
 }
}


