#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RedRunner.Networking;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class CrashCheck : PlayerCheck
    {
        private const float KillAfterSeconds = 8f;
        private const float StandStillAfterSeconds = 3.5f;

        private Process m_Bot;

        protected override void Begin()
        {
            JoinFromStartScreen("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            m_Bot = Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath("Builds/Windows/RedRunner.exe"),
                Arguments = $"-batchmode -nographics -bot 127.0.0.1 -bot-seconds 120 -logFile \"{Path.GetFullPath("bot.log")}\"",
                UseShellExecute = false,
            });

            NetworkPlayer remote = null;
            for (float waited = 0f; remote == null; waited += Time.unscaledDeltaTime)
            {
                remote = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(player => !player.NetworkObject.IsOwner);
                if (waited > 40f)
                {
                    Finish(false, "the bot's character never appeared");
                    yield break;
                }

                yield return null;
            }

            yield return new WaitForSecondsRealtime(KillAfterSeconds);
            m_Bot.Kill();
            float killedAt = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(StandStillAfterSeconds);
            Vector3 settled = remote.transform.position;
            float moved = 0f;
            int deaths = 0;
            bool dead = remote.IsDead;
            while (remote != null)
            {
                if (remote.IsDead && !dead)
                {
                    deaths++;
                }

                dead = remote.IsDead;
                moved = Mathf.Max(moved, Mathf.Abs(remote.transform.position.x - settled.x));
                if (Time.realtimeSinceStartup - killedAt > 60f)
                {
                    Finish(false, "the server never ended the killed client");
                    yield break;
                }

                yield return null;
            }

            float despawnedAfter = Time.realtimeSinceStartup - killedAt;
            bool passed = despawnedAfter > 10f && moved < 0.05f && deaths == 0;
            Finish(passed, $"despawned {despawnedAfter:F1} s after the kill, moved {moved:F3} units sideways and died {deaths} times after the first {StandStillAfterSeconds} s");
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
