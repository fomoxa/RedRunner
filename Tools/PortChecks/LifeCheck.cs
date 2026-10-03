#if UNITY_EDITOR
using System.Collections;
using RedRunner.Networking;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.PortChecks
{
    public sealed class LifeCheck : PlayerCheck
    {
        private const float RunSeconds = 90f;

        protected override void Begin()
        {
            JoinFromStartScreen("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            NetworkPlayer player = Game.LocalPlayer;
            if (player.Character.DecidesDeath || !player.Character.CollectsItems)
            {
                Finish(false, $"the client's character decides death {player.Character.DecidesDeath}, collects {player.Character.CollectsItems}");
                yield break;
            }

            if (GameObject.Find("Score Text") != null || GameObject.Find("Coin Panel") != null)
            {
                Finish(false, "the score is still on screen");
                yield break;
            }

            int coins = GameManager.Singleton.m_Coin.Value;
            CrossPlatformInputManager.SwitchActiveInputMethod(CrossPlatformInputManager.ActiveInputMethod.Touch);
            CrossPlatformInputManager.SetAxis("Horizontal", 1f);
            float startedAt = Time.realtimeSinceStartup;
            float nextJump = startedAt + 1f;
            float farthest = player.transform.position.x;
            while (!player.Character.IsDead.Value)
            {
                farthest = Mathf.Max(farthest, player.transform.position.x);
                if (Time.realtimeSinceStartup > nextJump)
                {
                    CrossPlatformInputManager.SetButtonDown("Jump");
                    yield return null;
                    yield return null;
                    CrossPlatformInputManager.SetButtonUp("Jump");
                    nextJump = Time.realtimeSinceStartup + 1.3f;
                }

                if (Time.realtimeSinceStartup - startedAt > RunSeconds)
                {
                    Finish(false, $"no death in {RunSeconds} s, farthest x {farthest}");
                    yield break;
                }

                yield return null;
            }

            CrossPlatformInputManager.SetAxis("Horizontal", 0f);
            string cause = player.Character.DeathCause.ToString();
            Vector3 diedAt = player.transform.position;
            if (!player.IsDead || !player.Character.Skeleton.IsActive)
            {
                Finish(false, $"the character died on the client without the server's state: state dead {player.IsDead}");
                yield break;
            }

            float deadAt = Time.realtimeSinceStartup;
            while (player.Character.IsDead.Value && Time.realtimeSinceStartup - deadAt < 6f)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1f);
            if (player.Character.IsDead.Value || Vector2.Distance(player.transform.position, Game.SpawnPoint) > 5f)
            {
                Finish(false, $"after dying of {cause} at {diedAt}: dead {player.Character.IsDead.Value}, at {player.transform.position}");
                yield break;
            }

            Finish(true, $"died of {cause} at x {diedAt.x:F1} after {deadAt - startedAt:F1} s, respawned at {player.transform.position}, collected {GameManager.Singleton.m_Coin.Value - coins} coins");
        }
    }
}
#endif
