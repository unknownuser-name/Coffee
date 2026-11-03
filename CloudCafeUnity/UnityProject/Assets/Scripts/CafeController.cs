using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CloudCafe {
 public sealed class CafeController:MonoBehaviour {
  public CafeState State {get;private set;}
  public CafeView View {get;private set;}
  public CafeUi Ui {get;private set;}
  public CafeMilkStation MilkStation {get;private set;}
  public Vector2 Pitcher=new Vector2(227,34);
  Camera gameCamera;Rect frame;
  bool uiHeld,needsRelease,wasHeld,paidSaved,smoke,smokeFlowing;
  float clock;int lastWidth,lastHeight;Vector2 lastPointer;
  const string SaveKey="CloudCafeUnity.v1";
  static readonly string[] Lines={"오늘은 일이 좀 많았어.","힘든 하루였나 봐요. 잠깐 쉬었다 가세요.","퇴근하면 낮보다 컵이 가벼워서 좋더라고.","늘 마시던 따뜻한 라테로 부탁해."};
  void Awake(){
   Application.targetFrameRate=60;
   smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-cafeSmokeTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-cafeFluidSmoke")>=0;
   if(smoke){Application.runInBackground=true;Application.logMessageReceived+=OnSmokeError;}
   State=new CafeState(smoke?null:ReadSave());
   View=gameObject.AddComponent<CafeView>();View.Initialize();
   gameCamera=Camera.main;if(!gameCamera){gameCamera=new GameObject("Pixel Camera").AddComponent<Camera>();gameCamera.tag="MainCamera";}
   gameCamera.orthographic=true;gameCamera.orthographicSize=1.8f;gameCamera.transform.position=new Vector3(0,0,-10);gameCamera.clearFlags=CameraClearFlags.SolidColor;gameCamera.backgroundColor=new Color(.08f,.105f,.16f);gameCamera.allowMSAA=false;
   Layout();Ui=gameObject.AddComponent<CafeUi>();Ui.Initialize(this,gameCamera);MilkStation=gameObject.AddComponent<CafeMilkStation>();MilkStation.Initialize(gameCamera);Ui.SetMilkTexture(MilkStation.Texture);View.PhysicalMilk=true;lastPointer=Input.mousePosition;
  }
  IEnumerator Start(){if(smoke)yield return RunSmoke();}
  SaveData ReadSave(){try{return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey,"{}"));}catch{return null;}}
  void Save(){if(smoke)return;PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(State.Save()));PlayerPrefs.Save();}
  void Layout(){
   lastWidth=Screen.width;lastHeight=Screen.height;
   int scale=Mathf.Max(1,Mathf.FloorToInt(Mathf.Min(Screen.width/640f,Screen.height/360f)));
   float width=640*scale,height=360*scale;frame=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);
   gameCamera.rect=new Rect(frame.x/Screen.width,1-(frame.y+frame.height)/Screen.height,frame.width/Screen.width,frame.height/Screen.height);
  }
  public bool HitsCup(){return MilkStation!=null&&State.Stage==Phase.Milk?MilkStation.CupAligned:Vector2.Distance(Pitcher+new Vector2(93,194),new Vector2(320,236))<48;}
  public void SetUiHeld(bool value){uiHeld=value;}
  void ClearHeld(){uiHeld=false;wasHeld=false;needsRelease=true;MilkStation?.Release();}
  void Update(){
   if(Screen.width!=lastWidth||Screen.height!=lastHeight)Layout();clock+=Time.deltaTime;
   if(!smoke&&Input.GetKeyDown(KeyCode.Escape))Ui.ToggleHelp();
   MilkStation.SetStage(State.Stage);
   if(Ui.HelpOpen){ClearHeld();View.Render(State,Pitcher,false,clock);return;}
   bool worldHeld=false,keyHeld=false;
   if(!smoke){
    var screen=new Vector2(Input.mousePosition.x,Input.mousePosition.y);var mouse=new Vector2(screen.x,Screen.height-screen.y);bool overUi=Ui.BlocksWorld(screen);
    bool pointerMoved=(screen-lastPointer).sqrMagnitude>.25f;lastPointer=screen;
    if(State.Stage==Phase.Milk&&(pointerMoved||Input.GetMouseButtonDown(0))&&frame.Contains(mouse)&&!overUi){Pitcher=new Vector2(Mathf.Round(Mathf.Clamp((mouse.x-frame.x)/frame.width*640-93-CafeView.BrewOffset.x,0,268)),Mathf.Round(Mathf.Clamp((mouse.y-frame.y)/frame.height*360-164-CafeView.BrewOffset.y,-35,150)));}
    if(State.Stage==Phase.Milk){float x=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);float y=(Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.UpArrow)?1:0);Pitcher+=new Vector2(x,y)*90*Time.deltaTime;Pitcher.x=Mathf.Clamp(Pitcher.x,0,268);Pitcher.y=Mathf.Clamp(Pitcher.y,-35,150);}
    worldHeld=Input.GetMouseButton(0)&&frame.Contains(mouse)&&!overUi;keyHeld=Input.GetKey(KeyCode.Space);
   }
   bool raw=keyHeld||uiHeld||worldHeld;if(!raw)needsRelease=false;bool held=raw&&!needsRelease;
   var before=State.Stage;
   if(wasHeld&&!held)State.Release();
   if(!smoke||smokeFlowing){
    if(State.Stage==Phase.Milk){
     float pointerY=(Screen.height-Input.mousePosition.y-frame.y)/frame.height*360;
     MilkStation.Tick(Time.deltaTime,Pitcher.x,held,held&&worldHeld,pointerY);
     if(!State.ApplyPhysicalMilk(MilkStation.Remaining,MilkStation.InFlight,MilkStation.Collected,MilkStation.Spilled))throw new Exception("Physical milk snapshot rejected");
    }else State.Tick(Time.deltaTime,held,HitsCup());
   }
   if(before!=State.Stage){needsRelease=raw;held=false;uiHeld=false;}
   wasHeld=held;
   if(!smoke&&Input.GetKeyDown(KeyCode.Return))Next();
   if(State.Paid&&!paidSaved){Save();paidSaved=true;}
   MilkStation.SetStage(State.Stage);View.Render(State,Pitcher,held,clock);Ui.Refresh();
  }
  void OnApplicationFocus(bool focus){if(!focus&&!smoke){State?.Release();ClearHeld();}}
  public void Next(){
   switch(State.Stage){case Phase.Order:State.Start();break;case Phase.Ready:State.Serve();break;case Phase.Done:State=new CafeState(State.Save());paidSaved=false;Pitcher=new Vector2(227,34);break;default:State.Advance();break;}
   ClearHeld();Ui?.Refresh();
  }
  public void FinishPour(){ClearHeld();if(MilkStation.CanFinish)State.Finish();MilkStation.SetStage(State.Stage);Ui.Refresh();}
  public void RetryDrink(){State.Retry();Pitcher=new Vector2(227,34);ClearHeld();Ui.Refresh();}
  public void ResetSave(){if(!smoke){PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();}State=new CafeState();paidSaved=false;Pitcher=new Vector2(227,34);ClearHeld();Ui.Refresh();}
  public string DialogueBody(){
   switch(State.Stage){case Phase.Dialogue:return Lines[State.Line];case Phase.Order:return "따뜻한 라테 한 잔. 너무 급하게 만들지는 마.";case Phase.Reaction:return State.Score>=80?"이 정도면 오늘 하루도 잘 끝났네. 고마워.":State.Score>=50?"조금 서툴러도 괜찮아. 내일 또 오지.":"독특한 맛이군. 다음엔 같이 천천히 해 보자.";case Phase.Done:return "오늘은 여기까지. 내일 다시 만나요.";default:return "";}
  }
  void OnSmokeError(string message,string stack,LogType type){if(type==LogType.Exception){File.WriteAllText(Path.Combine(Application.dataPath,"../smoke-failure.txt"),message+"\n"+stack);Application.Quit(2);}}
  void CaptureCamera(string path){
   Canvas.ForceUpdateCanvases();var previousTarget=gameCamera.targetTexture;var previousActive=RenderTexture.active;var previousRect=gameCamera.rect;
   var target=RenderTexture.GetTemporary(640,360,24,RenderTextureFormat.ARGB32);target.filterMode=FilterMode.Point;var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
   try{gameCamera.targetTexture=target;gameCamera.rect=new Rect(0,0,1,1);gameCamera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,640,360),0,0);texture.Apply();
    var pixels=texture.GetPixels32();int changes=0;var first=pixels[0];for(int i=1;i<pixels.Length;i++)if(pixels[i].r!=first.r||pixels[i].g!=first.g||pixels[i].b!=first.b)changes++;
    if(changes<10000)throw new Exception("Rendered frame is unexpectedly blank");File.WriteAllBytes(path,texture.EncodeToPNG());
   }finally{gameCamera.targetTexture=previousTarget;gameCamera.rect=previousRect;RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(target);Destroy(texture);}
  }
  void PointerHold(bool down){var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};if(down){ExecuteEvents.Execute(Ui.HoldButton.gameObject,data,ExecuteEvents.pointerEnterHandler);ExecuteEvents.Execute(Ui.HoldButton.gameObject,data,ExecuteEvents.pointerDownHandler);}else{ExecuteEvents.Execute(Ui.HoldButton.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(Ui.HoldButton.gameObject,data,ExecuteEvents.pointerExitHandler);}}
  void Require(bool value,string message){if(!value)throw new Exception("UI smoke failed: "+message);}
  IEnumerator RunSmoke(){
   string review=Path.GetFullPath(Path.Combine(Application.dataPath,"../../CafeFluidIntegration/Review"));Directory.CreateDirectory(review);
   State.Tick(2);Ui.Refresh();View.Render(State,Pitcher,false,clock+=1);yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-dialogue.png"));
   Require(!Ui.HoldButton.gameObject.activeInHierarchy,"dialogue remains separate from crafting");
   for(int i=0;i<4;i++)Ui.DialogueNext.onClick.Invoke();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-order.png"));
   Ui.OrderButton.onClick.Invoke();smokeFlowing=true;yield return null;PointerHold(true);float deadline=Time.realtimeSinceStartup+5;
   while(State.Stage==Phase.Grind&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);Require(State.Stage==Phase.Extract,"existing grind input still works");yield return null;
   PointerHold(true);deadline=Time.realtimeSinceStartup+5;while(State.ExtractTime<3&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);yield return null;Require(State.Stage==Phase.Milk,"extraction enters physical milk stage");
   Require(MilkStation.ParticleCount>500&&MilkStation.UsesOriginalArtwork,"SPH engine and original vessel textures loaded");Require(!Ui.FinishButton.interactable,"cannot finish empty cup");
   Ui.ToggleHelp();int pausedAt=MilkStation.PhysicsSteps;yield return new WaitForSeconds(.25f);Require(MilkStation.PhysicsSteps==pausedAt,"help pauses fluid simulation");Ui.ToggleHelp();yield return null;
   MilkStation.RenderNow();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-milk-upright.png"));
   Pitcher=new Vector2(60,34);PointerHold(true);deadline=Time.realtimeSinceStartup+9;while(State.Spill<2&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);Require(State.Spill>1,"missing cup produces physical spill");
   Ui.RetryButton.onClick.Invoke();yield return null;Require(State.Stage==Phase.Grind&&State.Spill==0&&!MilkStation.Active,"retry clears milk stage and quantities");
   State.Tick(2,true);State.Tick(3,true);State.Release();Pitcher=new Vector2(227,34);yield return null;Require(State.Stage==Phase.Milk&&State.Remaining>239,"second drink initializes a fresh pitcher");
   yield return null;PointerHold(true);deadline=Time.realtimeSinceStartup+25;int capture=0,frames=0;bool pouringCaptured=false;
   while(State.Milk<110&&Time.realtimeSinceStartup<deadline){
    yield return null;frames++;
    if(frames%8==0&&capture<130){MilkStation.RenderNow();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"motion_"+(capture++).ToString("D3")+".png"));}
    if(!pouringCaptured&&State.Milk>50){MilkStation.RenderNow();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-milk-pouring.png"));pouringCaptured=true;}
   }
   PointerHold(false);Require(State.Milk>70,"SPH milk reaches actual game cup");deadline=Time.realtimeSinceStartup+9;
   while(!MilkStation.CanFinish&&Time.realtimeSinceStartup<deadline){yield return null;frames++;if(frames%8==0&&capture<145){MilkStation.RenderNow();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"motion_"+(capture++).ToString("D3")+".png"));}}
   Require(MilkStation.Angle<2&&MilkStation.CanFinish,"release returns vessel and last drops settle");Require(Mathf.Abs(State.Remaining+State.InFlight+State.Milk+State.Spill-240)<.05f,"game quantities conserve physical milk");
   Ui.Refresh();Require(Ui.FinishButton.interactable,"settled physical pour enables finish");Ui.FinishButton.onClick.Invoke();Require(State.Stage==Phase.Ready,"physical pour finishes into existing ready phase");
   MilkStation.RenderNow();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-ready.png"));float collected=State.Milk,remaining=State.Remaining,spill=State.Spill;int totalParticles=MilkStation.ParticleCount;
   Ui.ServeButton.onClick.Invoke();Require(State.Stage==Phase.Reaction&&State.Coins>120,"physical milk connects to customer reward");yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"cafe-served.png"));Require(!State.Serve(),"no duplicate serving reward");
   Ui.DialogueNext.onClick.Invoke();Ui.DialogueNext.onClick.Invoke();yield return null;Require(State.Day==2&&!MilkStation.Active,"next day exits fluid station");
   File.WriteAllText(Path.Combine(review,"native-result.json"),"{\"passed\":true,\"particles\":"+totalParticles+",\"milkMl\":"+collected.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"remainingMl\":"+remaining.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"spillMl\":"+spill.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"originalArtworkTextures\":true,\"nextDay\":2,\"motionFrames\":"+capture+"}");
   Debug.Log("CAFE_FLUID_NATIVE_SMOKE_PASSED");Application.Quit(0);
  }
  IEnumerator RunLegacySmoke(){
   string review=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Review"));Directory.CreateDirectory(review);
   State.Tick(2);Ui.Refresh();View.Render(State,Pitcher,false,clock+=1);yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-dialogue-v4.png"));
   Require(!Ui.OrderButton.gameObject.activeInHierarchy&&!Ui.HoldButton.gameObject.activeInHierarchy,"dialogue hides craft buttons");
   // Compare the actual UI with a capture from the same render state without the Canvas.
   Ui.Canvas.enabled=false;CaptureCamera(Path.Combine(review,"ui-world-only-v4.png"));Ui.Canvas.enabled=true;
   Require(View.LoadedImages==View.Art.keys.Length,"artwork decoded");
   for(int i=0;i<4;i++)Ui.DialogueNext.onClick.Invoke();Ui.Refresh();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-order-v4.png"));
   Require(Ui.OrderButton.gameObject.activeInHierarchy&&!Ui.HoldButton.gameObject.activeInHierarchy,"order exposes make button");
   var orderRect=(RectTransform)Ui.OrderButton.transform;Require(Ui.BlocksWorld(gameCamera.WorldToScreenPoint(orderRect.TransformPoint(orderRect.rect.center))),"UI blocks duplicate world input");
   Ui.OrderButton.onClick.Invoke();smokeFlowing=true;yield return null;PointerHold(true);
   float deadline=Time.realtimeSinceStartup+4;while(State.Stage==Phase.Grind&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);Require(State.Stage==Phase.Extract,"hold pointer grinds beans");
   yield return null;PointerHold(true);deadline=Time.realtimeSinceStartup+5;while(State.ExtractTime<3&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);yield return null;
   Require(State.Stage==Phase.Milk,"pointer release finishes extraction");Ui.Refresh();Require(!Ui.FinishButton.interactable,"empty finish disabled");
   // Miss once to verify UI input and retry wiring.
   Pitcher=new Vector2(60,34);yield return null;PointerHold(true);yield return new WaitForSeconds(.35f);PointerHold(false);yield return null;Require(State.Spill>5,"off-cup spill");Ui.RetryButton.onClick.Invoke();Require(State.Stage==Phase.Grind&&State.Spill==0,"retry wired to model");
   yield return null;PointerHold(true);deadline=Time.realtimeSinceStartup+4;while(State.Stage==Phase.Grind&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);yield return null;
   PointerHold(true);deadline=Time.realtimeSinceStartup+5;while(State.ExtractTime<1.5f&&Time.realtimeSinceStartup<deadline)yield return null;Ui.Refresh();yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-extract-v4.png"));
   while(State.ExtractTime<3&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);yield return null;Require(State.Stage==Phase.Milk,"second extraction finishes");
   yield return null;PointerHold(true);deadline=Time.realtimeSinceStartup+7;while(State.Milk<90&&Time.realtimeSinceStartup<deadline)yield return null;Ui.Refresh();View.VerifyLiquidMasks(true);yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-pour-v4.png"));
   while(State.Milk<149.5f&&Time.realtimeSinceStartup<deadline)yield return null;PointerHold(false);yield return null;Require(Ui.FinishButton.interactable,"pour enables finish");Ui.FinishButton.onClick.Invoke();Require(State.Stage==Phase.Ready&&Ui.ServeButton.gameObject.activeInHierarchy,"finish exposes serve");yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-ready-v4.png"));
   Ui.ServeButton.onClick.Invoke();Require(State.Stage==Phase.Reaction&&State.Score>=98&&State.Coins>=139,"serve pays reward");yield return new WaitForEndOfFrame();CaptureCamera(Path.Combine(review,"ui-result-v4.png"));
   Require(!State.Serve(),"no duplicate reward");Ui.DialogueNext.onClick.Invoke();Ui.DialogueNext.onClick.Invoke();Require(State.Day==2,"next day");
   File.WriteAllText(Path.Combine(review,"ui-player-v4.json"),"{\"passed\":true,\"ui\":\"native uGUI Canvas/Image/Text/Button\",\"logicalSize\":[640,360],\"loadedImages\":"+View.LoadedImages+",\"checks\":[\"dialogue and craft separation\",\"actual button commands\",\"pointer down and up hold\",\"live extraction timing\",\"disabled finish\",\"spill and retry\",\"clipped milk\",\"serve and reward\",\"next day\"]}");
   Debug.Log("CAFE_NATIVE_UI_SMOKE_PASSED");yield return new WaitForSeconds(.3f);Application.Quit(0);
  }
  void OnDestroy(){if(smoke)Application.logMessageReceived-=OnSmokeError;}
 }
}
