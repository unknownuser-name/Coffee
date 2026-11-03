using UnityEngine;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public sealed partial class FrontGrindArt {
  readonly Dictionary<EasyLatteModel.Parcel,Vector3> milkOrigins=new Dictionary<EasyLatteModel.Parcel,Vector3>();
  public Transform LattePitcher;Transform pitcherMilk,cupMilk,milkStream,milkChannel,aimDot;readonly List<Transform> milkDrops=new List<Transform>();Texture2D latteTexture;float finishOffset;
  public Vector3 LatteSpout=>LattePitcher.TransformPoint(new Vector3(-.4f,.515f,0));
  public Vector3 LatteImpact(EasyLatteModel m,float x,float z){float ax,az;EasyLatteModel.AssistedAim(x,z,out ax,out az);return new Vector3(Cup.position.x+ax*.13f,LatteLevel(m),Cup.position.z+az*.13f);}
  float LatteLevel(EasyLatteModel m){return Cup.position.y+.035f+.17f*(36+m.Cup)/170;}
  Transform Surface(string name,Material mat){var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(root,false);o.GetComponent<MeshFilter>().sharedMesh=new Mesh();o.GetComponent<MeshRenderer>().sharedMaterial=mat;return o.transform;}
  public void EnsureLatte(){if(LattePitcher!=null)return;
   var milk=MatColor("warm milk","#FFF8E6");LattePitcher=new GameObject("milk pitcher controlled by Space").transform;LattePitcher.SetParent(root,false);
   var shell=Lathe("pitcher shell",LattePitcher,Vector3.zero,new Vector2[]{new Vector2(.19f,0),new Vector2(.27f,.48f),new Vector2(.245f,.48f),new Vector2(.175f,.04f),new Vector2(0,.04f)},steel,32);SpoutShape(shell);
   var lip=Ring("pitcher lip",LattePitcher,new Vector3(0,.48f,0),.272f,.245f,.009f,light);SpoutShape(lip);
   var handle=Ring("pitcher handle",LattePitcher,new Vector3(.34f,.27f,0),.18f,.12f,.04f,steel);handle.localRotation=Quaternion.Euler(90,0,0);
   pitcherMilk=Surface("independent horizontal pitcher milk",milk);cupMilk=Surface("latte surface",MatColor("latte texture", "#FFFFFF"));latteTexture=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};cupMilk.GetComponent<Renderer>().sharedMaterial.mainTexture=latteTexture;
   milkStream=Cylinder("continuous milk from spout",root,Vector3.zero,.012f,1,milk);
   milkChannel=Surface("milk inside spout",milk);milkChannel.SetParent(LattePitcher,false);var mesh=milkChannel.GetComponent<MeshFilter>().sharedMesh;mesh.vertices=new[]{new Vector3(-.23f,.484f,-.027f),new Vector3(-.405f,.518f,-.021f),new Vector3(-.405f,.518f,.021f),new Vector3(-.23f,.484f,.027f)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();
   aimDot=Ring("pour aim assistance",root,Vector3.zero,.018f,.011f,.003f,gold);
   for(int i=0;i<40;i++){var d=Cube("last milk drops",root,Vector3.zero,new Vector3(.014f,.025f,.014f),milk);d.gameObject.SetActive(false);milkDrops.Add(d);}
  }
  void SpoutShape(Transform part){var mesh=part.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){var p=vertices[i];float height=p.y+part.localPosition.y;float radius=Mathf.Sqrt(p.x*p.x+p.z*p.z);if(radius>.01f&&height>.35f){float w=Mathf.Clamp01((-p.x/radius-.78f)/.22f)*Mathf.Clamp01((height-.35f)/.13f);p.x-=.13f*w;p.y+=.035f*w;vertices[i]=p;}}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();}
  // Intersect the tilted vessel interior with a world-horizontal plane.
  void LiquidCap(Transform surface,Transform vessel,float bottom,float top,float low,float high,float level,float uvRadius){
   var points=new List<Vector3>();var lo=new Vector3[24];var hi=new Vector3[24];for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;lo[i]=vessel.TransformPoint(new Vector3(Mathf.Cos(a)*bottom,low,Mathf.Sin(a)*bottom));hi[i]=vessel.TransformPoint(new Vector3(Mathf.Cos(a)*top,high,Mathf.Sin(a)*top));}
   System.Action<Vector3,Vector3> edge=(a,b)=>{float da=a.y-level,db=b.y-level;if((da<=0&&db>=0)||(da>=0&&db<=0)){if(Mathf.Abs(a.y-b.y)<.00001f)return;var p=Vector3.Lerp(a,b,(level-a.y)/(b.y-a.y));if(!points.Exists(v=>(v-p).sqrMagnitude<.0000001f))points.Add(p);}};
   for(int i=0;i<24;i++){int j=(i+1)%24;edge(lo[i],hi[i]);edge(lo[i],lo[j]);edge(hi[i],hi[j]);}
   var mesh=surface.GetComponent<MeshFilter>().sharedMesh;mesh.Clear();if(points.Count<3)return;Vector3 center=Vector3.zero;foreach(var p in points)center+=p;center/=points.Count;points.Sort((a,b)=>Mathf.Atan2(a.z-center.z,a.x-center.x).CompareTo(Mathf.Atan2(b.z-center.z,b.x-center.x)));
   var uv=new Vector2[points.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(.5f+(points[i].x-vessel.position.x)/(uvRadius*2),.5f+(points[i].z-vessel.position.z)/(uvRadius*2));var triangles=new List<int>();for(int i=1;i<points.Count-1;i++){triangles.Add(0);triangles.Add(i+1);triangles.Add(i);}mesh.SetVertices(points);mesh.SetTriangles(triangles,0);mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();
  }
  void PaintLatte(EasyLatteModel m){var colors=new Color32[4096];Color baseColor=Color.Lerp(new Color32(104,58,30,255),new Color32(187,129,74,255),Mathf.Clamp01(m.Cup/70));for(int i=0;i<colors.Length;i++)colors[i]=baseColor;float sum=0;foreach(var mark in m.Marks){sum+=mark.Volume;int cx=Mathf.RoundToInt(32+mark.X*24),cy=Mathf.RoundToInt(32+mark.Z*24);float radius=Mathf.Min(10,2+Mathf.Sqrt(sum)*.8f);for(int y=Mathf.Max(0,(int)(cy-radius));y<Mathf.Min(64,cy+radius);y++)for(int x=Mathf.Max(0,(int)(cx-radius));x<Mathf.Min(64,cx+radius);x++)if((x-cx)*(x-cx)+(y-cy)*(y-cy)<radius*radius)colors[y*64+x]=new Color32(255,246,222,255);}latteTexture.SetPixels32(colors);latteTexture.Apply(false);}
  public void UpdateLatteArt(EasyLatteModel m,float x,float z){
   Cup.rotation=Quaternion.Euler(0,180,22*(1-m.Cup/110));CoffeeSurface.gameObject.SetActive(false);Vector3 to=LatteImpact(m,x,z);LattePitcher.rotation=Quaternion.Euler(0,0,m.Tilt);Vector3 spout=to+new Vector3(.015f,.075f+.4f*m.Height,0);finishOffset=Mathf.MoveTowards(finishOffset,m.Complete?.35f:0,Time.deltaTime*1.5f);LattePitcher.position=spout-LattePitcher.rotation*new Vector3(-.4f,.515f,0)+Vector3.right*finishOffset;
   float level=LattePitcher.TransformPoint(new Vector3(0,Mathf.Lerp(.055f,.42f,m.Remaining/180),0)).y;LiquidCap(pitcherMilk,LattePitcher,.175f,.245f,.04f,.475f,level,.27f);LiquidCap(cupMilk,Cup,.095f,.14f,.025f,.23f,LatteLevel(m),.16f);PaintLatte(m);
   var stale=new List<EasyLatteModel.Parcel>();foreach(var key in milkOrigins.Keys)if(!m.Parcels.Contains(key))stale.Add(key);foreach(var key in stale)milkOrigins.Remove(key);
   milkStream.gameObject.SetActive(m.Flow>0);milkChannel.gameObject.SetActive(m.Flow>0);milkStream.position=(LatteSpout+to)/2;milkStream.rotation=Quaternion.FromToRotation(Vector3.up,LatteSpout-to);float width=Mathf.Lerp(.55f,1.1f,1-m.Height);milkStream.localScale=new Vector3(width,Vector3.Distance(LatteSpout,to),width);
   aimDot.gameObject.SetActive(m.Flow==0&&!m.Complete);aimDot.position=to+Vector3.up*.006f;for(int i=0;i<milkDrops.Count;i++)milkDrops[i].gameObject.SetActive(false);int n=0;foreach(var p in m.Parcels){if(n>=milkDrops.Count)break;var d=milkDrops[n++];d.gameObject.SetActive(true);float t=Mathf.Clamp01(p.Age/p.Travel);var end=new Vector3(Cup.position.x+p.X*.13f,LatteLevel(m),Cup.position.z+p.Z*.13f);Vector3 start;if(!milkOrigins.TryGetValue(p,out start)){start=LatteSpout;milkOrigins[p]=start;}d.position=Vector3.Lerp(start,end,t*t);}
  }
  public void SetLatteVisible(bool show){if(LattePitcher==null)return;LattePitcher.gameObject.SetActive(show);pitcherMilk.gameObject.SetActive(show);cupMilk.gameObject.SetActive(show);milkStream.gameObject.SetActive(show);aimDot.gameObject.SetActive(show);if(!show)foreach(var d in milkDrops)d.gameObject.SetActive(false);}
 }
}
