using Fomoxa.Unity;
using Generated;
using RedRunner.Networking.Models;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEngine;

namespace RedRunner.Networking
{
    public sealed class NetworkLevel : NetworkBehaviour
    {
        [SerializeField]
        private TerrainGenerator m_Generator;

        private readonly LevelState m_State = new LevelState();
        private PathFollower[] m_Followers = new PathFollower[0];
        private NetworkGame m_Game;

        public uint Seed => m_State.Seed;

        public TerrainGenerator Generator => m_Generator;

        public PathFollower[] Followers => m_Followers;

        private void Awake()
        {
            m_Game = FindFirstObjectByType<NetworkGame>();
        }

        private void OnDestroy()
        {
            if (m_Game != null && m_Game.Manager != null && m_Game.Manager.TimeManager != null)
            {
                m_Game.Manager.TimeManager.OnPreTick -= MoveFollowers;
            }
        }

        protected override void OnRegisterState(NetworkState state)
        {
            state.Use(LevelStateNetAdapter.Instance, m_State, previous => Build());
        }

        public override void OnStartServer()
        {
            m_State.Seed = (uint)new System.Random().Next(1, int.MaxValue);
            Debug.Log($"[RedRunner] level seed {m_State.Seed}");
            Build();
        }

        public override void OnStartClient()
        {
            Build();
        }

        private void Build()
        {
            if (m_State.Seed == 0 || m_Generator.IsBuilt)
            {
                return;
            }

            m_Generator.Build((int)m_State.Seed);
            m_Followers = FindObjectsByType<PathFollower>(FindObjectsSortMode.None);
            m_Game.Manager.TimeManager.OnPreTick += MoveFollowers;
        }

        private void MoveFollowers()
        {
            TimeManager time = m_Game.Manager.TimeManager;
            uint tick = time.PredictionTick;
            float tickDelta = (float)time.TickDelta;
            foreach (PathFollower follower in m_Followers)
            {
                follower.FollowTick(tick, tickDelta);
            }
        }
    }
}
