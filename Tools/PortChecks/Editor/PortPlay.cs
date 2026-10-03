using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortPlay
    {
        public static void Connection() => Enter<ConnectionCheck>();

        public static void Level() => Enter<LevelCheck>();

        public static void Join() => Enter<JoinCheck>();

        public static void Host() => Enter<HostCheck>();

        public static void Move() => Enter<MoveCheck>();

        public static void HostLife() => Enter<HostLifeCheck>();

        public static void Life() => Enter<LifeCheck>();

        public static void Path() => Enter<PathCheck>();

        public static void Crowd() => Enter<CrowdCheck>();

        public static void Crash() => Enter<CrashCheck>();

        private static void Enter<TCheck>() where TCheck : MonoBehaviour
        {
            EditorSceneManager.OpenScene(PortSetup.PlayScene, OpenSceneMode.Single);
            new GameObject(typeof(TCheck).Name).AddComponent<TCheck>();
            Debug.Log($"PORT-CHECK {typeof(TCheck).Name}: entering play mode");
            EditorApplication.isPlaying = true;
        }
    }
}
