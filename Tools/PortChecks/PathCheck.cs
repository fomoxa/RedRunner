#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using RedRunner.Utilities;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class PathCheck : PlayerCheck
    {
        private const float SampleSeconds = 10f;
        private const float Tolerance = 0.01f;

        private readonly Dictionary<uint, Vector2> m_Client = new Dictionary<uint, Vector2>();
        private PathFollower m_Follower;

        protected override void Begin()
        {
            JoinFromStartScreen("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            m_Follower = PathLog.FirstOf(Level);
            if (m_Follower == null)
            {
                Finish(false, $"seed {Level.Seed} has no moving path follower");
                yield break;
            }

            Game.Manager.TimeManager.OnTick += Record;
            yield return new WaitForSecondsRealtime(SampleSeconds);
            Game.Manager.TimeManager.OnTick -= Record;
            yield return new WaitForSecondsRealtime(1f);

            var server = new Dictionary<uint, Vector2>();
            using (var stream = new FileStream("server.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                foreach (Match line in Regex.Matches(reader.ReadToEnd(), @"PORT-PATH (\d+) (-?[\d.]+) (-?[\d.]+)"))
                {
                    server[uint.Parse(line.Groups[1].Value)] = new Vector2(float.Parse(line.Groups[2].Value, CultureInfo.InvariantCulture), float.Parse(line.Groups[3].Value, CultureInfo.InvariantCulture));
                }
            }

            int matched = 0;
            float worst = 0f;
            var distinct = new HashSet<Vector2>();
            foreach (KeyValuePair<uint, Vector2> sample in m_Client)
            {
                if (server.TryGetValue(sample.Key, out Vector2 serverPosition))
                {
                    matched++;
                    worst = Mathf.Max(worst, Vector2.Distance(serverPosition, sample.Value));
                    distinct.Add(sample.Value);
                }
            }

            if (matched < 10 || worst > Tolerance || distinct.Count < 3)
            {
                Finish(false, $"{matched} ticks in both logs, worst distance {worst}, {distinct.Count} distinct positions, cycle {m_Follower.CycleLength} ticks");
                yield break;
            }

            Finish(true, $"{m_Follower.name} at the same position on server and client at {matched} ticks (worst {worst:F4}), {distinct.Count} distinct positions, cycle {m_Follower.CycleLength} ticks");
        }

        private void Record()
        {
            uint tick = Game.Manager.TimeManager.PredictionTick;
            if (tick % PathLog.Every == 0)
            {
                Vector3 position = m_Follower.transform.position;
                m_Client[tick] = new Vector2(Mathf.Round(position.x * 1000f) / 1000f, Mathf.Round(position.y * 1000f) / 1000f);
            }
        }
    }
}
#endif
