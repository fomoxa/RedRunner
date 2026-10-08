using System.IO;
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Unity;
using Fomoxa.Unity.Editor;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RedRunner.Characters;
using RedRunner.UI;

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
        public const string PlayerPrefabPath = "Assets/Prefabs/RedRunner.prefab";
        public const int TickRate = 50;

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

        public static void Joining()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefab.GetComponent<NetworkObject>() == null)
            {
                prefab.AddComponent<NetworkObject>();
            }

            NetworkTransform networkTransform = prefab.GetComponent<NetworkTransform>() ?? prefab.AddComponent<NetworkTransform>();
            var transformSerialized = new SerializedObject(networkTransform);
            transformSerialized.FindProperty("syncRotation").boolValue = false;
            transformSerialized.FindProperty("syncScale").boolValue = true;
            transformSerialized.ApplyModifiedPropertiesWithoutUndo();
            if (prefab.GetComponent<NetworkPlayer>() == null)
            {
                prefab.AddComponent<NetworkPlayer>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            FomoxaPrefabStep.RunAndLog();

            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Character>() != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            NetworkManager manager = FindInScene<NetworkManager>(scene);
            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("tickRate").intValue = TickRate;
            managerSerialized.FindProperty("maxReconnectRetries").intValue = 3;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
            SimulatePhysics(manager);

            FomoxaPrefabStep.AssignCollected(scene, FomoxaPrefabStep.FindList());
            FomoxaSceneStep.AssignCollected(scene, FomoxaSceneStep.FindList());

            NetworkGame game = manager.GetComponent<NetworkGame>() ?? manager.gameObject.AddComponent<NetworkGame>();
            var gameSerialized = new SerializedObject(game);
            gameSerialized.FindProperty("m_Manager").objectReferenceValue = manager;
            gameSerialized.FindProperty("m_PlayerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NetworkObject>(PlayerPrefabPath);
            gameSerialized.ApplyModifiedPropertiesWithoutUndo();

            StartScreen startScreen = FindInScene<StartScreen>(scene);
            InputField address = startScreen.GetComponentInChildren<InputField>(true);
            if (address == null)
            {
                address = CreateAddressField(startScreen.transform, startScreen.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true).First().font);
            }

            var startSerialized = new SerializedObject(startScreen);
            startSerialized.FindProperty("AddressField").objectReferenceValue = address;
            startSerialized.FindProperty("Network").objectReferenceValue = game;
            startSerialized.ApplyModifiedPropertiesWithoutUndo();

            var pauseSerialized = new SerializedObject(FindInScene<PauseScreen>(scene));
            pauseSerialized.FindProperty("Network").objectReferenceValue = game;
            pauseSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"PORT-CHECK joining setup: PASS player prefab id {AssetDatabase.LoadAssetAtPath<NetworkObject>(PlayerPrefabPath).PrefabId:X8}");
        }

        public static void PhysicsSimulation()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            NetworkManager manager = FindInScene<NetworkManager>(scene);
            SimulatePhysics(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"PORT-CHECK physics setup: PASS {manager.GetComponent<RigidbodyPhysics>().Simulates}");
        }

        private static void SimulatePhysics(NetworkManager manager)
        {
            RigidbodyPhysics physics = manager.GetComponent<RigidbodyPhysics>() ?? manager.gameObject.AddComponent<RigidbodyPhysics>();
            var physicsSerialized = new SerializedObject(physics);
            physicsSerialized.FindProperty("simulatePhysics").boolValue = true;
            physicsSerialized.ApplyModifiedPropertiesWithoutUndo();
            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("physics").objectReferenceValue = physics;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Score()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            Transform inGame = FindInScene<InGameScreen>(scene).transform;
            foreach (string hidden in new[] { "Coin Panel", "High Score Text", "Score Text" })
            {
                inGame.Find(hidden).gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("PORT-CHECK score setup: PASS");
        }

        private static InputField CreateAddressField(Transform parent, Font font)
        {
            var resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            };
            GameObject fieldObject = DefaultControls.CreateInputField(resources);
            fieldObject.name = "Address Field";
            fieldObject.layer = parent.gameObject.layer;
            foreach (Transform child in fieldObject.transform)
            {
                child.gameObject.layer = parent.gameObject.layer;
            }

            var rect = (RectTransform)fieldObject.transform;
            rect.SetParent(parent, false);
            InputField field = fieldObject.GetComponent<InputField>();
            field.text = "localhost";
            foreach (Text text in fieldObject.GetComponentsInChildren<Text>(true))
            {
                text.font = font;
            }

            LayOutAddressField(field);

            ((Text)field.placeholder).text = "Server address";
            return field;
        }

        public static void AddressFieldLayout()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            LayOutAddressField(FindInScene<StartScreen>(scene).GetComponentInChildren<InputField>(true));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("PORT-CHECK address field layout: PASS");
        }

        private static void LayOutAddressField(InputField field)
        {
            var rect = (RectTransform)field.transform;
            rect.anchoredPosition = new Vector2(0f, -222f);
            rect.sizeDelta = new Vector2(320f, 48f);
            foreach (Text text in field.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<T>(true))
                .FirstOrDefault(found => found != null);
    }
}
