#if UNITY_EDITOR
using System.Collections;
using Fomoxa.Networking.Simulation;
using RedRunner.Characters;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class HostLifeCheck : PlayerCheck
    {
        protected override void Begin()
        {
            Game.Host(0);
        }

        protected override IEnumerator Verify()
        {
            NetworkPlayer player = Game.LocalPlayer;
            if (!player.Character.DecidesDeath || !player.Character.CollectsItems)
            {
                Finish(false, $"the host's character decides death {player.Character.DecidesDeath}, collects {player.Character.CollectsItems}");
                yield break;
            }

            Teleport(player, new Vector2(Game.SpawnPoint.x + 3f, -4f));
            yield return new WaitForSecondsRealtime(0.3f);
            if (!player.IsDead || !player.Character.IsDead.Value || player.Character.DeathCause != DeathCause.Fall || !player.Character.Skeleton.IsActive)
            {
                Finish(false, $"falling below 0: state dead {player.IsDead}, character dead {player.Character.IsDead.Value}, cause {player.Character.DeathCause}, skeleton {player.Character.Skeleton.IsActive}");
                yield break;
            }

            yield return new WaitForSecondsRealtime(1.2f);
            if (!player.IsDead)
            {
                Finish(false, "the character respawned before the delay");
                yield break;
            }

            yield return new WaitForSecondsRealtime(1.5f);
            if (player.IsDead || player.Character.IsDead.Value || Vector2.Distance(player.transform.position, Game.SpawnPoint) > 5f)
            {
                Finish(false, $"after the delay: dead {player.IsDead}, at {player.transform.position}");
                yield break;
            }

            float endX = TerrainGenerator.Singleton.EndX;
            Teleport(player, new Vector2(endX + 4f, 12f));
            yield return new WaitForSecondsRealtime(0.3f);
            if (player.IsDead || Vector2.Distance(player.transform.position, Game.SpawnPoint) > 5f)
            {
                Finish(false, $"reaching the end block at {endX}: dead {player.IsDead}, at {player.transform.position}");
                yield break;
            }

            Finish(true, $"fell and respawned after the delay, end block at x {endX} sent the player back to {player.transform.position}");
        }

        private static void Teleport(NetworkPlayer player, Vector2 position)
        {
            PhysicsBody2D body = player.NetworkObject.Body2D;
            BodyState2D state = body.State;
            state.Position = new System.Numerics.Vector2(position.x, position.y);
            state.Velocity = System.Numerics.Vector2.Zero;
            body.State = state;
        }
    }
}
#endif
