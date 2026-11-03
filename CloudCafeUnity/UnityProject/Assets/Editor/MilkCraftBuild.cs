using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using CloudCafe.Crafting;
public static class MilkCraftBuild {
 [MenuItem("Cloud Cafe/Build Direct Milk Craft")]
 public static void Build(){
  foreach(string name in new[]{"image_21","image_22","image_23","image_24","image_25","revision_counter"}){var t=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Cafe/"+name+".png");if(!t)throw new Exception("Missing own art "+name);t.isReadable=true;t.filterMode=FilterMode.Point;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Craft Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.08f,.11f);camera.cullingMask=0;
  new GameObject("Direct Milk Craft - own assets").AddComponent<MilkCraftController>();
  Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/MilkCraft.unity");
  PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
  string output=Path.GetFullPath("../BuildMilkCraft/MilkCraft.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MilkCraft.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Milk craft build failed: "+report.summary.result);
  Debug.Log("MILK_CRAFT_BUILD_PASSED "+output);
 }
}
