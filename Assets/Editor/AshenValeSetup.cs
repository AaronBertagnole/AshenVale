using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class AshenValeSetup
{
    const string ScenePath="Assets/Scenes/AshenVale.unity";
    static AshenValeSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(ScenePath)) CreateScene();
        };
    }
    [MenuItem("Ashen Vale/Rebuild Prototype Scene")]
    public static void CreateScene()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera");
        camera.tag="MainCamera";
        var c=camera.AddComponent<Camera>();c.orthographic=true;c.orthographicSize=5.4f;
        camera.transform.position=new Vector3(0,0,-10);
        var root=new GameObject("Ashen Vale Game");root.AddComponent<AshenValeGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();
        Debug.Log("Ashen Vale scene created. Open Assets/Scenes/AshenVale.unity and press Play.");
    }
}
