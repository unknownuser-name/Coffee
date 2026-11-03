#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Start the cafe without replacing saved or modified Editor work.</summary>
[InitializeOnLoad]
public static class CoffeeProjectEntry
{
    private const string StartScene = "Assets/Scenes/FrontGrind.unity";
    private const string CheckedThisSession = "CloudCafe.CheckedStartScene";

    static CoffeeProjectEntry()
    {
        if (!Application.isBatchMode) EditorApplication.delayCall += OpenCleanUntitled;
    }

    private static void OpenCleanUntitled()
    {
        if (Application.isBatchMode || SessionState.GetBool(CheckedThisSession, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += OpenCleanUntitled;
            return;
        }
        if (SceneManager.sceneCount == 0)
        {
            EditorApplication.delayCall += OpenCleanUntitled;
            return;
        }
        SessionState.SetBool(CheckedThisSession, true);
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1) return;
        Scene current = SceneManager.GetSceneAt(0);
        if (!current.IsValid() || current.isDirty || !string.IsNullOrEmpty(current.path)) return;
        if (File.Exists(StartScene)) EditorSceneManager.OpenScene(StartScene, OpenSceneMode.Single);
    }

    [MenuItem("Cloud Cafe/Open Coffee Game")]
    public static void OpenStartScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path == StartScene) return;
        if (!File.Exists(StartScene)) throw new FileNotFoundException("Coffee start scene is missing.", StartScene);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(StartScene, OpenSceneMode.Single);
    }

    public static void ValidateEntryScene()
    {
        if (!File.Exists(StartScene) || AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScene) == null)
            throw new FileNotFoundException("The coffee entry scene could not be loaded.", StartScene);
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0 || !scenes[0].enabled || scenes[0].path != StartScene)
            throw new InvalidDataException("FrontGrind must be the first enabled build scene.");
        CoffeeBagCatalogEditor.ValidateRegisteredAssets();
        Debug.Log("COFFEE_PROJECT_ENTRY_VALIDATED");
    }
}
#endif
