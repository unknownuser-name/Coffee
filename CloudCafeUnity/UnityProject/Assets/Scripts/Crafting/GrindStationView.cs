using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed class GrindStationView:System.IDisposable {
  readonly Texture2D grinder,back,rim,wood,grounds;
  readonly Color32[] grainPixels=new Color32[128*160];
  float priorGround=-1;
  static Color32[] capture;
  readonly System.Collections.Generic.Dictionary<Texture2D,Color32[]> cached=new System.Collections.Generic.Dictionary<Texture2D,Color32[]>();
  public GrindStationView(){grinder=Resources.Load<Texture2D>("Cafe/revision_grinder");back=Resources.Load<Texture2D>("Cafe/image_30");rim=Resources.Load<Texture2D>("Cafe/image_31");wood=Resources.Load<Texture2D>("Cafe/revision_counter");grounds=new Texture2D(128,160,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};}
  static void Paint(int x,int y,Color32 c){if(x<0||x>=640||y<0||y>=360||c.a==0)return;int i=(359-y)*640+x;var b=capture[i];float a=c.a/255f;capture[i]=new Color32((byte)(b.r+(c.r-b.r)*a),(byte)(b.g+(c.g-b.g)*a),(byte)(b.b+(c.b-b.b)*a),255);}
  public static void Box(Rect r,Color c){if(capture!=null){for(int y=Mathf.FloorToInt(r.y);y<r.yMax;y++)for(int x=Mathf.FloorToInt(r.x);x<r.xMax;x++)Paint(x,y,(Color32)c);return;}var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
  void Image(Rect r,Texture2D texture,ScaleMode mode=ScaleMode.StretchToFill,bool alpha=true){
   if(capture==null){GUI.DrawTexture(r,texture,mode,alpha);return;}
   if(mode==ScaleMode.ScaleToFit){float s=Mathf.Min(r.width/texture.width,r.height/texture.height);r=new Rect(r.center.x-texture.width*s/2,r.center.y-texture.height*s/2,texture.width*s,texture.height*s);}
   Color32[] colors;if(texture==grounds)colors=grainPixels;else if(!cached.TryGetValue(texture,out colors)){colors=texture.GetPixels32();cached[texture]=colors;}
   for(int y=Mathf.Max(0,(int)r.y);y<Mathf.Min(360,r.yMax);y++)for(int x=Mathf.Max(0,(int)r.x);x<Mathf.Min(640,r.xMax);x++){int sx=Mathf.Clamp((int)((x-r.x)*texture.width/r.width),0,texture.width-1),sy=Mathf.Clamp((int)((y-r.y)*texture.height/r.height),0,texture.height-1);Paint(x,y,colors[(texture.height-1-sy)*texture.width+sx]);}
  }
  public byte[] Capture(GrindModel m,Vector2 position,float time){try{capture=new Color32[640*360];Draw(m,position,time,m.Slot==CraftSlot.Held);var t=new Texture2D(640,360,TextureFormat.RGBA32,false);t.SetPixels32(capture);t.Apply();var bytes=t.EncodeToPNG();Object.Destroy(t);return bytes;}finally{capture=null;}}
  void UpdateGrounds(float amount){if(Mathf.Abs(priorGround-amount)<.015f)return;priorGround=amount;System.Array.Clear(grainPixels,0,grainPixels.Length);
   for(int y=29;y<65;y++)for(int x=30;x<99;x++)if((x-64)*(x-64)/900f+(y-47)*(y-47)/256f<1)grainPixels[(159-y)*128+x]=new Color32(30,34,42,255);
   if(amount>.01f)for(int y=25;y<71;y++)for(int x=20;x<109;x++){float fill=Mathf.Clamp01(amount/18);float rx=27+fill*6,ry=12+fill*6;float dx=(x-64)/rx,dy=(y-48+(fill*3))/ry;if(dx*dx+dy*dy<1){int h=(x*73+y*131+x*y*17)&31;byte v=(byte)(h<5?105:h<14?73:54);grainPixels[(159-y)*128+x]=new Color32(v,(byte)(v*.59f),(byte)(v*.32f),255);}}
   grounds.SetPixels32(grainPixels);grounds.Apply(false);
  }
  public void Draw(GrindModel model,Vector2 position,float time,bool held){
   Image(new Rect(0,0,640,360),wood);Box(new Rect(0,0,640,360),new Color(.04f,.06f,.09f,.37f));
   // Stable workbench slots; only a restrained highlight indicates a valid drop.
   Box(new Rect(409,184,151,143),new Color(.045f,.055f,.065f,.93f));
   for(int y=189;y<323;y+=7)Box(new Rect(414,y,140,1),new Color(.25f,.28f,.29f,.4f));
   Box(new Rect(203,269,114,48),new Color(.07f,.08f,.1f,.85f));
   if(held){Box(new Rect(219,210,82,44),new Color(.74f,.83f,.68f,.2f));Box(new Rect(439,210,82,44),new Color(.74f,.83f,.68f,.2f));}
   float shake=model.Running?Mathf.Sin(time*75)*.6f:0;
   Image(new Rect(125+shake,32,120,240),grinder,ScaleMode.ScaleToFit,true);
   // Interim outlet attachment. The grinder artwork itself has no separated animated spout.
   Box(new Rect(232,173,31,10),new Color32(48,60,75,255));Box(new Rect(235,174,28,3),new Color32(169,183,196,255));Box(new Rect(256,180,9,16),new Color32(43,50,61,255));Box(new Rect(258,182,3,11),new Color32(125,139,154,255));
   if(model.Slot==CraftSlot.Grinder||held)Box(new Rect(position.x+24,position.y+133,88,6),new Color(0,0,0,.2f));
   Image(new Rect(position.x,position.y,128,160),back);
   // Empty basket interior is independent of the metal shell.
   UpdateGrounds(model.Grounds);Image(new Rect(position.x,position.y,128,160),grounds);
   Image(new Rect(position.x,position.y,128,160),rim);
   // Airborne quantities belong to the model; these dots only visualize their paths.
   int index=0;foreach(var p in model.Parcels){float t=p.Age/GrindModel.Travel;for(int j=0;j<3;j++){float x=260+Mathf.Sin(index*7+j*3)*3;float y=196+26*t*t+j;Box(new Rect(Mathf.Round(x),Mathf.Round(y),1,2),new Color32((byte)(120+j*15),75,38,255));}index++;}
  }
  public void Dispose(){if(grounds)Object.Destroy(grounds);}
 }
}
