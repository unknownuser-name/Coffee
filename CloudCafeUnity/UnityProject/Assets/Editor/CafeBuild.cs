using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using CloudCafe;
public sealed class CafeTextureImport : AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Resources/Cafe/"))return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;}
}
public static class CafeBuild {
 [MenuItem("Cloud Cafe/Verify and Build Windows")]
 public static void Build(){
  // UI is constructed at runtime, so its shaders have no scene material reference.
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");
  foreach(string name in new[]{"UI/Default","Sprites/Default"}){var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing required shader "+name);bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;if(!found){int at=included.arraySize;included.InsertArrayElementAtIndex(at);included.GetArrayElementAtIndex(at).objectReferenceValue=shader;}}
  graphics.ApplyModifiedPropertiesWithoutUndo();
  CafeVerification.Run();
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Cafe"})){var path=AssetDatabase.GUIDToAssetPath(guid);var t=(TextureImporter)AssetImporter.GetAtPath(path);if(t.textureType!=TextureImporterType.Sprite||t.filterMode!=FilterMode.Point||t.mipmapEnabled){t.textureType=TextureImporterType.Sprite;t.spritePixelsPerUnit=100;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;t.npotScale=TextureImporterNPOTScale.None;t.SaveAndReimport();}}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var camera=new GameObject("Pixel Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=1.8f;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.105f,.16f);camera.allowMSAA=false;
  new GameObject("Cloud Cafe - Play Controller").AddComponent<CafeController>();
  Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Cafe.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Cafe.unity",true)};
  PlayerSettings.companyName="Cloud Cafe Local";PlayerSettings.productName="Cloud Cafe Prototype";
  PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  var settings=new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
  AssetDatabase.SaveAssets();
  string output=Path.GetFullPath("../Build/CloudCafe.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Cafe.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Cafe build failed: "+report.summary.result);
  File.WriteAllText("../Review/build-result.json","{\"passed\":true,\"target\":\"Windows x64\",\"bytes\":"+report.summary.totalSize+",\"errors\":"+report.summary.totalErrors+"}");Debug.Log("CAFE_UNITY_BUILD_SUCCESS "+output);
 }
}
