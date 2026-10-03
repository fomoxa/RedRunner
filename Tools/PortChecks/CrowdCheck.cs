#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RedRunner.Networking;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class CrowdCheck : PlayerCheck
    {
        private const string BotSeconds = "45";

        private Process m_Bot;

        protected override void Begin()
        {
            JoinFromStartScreen("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            NetworkPlayer local = Game.LocalPlayer;
            Vector3 localStart = local.transform.position;
            m_Bot = Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath("Builds/Windows/RedRunner.exe"),
                Arguments = $"-batchmode -nographics -bot 127.0.0.1 -bot-seconds {BotSeconds} -logFile \"{Path.GetFullPath("bot.log")}\"",
                UseShellExecute = false,
            });

            NetworkPlayer remote = null;
            float waited = 0f;
            while (remote == null)
            {
                remote = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(player => !player.NetworkObject.IsOwner);
                waited += Time.unscaledDeltaTime;
                if (waited > 40f)
                {
                    Finish(false, "the second client's character never appeared");
                    yield break;
                }

                yield return null;
            }

            float firstSeenX = remote.transform.position.x;
            bool kinematic = remote.Character.Rigidbody2D.bodyType == RigidbodyType2D.Kinematic;
            float farthest = firstSeenX;
            float fastest = 0f;
            float localDrift = 0f;
            bool facedRight = false;
            bool sawDeath = false;
            bool sawRespawn = false;
            float biggestJump = 0f;
            Vector3 previous = remote.transform.position;
            float watched = 0f;
            float lastMovedAt = 0f;
            while (remote != null)
            {
                Vector3 position = remote.transform.position;
                if (!remote.Character.IsDead.Value && !remote.IsDead)
                {
                    biggestJump = Mathf.Max(biggestJump, Vector3.Distance(position, previous) > 20f ? 0f : Vector3.Distance(position, previous));
                }

                if (Vector3.Distance(position, previous) > 0.01f || remote.Character.IsDead.Value)
                {
                    lastMovedAt = watched;
                }

                previous = position;
                farthest = Mathf.Max(farthest, position.x);
                fastest = Mathf.Max(fastest, remote.Character.Animator.GetFloat("Speed"));
                facedRight |= remote.transform.localScale.x > 0f && position.x > firstSeenX + 1f;
                localDrift = Mathf.Max(localDrift, Mathf.Abs(local.transform.position.x - localStart.x));
                if (remote.Character.IsDead.Value)
                {
                    sawDeath = true;
                }
                else if (sawDeath)
                {
                    sawRespawn = true;
                }

                watched += Time.unscaledDeltaTime;
                if (watched > 90f)
                {
                    Finish(false, "the second client's character was not despawned after its client left");
                    yield break;
                }

                yield return null;
            }

            float stillBeforeDespawn = watched - lastMovedAt;
            int serverDeaths = Count("server.log", "] player ", " died: ");
            int botDeaths = Count("bot.log", "PORT-BOT died", null);
            string detail = $"still for {stillBeforeDespawn:F1} s before despawn, {serverDeaths} deaths on the server, {botDeaths} seen by the bot, remote first seen at x {firstSeenX:F2}, kinematic {kinematic}, ran to x {farthest:F1}, animator speed up to {fastest:F2}, faced right {facedRight}, died {sawDeath}, respawned {sawRespawn}, despawned after {watched:F1} s, local character moved {localDrift:F3}, largest frame step {biggestJump:F2}";
            bool passed = kinematic
                && Mathf.Abs(firstSeenX - Game.SpawnPoint.x) < 2f
                && farthest - firstSeenX > 5f
                && fastest > 1f
                && facedRight
                && sawDeath
                && sawRespawn
                && localDrift < 0.05f
                && stillBeforeDespawn > 10f;
            Finish(passed, detail);
        }

        private static int Count(string file, string start, string end)
        {
            if (!File.Exists(file))
            {
                return -1;
            }

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd().Split('\n').Count(line => line.Contains(start) && (end == null || line.Contains(end)));
            }
        }

        private void OnDestroy()
        {
            if (m_Bot != null && !m_Bot.HasExited)
            {
                m_Bot.Kill();
            }
        }
    }
}
#endif
