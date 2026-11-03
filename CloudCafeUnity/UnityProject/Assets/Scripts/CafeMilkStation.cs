using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MilkStudy;
namespace MilkStudy {
 public static class VesselGeometry {public static readonly Vector3 Anchor=new Vector3(0,.65f,-.28f);}
 [Serializable] public class MeshDocument {public MeshPart[] objects;}
 [Serializable] public class MeshPart {public string name;public float[] vertices,normals;public int[] triangles;}
}
namespace CloudCafe {
 public sealed class CafeMilkStation:MonoBehaviour {
  const int Layer=28;
  GameObject world;Transform pitcher;SphMilk solver;Camera cameraView;RenderTexture target;Transform[] particles;Material milk,steel,ceramic;float angle,dragY,dragAngle;bool priorDrag,active,finished;Vector3 location=new Vector3(0,2.15f,-.3f);
  public float Remaining {get;private set;}=240;public float InFlight {get;private set;}public float Collected {get;private set;}public float Spilled {get;private set;}
  public float Angle=>angle;public int ParticleCount=>solver==null?0:solver.Count;public int PhysicsSteps=>solver==null?0:solver.Steps;
  public bool CanFinish=>active&&angle<2&&InFlight<.5f&&Collected+Spilled>0;
  public bool CupAligned=>Mathf.Abs(location.x)<.35f;
  public bool UsesOriginalArtwork=>steel.GetTexture("_Paint")==Resources.Load<Texture2D>("Cafe/image_12")&&ceramic.GetTexture("_Paint")==Resources.Load<Texture2D>("Cafe/image_13");
  public RenderTexture Texture=>target;public bool Active=>active;
  public void Initialize(Camera main){
   main.cullingMask&=~(1<<Layer);world=new GameObject("Milk craft - isolated fluid layer");world.transform.SetParent(transform,false);
   steel=Painted("Cafe/image_12",new Color(.38f,.48f,.64f),false);ceramic=Painted("Cafe/image_13",new Color(.84f,.8f,.72f),true);milk=new Material(Shader.Find("MilkStudy/SPHMilk"));
   cameraView=new GameObject("Milk craft camera").AddComponent<Camera>();cameraView.transform.SetParent(world.transform,false);cameraView.transform.position=new Vector3(0,4.6f,6.5f);cameraView.transform.LookAt(new Vector3(0,1.15f,.3f));cameraView.orthographic=true;cameraView.orthographicSize=1.9f;cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=Color.clear;cameraView.cullingMask=1<<Layer;cameraView.allowMSAA=false;cameraView.enabled=false;
   target=new RenderTexture(440,270,24,RenderTextureFormat.ARGB32){filterMode=FilterMode.Point,antiAliasing=1};target.Create();cameraView.targetTexture=target;
   pitcher=new GameObject("Painted pitcher - original texture").transform;pitcher.SetParent(world.transform,false);pitcher.localScale=new Vector3(-1,1,1);ApplyPose();
   foreach(var part in JsonUtility.FromJson<MeshDocument>(Resources.Load<TextAsset>("pitcher_mesh").text).objects){var mesh=new Mesh();var v=new Vector3[part.vertices.Length/3];var normals=new Vector3[v.Length];for(int i=0;i<v.Length;i++){v[i]=new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2])-VesselGeometry.Anchor;normals[i]=new Vector3(part.normals[i*3],part.normals[i*3+1],part.normals[i*3+2]);}mesh.vertices=v;mesh.normals=normals;mesh.triangles=part.triangles;mesh.RecalculateBounds();var g=new GameObject(part.name);g.transform.SetParent(pitcher,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=steel;}
   Cup();solver=new SphMilk(pitcher);particles=new Transform[solver.Count];var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);var sphereMesh=sphere.GetComponent<MeshFilter>().sharedMesh;Destroy(sphere);
   for(int i=0;i<particles.Length;i++){var g=new GameObject("Milk particle "+i);g.transform.SetParent(world.transform,false);g.AddComponent<MeshFilter>().sharedMesh=sphereMesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=milk;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;g.transform.localScale=Vector3.one*.09f;particles[i]=g.transform;}
   foreach(var t in world.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;world.SetActive(false);
  }
  Material Painted(string source,Color basis,bool cup){var m=new Material(Shader.Find("CloudCafe/PaintedVessel"));m.SetTexture("_Paint",Resources.Load<Texture2D>(source));m.SetTexture("_TopPaint",Resources.Load<Texture2D>("FluidArt/pitcher_paint"));m.SetColor("_BaseColor",basis);m.SetFloat("_Cup",cup?1:0);return m;}
  void Cube(string name,Vector3 pos,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(world.transform,false);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=ceramic;}
  void Cup(){
   const int n=64;var profile=new[]{new Vector2(.47f,.04f),new Vector2(.59f,.82f),new Vector2(.54f,.82f),new Vector2(.43f,.1f),new Vector2(0,.1f)};var vertices=new List<Vector3>();var triangles=new List<int>();foreach(var p in profile)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;vertices.Add(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x+.95f));}for(int j=0;j<profile.Length-1;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n;triangles.AddRange(new[]{a,b,b+n,a,b+n,a+n});}var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();var g=new GameObject("Cup - existing ceramic paint");g.transform.SetParent(world.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=ceramic;
   Cube("Handle",new Vector3(-.85f,.43f,.95f),new Vector3(.1f,.5f,.12f));Cube("Handle top",new Vector3(-.72f,.65f,.95f),new Vector3(.35f,.1f,.12f));Cube("Handle base",new Vector3(-.69f,.22f,.95f),new Vector3(.35f,.1f,.12f));
   // Espresso remains a separate surface beneath the simulated milk.
   var coffee=GameObject.CreatePrimitive(PrimitiveType.Cylinder);coffee.name="Extracted espresso surface";coffee.transform.SetParent(world.transform,false);coffee.transform.position=new Vector3(0,.15f,.95f);coffee.transform.localScale=new Vector3(.83f,.008f,.83f);var c=new Material(Shader.Find("MilkStudy/SPHMilk"));c.color=new Color(.44f,.25f,.12f);coffee.GetComponent<Renderer>().sharedMaterial=c;
  }
  void ApplyPose(){pitcher.position=location;pitcher.rotation=Quaternion.Euler(angle,0,0);}
  public void SetStage(Phase stage){bool show=stage==Phase.Milk||stage==Phase.Ready;if(show&&!active){angle=0;location=new Vector3(0,2.15f,-.3f);ApplyPose();solver.Reset();Remaining=240;Collected=InFlight=Spilled=0;priorDrag=false;}active=show;finished=stage==Phase.Ready;world.SetActive(show);}
  public void Tick(float dt,float aim,bool held,bool drag,float pointerY){
   if(!active||finished)return;dt=Mathf.Clamp(dt,0,1/60f);
   if(drag&&!priorDrag){dragY=pointerY;dragAngle=angle;}float desired=drag?Mathf.Clamp(dragAngle+(pointerY-dragY)/2.1f,0,75):held?70:0;priorDrag=drag;
   location.x=Mathf.MoveTowards(location.x,Mathf.Clamp((aim-227)/100,-1.6f,1.6f),dt*1.4f);angle=Mathf.MoveTowards(angle,desired,dt*(held||drag?45:150));ApplyPose();solver.Step(dt,pitcher);ReadAmounts();
  }
  public void Release(){priorDrag=false;}
  static bool InsidePitcher(Vector3 p){float r=new Vector2(p.x,p.z).magnitude;float front=Mathf.Pow(Mathf.Clamp01(p.z/Mathf.Max(.001f,r)),24);float radius=p.y<.95f?Mathf.Lerp(.49f,.56f,Mathf.Clamp01((p.y-.14f)/.81f)):Mathf.Lerp(.56f,.58f,Mathf.Clamp01((p.y-.95f)/.23f));radius+=.22f*front*Mathf.Clamp01((p.y-.14f)/.81f);return p.y>.1f&&p.y<1.18f+.07f*front+.04f&&r<radius+.025f;}
  void ReadAmounts(){int inside=0,cup=0,flight=0,spill=0;foreach(var p in solver.Points){if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new Exception("Invalid fluid particle");var local=pitcher.InverseTransformPoint(p)+VesselGeometry.Anchor;if(InsidePitcher(local))inside++;else if(p.y<.85f&&p.y>.08f&&new Vector2(p.x,p.z-.95f).magnitude<Mathf.Lerp(.43f,.54f,Mathf.Clamp01((p.y-.1f)/.72f))-.019f)cup++;else if(p.y<.08f)spill++;else flight++;}float ml=240f/solver.Count;Remaining=inside*ml;Collected=cup*ml;InFlight=flight*ml;Spilled=spill*ml;}
  public void RenderNow(){if(!active)return;for(int i=0;i<particles.Length;i++)particles[i].position=solver.Points[i];cameraView.Render();}
  void LateUpdate(){RenderNow();}
  void OnDestroy(){solver?.Dispose();if(target){target.Release();Destroy(target);}if(steel)Destroy(steel);if(ceramic)Destroy(ceramic);if(milk)Destroy(milk);}
 }
}
