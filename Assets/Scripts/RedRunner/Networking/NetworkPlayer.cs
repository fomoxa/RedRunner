using System.Collections.Generic;
using Fomoxa.Networking;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;
using Fomoxa.Unity;
using Fomoxa.Unity.Rapier;
using RedRunner.Characters;
using RedRunner.Simulation;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.Networking
{
    [RequireComponent(typeof(RedCharacter))]
    [NetworkStateModel("PlayerState")]
    [NetworkInputModel("PlayerInput")]
    [NetworkReconcileModel("PlayerMotion")]
    public sealed partial class NetworkPlayer : NetworkBehaviour
    {
        private const float PositionTolerance = 0.01f;
        private const float VelocityTolerance = 0.05f;

        [SerializeField]
        private float m_RespawnDelay = 2f;

        [SerializeField]
        private int m_MaxRepeatedTicks = 10;

        [NetworkState(OnChanged = nameof(OnDeathChanged))]
        private bool dead;

        [NetworkState(OnChanged = nameof(OnCauseChanged))]
        private byte cause;

        [NetworkInput]
        private float move;

        [NetworkInput]
        private bool jump;

        [NetworkInput]
        private bool roll;

        [NetworkReconcile(Tolerance = PositionTolerance)]
        private float x;

        [NetworkReconcile(Tolerance = PositionTolerance)]
        private float y;

        [NetworkReconcile(Tolerance = VelocityTolerance)]
        private float velocityX;

        [NetworkReconcile(Tolerance = VelocityTolerance)]
        private float velocityY;

        [NetworkReconcile(Tolerance = VelocityTolerance)]
        private float runSmoothVelocity;

        private RedCharacter m_Character;
        private NetworkTransform m_NetworkTransform;
        private NetworkGame m_Game;
        private bool m_JumpPressed;
        private bool m_RollPressed;
        private uint m_RespawnTick;
        private bool m_OnRapier;
        private bool m_Held;
        private Vector2 m_HeldAt;
        private BoxCollider2D m_Sensor;
        private NetworkLevel m_Level;
        private readonly List<Collider2D> m_Sources = new List<Collider2D>();
        private readonly List<RapierCollider> m_Touching = new List<RapierCollider>();

        public RedCharacter Character => m_Character;

        public float RespawnDelay => m_RespawnDelay;

        public int MaxRepeatedTicks => m_MaxRepeatedTicks;

        public bool IsDead => dead;

        private void Awake()
        {
            m_Character = GetComponent<RedCharacter>();
            m_NetworkTransform = GetComponent<NetworkTransform>();
            m_Game = FindFirstObjectByType<NetworkGame>();
            m_OnRapier = m_Game.Manager.TryGetComponent(out RapierPhysics _);
            m_Character.BodySource = () => NetworkObject != null ? NetworkObject.Body2D : default;
            if (m_OnRapier)
            {
                m_Sensor = AddGroundSensor(m_Character);
                m_Character.GroundCheck.Probe = Grounded;
            }
        }

        public static BoxCollider2D AddGroundSensor(RedCharacter character)
        {
            GroundCheck check = character.GroundCheck;
            Collider2D probe = check.Collider2D;
            Vector2 offset = probe is BoxCollider2D box ? box.offset : probe.offset;
            Vector2 size = probe is BoxCollider2D sized ? sized.size : (Vector2)probe.bounds.size;
            var sensorObject = new GameObject("Ground Sensor");
            sensorObject.transform.SetParent(probe.transform, false);
            var sensor = sensorObject.AddComponent<BoxCollider2D>();
            sensor.isTrigger = true;
            sensor.size = new Vector2(size.x, check.RayDistance);
            sensor.offset = new Vector2(offset.x, offset.y - check.RayDistance * 0.5f);
            return sensor;
        }

        private bool Grounded() =>
            TryGetWorld(out RapierWorld2D world, out BodyHandle body) && RunnerContacts.Grounded(world, body, m_Sources.IndexOf(m_Sensor), m_Level, m_Touching);

        private void DieOfHazards()
        {
            if (!TryGetWorld(out RapierWorld2D world, out BodyHandle body))
            {
                return;
            }

            switch (RunnerContacts.Hazard(world, body, m_Sources.Count, Bottom(), m_Level, m_Touching))
            {
                case Surface.Water:
                    m_Character.Die(DeathCause.Water);
                    break;
                case Surface.Saw:
                    m_Character.Die(DeathCause.Saw);
                    break;
                case Surface.Spike:
                    m_Character.Die(DeathCause.Spike);
                    break;
                case Surface.Mace:
                    m_Character.Die(DeathCause.Mace);
                    break;
            }
        }

        private bool TryGetWorld(out RapierWorld2D world, out BodyHandle body)
        {
            if (m_Level == null)
            {
                m_Level = FindFirstObjectByType<NetworkLevel>();
            }

            NetworkBody2D networkBody = NetworkObject != null ? NetworkObject.Body2D : default;
            world = m_Level != null ? m_Level.World : null;
            body = networkBody.Handle;
            if (world == null || !networkBody.IsValid)
            {
                return false;
            }

            if (m_Sources.Count == 0)
            {
                BodyDescriptions.TryDescribe2D(gameObject, out _, m_Sources);
            }

            return true;
        }

        private void HoldWhileDead()
        {
            NetworkBody2D body = NetworkObject != null ? NetworkObject.Body2D : default;
            if (!m_OnRapier || !body.IsValid)
            {
                return;
            }

            if (!m_Character.IsDead.Value)
            {
                m_Held = false;
                return;
            }

            if (!m_Held)
            {
                m_Held = true;
                m_HeldAt = body.Position;
            }

            body.State = new NetworkBodyState2D { Position = m_HeldAt, Rotation = body.Rotation };
        }

        private float Bottom()
        {
            var capsule = (CapsuleCollider2D)m_Character.Collider2D;
            return NetworkObject.Body2D.Position.y + capsule.offset.y - capsule.size.y * 0.5f;
        }

        public override void OnStartServer()
        {
            m_Character.DecidesDeath = !m_OnRapier;
            m_Character.CollectsItems = false;
            NetworkManager.TimeManager.OnTick += ServerTick;
        }

        public override void OnStopServer()
        {
            NetworkManager.TimeManager.OnTick -= ServerTick;
        }

        public override void OnStartClient()
        {
            m_Character.CollectsItems = NetworkObject.IsOwner;
            if (!m_Game.IsServer)
            {
                m_Character.DecidesDeath = false;
                ShowState();
            }

            if (NetworkObject.IsOwner)
            {
                m_Game.SetLocalPlayer(this);
            }
        }

        public override void OnStopClient()
        {
            m_Game.ClearLocalPlayer(this);
        }

        private void Update()
        {
            if (NetworkObject == null || !NetworkObject.IsOwner)
            {
                return;
            }

            m_JumpPressed |= CrossPlatformInputManager.GetButtonDown("Jump");
            m_RollPressed |= CrossPlatformInputManager.GetButtonDown("Roll");
        }

        protected override void OnGatherInput()
        {
            bool playing = GameManager.Singleton.gameRunning;
            move = playing ? CrossPlatformInputManager.GetAxis("Horizontal") : 0f;
            jump = playing && m_JumpPressed;
            roll = playing && m_RollPressed;
            m_JumpPressed = false;
            m_RollPressed = false;
        }

        protected override void OnApplyInput(InputContext context)
        {
            float tickDelta = (float)NetworkManager.TimeManager.TickDelta;
            bool fresh = !context.Repeated;
            float axis = context.RepeatedTicks > m_MaxRepeatedTicks ? 0f : move;
            m_Character.Simulate(axis, fresh && jump, fresh && roll, tickDelta, !context.Replaying);
            HoldWhileDead();
        }

        private void ServerTick()
        {
            TimeManager time = NetworkManager.TimeManager;
            if (dead)
            {
                if (time.Tick >= m_RespawnTick)
                {
                    Respawn();
                }

                return;
            }

            if (m_OnRapier && !m_Character.IsDead.Value)
            {
                DieOfHazards();
            }

            HoldWhileDead();

            if (!m_Character.IsDead.Value && transform.position.y < 0f)
            {
                m_Character.Die(DeathCause.Fall);
            }

            if (m_Character.IsDead.Value)
            {
                dead = true;
                cause = (byte)m_Character.DeathCause;
                m_RespawnTick = time.Tick + (uint)Mathf.CeilToInt(m_RespawnDelay * time.TickRate);
                Debug.Log($"[RedRunner] player {NetworkObject.OwnerId} died: {m_Character.DeathCause}");
            }
            else if (TerrainGenerator.Singleton.IsBuilt && transform.position.x >= TerrainGenerator.Singleton.EndX)
            {
                Debug.Log($"[RedRunner] player {NetworkObject.OwnerId} reached the end block");
                Respawn();
            }
        }

        private void Respawn()
        {
            m_Character.Reset();
            NetworkBody2D body = NetworkObject.Body2D;
            NetworkBodyState2D state = body.State;
            state.Position = m_Game.SpawnPoint;
            state.Velocity = Vector2.zero;
            body.State = state;
            transform.position = new Vector3(m_Game.SpawnPoint.x, m_Game.SpawnPoint.y, transform.position.z);
            m_NetworkTransform.Teleport();
            m_Character.RunSmoothVelocity = 0f;
            dead = false;
            cause = 0;
        }

        private void OnDeathChanged(bool previous, bool current) => ShowState();

        private void OnCauseChanged(byte previous, byte current) => ShowState();

        private void ShowState()
        {
            if (m_Game.IsServer)
            {
                return;
            }

            if (dead && !m_Character.IsDead.Value)
            {
                m_Character.Die((DeathCause)cause);
            }
            else if (!dead && m_Character.IsDead.Value)
            {
                m_Character.Reset();
            }
        }

        protected override void OnCaptureState()
        {
            NetworkBodyState2D state = NetworkObject.Body2D.State;
            x = state.Position.x;
            y = state.Position.y;
            velocityX = state.Velocity.x;
            velocityY = state.Velocity.y;
            runSmoothVelocity = m_Character.RunSmoothVelocity;
        }

        protected override void OnRestoreState()
        {
            NetworkBody2D body = NetworkObject.Body2D;
            NetworkBodyState2D state = body.State;
            state.Position = new Vector2(x, y);
            state.Velocity = new Vector2(velocityX, velocityY);
            body.State = state;
            m_Character.RunSmoothVelocity = runSmoothVelocity;
        }
    }
}
