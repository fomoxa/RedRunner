using Fomoxa.Unity;
using Generated;
using RedRunner.Networking.Models;
using RedRunner.TerrainGeneration;
using UnityEngine;

namespace RedRunner.Networking
{
    public sealed class NetworkLevel : NetworkBehaviour
    {
        [SerializeField]
        private TerrainGenerator m_Generator;

        private readonly LevelState m_State = new LevelState();

        public uint Seed => m_State.Seed;

        public TerrainGenerator Generator => m_Generator;

        protected override void OnRegisterState(NetworkState state)
        {
            state.Use(LevelStateNetAdapter.Instance, m_State, changed => Build());
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
            if (m_State.Seed != 0)
            {
                m_Generator.Build((int)m_State.Seed);
            }
        }
    }
}
