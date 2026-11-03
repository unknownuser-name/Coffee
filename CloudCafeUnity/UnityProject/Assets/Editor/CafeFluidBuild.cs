using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using CloudCafe;
public sealed class FluidArtImport:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Resources/FluidArt/"))return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.npotScale=TextureImporterNPOTScale.None;}
}
public static class CafeFluidBuild {
 [MenuItem("Cloud Cafe/Build Fluid Integration")]
 public static void Build(){
  CafeVerification.Run();
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");
  foreach(string name in new[]{"UI/Default","Sprites/Default","CloudCafe/PaintedVessel","MilkStudy/SPHMilk"}){var shader=Shader.Find(name);if(!shader)throw new Exception("Missing shader "+name);bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int at=included.arraySize;included.InsertArrayElementAtIndex(at);included.GetArrayElementAtIndex(at).objectReferenceValue=shader;}}graphics.ApplyModifiedPropertiesWithoutUndo();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Pixel Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=1.8f;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.105f,.16f);camera.allowMSAA=false;
  new GameObject("Cloud Cafe - Play Controller").AddComponent<CafeController>();Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/CafeFluid.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/CafeFluid.unity",true)};
  PlayerSettings.companyName="Cloud Cafe Local";PlayerSettings.productName="Cloud Cafe Prototype";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.colorSpace=ColorSpace.Gamma;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);var settings=new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}AssetDatabase.SaveAssets();
  string output=Path.GetFullPath("../BuildFluidCafe/CloudCafe.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/CafeFluid.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed "+report.summary.result);Debug.Log("CAFE_FLUID_BUILD_SUCCESS "+output);
 }
}
