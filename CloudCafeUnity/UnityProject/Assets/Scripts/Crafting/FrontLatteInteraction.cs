using System;
using System.Collections;
using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed partial class FrontGrindController {
  public readonly EasyLatteModel Latte=new EasyLatteModel();
  bool latteActive,lattePourHeld,latteSmoke,lattePointerCaptured;float latteX,latteZ,latteHeight=-1;PixelHandCursor supportHand;
  void BeginLatte(){Kill();motor.volume=0;latteActive=true;lattePourHeld=false;latteX=latteZ=0;latteHeight=-1;Latte.Reset();held=prepHeld=cupHeld=rotating=pressing=brewTurning=false;art.Hand.gameObject.SetActive(false);art.PrepHand.gameObject.SetActive(false);Brew.Running=Brew.CupDocked=false;art.EnsureLatte();art.SetLatteVisible(true);art.Cup.position=new Vector3(2.5f,.62f,-1f);art.Camera.transform.position=new Vector3(2.75f,3.8f,-3.8f);art.Camera.transform.LookAt(new Vector3(2.75f,.95f,-1f));art.Camera.orthographicSize=.9f;if(supportHand==null)supportHand=new PixelHandCursor(art.Camera);art.UpdateLatteArt(Latte,latteX,latteZ);if(Input.GetMouseButton(0))CaptureLattePointer();}
  void SeedLattePractice(){Brew.Reset();Brew.Insert(true);Brew.Turn(40);Brew.CupDocked=true;Brew.TogglePump();for(int i=0;i<300;i++)Brew.Step(.05f);BeginLatte();}
  void ReleaseLattePointer(){lattePointerCaptured=lattePourHeld=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  void CaptureLattePointer(){if(!Application.isFocused||smoke)return;if(paused)TogglePause();lattePointerCaptured=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  void EndLatteView(){ReleaseLattePointer();latteActive=lattePourHeld=false;if(supportHand!=null)supportHand.Set(HandPose.Open,Vector2.zero,false);art.SetLatteVisible(false);}
  void UpdateLatte(){
   if(!smoke){
    bool capturedNow=false;
    if(Input.GetKeyDown(KeyCode.Escape)){if(!paused)TogglePause();ReleaseLattePointer();}
    else if(Input.GetMouseButtonDown(0)&&Application.isFocused&&!lattePointerCaptured){CaptureLattePointer();capturedNow=true;}
    if(lattePointerCaptured&&(!Application.isFocused||Cursor.lockState!=CursorLockMode.Locked)){if(!paused)TogglePause();ReleaseLattePointer();}
    if(lattePointerCaptured&&!paused){
     if(Input.GetKeyDown(KeyCode.R)){Latte.Reset();latteHeight=-1;latteX=latteZ=0;}
     if(!capturedNow){Vector3 right=art.Camera.transform.right,up=art.Camera.transform.up;right.y=up.y=0;right.Normalize();up.Normalize();float dx=Input.GetAxisRaw("Mouse X")*.12f,dy=Input.GetAxisRaw("Mouse Y")*.12f;EasyLatteModel.MoveAim(ref latteX,ref latteZ,right.x*dx+up.x*dy,right.z*dx+up.z*dy);}
     if(Input.mouseScrollDelta.y!=0)latteHeight=Mathf.Clamp01((latteHeight<0?Latte.Height:latteHeight)+Input.mouseScrollDelta.y*.12f);
     lattePourHeld=!capturedNow&&Input.GetKey(KeyCode.Space);
    }else lattePourHeld=false;
   }
   if(!paused)Latte.Step(Time.deltaTime,lattePourHeld,latteX,latteZ,latteHeight);art.UpdateLatteArt(Latte,latteX,latteZ);
  }
  void UpdateLatteCursor(){bool visible=smoke||(lattePointerCaptured&&!paused&&Application.isFocused);var grip=art.Camera.WorldToViewportPoint(art.LattePitcher.TransformPoint(new Vector3(.43f,.27f,0)));handCursor.Set(HandPose.Grip,new Vector2(grip.x,grip.y),visible);var left=art.Camera.WorldToViewportPoint(art.Cup.TransformPoint(new Vector3(.19f,.13f,0)));supportHand.Set(HandPose.Grip,new Vector2(left.x,left.y),visible);if(!smoke)Cursor.visible=!visible;}
  void DrawLatteGUI(){GUI.DrawTexture(frame,art.Pixels,ScaleMode.StretchToFill,false);var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(frame.x,frame.y),Quaternion.identity,Vector3.one*frame.width/960);var label=new GUIStyle(GUI.skin.label){font=font,fontSize=18,normal={textColor=new Color(1,.94f,.83f)}};GrindStationView.Box(new Rect(0,0,960,42),new Color(.07f,.09f,.14f,.96f));GrindStationView.Box(new Rect(0,477,960,63),new Color(.07f,.09f,.14f,.96f));GUI.Label(new Rect(18,9,620,30),"우유 붓기 · Space를 누르고, 놓으면 멈춰요",label);GUI.Label(new Rect(745,9,205,30),"우유 "+Latte.Cup.ToString("0")+" / 110ml",label);string text=!smoke&&!lattePointerCaptured?"게임 안을 클릭하면 손으로 조작해요 · Esc: 마우스 풀기":paused?"잠시 쉬는 중 · 게임을 클릭해서 계속":Latte.Complete?"라테 한 잔 완성! R로 다시 연습할 수 있어요.":Latte.Flow>0?(Latte.Height>.43f?"먼저 커피와 우유를 부드럽게 섞어요.":"표면 가까이에서 흰 무늬가 퍼지고 있어요."):"Space를 누르고 있어 보세요. 손을 움직여 붓는 위치를 맞춰 보세요.";GUI.Label(new Rect(18,483,930,28),text,label);label.fontSize=13;GUI.Label(new Rect(18,515,930,23),"클릭: 마우스 고정 · 마우스: 손/피처 이동 · Space: 붓기 · 휠: 높이 · R: 다시 · Esc: 풀기",label);GUI.matrix=old;}
  IEnumerator LatteSmoke(){
   if(!latteActive)Check(GrabCup()&&latteActive,"finished espresso cup enters latte view");Check(Brew.Complete,"espresso ready for milk practice");yield return null;yield return Shot("latte_00-ready");lattePourHeld=true;
   // The model caps a frame at 50ms. Cold-start rendering can take longer,
   // so wait for simulated pouring time rather than wall time.
   float pourTime=0;while(pourTime<.65f){yield return null;pourTime+=Mathf.Min(Time.deltaTime,.05f);}
   Check(Latte.Cup>0&&Latte.Flying>0,"Space starts milk stream");yield return Shot("latte_01-pour");lattePourHeld=false;yield return null;float remaining=Latte.Remaining;yield return new WaitForSeconds(.45f);Check(Latte.Remaining==remaining&&Latte.Flying==0,"Space release stops source and lands last drops");yield return Shot("latte_02-stopped");
   lattePourHeld=true;float deadline=Time.realtimeSinceStartup+12;int n=0;while(!Latte.Complete&&Time.realtimeSinceStartup<deadline){latteX=Mathf.Sin(n*.3f)*.7f;yield return new WaitForSeconds(.15f);Check(Mathf.Abs(Latte.Remaining+Latte.Flying+Latte.Cup-180)<.01f,"latte milk conservation");yield return Shot("latte_motion_"+(n++).ToString("D3"));}Check(Latte.Complete&&Latte.Marks.Count>0,"easy latte completes and leaves white pattern");lattePourHeld=false;yield return new WaitForSeconds(.3f);yield return Shot("latte_03-finished");
  }
 }
}
