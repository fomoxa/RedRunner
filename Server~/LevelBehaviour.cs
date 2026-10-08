using System;
using Fomoxa.Networking;
using Generated;
using RedRunner.Networking.Models;

namespace RedRunner.Server
{
    public sealed class LevelBehaviour : EntityBehaviour
    {
        private readonly ServerGame game;
        private readonly LevelState state = new LevelState();

        public LevelBehaviour(ServerGame game)
        {
            this.game = game;
        }

        public override void OnStartServer()
        {
            state.Seed = (uint)new Random().Next(1, int.MaxValue);
            game.Log($"[RedRunner] level seed {state.Seed}");
            game.BuildLevel(state.Seed);
        }

        protected override void OnRegisterState(NetworkState networkState)
        {
            networkState.Use(LevelStateNetAdapter.Instance, state, previous => { });
        }
    }
}
