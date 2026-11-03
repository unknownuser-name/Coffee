using System;
using System.IO;
using DG.Tweening;
using UnityEngine;

// Tests the installed vendor engine against the motions needed by the next workbench.
public static class CafePluginVerification {
 static int checks;
 static void Require(bool value,string reason){checks++;if(!value)throw new Exception("PLUGIN CHECK: "+reason);}
 [UnityEditor.MenuItem("Cloud Cafe/Verify Installed Motion Plugin")]
 public static void Run(){
  checks=0;
  Configure();
  DOTween.Init(false,true,LogBehaviour.ErrorsOnly);
  var item=new GameObject("Plugin check - portafilter snap target");
  try {
   var target=new Vector3(2,1,0);
   var tween=DOTween.To(()=>item.transform.localPosition,p=>item.transform.localPosition=p,target,.4f)
    .SetEase(Ease.OutCubic).SetUpdate(UpdateType.Manual).SetTarget(item).SetAutoKill(false);
   DOTween.ManualUpdate(.1f,.1f);
   Require(item.transform.localPosition.x>0&&item.transform.localPosition.x<2,"motion interpolates toward socket");
   tween.Pause();var paused=item.transform.localPosition;DOTween.ManualUpdate(.1f,.1f);
   Require(item.transform.localPosition==paused,"pause keeps tool in place");
   tween.Play();DOTween.ManualUpdate(.5f,.5f);
   Require(Vector3.Distance(item.transform.localPosition,target)<.001f,"snap reaches exact socket");
   tween.Kill();
   var returnMotion=DOTween.To(()=>item.transform.localPosition,p=>item.transform.localPosition=p,Vector3.zero,.3f)
    .SetEase(Ease.OutCubic).SetUpdate(UpdateType.Manual).SetTarget(item);
   DOTween.ManualUpdate(.1f,.1f);returnMotion.Pause();returnMotion.Kill();var stopped=item.transform.localPosition;
   DOTween.ManualUpdate(.5f,.5f);
   Require(Vector3.Distance(item.transform.localPosition,stopped)<.001f,"grabbing cancels return motion without a jump: "+stopped+" -> "+item.transform.localPosition+" active="+returnMotion.IsActive());
   float turn=0;DOTween.To(()=>turn,v=>turn=v,90,.25f).SetEase(Ease.Linear).SetUpdate(UpdateType.Manual);
   DOTween.ManualUpdate(.3f,.3f);Require(Mathf.Abs(turn-90)<.001f,"locking turn completes");
   Directory.CreateDirectory("../PluginSetup");
   File.WriteAllText("../PluginSetup/verification.json","{\"passed\":true,\"checks\":"+checks+",\"plugin\":\"DOTween\",\"version\":\""+DOTween.Version+"\",\"scope\":\"Unity editor manual update; not gameplay integration\"}");
   Debug.Log("CAFE_PLUGIN_VERIFICATION_PASSED "+checks+" DOTween "+DOTween.Version);
  } finally {DOTween.KillAll();UnityEngine.Object.DestroyImmediate(item);DOTween.Clear(true);}
 }
 public static void Configure(){
  // The official setup window creates the settings asset; apply its module setup in batch mode.
  var window=ScriptableObject.CreateInstance<DG.DOTweenEditor.UI.DOTweenUtilityWindow>();
  try {
   var connect=window.GetType().GetMethod("Connect",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   if(connect==null)throw new Exception("Pinned DOTween setup entry point changed");
   connect.Invoke(window,new object[]{false});
   var settings=DG.DOTweenEditor.UI.DOTweenUtilityWindow.GetDOTweenSettings();
   if(!settings)throw new Exception("DOTween settings were not created");
   settings.modules.physicsEnabled=false;settings.modules.physics2DEnabled=false;
   DG.DOTweenEditor.UI.DOTweenUtilityWindowModules.ApplyModulesSettings();
   UnityEditor.EditorUtility.SetDirty(settings);UnityEditor.AssetDatabase.SaveAssets();
  } finally {UnityEngine.Object.DestroyImmediate(window);}
 }
}
