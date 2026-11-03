using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CloudCafe.Crafting;
public static class GrindStationBuild {
 [MenuItem("Cloud Cafe/Build Grind Station")]
 public static void Build(){
  foreach(string name in new[]{"revision_grinder","revision_counter","image_30","image_31"}){var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Cafe/"+name+".png");importer.isReadable=true;importer.SaveAndReimport();}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Workbench camera").AddComponent<Camera>();camera.tag="MainCamera";camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.08f,.12f);
  new GameObject("Grind station - direct manipulation").AddComponent<GrindStationController>();Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/GrindStation.unity");
  PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
  string output=Path.GetFullPath("../GrindStation/Build/GrindStation.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
  var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/GrindStation.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Grind build failed "+result.summary.result);Debug.Log("GRIND_STATION_BUILD_PASSED "+output);
 }
}
