using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortPlay
    {
        public static void Connection() => Enter<ConnectionCheck>();

        public static void Level() => Enter<LevelCheck>();

        private static void Enter<TCheck>() where TCheck : MonoBehaviour
        {
            EditorSceneManager.OpenScene(PortSetup.PlayScene, OpenSceneMode.Single);
            new GameObject(typeof(TCheck).Name).AddComponent<TCheck>();
            Debug.Log($"PORT-CHECK {typeof(TCheck).Name}: entering play mode");
            EditorApplication.isPlaying = true;
        }
    }
}
