using System;
using System.Collections.Generic;
using System.Numerics;
using Fomoxa.Networking;
using Fomoxa.Networking.Messaging;
using Fomoxa.Networking.Objects;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;
using Fomoxa.Networking.Standalone;
using Generated;
using RedRunner.Networking.Models;
using RedRunner.Simulation;

namespace RedRunner.Server
{
    public sealed class PlayerEntity : StandaloneEntity
    {
        private readonly PlayerTransform transform;
        private readonly PlayerBehaviour player;

        public PlayerEntity(ServerGame game)
            : this(game, new PlayerTransform(), new PlayerBehaviour(game))
        {
        }

        private PlayerEntity(ServerGame game, PlayerTransform transform, PlayerBehaviour player)
            : base(FomoxaPrefabs.RedRunnerId, new EntityBehaviour[] { transform, player })
        {
            this.transform = transform;
            this.player = player;
            transform.Entity = this;
            player.Entity = this;
            Game = game;
        }

        public ServerGame Game { get; }

        public override Vector3 Scale
        {
            get => new Vector3(player.Motion.Facing, 1f, 1f);
            set
            {
            }
        }

        public void Teleport() => transform.Sync.Teleport();

        public override bool TryGetBody2D(out BodyDesc2D body)
        {
            CataloguePlayer catalogued = Game.Catalogue.Player;
            Vector3 at = Position;
            var placed = new SceneFileObject
            {
                Pose = new SceneFilePose { PositionX = at.X, PositionY = at.Y, PositionZ = at.Z, RotationW = 1f, ScaleX = 1f, ScaleY = 1f, ScaleZ = 1f },
                Body2D = catalogued.Body,
            };
            return SceneFileGeometry.TryGetBody2D(placed, out body);
        }
    }

    public sealed class PlayerTransform : EntityBehaviour, ITransformSource, ITransformReceiver
    {
        public PlayerTransform()
        {
            Sync = new TransformSync(new TransformSyncSettings(true, false, true, 0.01f, 0.1f, 0.01f));
            SyncTransform(Sync, this, this);
        }

        public TransformSync Sync { get; }

        public StandaloneEntity Entity { get; set; }

        public void ReadLocal(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
        {
            localPosition = Entity.Position;
            localRotation = Entity.Rotation;
            localScale = Entity.Scale;
        }

        public void ResetReceive()
        {
        }

        public void Receive(in TransformSample sample)
        {
        }
    }

    public sealed class PlayerBehaviour : EntityBehaviour
    {
        private const float PositionTolerance = 0.01f;
        private const float VelocityTolerance = 0.05f;

        private readonly ServerGame game;
        private readonly PlayerState state = new PlayerState();
        private readonly List<RapierCollider> touching = new List<RapierCollider>();
        private bool dead;
        private byte cause;
        private uint respawnTick;
        private bool held;
        private Vector2 heldAt;

        public PlayerBehaviour(ServerGame game)
        {
            this.game = game;
        }

        public RunnerMotion Motion { get; } = new RunnerMotion();

        public PlayerEntity Entity { get; set; }

        public override void OnStartServer()
        {
            game.Runtime.TimeManager.OnTick += ServerTick;
        }

        public override void OnStopServer()
        {
            game.Runtime.TimeManager.OnTick -= ServerTick;
        }

        protected override void OnRegisterInput(NetworkInput input)
        {
            input.Use(PlayerInputNetAdapter.Instance, gathered => { }, Apply);
            input.Reconcile(PlayerMotionNetAdapter.Instance, Capture, Restore, Matches);
        }

        protected override void OnRegisterState(NetworkState networkState)
        {
            networkState.Use(PlayerStateNetAdapter.Instance, state, previous => { });
        }

        private RunnerSettings Settings
        {
            get
            {
                CataloguePlayer player = game.Catalogue.Player;
                return new RunnerSettings(player.RunSpeed, player.MaxRunSpeed, player.RunSmoothTime, player.JumpStrength, player.RollForce);
            }
        }

        private void Apply(PlayerInput input, InputContext context)
        {
            PhysicsBody2D body = Entity.Body2D;
            if (!body.IsValid)
            {
                return;
            }

            float tickDelta = (float)game.Runtime.TimeManager.TickDelta;
            bool fresh = !context.Repeated;
            float move = context.RepeatedTicks > game.Catalogue.Player.MaxRepeatedTicks ? 0f : input.Move;
            bool jump = fresh && input.Jump;
            Motion.Simulate(body, Settings, move, jump, fresh && input.Roll, jump && Grounded(body), dead, tickDelta);
            HoldWhileDead();
        }

