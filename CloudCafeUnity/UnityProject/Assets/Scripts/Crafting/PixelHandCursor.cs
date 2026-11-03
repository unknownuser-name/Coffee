using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed class PixelHandCursor:System.IDisposable {
  readonly Camera camera;readonly Transform quad;readonly Renderer renderer;readonly Material material;readonly Texture2D[] textures=new Texture2D[4];
  public HandPose Pose{get;private set;}public Vector2 Anchor{get;private set;}public bool Visible=>renderer.enabled;
  public PixelHandCursor(Camera camera){this.camera=camera;for(int i=0;i<4;i++)textures[i]=Draw((HandPose)i);var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);obj.name="single pixel hand cursor";Object.Destroy(obj.GetComponent<Collider>());quad=obj.transform;quad.SetParent(camera.transform,false);renderer=obj.GetComponent<Renderer>();material=new Material(Shader.Find("CloudCafe/PixelHandCursor"));renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
  public void Set(HandPose pose,Vector2 viewport,bool show){Pose=pose;Anchor=viewport;renderer.enabled=show;if(!show)return;material.mainTexture=textures[(int)pose];
   bool grip=pose==HandPose.Grip||pose==HandPose.Press;Vector2 hotspot=grip?new Vector2(11,15):new Vector2(7,pose==HandPose.Open?3:6);
   Vector2 center=viewport+new Vector2((12-hotspot.x)/480f,(hotspot.y-14)/270f);
   quad.position=camera.ViewportToWorldPoint(new Vector3(center.x,center.y,camera.nearClipPlane+.05f));quad.localRotation=Quaternion.identity;
   float pixel=camera.orthographicSize*2/270;quad.localScale=new Vector3(24*pixel,28*pixel,1);
  }
  static Texture2D Draw(HandPose pose){var mask=new bool[24*28];
   System.Action<int,int,int,int> rect=(x,y,w,h)=>{for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)if(xx>=0&&xx<24&&yy>=0&&yy<28)mask[yy*24+xx]=true;};
   rect(6,12,13,11);rect(8,21,9,5);
   if(pose==HandPose.Open){rect(5,3,4,13);rect(10,1,4,14);rect(15,3,4,14);rect(19,7,4,12);rect(1,13,7,5);}
   else if(pose==HandPose.Hover){rect(5,6,4,10);rect(10,4,4,12);rect(15,6,4,11);rect(19,9,4,9);rect(2,13,6,6);}
   else {rect(4,8,5,10);rect(9,7,5,10);rect(14,8,5,10);rect(18,10,4,9);rect(3,14,7,6);}
   var data=new Color32[24*28];Color32 ink=new Color32(37,41,57,255),skin=new Color32(201,157,126,255),shade=new Color32(153,105,88,255),light=new Color32(237,197,151,255);
   for(int y=0;y<28;y++)for(int x=0;x<24;x++){if(!mask[y*24+x])continue;bool edge=x==0||x==23||y==0||y==27||!mask[y*24+x-1]||!mask[y*24+x+1]||!mask[(y-1)*24+x]||!mask[(y+1)*24+x];data[(27-y)*24+x]=edge?ink:x<9?light:x>17?shade:skin;}
   for(int y=24;y<28;y++)for(int x=6;x<19;x++)data[(27-y)*24+x]=y==24?light:ink;
   if(pose==HandPose.Grip||pose==HandPose.Press)for(int x=7;x<19;x++)data[(27-13)*24+x]=shade;
   var texture=new Texture2D(24,28,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="hand "+pose};texture.SetPixels32(data);texture.Apply();return texture;
  }
  public void Dispose(){Cursor.visible=true;foreach(var t in textures)if(t)Object.Destroy(t);if(material)Object.Destroy(material);if(quad)Object.Destroy(quad.gameObject);}
 }
}
