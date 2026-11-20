using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PicoPark.EditorTools
{
    public static class PicoParkMenu
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Pico Park/创建或重建主场景", false, 10)]
        public static void CreateMainScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("PicoPark");
            go.AddComponent<GameBootstrap>();

            GameObject light = new GameObject("Light");
            Light l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PicoPark] 主场景已生成: " + ScenePath);
        }

        [MenuItem("Pico Park/生成 Steam AppID 文件", false, 11)]
        public static void WriteAppId()
        {
            string root = System.IO.Directory.GetParent(Application.dataPath).FullName;
            System.IO.File.WriteAllText(System.IO.Path.Combine(root, "steam_appid.txt"), "480");
            AssetDatabase.Refresh();
            Debug.Log("[PicoPark] steam_appid.txt = 480");
        }
    }
}
