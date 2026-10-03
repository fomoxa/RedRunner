using System.IO;
using System.Linq;
using Fomoxa.Unity;
using Fomoxa.Unity.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedRunner.PortChecks
{
    public static class PortSetup
    {
        public const string PlayScene = "Assets/Scenes/Play.unity";
        public const string NetworkingFolder = "Assets/Scripts/RedRunner/Networking";
        public const string ModelsFolder = NetworkingFolder + "/Models";
        public const string GeneratedFolder = NetworkingFolder + "/Generated";
        public const string SettingsPath = NetworkingFolder + "/FomoxaSettings.asset";
        public const string ManagerName = "Network Manager";

        public static void Networking()
        {
            Directory.CreateDirectory(ModelsFolder);
            File.WriteAllText(Path.Combine(ModelsFolder, ".gitkeep"), "");
            AssetDatabase.Refresh();

            if (AssetDatabase.LoadAssetAtPath<FomoxaSettings>(SettingsPath) == null)
            {
                var settings = ScriptableObject.CreateInstance<FomoxaSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(AssetDatabase.LoadAssetAtPath<FomoxaSettings>(SettingsPath));
            serialized.FindProperty("modelsFolder").stringValue = ModelsFolder;
            serialized.FindProperty("generatedFolder").stringValue = GeneratedFolder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PlayScene, true) };
            FomoxaHook.GenerateFromMenu();
            FomoxaSceneStep.RunAndLog();

            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            NetworkManager manager = scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<NetworkManager>(true))
                .FirstOrDefault(found => found != null);
            if (manager == null)
            {
                var managerObject = new GameObject(ManagerName);
                SceneManager.MoveGameObjectToScene(managerObject, scene);
                managerObject.AddComponent<UdpNetworkTransport>();
                manager = managerObject.AddComponent<NetworkManager>();
            }

            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("transport").objectReferenceValue = manager.GetComponent<UdpNetworkTransport>();
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                Debug.Log($"PORT-CHECK build scene: {buildScene.path} enabled={buildScene.enabled}");
            }

            Debug.Log("PORT-CHECK setup: PASS");
        }
    }
}
