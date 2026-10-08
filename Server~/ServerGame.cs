using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Fomoxa.Networking;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Sessions;
using Fomoxa.Networking.Simulation;
using Fomoxa.Networking.Standalone;
using Fomoxa.Networking.Transports;
using Generated;
using RedRunner.Networking.Models;
using RedRunner.Simulation;

namespace RedRunner.Server
{
    public sealed class ServerGame : IDisposable
    {
        public const int TickRate = 50;
        private const string LevelBehaviour = "RedRunner.Networking.NetworkLevel";
        private const int PathEvery = 10;

        private readonly RapierScenes physics = new RapierScenes(new Vector3(0f, -9.81f, 0f));
        private readonly Action<string> log;
        private readonly bool logPaths;
        private LevelMover? pathMover;

        public ServerGame(byte[] catalogue, string scenes, Func<TimeSpan> clock, Action<string> log, bool logPaths)
        {
            this.log = log;
            this.logPaths = logPaths;
            Catalogue = new LevelCatalogue();
            LevelCatalogueNetAdapter.Instance.Decode(catalogue, ref Catalogue);
            var registry = new FomoxaRegistry();
            FomoxaAdapters.RegisterAll(registry);
            var prefabs = new StandalonePrefabs();
            prefabs.Register(FomoxaPrefabs.RedRunnerId, FomoxaPrefabs.RedRunnerFingerprint, () => new PlayerEntity(this));
            var behaviours = new StandaloneBehaviours();
            behaviours.Register(LevelBehaviour, () => new LevelBehaviour(this));
            var settings = new NetworkSettings { TickRate = TickRate, PhysicsBackend = PhysicsBackend.Rapier };
            var networkLog = new NetworkLog(exception => log(exception.ToString()), warning => log(warning), error => log(error));
            Runtime = StandaloneRuntime.Create(registry, settings, new UdpTransportFactory(), prefabs, behaviours, new SceneFileDirectory(scenes), clock, networkLog, physics, new[] { FomoxaScenes.PlayId });
            Runtime.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            Runtime.TimeManager.OnPreTick += MoveLevel;
            Runtime.TimeManager.OnTick += LogPath;
        }

        public LevelCatalogue Catalogue;

        public NetworkRuntime Runtime { get; }

        public BuiltLevel Level { get; private set; }

        public void Start(ushort port)
        {
            Runtime.ServerManager.StartConnection(port);
            Log($"[RedRunner] server listening on UDP port {Runtime.ServerManager.Port}");
        }

        public void Frame(TimeSpan now, double elapsedSeconds)
        {
            Runtime.BeginFrame(now, elapsedSeconds);
            Runtime.EndFrame();
        }

        public void BuildLevel(uint seed)
        {
            Level = BuiltLevel.Build(Catalogue, (int)seed, (float)Runtime.TimeManager.TickDelta, new ConsoleLevelPhysics(physics, FomoxaScenes.PlayId));
            pathMover = Level.Movers
                .Where(mover => mover.Cycle.Count > 1)
                .OrderBy(mover => mover.PositionAt(0).X)
                .ThenBy(mover => mover.PositionAt(0).Y)
                .Select(mover => (LevelMover?)mover)
                .FirstOrDefault();
        }

        public void Log(string line) => log(line);

        public void Dispose()
        {
            Runtime.ServerManager.StopConnection();
            physics.Dispose();
        }

        private void OnRemoteConnectionState(ConnectionStateArgs args)
        {
            if (args.State != ConnectionState.Started)
            {
                return;
            }

            CataloguePlayer player = Catalogue.Player;
            var entity = new PlayerEntity(this)
            {
                SceneId = FomoxaScenes.PlayId,
                Position = new Vector3(player.SpawnX, player.SpawnY, 0f),
            };
            Runtime.ServerManager.Spawn(entity, args.PeerId);
            Log(FormattableString.Invariant($"[RedRunner] spawned a player for peer {args.PeerId} at ({player.SpawnX:F2}, {player.SpawnY:F2})"));
        }

        private void MoveLevel() => Level?.Move(Runtime.TimeManager.PredictionTick);

        private void LogPath()
        {
            uint tick = Runtime.TimeManager.PredictionTick;
            if (logPaths && pathMover.HasValue && tick % PathEvery == 0)
            {
                Vector3 position = pathMover.Value.PositionAt(tick);
                Log(FormattableString.Invariant($"PORT-PATH {tick} {position.X:F3} {position.Y:F3}"));
            }
        }

        private sealed class ConsoleLevelPhysics : ILevelPhysics
        {
            private readonly RapierScenes physics;
            private readonly uint sceneId;

            public ConsoleLevelPhysics(RapierScenes physics, uint sceneId)
            {
                this.physics = physics;
                this.sceneId = sceneId;
            }

            public RapierWorld2D World => physics.WorldOf2D(sceneId);

            public StaticGroup AddStatics(IReadOnlyList<ColliderDesc2D> colliders) => physics.AddStatic2D(sceneId, colliders);

            public PhysicsBody2D AddBody(in BodyDesc2D body) => physics.AddBody2D(sceneId, body);
        }
    }
}
