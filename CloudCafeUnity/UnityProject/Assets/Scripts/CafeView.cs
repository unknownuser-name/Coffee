using System;
using System.Collections.Generic;
using UnityEngine;
namespace CloudCafe {
 [Serializable] public class PixelRun {public int y,a,b;}
 [Serializable] public class ArtKey {public string id,key;}
 [Serializable] public class SceneLayer {public string id,key,group;public float x,y,w,h;public bool flipX;}
 [Serializable] public class SceneArt {public SceneLayer[] layers;public ArtKey[] keys;public PixelRun[] cupMask,pitcherMask,tiltMask;}
 public sealed class CafeView : MonoBehaviour {
  public bool PhysicalMilk {get;set;}
  public SceneArt Art {get;private set;}
  public int LoadedImages {get;private set;}
  readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
  readonly Dictionary<string,SpriteRenderer> main=new Dictionary<string,SpriteRenderer>();
  readonly Dictionary<string,SpriteRenderer> brew=new Dictionary<string,SpriteRenderer>();
  GameObject mainRoot,brewRoot,trayRoot;
  SpriteRenderer cupLiquid,pitcherLiquid,stream,ground,mainCupSurface,dim,groupHead;
  Texture2D cupTexture,pitcherTexture,groundTexture,headTexture;
  Color32[] cupPixels=new Color32[192*192],pitcherPixels=new Color32[192*192],groundPixels=new Color32[192*192];
  float surfaceClock,enterClock;bool wasPreparing;
  static readonly Color32 Clear=new Color32(0,0,0,0);
  // Crafting happens on a counter close-up that rises over the cafe; brew art keeps its own 640x360 coordinates and is shifted by this offset.
  public static readonly Vector2 BrewOffset=new Vector2(-84,55);
  const float TrayTop=185;
  public static Vector3 Position(float x,float y,float w=0,float h=0){return new Vector3((x+w/2-320)/100,(180-y-h/2)/100,0);}
  public void Initialize(){
   Art=JsonUtility.FromJson<SceneArt>(Resources.Load<TextAsset>("Cafe/scene_data").text);
   foreach(var k in Art.keys){var sprite=Resources.Load<Sprite>("Cafe/"+k.key);if(sprite==null)throw new Exception("Missing sprite: "+k.id);sprites[k.id]=sprite;LoadedImages++;}
   mainRoot=new GameObject("MAIN - independent artwork layers");mainRoot.transform.SetParent(transform);
   int order=0;
   foreach(var l in Art.layers){var sr=New(mainRoot,l.id,Resources.Load<Sprite>("Cafe/"+l.key),order*2);order++;Place(sr,l.x,l.y,l.w,l.h);sr.flipX=l.flipX;main[l.id]=sr;}
   var mainCoffee=new Texture2D(24,8,TextureFormat.RGBA32,false);mainCoffee.filterMode=FilterMode.Point;var coffeePixels=new Color32[24*8];for(int y=0;y<8;y++)for(int x=0;x<24;x++)if((x-11.5f)*(x-11.5f)/132f+(y-3.5f)*(y-3.5f)/12f<1)coffeePixels[y*24+x]=new Color32(211,167,112,255);mainCoffee.SetPixels32(coffeePixels);mainCoffee.Apply();
   mainCupSurface=New(mainRoot,"Served latte surface - code",Sprite.Create(mainCoffee,new Rect(0,0,24,8),new Vector2(.5f,.5f),100),main["cup"].sortingOrder+1);Place(mainCupSurface,269,260,18,5);mainCupSurface.enabled=false;
   var white=new Texture2D(1,1);white.SetPixel(0,0,Color.white);white.Apply();white.filterMode=FilterMode.Point;var whiteSprite=Sprite.Create(white,new Rect(0,0,1,1),new Vector2(.5f,.5f),100);
   dim=New(gameObject,"Cafe dim while crafting",whiteSprite,100);Place(dim,0,0,640,360);dim.color=new Color(.06f,.08f,.14f,.38f);dim.enabled=false;
   trayRoot=new GameObject("TRAY - counter close-up over the cafe");trayRoot.transform.SetParent(transform);
   var shadow=New(trayRoot,"Tray shadow",whiteSprite,109);Place(shadow,0,TrayTop-4,640,4);shadow.color=new Color(.04f,.05f,.09f,.45f);
   Place(New(trayRoot,"Counter close-up",sprites["revision_counter"],110),0,TrayTop,640,104);
   var lower=New(trayRoot,"Counter close-up lower",sprites["revision_counter"],110);Place(lower,0,TrayTop+104,640,104);lower.flipY=true;
   var lip=New(trayRoot,"Counter lip",whiteSprite,111);Place(lip,0,TrayTop,640,2);lip.color=new Color32(232,190,130,255);
   trayRoot.SetActive(false);
   brewRoot=new GameObject("TOP - separate containers and code liquids");brewRoot.transform.SetParent(transform);
   // Portafilter art is 128x160; keep its proportions so the grounds sit inside the basket opening.
   Create("portafilter_back",224,80,192,240,1);
   ground=Dynamic("Grounds",out groundTexture,3);Place(ground,224,80,192,192);
   Create("portafilter_rim",224,80,192,240,4);
   Create("cup_top0_back",224,140,192,192,2);
   cupLiquid=Dynamic("Coffee and milk - clipped surface",out cupTexture,3);Place(cupLiquid,224,140,192,192);
   Create("cup_top0_inner_shade",224,140,192,192,4);Create("cup_top0_front",224,140,192,192,5);
   Create("pitcher_top0_back",227,34,192,192,6);
   pitcherLiquid=Dynamic("Milk remaining - separate mask",out pitcherTexture,7);
   Create("pitcher_top0_front",227,34,192,192,8);
   stream=New(brewRoot,"Procedural flow - no baked milk sprite",whiteSprite,209);
   // Machine group head reaching into the close-up from above, drawn in code like the liquids.
   headTexture=new Texture2D(40,16,TextureFormat.RGBA32,false);headTexture.filterMode=FilterMode.Point;var head=new Color32[640];
   for(int y=0;y<16;y++)for(int x=0;x<40;x++){Color32 c=Clear;
    if(y<10&&x>=2&&x<38)c=y==0?new Color32(206,212,222,255):y==9||x==2||x==37?new Color32(70,78,96,255):new Color32(132,142,160,255);
    else if(y>=10&&((x>=15&&x<19)||(x>=21&&x<25)))c=y==15?new Color32(70,78,96,255):new Color32(160,168,184,255);
    head[(15-y)*40+x]=c;}
   headTexture.SetPixels32(head);headTexture.Apply();
   groupHead=New(brewRoot,"Group head - code",Sprite.Create(headTexture,new Rect(0,0,40,16),new Vector2(.5f,.5f),100),210);Place(groupHead,300,116,40,16);
   brewRoot.SetActive(false);
  }
  SpriteRenderer New(GameObject parent,string id,Sprite sprite,int order){var go=new GameObject(id);go.transform.SetParent(parent.transform);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;return sr;}
  // Brew art draws above the cafe dim (100) and the tray (109-111).
  SpriteRenderer Create(string id,float x,float y,float w,float h,int order){var sr=New(brewRoot,id,sprites[id],order+200);Place(sr,x,y,w,h);brew[id]=sr;return sr;}
  SpriteRenderer Dynamic(string id,out Texture2D texture,int order){texture=new Texture2D(192,192,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;return New(brewRoot,id,Sprite.Create(texture,new Rect(0,0,192,192),new Vector2(.5f,.5f),100),order+200);}
  static void Place(SpriteRenderer sr,float x,float y,float w,float h){sr.transform.localPosition=Position(Mathf.Round(x),Mathf.Round(y),w,h);sr.transform.localScale=new Vector3(w/(sr.sprite.rect.width),h/(sr.sprite.rect.height),1);}
  void SetVisible(string id,bool visible){brew[id].enabled=visible;}
  public static bool IsBrew(Phase phase){return phase==Phase.Grind||phase==Phase.Extract||phase==Phase.Milk||phase==Phase.Ready;}
  public void Render(CafeState s,Vector2 pitcher,bool held,float clock){
   bool preparing=IsBrew(s.Stage);brewRoot.SetActive(preparing);trayRoot.SetActive(preparing);dim.enabled=preparing;
   if(preparing&&!wasPreparing)enterClock=clock;wasPreparing=preparing;
   // The cafe and customer stay on screen while crafting, dimmed behind the counter close-up.
   foreach(var l in Art.layers){var sr=main[l.id];float x=l.x,y=l.y;float alpha=1;
    if(l.group=="sky"&&l.id!="sky")x+=Mathf.Round(Mathf.Sin(clock*.05f+l.x)*4);
    if(l.group=="whale"){x+=Mathf.Round(Mathf.Sin(clock*.07f)*3);y+=Mathf.Round(Mathf.Sin(clock*.8f));alpha=.85f;}
    if(l.group=="customer"){alpha=s.Stage==Phase.Arrival?Mathf.Min(1,s.Arrival/1.8f):s.Stage==Phase.Done?.45f:1;x+=(1-alpha)*55;}
    Place(sr,x,y,l.w,l.h);sr.color=new Color(1,1,1,alpha);
   }
   mainCupSurface.enabled=s.Stage==Phase.Reaction||s.Stage==Phase.Done;
   if(!preparing)return;
   float slide=Mathf.Round(Mathf.Pow(1-Mathf.Clamp01((clock-enterClock)/.22f),2)*220);
   trayRoot.transform.localPosition=new Vector3(0,-slide/100,0);
   brewRoot.transform.localPosition=new Vector3(BrewOffset.x/100,-(BrewOffset.y+slide)/100,0);
   groupHead.enabled=s.Stage==Phase.Extract;
   bool grinding=s.Stage==Phase.Grind;
   foreach(string id in new[]{"portafilter_back","portafilter_rim"})SetVisible(id,grinding);
   ground.enabled=grinding;
   bool physical=PhysicalMilk&&(s.Stage==Phase.Milk||s.Stage==Phase.Ready);
   foreach(string id in new[]{"cup_top0_back","cup_top0_inner_shade","cup_top0_front"})SetVisible(id,!grinding&&!physical);
   cupLiquid.enabled=!grinding&&!physical;
   bool pouring=s.Stage==Phase.Milk;
   SetVisible("pitcher_top0_back",pouring&&!physical);SetVisible("pitcher_top0_front",pouring&&!physical);pitcherLiquid.enabled=pouring&&!physical;
   bool tilt=pouring&&held&&s.Remaining>0;
   if(pouring){
    brew["pitcher_top0_back"].sprite=sprites[tilt?"pitcher_pour35_back":"pitcher_top0_back"];
    brew["pitcher_top0_front"].sprite=sprites[tilt?"pitcher_pour35_front":"pitcher_top0_front"];
    foreach(string id in new[]{"pitcher_top0_back","pitcher_top0_front"})Place(brew[id],pitcher.x,pitcher.y,192,192);
    Place(pitcherLiquid,pitcher.x,pitcher.y,192,192);
   }
   stream.enabled=(tilt&&!physical)||(s.Stage==Phase.Extract&&held);
   if(tilt){Place(stream,pitcher.x+92,pitcher.y+164,3,30);stream.color=new Color(1,.95f,.83f);}
   else if(stream.enabled){Place(stream,319,132,2,104);stream.color=new Color(.42f,.24f,.13f);}
   if(clock-surfaceClock<.075f)return;surfaceClock=clock;
   // Basket opening of the rim art, in this texture's pixels: centre (95.5,68), radii 37x25. Fills from the bottom up.
   if(grinding){Array.Clear(groundPixels,0,groundPixels.Length);for(int y=40;y<96;y++)for(int x=55;x<137;x++){float ex=(x-95.5f)/37,ey=(y-68)/25f;if(ex*ex+ey*ey<1&&y>=93-s.Ground*50)groundPixels[(191-y)*192+x]=new Color32((byte)(70+(x*13+y*7)%45),43,29,255);}groundTexture.SetPixels32(groundPixels);groundTexture.Apply();}
   else{
    Array.Clear(cupPixels,0,cupPixels.Length);float f=s.Milk/150;
    // During extraction the espresso pools outward from the centre as time passes.
    float pool=s.Stage==Phase.Extract?Mathf.Clamp01(s.ExtractTime/3):1;
    foreach(var r in Art.cupMask)for(int x=r.a;x<=r.b;x++){
     float radius=Mathf.Sqrt((x-96)*(x-96)+(r.y-96)*(r.y-96));if(pool<1&&radius>pool*56)continue;
     var c=Color32.Lerp(new Color32(111,65,37,255),new Color32(211,167,112,255),f);
     if(radius>48)c=new Color32(211,161,100,255);
     // Temporary foam marks are generated on the surface, never container artwork.
     // Heart: two lobes over a narrowing point.
     bool lobe=(x-90.5f)*(x-90.5f)+(r.y-94)*(r.y-94)<42||(x-101.5f)*(x-101.5f)+(r.y-94)*(r.y-94)<42;
     if(s.Milk>90&&(lobe||(r.y>=94&&r.y<=107&&Mathf.Abs(x-96)<=107-r.y)))c=new Color32(245,229,189,255);
     cupPixels[(191-r.y)*192+x]=c;
    }cupTexture.SetPixels32(cupPixels);cupTexture.Apply();
   }
   if(pouring){Array.Clear(pitcherPixels,0,pitcherPixels.Length);float level=142-(s.Remaining/240)*96;foreach(var r in tilt?Art.tiltMask:Art.pitcherMask){if(r.y<level)continue;for(int x=r.a;x<=r.b;x++)pitcherPixels[(191-r.y)*192+x]=new Color32(238,229,207,255);}pitcherTexture.SetPixels32(pitcherPixels);pitcherTexture.Apply();}
  }
  public void VerifyLiquidMasks(bool tilted){
   CheckMask(cupTexture,Art.cupMask);CheckMask(pitcherTexture,tilted?Art.tiltMask:Art.pitcherMask);
  }
  static void CheckMask(Texture2D texture,PixelRun[] rows){var allowed=new bool[192*192];foreach(var r in rows)for(int x=r.a;x<=r.b;x++)allowed[(191-r.y)*192+x]=true;var pixels=texture.GetPixels32();int painted=0;for(int i=0;i<pixels.Length;i++)if(pixels[i].a>0){painted++;if(!allowed[i])throw new Exception("Liquid escaped its opening mask");}if(painted==0)throw new Exception("Liquid texture unexpectedly empty");}
  void OnDestroy(){if(cupTexture)Destroy(cupTexture);if(pitcherTexture)Destroy(pitcherTexture);if(groundTexture)Destroy(groundTexture);if(headTexture)Destroy(headTexture);}
 }
}
