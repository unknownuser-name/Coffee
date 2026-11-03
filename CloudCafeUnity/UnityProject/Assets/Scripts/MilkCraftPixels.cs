using System;
using UnityEngine;
namespace CloudCafe.Crafting {
 // All vessel artwork belongs to this project. Liquid/stream pixels are generated separately.
 public sealed class MilkCraftPixels : IDisposable {
  public const int Width=640,Height=360;
  public Texture2D Texture {get;private set;}
  readonly Color32[] pixels=new Color32[Width*Height];
  readonly Color32[] back,front,cupBack,cupFront,cupShade,wood;
  readonly int woodW,woodH;
  readonly bool[] cupMask=new bool[192*192],pitcherMask=new bool[192*192];
  readonly float[] dye=new float[192*192];
  static readonly Color32 Milk=new Color32(247,238,210,255);
  float time;
  public MilkCraftPixels(){
   back=Read("image_24");front=Read("image_25");cupBack=Read("image_21");cupFront=Read("image_22");cupShade=Read("image_23");
   var counter=Resources.Load<Texture2D>("Cafe/revision_counter");wood=counter.GetPixels32();woodW=counter.width;woodH=counter.height;
   var data=JsonUtility.FromJson<SceneArt>(Resources.Load<TextAsset>("Cafe/scene_data").text);
   foreach(var row in data.cupMask)for(int x=row.a;x<=row.b;x++)cupMask[row.y*192+x]=true;
   foreach(var row in data.pitcherMask)for(int x=row.a;x<=row.b;x++)pitcherMask[row.y*192+x]=true;
   Texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
  }
  static Color32[] Read(string key){var t=Resources.Load<Texture2D>("Cafe/"+key);if(t.width!=192||t.height!=192)throw new Exception("Craft sprite size "+key);return t.GetPixels32();}
  public void Reset(){Array.Clear(dye,0,dye.Length);}
  public static float PitchY(float y,float angle){float a=angle*Mathf.Deg2Rad;return 100+(y-100)*(y<=100?Mathf.Cos(a):1-.18f*Mathf.Sin(a))+28*Mathf.Sin(a);}
  public static Vector2 Spout(float left,float top,float angle){return new Vector2(left+96,top+PitchY(163,angle));}
  void Pixel(int x,int y,Color32 c){if(x<0||x>=Width||y<0||y>=Height||c.a==0)return;int i=(Height-1-y)*Width+x;if(c.a==255)pixels[i]=c;else{var b=pixels[i];float a=c.a/255f;pixels[i]=new Color32((byte)(b.r+(c.r-b.r)*a),(byte)(b.g+(c.g-b.g)*a),(byte)(b.b+(c.b-b.b)*a),255);}}
  void Disc(int x,int y,int r,Color32 c){for(int dy=-r;dy<=r;dy++)for(int dx=-r;dx<=r;dx++)if(dx*dx+dy*dy<=r*r)Pixel(x+dx,y+dy,c);}
  void Line(Vector2 a,Vector2 b,int radius,Color32 c){int n=Mathf.CeilToInt(Vector2.Distance(a,b));for(int i=0;i<=n;i++){var p=Vector2.Lerp(a,b,n==0?0:i/(float)n);Disc(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),radius,c);}}
  void Blit(Color32[] src,int left,int top){for(int y=0;y<192;y++)for(int x=0;x<192;x++)Pixel(left+x,top+y,src[(191-y)*192+x]);}
  void Pitcher(Color32[] src,float left,float top,float angle){
   // Inverse sampling keeps the projected shell contiguous at every angle.
   float shift=28*Mathf.Sin(angle*Mathf.Deg2Rad),upper=Mathf.Max(.2f,Mathf.Cos(angle*Mathf.Deg2Rad)),lower=1-.18f*Mathf.Sin(angle*Mathf.Deg2Rad);
   for(int y=-5;y<220;y++){float sy=100+(y-100-shift)/(y<=100+shift?upper:lower);int iy=Mathf.RoundToInt(sy);if(iy<0||iy>=192)continue;for(int x=0;x<192;x++)Pixel(Mathf.RoundToInt(left)+x,Mathf.RoundToInt(top)+y,src[(191-iy)*192+x]);}
  }
  public void Render(MilkCraftModel m,float left,float top,float angle,float dt){
   time+=dt;
   for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){var c=wood[Mathf.Clamp(woodH-1-y*woodH/Height,0,woodH-1)*woodW+x*woodW/Width];float shade=.54f+.13f*(1-Mathf.Abs(x-320)/320f);pixels[(Height-1-y)*Width+x]=new Color32((byte)(c.r*shade),(byte)(c.g*shade),(byte)(c.b*shade),255);}
   // Ground shadows anchor both objects to the counter.
   for(int y=272;y<300;y++)for(int x=244;x<410;x++)if((x-327)*(x-327)/6800f+(y-286)*(y-286)/180f<1)Pixel(x,y,new Color32(18,23,37,75));
   Blit(cupBack,224,140);
   float amount=m.Cup/150f;Color coffee=Color.Lerp(new Color32(91,53,34,255),new Color32(191,139,88,255),amount);
   if(m.Cup>0&&m.Flying>.01f&&Mathf.Abs(m.LastImpactX-320)<48){
    float px=m.LastImpactX-224;float brushRadius=3+Mathf.Clamp(m.Flow/6,0,7);for(int y=78;y<123;y++)for(int x=35;x<157;x++){float d=(x-px)*(x-px)+(y-110)*(y-110)*2;if(d<brushRadius*brushRadius*4)dye[y*192+x]=Mathf.Clamp01(dye[y*192+x]+dt*.5f*Mathf.Exp(-d/(brushRadius*brushRadius)));}
   }
   float fill=(20+m.Cup)/170f,offset=(1-fill)*15,rx=44+8*fill,ry=31+20*fill;
   for(int y=0;y<192;y++)for(int x=0;x<192;x++)if(cupMask[y*192+x]){
    float ex=(x-96)/rx,ey=(y-98-offset)/ry;
    if(ex*ex+ey*ey>1){Pixel(224+x,140+y,new Color32(46,38,43,255));continue;}
    float distance=Mathf.Sqrt((x-96)*(x-96)+(y-110)*(y-110));
    float ring=Mathf.Sin(distance*.9f-time*14)*Mathf.Min(.025f,m.Flow*.001f)*Mathf.Exp(-distance/18);
    float white=Mathf.Floor(dye[y*192+x]*7)/7;
    Color c=Color.Lerp(coffee,Milk,white);c*=1+ring;Pixel(224+x,140+y,(Color32)c);
   }
   Blit(cupShade,224,140);
   int radius=m.Flow>28?2:1;
   // Adjacent airborne parcels form a continuous ribbon with a free trailing end after release.
   for(int i=0;i<m.Drops.Count;i++){var d=m.Drops[i];Vector2 p=new Vector2(d.X,d.Y);if(i+1<m.Drops.Count){var n=m.Drops[i+1];Line(p,new Vector2(n.X,n.Y),radius,Milk);}else if(m.Flow>0)Line(p,Spout(left,top,angle),radius,Milk);else Disc((int)d.X,(int)d.Y,1,Milk);}
   if(m.Flow>0&&m.Cup>0){int ix=Mathf.RoundToInt(m.LastImpactX);if(Mathf.Abs(ix-320)<48){Disc(ix,251,3,Milk);for(int i=0;i<4;i++){int sx=ix+Mathf.RoundToInt(Mathf.Sin(time*19+i*2)*8);int sy=250-Mathf.RoundToInt(Mathf.Abs(Mathf.Cos(time*15+i))*7);Pixel(sx,sy,Milk);}}}
   Blit(cupFront,224,140);
   Pitcher(back,left,top,angle);
   float level=139-80*m.Remaining/240f;
   for(int y=0;y<192;y++)for(int x=0;x<192;x++)if(pitcherMask[y*192+x]){int yy=Mathf.RoundToInt(top+PitchY(y,angle));var c=y<level?new Color32(31,49,72,255):y<level+2?new Color32(255,248,226,255):Milk;Pixel(Mathf.RoundToInt(left)+x,yy,c);Pixel(Mathf.RoundToInt(left)+x,yy+1,c);}
   Pitcher(front,left,top,angle);
   if(m.Spill>.1f){int spillX=Mathf.Clamp(Mathf.RoundToInt(m.LastImpactX),25,600);for(int y=297;y<306;y++)for(int x=spillX-8;x<spillX+Mathf.Min(35,(int)m.Spill);x++)if((x+y)%7!=0)Pixel(x,y,new Color32(225,215,187,190));}
   Texture.SetPixels32(pixels);Texture.Apply(false);
  }
  public void Dispose(){if(Texture)UnityEngine.Object.Destroy(Texture);}
 }
}