        private bool Grounded(PhysicsBody2D body)
        {
            BuiltLevel level = game.Level;
            return level != null && RunnerContacts.Grounded(level.World, body.Handle, (int)game.Catalogue.Player.SensorIndex, level, touching);
        }

        private void ServerTick()
        {
            TimeManager time = game.Runtime.TimeManager;
            PhysicsBody2D body = Entity.Body2D;
            if (!body.IsValid)
            {
                return;
            }

            if (state.Dead)
            {
                if (time.Tick >= respawnTick)
                {
                    Respawn();
                }

                return;
            }

            BuiltLevel level = game.Level;
            if (!dead && level != null)
            {
                CataloguePlayer player = game.Catalogue.Player;
                Surface hazard = RunnerContacts.Hazard(level.World, body.Handle, player.Body.Colliders.Count, body.Position.Y + player.BottomOffset, level, touching);
                Die(CauseOf(hazard));
            }

            HoldWhileDead();
            if (!dead && body.Position.Y < 0f)
            {
                Die(DeathCause.Fall);
            }

            if (dead)
            {
                state.Dead = true;
                state.Cause = cause;
                respawnTick = time.Tick + (uint)Math.Ceiling(game.Catalogue.Player.RespawnDelay * time.TickRate);
                game.Log($"[RedRunner] player {Entity.Record.OwnerId} died: {(DeathCause)cause}");
            }
            else if (level != null && body.Position.X >= level.EndX)
            {
                game.Log($"[RedRunner] player {Entity.Record.OwnerId} reached the end block");
                Respawn();
            }
        }

        private void Die(DeathCause? deathCause)
        {
            if (!dead && deathCause.HasValue)
            {
                dead = true;
                cause = (byte)deathCause.Value;
            }
        }

        private void HoldWhileDead()
        {
            PhysicsBody2D body = Entity.Body2D;
            if (!body.IsValid)
            {
                return;
            }

            if (!dead)
            {
                held = false;
                return;
            }

            if (!held)
            {
                held = true;
                heldAt = body.Position;
            }

            body.State = new BodyState2D { Position = heldAt, Rotation = body.Rotation };
        }

        private void Respawn()
        {
            CataloguePlayer player = game.Catalogue.Player;
            dead = false;
            held = false;
            Motion.Facing = 1;
            Motion.RunSmoothVelocity = 0f;
            PhysicsBody2D body = Entity.Body2D;
            BodyState2D respawned = body.State;
            respawned.Position = new Vector2(player.SpawnX, player.SpawnY);
            respawned.Velocity = Vector2.Zero;
            body.State = respawned;
            Entity.Teleport();
            state.Dead = false;
            state.Cause = 0;
        }

        private void Capture(PlayerMotion motion)
        {
            BodyState2D body = Entity.Body2D.State;
            motion.X = body.Position.X;
            motion.Y = body.Position.Y;
            motion.VelocityX = body.Velocity.X;
            motion.VelocityY = body.Velocity.Y;
            motion.RunSmoothVelocity = Motion.RunSmoothVelocity;
        }

        private void Restore(PlayerMotion motion)
        {
            PhysicsBody2D body = Entity.Body2D;
            BodyState2D restored = body.State;
            restored.Position = new Vector2(motion.X, motion.Y);
            restored.Velocity = new Vector2(motion.VelocityX, motion.VelocityY);
            body.State = restored;
            Motion.RunSmoothVelocity = motion.RunSmoothVelocity;
        }

        private static bool Matches(PlayerMotion server, PlayerMotion predicted) =>
            Math.Abs(server.X - predicted.X) <= PositionTolerance
            && Math.Abs(server.Y - predicted.Y) <= PositionTolerance
            && Math.Abs(server.VelocityX - predicted.VelocityX) <= VelocityTolerance
            && Math.Abs(server.VelocityY - predicted.VelocityY) <= VelocityTolerance
            && Math.Abs(server.RunSmoothVelocity - predicted.RunSmoothVelocity) <= VelocityTolerance;

        private static DeathCause? CauseOf(Surface hazard)
        {
            switch (hazard)
            {
                case Surface.Water:
                    return DeathCause.Water;
                case Surface.Saw:
                    return DeathCause.Saw;
                case Surface.Spike:
                    return DeathCause.Spike;
                case Surface.Mace:
                    return DeathCause.Mace;
                default:
                    return null;
            }
        }
    }

    public enum DeathCause : byte
    {
        Fall,
        Saw,
        Spike,
        Water,
        Mace,
    }
}
