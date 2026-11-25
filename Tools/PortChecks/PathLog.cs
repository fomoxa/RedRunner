using System;
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Unity;
using RedRunner.Networking;
using RedRunner.Utilities;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class PathLog : MonoBehaviour
    {
        public const int Every = 10;

        private NetworkGame m_Game;
        private NetworkLevel m_Level;
        private PathFollower m_Follower;
        private bool m_Ticking;

        public static PathFollower FirstOf(NetworkLevel level) =>
            level.Followers
                .Where(follower => follower.CycleLength > 1)
                .OrderBy(follower => follower.PositionAt(0).x)
                .ThenBy(follower => follower.PositionAt(0).y)
                .FirstOrDefault();

        public static string Describe(PathFollower follower)
        {
            Vector3 start = follower.PositionAt(0);
            return FormattableString.Invariant($"{follower.name} {start.x:F3} {start.y:F3}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-log-paths") >= 0)
            {
                new GameObject(nameof(PathLog)).AddComponent<PathLog>();
            }
        }

        private void Update()
        {
            if (m_Ticking)
            {
                return;
            }

            m_Game = m_Game != null ? m_Game : FindFirstObjectByType<NetworkGame>();
            m_Level = m_Level != null ? m_Level : FindFirstObjectByType<NetworkLevel>();
            if (m_Game == null || m_Level == null || m_Level.Followers.Length == 0)
            {
                return;
            }

            m_Ticking = true;
            m_Game.Manager.TimeManager.OnTick += Log;
        }

        private void Log()
        {
            if (m_Follower == null)
            {
                m_Follower = FirstOf(m_Level);
                if (m_Follower == null)
                {
                    m_Game.Manager.TimeManager.OnTick -= Log;
                    return;
                }

                Debug.Log($"PORT-PATH-FOLLOWER {Describe(m_Follower)}");
            }

            uint tick = m_Game.Manager.TimeManager.PredictionTick;
            if (tick % Every == 0)
            {
                Vector3 position = m_Follower.transform.position;
                Debug.Log(FormattableString.Invariant($"PORT-PATH {tick} {position.x:F3} {position.y:F3}"));
            }
        }
    }
}
