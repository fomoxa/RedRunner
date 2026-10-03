using System;
using Fomoxa.Unity;
using Fomoxa.Unity.Simulation;
using Generated;
using RedRunner.Characters;
using RedRunner.Networking.Models;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.Networking
{
    [RequireComponent(typeof(RedCharacter))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private const float PositionTolerance = 0.01f;
        private const float VelocityTolerance = 0.05f;

        private RedCharacter m_Character;
        private NetworkGame m_Game;
        private bool m_JumpPressed;
        private bool m_RollPressed;

        public RedCharacter Character => m_Character;

        private void Awake()
        {
            m_Character = GetComponent<RedCharacter>();
            m_Game = FindFirstObjectByType<NetworkGame>();
        }

        protected override void OnRegisterInput(NetworkInput input)
        {
            input.Use(PlayerInputNetAdapter.Instance, Gather, Apply);
            input.Reconcile(PlayerMotionNetAdapter.Instance, Capture, Restore, Matches);
        }

        public override void OnStartClient()
        {
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
            m_Character.Simulate(input.Move, fresh && input.Jump, fresh && input.Roll, tickDelta, !context.Replaying);
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
