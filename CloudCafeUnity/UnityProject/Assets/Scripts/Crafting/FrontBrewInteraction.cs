using DG.Tweening;
using System.Collections;
using UnityEngine;
namespace CloudCafe.Crafting {
 public sealed partial class FrontGrindController {
  public readonly BrewModel Brew=new BrewModel();
  Tween cameraMotion;bool brewView,cupHeld,brewTurning;float turnX;Vector3 cupOffset;
  string blockedDockMessage;
  bool NearMachine(){return DockSnapModel.Near(art.Tool.position.x-art.GroupSeat.x,art.Tool.position.z-art.GroupSeat.z);}
  string PrepRequired(){if(professionalMode&&!Model.DoseCommitted)return "먼저 매트에서 원두 양을 확정해 주세요.";return !Model.Ready?"먼저 원두를 18g 받아 주세요.":!Prep.Leveled?"먼저 낮고 둥근 도구로 가루 표면을 고르게 돌려 주세요.":"먼저 탬퍼로 가루를 꾹 눌러 다져 주세요.";}
  public void TogglePause(){paused=!paused;if(paused){if(!professionalMode)Model.Running=Brew.Running=false;brewTurning=false;if(!smoke)ReleaseLattePointer();}if(motion!=null&&motion.IsActive()){if(paused)motion.Pause();else motion.Play();}if(cameraMotion!=null&&cameraMotion.IsActive()){if(paused)cameraMotion.Pause();else cameraMotion.Play();}}
  void EnterBrewView(){if(brewView)return;brewView=true;message="탬핑 완료. 손잡이를 잡고 오른쪽으로 옮기면 시야도 손을 따라갑니다.";}
  void LateUpdate(){UpdateStepGuidance();if(UpdateEspressoOrderCursor()||UpdateCoffeeBeanCursor()||UpdateProfessionalCursor())return;if(counterView){UpdateCounterCursor();return;}if(AmericanoView){Cursor.visible=true;handCursor?.Set(HandPose.Open,Vector2.zero,false);return;}if(latteActive){UpdateLatteCursor();return;}FollowHandCamera(Time.deltaTime);UpdateHandCursor();}
  void FollowHandCamera(float dt){if(paused||(cameraMotion!=null&&cameraMotion.IsActive()))return;
   Transform handTarget=held?art.Tool:prepHeld?prepTool:cupHeld?art.Cup:null;
   if(handTarget==null&&art.Hand.gameObject.activeSelf&&motion!=null&&motion.IsActive())handTarget=art.Tool;
   if(handTarget==null&&art.PrepHand.gameObject.activeSelf&&motion!=null&&motion.IsActive())handTarget=prepTool!=null?prepTool:art.Cup;
   if(handTarget==null)return;var p=art.Camera.transform.position;p.x=HandCameraModel.Next(p.x,handTarget.position.x,art.Camera.orthographicSize*2*art.Camera.aspect,dt);art.Camera.transform.position=p;
  }
  Vector2 MouseViewport(){return new Vector2((Input.mousePosition.x-frame.x)/frame.width,(Input.mousePosition.y-frame.y)/frame.height);}
  void DragHeld(Vector2 viewport){float enter;var ray=art.Camera.ViewportPointToRay(new Vector3(viewport.x,viewport.y,0));if(new Plane(Vector3.up,new Vector3(0,.72f,0)).Raycast(ray,out enter)){Reach(ray.GetPoint(enter)+pointerOffset);if(NearMachine()&&Prep.Complete)art.Tool.position=Vector3.MoveTowards(art.Tool.position,new Vector3(art.GroupSeat.x,.72f,art.GroupSeat.z),.55f);}}
  IEnumerator HandCameraSmoke(){
   yield return null;yield return Shot("handcamera_00-start");Check(Grab(),"camera test grab before preparation");yield return Settle();pointerOffset=Vector3.zero;
   for(int i=0;i<90;i++){DragHeld(new Vector2(.9f,.45f));yield return new WaitForSeconds(1f/60);}
   yield return Shot("handcamera_01-right");Check(!Prep.Complete&&art.Tool.position.x>2.5f&&art.Camera.transform.position.x>1.4f,"hand drag reaches right before preparation; tool="+art.Tool.position+" camera="+art.Camera.transform.position+" held="+held+" moving="+Moving+" dt="+Time.deltaTime);
   TogglePause();float stoppedX=art.Camera.transform.position.x;yield return new WaitForSeconds(.12f);Check(art.Camera.transform.position.x==stoppedX,"pause freezes hand camera");TogglePause();
   for(int i=0;i<100;i++){DragHeld(new Vector2(.1f,.45f));yield return new WaitForSeconds(1f/60);}
   Check(art.Tool.position.x<0&&art.Camera.transform.position.x<.4f,"hand drag brings view back left");yield return Shot("handcamera_02-left");
   Reach(art.GroupSeat+new Vector3(.3f,0,-.6f));Drop();yield return Settle();Check(!Brew.Inserted&&message.Contains("먼저"),"unprepared dock explains missing step");ResetStation();Check(art.Camera.transform.position.x==0,"reset camera home");
  }
  void ResetBrew(){if(cameraMotion!=null&&cameraMotion.IsActive()){cameraMotion.Pause();cameraMotion.Kill();}cameraMotion=null;brewView=cupHeld=brewTurning=false;Brew.Reset();art.Camera.transform.position=new Vector3(0,2.9f,-4.6f);art.Cup.position=art.CupHome;art.Cup.rotation=Quaternion.identity;art.Camera.orthographicSize=1.55f;art.Camera.transform.LookAt(new Vector3(0,.86f,.12f));}
  bool InsertAtMachine(){if(!Prep.Complete||!NearMachine())return false;if(!Brew.Insert(true))return false;
   held=false;Model.Drop(CraftSlot.Machine);motion=DOTween.Sequence().Append(art.Tool.DOMove(new Vector3(art.GroupSeat.x,.72f,art.GroupSeat.z-.25f),.25f).SetEase(Ease.InOutSine)).Join(art.Tool.DORotate(new Vector3(0,40,0),.25f)).Append(art.Tool.DOMove(art.GroupSeat,.26f).SetEase(Ease.OutSine)).AppendCallback(()=>{contact.Play();message="끼웠어요. 손잡이를 누른 채 오른쪽으로 당겨 돌려 잠그세요.";}).OnComplete(()=>motion=null);return true;
  }
  public void TurnBrew(float degrees){if(paused||Moving||!Brew.Inserted||Brew.Locked)return;Brew.Turn(degrees);art.Tool.rotation=Quaternion.Euler(0,40-Brew.Angle,0);if(Brew.Locked){art.Hand.gameObject.SetActive(false);brewTurning=false;contact.Play();message="잠겼어요. 오른쪽 컵을 잡아 추출구 아래 받침에 놓으세요.";}}
  public void SwitchBrew(){if(paused||Moving||cupHeld||brewTurning)return;PrepareProfessionalBrew();if(Brew.TogglePump())message=Brew.Running?"압력이 차오른 뒤 커피가 떨어집니다. 금색 버튼으로 멈출 수 있어요.":professionalMode&&!Brew.ShotFinalized?"물이 퍽을 적시는 중에 멈췄어요. 다시 추출을 시작할 수 있어요.":"추출을 멈췄어요. 남은 커피가 마저 떨어집니다.";else message=!Brew.Locked?"포터필터를 끝까지 돌려 잠가 주세요.":!Brew.CupDocked?"추출구 아래에 컵을 먼저 놓아 주세요.":"에스프레소 추출이 끝났어요.";}
  public bool GrabCup(){if(paused||Moving||held||prepKind!=0||cupHeld||!Brew.Locked||Brew.Running||Brew.Parcels.Count!=0)return false;if(Brew.Complete)return !EspressoShotServed&&TryOpenDrinkReport();cupHeld=true;Brew.CupDocked=false;motion=DOTween.Sequence().Append(art.Cup.DOMoveY(.55f,.23f)).OnComplete(()=>{cupOffset=art.Cup.position-CupReachPoint();cupOffset.y=0;motion=null;});art.PrepHand.gameObject.SetActive(true);message="컵을 머신의 추출구 아래 받침에 가져다 놓으세요.";return true;}
  Vector3 CupReachPoint(){float distance;var ray=MouseRay();return new Plane(Vector3.up,new Vector3(0,.55f,0)).Raycast(ray,out distance)?ray.GetPoint(distance):art.Cup.position;}
  bool NearCupSeat(){var p=art.Cup.position;var b=art.CupSeat;var pv=art.Camera.WorldToViewportPoint(p);var bv=art.Camera.WorldToViewportPoint(b);return PrepPlacementModel.Near(p.x-b.x,p.z-b.z,pv.x-bv.x,pv.y-bv.y);}
  public void ReachCup(Vector3 point){if(!cupHeld||Moving)return;art.Cup.position=new Vector3(Mathf.Clamp(point.x,.3f,4.3f),.55f,Mathf.Clamp(point.z,-1.1f,.25f));if(NearCupSeat())art.Cup.position=Vector3.MoveTowards(art.Cup.position,new Vector3(art.CupSeat.x,.55f,art.CupSeat.z),.45f);}
  public void DropCup(){if(!cupHeld||Moving)return;cupHeld=false;var p=art.Cup.position;bool valid=NearCupSeat();Vector3 target=valid?art.CupSeat:art.CupHome;motion=DOTween.Sequence().Append(art.Cup.DOMove(new Vector3(target.x,.48f,target.z),.2f)).Append(art.Cup.DOMove(target,.23f).SetEase(Ease.InSine)).AppendCallback(()=>{Brew.CupDocked=valid;art.PrepHand.gameObject.SetActive(false);contact.Play();message=valid?"컵을 놓았어요. 머신 오른쪽 금색 버튼을 눌러 추출하세요.":"컵을 원래 자리로 내려놓았어요.";}).OnComplete(()=>motion=null);}
  bool TryBrewClick(Transform target){if(target==art.BrewButton){SwitchBrew();return true;}if(target.IsChildOf(art.Cup)){GrabCup();return true;}if(Brew.Inserted&&target.IsChildOf(art.Tool)){if(!Brew.Locked){brewTurning=true;turnX=Input.mousePosition.x;art.Hand.gameObject.SetActive(true);}else message="포터필터가 잠겼어요. 컵을 놓고 추출해 주세요.";return true;}return false;}
  void UpdateBrew(){art.SetCupHint(cupHeld&&Brew.Locked,cupHeld&&NearCupSeat());if(cupHeld&&!Moving&&NearCupSeat())message="손을 놓으면 컵이 받침 가운데에 맞춰 들어갑니다.";art.SetDockHint(held&&art.Tool.position.x>1.2f&&!Brew.Inserted,Prep.Complete,held&&NearMachine());if(held&&!Moving&&NearMachine())message=Prep.Complete?"위치가 맞았어요. 손을 놓으면 위로 끼워집니다.":PrepRequired();if(!paused){StepStationBrew(Time.deltaTime);if(!smoke){if(cupHeld){if(!Moving)ReachCup(CupReachPoint()+cupOffset);if(!Moving&&!Input.GetMouseButton(0))DropCup();}if(brewTurning){float x=Input.mousePosition.x;TurnBrew((x-turnX)/frame.width*640);turnX=x;if(Input.GetMouseButtonUp(0)){brewTurning=false;if(!Brew.Locked)message="아직 덜 잠겼어요. 손잡이를 다시 잡고 오른쪽으로 돌려 주세요.";}}}}
   if(cupHeld){art.PrepHand.position=art.Cup.position+new Vector3(.15f,-.04f,0);art.PrepHand.rotation=Quaternion.identity;}if(latteActive)return;art.UpdateBrew(Brew);if(Brew.Complete)message=EspressoShotServed?"손님에게 제공했어요. 리포트에서 다음 주문을 받을 수 있어요.":finishedDrink.HasValue?"음료가 완성됐어요. 컵이나 리포트 버튼을 눌러 확인하세요.":"샷이 완성됐어요. 컵이나 음료 선택 버튼을 눌러 음료를 고르세요.";
  }
  void StopBrewForFocus(){if(!professionalMode)Brew.Running=false;brewTurning=false;if(cupHeld){Kill();cupHeld=false;Brew.CupDocked=false;art.Cup.position=art.CupHome;art.PrepHand.gameObject.SetActive(false);}}
 }
}
