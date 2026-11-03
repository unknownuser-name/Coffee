using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using CloudCafe.Crafting;
public static class FrontGrindBuild {
 [MenuItem("Cloud Cafe/Build Front Grind Motion")]
 public static void Build(){
  BuildTo("FrontGrind");
 }
 [MenuItem("Cloud Cafe/Build Coffee Through Espresso")]
 public static void BuildBrew(){BuildTo("FrontBrew");}
 [MenuItem("Cloud Cafe/Build Hand Following Camera")]
 public static void BuildHandCamera(){BuildTo("CoffeeCraftHand");}
 [MenuItem("Cloud Cafe/Build Assisted Machine Dock")]
 public static void BuildDockSnap(){BuildTo("CoffeeCraftSnap");}
 [MenuItem("Cloud Cafe/Build Forgiving Tool Placement")]
 public static void BuildEasyPlacement(){BuildTo("CoffeeCraftEasy");}
 [MenuItem("Cloud Cafe/Build Single Hand Cursor")]
 public static void BuildHandCursor(){BuildTo("CoffeeCraftCursor");}
 [MenuItem("Cloud Cafe/Build Assisted Cup Placement")]
 public static void BuildCupPlacement(){BuildTo("CoffeeCraftCup");}
 [MenuItem("Cloud Cafe/Build Easy Latte Pour")]
 public static void BuildEasyLatte(){BuildTo("CoffeeCraftLatte");}
 public static void BuildLatteMouse(){BuildTo("CoffeeCraftLatteMouse");}
 static void BuildTo(string folder){
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Front grind motion").AddComponent<FrontGrindController>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/FrontGrind.unity");
  // Runtime generated materials still need their shader explicitly included in the player.
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=settings.FindProperty("m_AlwaysIncludedShaders");foreach(string shaderName in new[]{"Legacy Shaders/Diffuse","CloudCafe/PixelHandCursor"}){var shader=Shader.Find(shaderName);bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;settings.ApplyModifiedProperties();}}
  PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=810;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
  string output=Path.GetFullPath("../"+folder+"/Build/"+folder+".exe");Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/FrontGrind.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Front grind build failed");Debug.Log("FRONT_GRIND_BUILD_PASSED "+folder);
 }
}
