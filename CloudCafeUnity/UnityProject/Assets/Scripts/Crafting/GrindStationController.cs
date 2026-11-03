using System;
using System.Collections;
using System.IO;
using DG.Tweening;
using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed class GrindStationController:MonoBehaviour {
  public readonly GrindModel Model=new GrindModel();
  public Vector2 Position=new Vector2(416,176);
  GrindStationView view;Tween motion;Font font;AudioSource audioLoop;Rect frame;
  Vector2 grabOffset;bool paused,smoke;float clock;string message="포터필터 손잡이를 잡아 그라인더 밑으로 옮겨 주세요.",review;
  public bool Moving=>motion!=null&&motion.IsActive()&&motion.IsPlaying();
  void Awake(){Application.targetFrameRate=60;Application.runInBackground=true;smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-grindSmoke")>=0;view=new GrindStationView();font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",16);audioLoop=gameObject.AddComponent<AudioSource>();audioLoop.clip=MakeMotor();audioLoop.loop=true;audioLoop.volume=0;audioLoop.Play();review=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../GrindStation/Review"));if(smoke){Directory.CreateDirectory(review);Application.logMessageReceived+=LogFailure;}}
  AudioClip MakeMotor(){var data=new float[22050];var r=new System.Random(741);float filtered=0;for(int i=0;i<data.Length;i++){filtered=Mathf.Lerp(filtered,(float)(r.NextDouble()*2-1),.3f);float envelope=Mathf.Min(1,Mathf.Min(i,22049-i)/120f);data[i]=(filtered*.24f+Mathf.Sin(i*.065f)*.06f)*envelope;}var clip=AudioClip.Create("Interim grinder motor",data.Length,1,22050,false);clip.SetData(data,0);return clip;}
  void Layout(){float fit=Mathf.Min(Screen.width/640f,Screen.height/360f);float scale=fit>=1?Mathf.Floor(fit):Mathf.Max(.1f,fit);frame=new Rect((Screen.width-640*scale)/2,(Screen.height-360*scale)/2,640*scale,360*scale);}
  Vector2 Pointer(){return new Vector2((Input.mousePosition.x-frame.x)*640/frame.width,(Screen.height-Input.mousePosition.y-frame.y)*360/frame.height);}
  IEnumerator Start(){if(smoke)yield return Smoke();}
  public bool Grab(Vector2 pointer){
   if(paused)return false;
   if(!Model.PickUp()){message=Model.Flying>0?"마지막 가루가 떨어질 때까지 잠깐 기다려 주세요.":"분쇄를 멈췄어요. 다시 잡으면 꺼낼 수 있어요.";return false;}
   CancelMotion();grabOffset=pointer-Position;return true;
  }
  public void MoveHeld(Vector2 pointer){if(Model.Slot==CraftSlot.Held)Position=new Vector2(Mathf.Clamp(pointer.x-grabOffset.x,12,500),Mathf.Clamp(pointer.y-grabOffset.y,65,176));}
  public void PutDown(){
   if(Model.Slot!=CraftSlot.Held)return;Vector2 center=Position+new Vector2(64,46);CraftSlot slot=Vector2.Distance(center,new Vector2(260,222))<55?CraftSlot.Grinder:Vector2.Distance(center,new Vector2(480,222))<65?CraftSlot.Mat:CraftSlot.Invalid;
   Model.Drop(slot);var target=Model.Slot==CraftSlot.Grinder?new Vector2(196,176):new Vector2(416,176);Snap(target);
   message=Model.Complete?"가루 받기 완료. 같은 포터필터로 탬핑을 준비할 수 있어요.":slot==CraftSlot.Invalid?"놓을 자리가 아니어서 원래 위치로 돌아갑니다.":Model.Slot==CraftSlot.Grinder?"받침에 놓았어요. 스위치나 Space로 분쇄를 시작하세요.":"작업 매트에 놓았어요.";
  }
  void Snap(Vector2 target){CancelMotion();motion=DOTween.To(()=>Position,v=>Position=v,target,.22f).SetEase(Ease.OutCubic).SetTarget(this).OnComplete(()=>Position=target);}
  void CancelMotion(){if(motion!=null&&motion.IsActive()){motion.Pause();motion.Kill();}motion=null;}
  public void Switch(){if(paused||Moving||Model.Slot==CraftSlot.Held)return;bool changed=Model.Toggle();message=!changed?(Model.Ready?"목표량을 받았어요. 매트로 옮겨 주세요.":Model.Beans<=0?"호퍼에 원두가 없어요.":"먼저 포터필터를 분쇄구 아래에 놓아 주세요."):Model.Running?"가루를 받고 있어요. 다시 누르면 멈춥니다.":"분쇄를 멈췄어요.";}
  public void Pause(){if(Model.Slot==CraftSlot.Held)PutDown();paused=!paused;Model.Running=false;if(motion!=null&&motion.IsActive()){if(paused)motion.Pause();else motion.Play();}}
  public void ResetStation(){CancelMotion();Model.Reset();Position=new Vector2(416,176);paused=false;message="포터필터를 분쇄구 아래에 놓아 주세요.";}
  void Update(){Layout();clock+=Time.deltaTime;
   if(!smoke){if(Input.GetKeyDown(KeyCode.R))ResetStation();if(Input.GetKeyDown(KeyCode.Escape))Pause();if(!paused){var p=Pointer();bool overControl=new Rect(137,281,116,27).Contains(p)||new Rect(550,333,78,23).Contains(p);if(Input.GetMouseButtonDown(0)&&!overControl&&new Rect(Position.x+17,Position.y+20,97,134).Contains(p))Grab(p);if(Model.Slot==CraftSlot.Held){MoveHeld(p);if(Input.GetMouseButtonUp(0))PutDown();}if(Input.GetKeyDown(KeyCode.Space))Switch();if(Input.GetKeyDown(KeyCode.Alpha1))KeyboardPlace(CraftSlot.Grinder);if(Input.GetKeyDown(KeyCode.Alpha2))KeyboardPlace(CraftSlot.Mat);}}
   if(!paused){Model.Step(Time.deltaTime);if(Model.Ready&&Model.Slot==CraftSlot.Grinder)message="18g을 받았어요. 포터필터를 매트로 옮겨 주세요.";}
   audioLoop.volume=Mathf.MoveTowards(audioLoop.volume,Model.Running&&!paused?.23f:0,Time.deltaTime*2);
  }
  void KeyboardPlace(CraftSlot slot){if(Moving||paused)return;if(Grab(Position+new Vector2(64,120))){Position=slot==CraftSlot.Grinder?new Vector2(196,176):new Vector2(416,176);PutDown();}}
  void OnApplicationFocus(bool focus){if(!focus&&!smoke){if(Model.Slot==CraftSlot.Held)PutDown();Model.Running=false;}}
  void OnGUI(){Layout();var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(frame.x,frame.y),Quaternion.identity,Vector3.one*frame.width/640);view.Draw(Model,Position,clock,Model.Slot==CraftSlot.Held);
   GrindStationView.Box(new Rect(0,0,640,28),new Color(.06f,.08f,.12f,.95f));GrindStationView.Box(new Rect(0,328,640,32),new Color(.06f,.08f,.12f,.95f));
   var label=new GUIStyle(GUI.skin.label){font=font,fontSize=12,normal={textColor=new Color(1,.94f,.83f)}};var small=new GUIStyle(label){fontSize=10};var button=new GUIStyle(GUI.skin.button){font=font,fontSize=12};
   GUI.Label(new Rect(10,4,350,23),"심야 카페 · 직접 원두 갈기",label);GUI.Label(new Rect(386,4,250,23),"호퍼 "+Model.Beans.ToString("0.0")+"g    바스켓 "+Model.Grounds.ToString("0.0")+" / 18g",label);
   GUI.Label(new Rect(343,54,280,50),paused?"잠시 쉬는 중 · Esc로 계속":message,label);GUI.Label(new Rect(341,108,280,44),"손잡이를 잡아 끌고, 받침이나 매트에 놓으세요.",small);
   GUI.enabled=!paused&&!Moving&&Model.Slot!=CraftSlot.Held;
   if(GUI.Button(new Rect(137,281,116,27),Model.Running?"■ 분쇄 멈춤":"▶ 분쇄 스위치",button))Switch();GUI.enabled=true;
   GUI.Label(new Rect(438,158,130,23),"작업 매트",small);GUI.Label(new Rect(12,335,515,23),"Space 스위치 · 1 분쇄 받침 / 2 매트 · R 다시 · Esc 일시정지",small);
   if(GUI.Button(new Rect(550,333,78,23),"다시 시작",button))ResetStation();GUI.matrix=old;
  }
  void LogFailure(string msg,string stack,LogType type){if(type==LogType.Exception){File.WriteAllText(Path.Combine(review,"failure.txt"),msg+"\n"+stack);Application.Quit(2);}}
  void Check(bool condition,string label){if(!condition)throw new Exception("GRIND_NATIVE: "+label);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();File.WriteAllBytes(Path.Combine(review,name+".png"),view.Capture(Model,Position,clock));}
  IEnumerator Settle(){float end=Time.realtimeSinceStartup+3;while(Moving&&Time.realtimeSinceStartup<end)yield return null;Check(!Moving,"snap settles");}
  IEnumerator Smoke(){
   yield return null;yield return Shot("01-empty-mat");Switch();Check(!Model.Running,"start blocked without basket");
   Check(Grab(Position+new Vector2(64,120)),"grab");MoveHeld(new Vector2(260,296));PutDown();yield return Settle();Check(Model.Slot==CraftSlot.Grinder&&Vector2.Distance(Position,new Vector2(196,176))<.1f,"drop snaps to grinder");yield return Shot("02-under-outlet");
   Switch();float timeout=Time.realtimeSinceStartup+12;int capture=0;while(!Model.Ready&&Time.realtimeSinceStartup<timeout){yield return new WaitForSeconds(.12f);yield return Shot("motion_"+(capture++).ToString("D3"));Check(Mathf.Abs(Model.Beans+Model.Grounds+Model.Flying+Model.Waste-60)<.01f,"conservation");}
   Check(Model.Ready,"dose completes");yield return Shot("03-full-basket");Check(Grab(Position+new Vector2(64,120)),"lift full basket");MoveHeld(new Vector2(480,296));PutDown();yield return Settle();Check(Model.Complete,"same grounds arrive on mat");yield return Shot("04-full-mat");
   Check(Grab(Position+new Vector2(64,120)),"regrab");MoveHeld(new Vector2(90,160));PutDown();yield return Settle();Check(Model.Complete&&Position==new Vector2(416,176),"invalid drop restores contents");
   ResetStation();KeyboardPlace(CraftSlot.Grinder);yield return Settle();Switch();yield return new WaitForSeconds(.3f);Check(!Grab(Position+new Vector2(64,120))&&!Model.Running,"pickup stops motor");yield return new WaitForSeconds(.4f);Check(Model.Flying==0,"last grounds land");Pause();float beans=Model.Beans;yield return new WaitForSeconds(.3f);Check(Model.Beans==beans,"paused state holds");Pause();
   for(int cycle=0;cycle<2;cycle++){ResetStation();KeyboardPlace(CraftSlot.Grinder);yield return Settle();Switch();timeout=Time.realtimeSinceStartup+10;while(!Model.Ready&&Time.realtimeSinceStartup<timeout)yield return null;Check(Model.Ready,"repeat dose");KeyboardPlace(CraftSlot.Mat);yield return Settle();Check(Model.Complete,"repeat completion");}
   File.WriteAllText(Path.Combine(review,"native-result.json"),"{\"passed\":true,\"cycles\":3,\"doseGrams\":"+Model.Grounds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"dotweenSnap\":true,\"invalidDrop\":true,\"pause\":true,\"scope\":\"scripted controller inputs, not OS mouse\"}");Debug.Log("GRIND_STATION_NATIVE_PASSED");Application.Quit(0);
  }
  void OnDestroy(){CancelMotion();view?.Dispose();if(audioLoop&&audioLoop.clip)Destroy(audioLoop.clip);if(font)Destroy(font);}
 }
}
