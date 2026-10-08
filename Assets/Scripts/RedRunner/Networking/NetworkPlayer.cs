using System;
using Fomoxa.Networking;
using Fomoxa.Networking.Simulation;
using Fomoxa.Unity;
using Generated;
using RedRunner.Characters;
using RedRunner.Networking.Models;
using RedRunner.TerrainGeneration;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.Networking
{
    [RequireComponent(typeof(RedCharacter))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private const float PositionTolerance = 0.01f;
        private const float VelocityTolerance = 0.05f;

        [SerializeField]
        private float m_RespawnDelay = 2f;

        [SerializeField]
        private int m_MaxRepeatedTicks = 10;

        private readonly PlayerState m_State = new PlayerState();
        private RedCharacter m_Character;
        private NetworkTransform m_NetworkTransform;
        private NetworkGame m_Game;
        private bool m_JumpPressed;
        private bool m_RollPressed;
        private uint m_RespawnTick;

        public RedCharacter Character => m_Character;

        public bool IsDead => m_State.Dead;

        private void Awake()
        {
            m_Character = GetComponent<RedCharacter>();
            m_NetworkTransform = GetComponent<NetworkTransform>();
            m_Game = FindFirstObjectByType<NetworkGame>();
        }

        protected override void OnRegisterInput(NetworkInput input)
        {
            input.Use(PlayerInputNetAdapter.Instance, Gather, Apply);
            input.Reconcile(PlayerMotionNetAdapter.Instance, Capture, Restore, Matches);
        }

        protected override void OnRegisterState(NetworkState state)
        {
            state.Use(PlayerStateNetAdapter.Instance, m_State, previous => ShowState());
        }

        public override void OnStartServer()
        {
            m_Character.DecidesDeath = true;
            m_Character.CollectsItems = false;
            m_Game.Manager.TimeManager.OnTick += ServerTick;
        }

        public override void OnStopServer()
        {
            m_Game.Manager.TimeManager.OnTick -= ServerTick;
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

        private void Gather(PlayerInput input)
        {
            bool playing = GameManager.Singleton.gameRunning;
            input.Move = playing ? CrossPlatformInputManager.GetAxis("Horizontal") : 0f;
            input.Jump = playing && m_JumpPressed;
            input.Roll = playing && m_RollPressed;
            m_JumpPressed = false;
            m_RollPressed = false;
        }

        private void Apply(PlayerInput input, InputContext context)
        {
            float tickDelta = (float)m_Game.Manager.TimeManager.TickDelta;
            bool fresh = !context.Repeated;
            float move = context.RepeatedTicks > m_MaxRepeatedTicks ? 0f : input.Move;
            m_Character.Simulate(move, fresh && input.Jump, fresh && input.Roll, tickDelta, !context.Replaying);
        }

        private void ServerTick()
        {
            TimeManager time = m_Game.Manager.TimeManager;
            if (m_State.Dead)
            {
                if (time.Tick >= m_RespawnTick)
                {
                    Respawn();
                }

                return;
            }

            if (!m_Character.IsDead.Value && transform.position.y < 0f)
            {
                m_Character.Die(DeathCause.Fall);
            }

            if (m_Character.IsDead.Value)
            {
                m_State.Dead = true;
                m_State.Cause = (byte)m_Character.DeathCause;
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
            PhysicsBody2D body = NetworkObject.Body2D;
            BodyState2D state = body.State;
            state.Position = new System.Numerics.Vector2(m_Game.SpawnPoint.x, m_Game.SpawnPoint.y);
            state.Velocity = System.Numerics.Vector2.Zero;
            body.State = state;
            m_NetworkTransform.Teleport();
            m_Character.RunSmoothVelocity = 0f;
            m_State.Dead = false;
            m_State.Cause = 0;
        }

        private void ShowState()
        {
            if (m_Game.IsServer)
            {
                return;
            }

            if (m_State.Dead && !m_Character.IsDead.Value)
            {
                m_Character.Die((DeathCause)m_State.Cause);
            }
            else if (!m_State.Dead && m_Character.IsDead.Value)
            {
                m_Character.Reset();
            }
        }

        private void Capture(PlayerMotion motion)
        {
            BodyState2D state = NetworkObject.Body2D.State;
            motion.X = state.Position.X;
            motion.Y = state.Position.Y;
            motion.VelocityX = state.Velocity.X;
            motion.VelocityY = state.Velocity.Y;
            motion.RunSmoothVelocity = m_Character.RunSmoothVelocity;
        }

        private void Restore(PlayerMotion motion)
        {
            PhysicsBody2D body = NetworkObject.Body2D;
            BodyState2D state = body.State;
            state.Position = new System.Numerics.Vector2(motion.X, motion.Y);
            state.Velocity = new System.Numerics.Vector2(motion.VelocityX, motion.VelocityY);
            body.State = state;
            m_Character.RunSmoothVelocity = motion.RunSmoothVelocity;
        }

        private static bool Matches(PlayerMotion server, PlayerMotion predicted) =>
            Math.Abs(server.X - predicted.X) <= PositionTolerance
            && Math.Abs(server.Y - predicted.Y) <= PositionTolerance
            && Math.Abs(server.VelocityX - predicted.VelocityX) <= VelocityTolerance
            && Math.Abs(server.VelocityY - predicted.VelocityY) <= VelocityTolerance
            && Math.Abs(server.RunSmoothVelocity - predicted.RunSmoothVelocity) <= VelocityTolerance;
    }
}
