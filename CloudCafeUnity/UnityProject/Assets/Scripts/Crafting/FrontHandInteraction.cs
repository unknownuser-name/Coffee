using UnityEngine;
using DG.Tweening;
namespace CloudCafe.Crafting {
 public sealed partial class FrontGrindController {
  PixelHandCursor handCursor;
  void CreateHandCursor(){foreach(var r in art.Hand.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var r in art.PrepHand.GetComponentsInChildren<Renderer>(true))r.enabled=false;handCursor=new PixelHandCursor(art.Camera);}
  Vector3 BasketGrip(){return art.Tool.TransformPoint(new Vector3(0,.12f,-.48f));}
  Vector3 PrepGrip(){return prepTool.TransformPoint(new Vector3(0,prepKind==1?.1f:.24f,0));}
  bool CursorHover(){if(paused||Moving)return false;RaycastHit hit;if(!Physics.Raycast(MouseRay(),out hit,20))return false;var t=hit.transform;
   if(t==art.Switch||t==art.BrewButton)return true;
   if(t.IsChildOf(art.Tool))return !Brew.Locked;
   if(t.IsChildOf(art.Distributor))return Model.Complete&&!Prep.Complete;
   if(t.IsChildOf(art.Tamper))return Model.Complete&&Prep.Leveled&&!Prep.Complete;
   return t.IsChildOf(art.Cup)&&Brew.Locked&&!Brew.Running&&Brew.Parcels.Count==0&&!EspressoShotServed;
  }
  void UpdateHandCursor(){if(handCursor==null)return;var pointer=smoke?new Vector2(.45f,.53f):MouseViewport();bool inside=pointer.x>=0&&pointer.x<=1&&pointer.y>=.12f&&pointer.y<=.92f;bool visible=smoke||(Application.isFocused&&inside&&!paused);bool gripping=false;Vector3 anchor=Vector3.zero;
   if(held||brewTurning){gripping=true;anchor=BasketGrip();}
   else if(prepHeld||rotating||pressing){if(prepTool!=null){gripping=true;anchor=PrepGrip();}}
   else if(cupHeld){gripping=true;anchor=art.Cup.TransformPoint(new Vector3(.19f,.13f,0));}
   else if(motion!=null&&motion.IsActive()){if(art.Hand.gameObject.activeSelf){gripping=true;anchor=BasketGrip();}else if(art.PrepHand.gameObject.activeSelf){gripping=true;anchor=prepTool!=null?PrepGrip():art.Cup.TransformPoint(new Vector3(.19f,.13f,0));}}
   HandPose pose=HandCursorModel.Resolve(gripping,pressing,!smoke&&CursorHover(),paused);if(gripping&&!paused){var v=art.Camera.WorldToViewportPoint(anchor);pointer=new Vector2(v.x,v.y);}
   handCursor.Set(pose,pointer,visible);if(!smoke)Cursor.visible=!visible;
  }
 }
}
