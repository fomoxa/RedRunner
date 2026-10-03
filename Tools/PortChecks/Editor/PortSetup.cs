using System.IO;
using System.Linq;
using Fomoxa.Unity;
using Fomoxa.Unity.Editor;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
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
        public const string LevelName = "Network Level";
        public const string GenerationSettingsPath = "Assets/Generation Settings.asset";
        public const string StartBlockPath = "Assets/Prefabs/Blocks/Start.prefab";
        public const int MiddleBlocks = 20;

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

        public static void Level()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TerrainGenerationSettings>(GenerationSettingsPath));
            settings.FindProperty("m_MiddleBlocksCount").intValue = MiddleBlocks;
            settings.FindProperty("m_EndBlocksCount").intValue = 1;
            SerializedProperty endBlocks = settings.FindProperty("m_EndBlocks");
            endBlocks.arraySize = 1;
            endBlocks.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Block>(StartBlockPath);
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            NetworkLevel level = FindInScene<NetworkLevel>(scene);
            if (level == null)
            {
                var levelObject = new GameObject(LevelName);
                SceneManager.MoveGameObjectToScene(levelObject, scene);
                levelObject.AddComponent<NetworkObject>();
                level = levelObject.AddComponent<NetworkLevel>();
            }

            var levelSerialized = new SerializedObject(level);
            levelSerialized.FindProperty("m_Generator").objectReferenceValue = FindInScene<TerrainGenerator>(scene);
            levelSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"PORT-CHECK level setup: PASS scene object id {level.GetComponent<NetworkObject>().SceneObjectId:X}");
        }

        private static T FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<T>(true))
                .FirstOrDefault(found => found != null);
    }
}
