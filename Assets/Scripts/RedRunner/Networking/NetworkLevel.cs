using System.Collections.Generic;
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;
using Fomoxa.Unity;
using Fomoxa.Unity.Rapier;
using Generated;
using RedRunner.Collectables;
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
        private readonly List<Collider2D> m_StaticSources = new List<Collider2D>();
        private readonly Dictionary<int, List<Collider2D>> m_BodySources = new Dictionary<int, List<Collider2D>>();
        private readonly Dictionary<PathFollower, PhysicsBody2D> m_FollowerBodies = new Dictionary<PathFollower, PhysicsBody2D>();
        private PathFollower[] m_Followers = new PathFollower[0];
        private NetworkGame m_Game;

        public uint Seed => m_State.Seed;

        public TerrainGenerator Generator => m_Generator;

        public PathFollower[] Followers => m_Followers;

        public RapierWorld2D World { get; private set; }

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

        public Collider2D SourceOf(RapierCollider collider)
        {
            if (collider.IsStatic)
            {
                return collider.Index < m_StaticSources.Count ? m_StaticSources[collider.Index] : null;
            }

            return m_BodySources.TryGetValue(collider.Body.Value, out List<Collider2D> sources) && collider.Index < sources.Count ? sources[collider.Index] : null;
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
            if (m_Game.Manager.TryGetComponent(out RapierPhysics rapier))
            {
                AddToRapier(rapier);
            }

            m_Game.Manager.TimeManager.OnPreTick += MoveFollowers;
        }

        private void AddToRapier(RapierPhysics rapier)
        {
            foreach (PathFollower follower in m_Followers)
            {
                if (!follower.TryGetComponent(out Rigidbody2D _))
                {
                    follower.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                }
            }

            foreach (Block block in FindObjectsByType<Block>(FindObjectsSortMode.None).OrderBy(block => block.transform.position.x))
            {
                AddStatics(rapier, block.gameObject);
                AddBodies(rapier, block.gameObject);
            }

            World = rapier.Scenes.WorldOf2D(m_Game.Manager.Scenes.IdOf(gameObject.scene.path));
        }

        private void AddStatics(RapierPhysics rapier, GameObject block)
        {
            var listed = new List<Collider2D>();
            var sources = new List<Collider2D>();
            var colliders = new List<ColliderDesc2D>();
            BodyDescriptions.StaticColliders2D(block, listed);
            for (int index = 0; index < listed.Count; index++)
            {
                Collider2D source = listed[index];
                if ((index > 0 && source == listed[index - 1]) || source.GetComponentInParent<Collectable>() != null)
                {
                    continue;
                }

                BodyDescriptions.DescribeStatic2D(source, null, colliders);
                while (sources.Count < colliders.Count)
                {
                    sources.Add(source);
                }
            }

            StaticGroup group = rapier.AddStatic2D(gameObject.scene, colliders);
            while (m_StaticSources.Count < group.FirstIndex + sources.Count)
            {
                m_StaticSources.Add(null);
            }

            for (int index = 0; index < sources.Count; index++)
            {
                m_StaticSources[group.FirstIndex + index] = sources[index];
            }
        }

        private void AddBodies(RapierPhysics rapier, GameObject block)
        {
            foreach (Rigidbody2D rigidbody in block.GetComponentsInChildren<Rigidbody2D>())
            {
                if (rigidbody.bodyType == RigidbodyType2D.Dynamic || rigidbody.GetComponentInParent<Collectable>() != null)
                {
                    continue;
                }

                var sources = new List<Collider2D>();
                if (!BodyDescriptions.TryDescribe2D(rigidbody.gameObject, out BodyDesc2D desc, sources))
                {
                    continue;
                }

                PhysicsBody2D body = rapier.AddBody2D(gameObject.scene, desc);
                m_BodySources[body.Handle.Value] = sources;
                if (rigidbody.TryGetComponent(out PathFollower follower))
                {
                    m_FollowerBodies[follower] = body;
                }
            }
        }

        private void MoveFollowers()
        {
            TimeManager time = m_Game.Manager.TimeManager;
            uint tick = time.PredictionTick;
            float tickDelta = (float)time.TickDelta;
            foreach (PathFollower follower in m_Followers)
            {
                follower.FollowTick(tick, tickDelta);
                if (follower.CycleLength > 0 && m_FollowerBodies.TryGetValue(follower, out PhysicsBody2D body))
                {
                    Vector3 position = follower.PositionAt(tick);
                    BodyState2D state = body.State;
                    state.Position = new System.Numerics.Vector2(position.x, position.y);
                    body.State = state;
                    follower.transform.position = position;
                }
            }
        }
    }
}
