using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed class MilkCraftController:MonoBehaviour {
  public readonly MilkCraftModel Model=new MilkCraftModel();
  MilkCraftPixels view;Font font;AudioSource sound;Rect frame;
  float left=330,top=10,angle,desired,grabY,grabAngle,grabX,grabLeft,time;
  bool dragging,finished,smoke,paused;string review;
  void Awake(){
   Application.targetFrameRate=60;Application.runInBackground=true;
   smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-milkCraftSmoke")>=0;
   view=new MilkCraftPixels();font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);
   sound=gameObject.AddComponent<AudioSource>();sound.clip=MakeSound();sound.loop=true;sound.volume=0;sound.Play();
   if(Camera.main)Camera.main.backgroundColor=new Color(.07f,.08f,.11f);
   review=Path.GetFullPath(Path.Combine(Application.dataPath,"../../MilkCraft/Review"));
   if(smoke){Directory.CreateDirectory(review);Application.logMessageReceived+=LogError;}
  }
  IEnumerator Start(){if(smoke)yield return Smoke();}
  AudioClip MakeSound(){int n=22050;var samples=new float[n];var random=new System.Random(527);float filtered=0;for(int i=0;i<n;i++){filtered=Mathf.Lerp(filtered,(float)(random.NextDouble()*2-1),.09f);samples[i]=filtered*.3f+Mathf.Sin(i*.08f)*.007f;}var c=AudioClip.Create("Generated gentle pouring",n,1,22050,false);c.SetData(samples,0);return c;}
  void Layout(){float s=Mathf.Max(1,Mathf.Floor(Mathf.Min(Screen.width/640f,Screen.height/360f)));frame=new Rect((Screen.width-640*s)/2,(Screen.height-360*s)/2,640*s,360*s);}
  Vector2 Pointer(){return new Vector2((Input.mousePosition.x-frame.x)/frame.width*640,(Screen.height-Input.mousePosition.y-frame.y)/frame.height*360);}
  void Update(){
   Layout();float dt=Mathf.Min(Time.deltaTime,.05f);time+=dt;
   if(!smoke){
    if(Input.GetKeyDown(KeyCode.R))ResetDrink();if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;dragging=false;desired=0;}
    if(!paused&&!finished){var p=Pointer();
     if(Input.GetMouseButtonDown(0)&&p.x>left+30&&p.x<left+180&&p.y>top+20&&p.y<top+190){dragging=true;grabY=p.y;grabX=p.x;grabAngle=angle;grabLeft=left;}
     if(!Input.GetMouseButton(0))dragging=false;
     if(dragging){left=Mathf.Clamp(grabLeft+p.x-grabX,100,440);desired=Mathf.Clamp(grabAngle+(p.y-grabY)*.9f,0,75);}
     else desired=Input.GetKey(KeyCode.Space)?70:0;
     left+=((Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0))*dt*95;
     left=Mathf.Clamp(left,100,440);
    }
   }
   if(!paused&&!finished)Simulate(dt);
   sound.volume=paused||finished?0:Mathf.Lerp(sound.volume,Model.Flow/44*.22f,dt*9);
   view.Render(Model,left,top,angle,paused?0:dt);
  }
  void Simulate(float dt){angle=Mathf.MoveTowards(angle,desired,dt*(desired>angle?65:135));var spout=MilkCraftPixels.Spout(left,top,angle);Model.Step(dt,angle,spout.x,spout.y,spout.x);}
  void OnApplicationFocus(bool focus){if(!focus&&!smoke){dragging=false;desired=0;}}
  void ResetDrink(){Model.Reset();view.Reset();left=330;top=10;angle=desired=0;dragging=finished=false;}
  void OnGUI(){
   Layout();GUI.DrawTexture(frame,view.Texture,ScaleMode.StretchToFill,false);
   var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(frame.x,frame.y,0),Quaternion.identity,Vector3.one*(frame.width/640));
   var label=new GUIStyle(GUI.skin.label){font=font,fontSize=12,normal={textColor=new Color(1,.94f,.81f)}};
   GUI.color=new Color(.06f,.08f,.13f,.9f);GUI.DrawTexture(new Rect(0,0,640,28),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,331,640,29),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(12,5,310,22),"구름 위의 카페 · 직접 우유 붓기",label);
   GUI.Label(new Rect(395,5,235,22),"피처 "+Model.Remaining.ToString("0")+"ml   컵 +"+Model.Cup.ToString("0")+"ml",label);
   GUI.Label(new Rect(10,335,450,22),finished?"완료 · 손님에게 내어줄 한 잔이 준비됐어요.":paused?"잠시 쉬는 중 · Esc로 계속":"피처 잡고 아래로 드래그 · ← → 이동 / Space 기울이기",label);
   var button=new GUIStyle(GUI.skin.button){font=font,fontSize=11};
   if(GUI.Button(new Rect(477,335,59,21),"다시 R",button))ResetDrink();
   GUI.enabled=Model.CanFinish&&!finished&&!paused;
   if(GUI.Button(new Rect(545,335,84,21),"붓기 완료",button)){finished=true;Model.Flow=0;}
   GUI.enabled=true;
   if(Model.Spill>0)GUI.Label(new Rect(468,306,165,22),"흘린 우유 "+Model.Spill.ToString("0")+"ml",label);
   if(dragging||Input.GetKey(KeyCode.Space))GUI.Label(new Rect(Mathf.Clamp(left-5,0,550),30,95,22),"기울기 "+angle.ToString("0")+"°",label);
   GUI.matrix=old;
  }
  void LogError(string message,string stack,LogType type){if(type==LogType.Exception){File.WriteAllText(Path.Combine(review,"failure.txt"),message+"\n"+stack);Application.Quit(2);}}
  void Check(bool value,string label){if(!value)throw new Exception("MILK CRAFT: "+label);}
  void SaveFrame(string name){File.WriteAllBytes(Path.Combine(review,name+".png"),view.Texture.EncodeToPNG());}
  IEnumerator Smoke(){
   yield return null;SaveFrame("01-upright");left=224;desired=70;
   int frames=0;bool mid=false,pour=false;
   while(Model.Cup<105&&frames<1200){yield return null;frames++;if(!mid&&angle>34){SaveFrame("02-forward-tilt");mid=true;}if(!pour&&Model.Cup>35){SaveFrame("03-pouring");pour=true;}if(frames%6==0&&frames/6<90)SaveFrame("motion_"+(frames/6).ToString("D3"));Check(Mathf.Abs(Model.Remaining+Model.Flying+Model.Cup+Model.Spill-240)<.05f,"volume conservation");}
   Check(Model.Cup>=100,"direct pour fills cup");desired=0;int settling=0;while(!Model.CanFinish&&settling++<300)yield return null;
   Check(angle<2&&Model.Flying==0,"release settles last drops");SaveFrame("04-stopped");float collected=Model.Cup;
   ResetDrink();left=400;desired=70;int missed=0;while(Model.Spill<20&&missed++<450)yield return null;Check(Model.Spill>10&&Model.Cup==0,"off cup spills");SaveFrame("05-missed");
   ResetDrink();Check(Model.Remaining==240&&Model.Cup==0&&Model.Drops.Count==0,"retry clears all state");
   File.WriteAllText(Path.Combine(review,"native-result.json"),"{\"passed\":true,\"cupMl\":"+collected.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"ownSplitArtwork\":true,\"spillVerified\":true,\"resetVerified\":true}");
   Debug.Log("MILK_CRAFT_NATIVE_PASSED");Application.Quit(0);
  }
  void OnDestroy(){view?.Dispose();if(sound&&sound.clip)Destroy(sound.clip);if(font)Destroy(font);}
 }
}
