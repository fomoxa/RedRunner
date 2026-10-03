#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fomoxa.Unity;
using Fomoxa.Unity.Sessions;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
using UnityEditor;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class JoinCheck : PlayerCheck
    {
        protected override void Begin()
        {
            Game.Join("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            string log = "";
            if (File.Exists("server.log"))
            {
                using (var stream = new FileStream("server.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    log = reader.ReadToEnd();
                }
            }
            Match seed = Regex.Match(log, @"\[RedRunner\] level seed (\d+)");
            if (!seed.Success || seed.Groups[1].Value != Level.Seed.ToString())
            {
                Finish(false, $"the server log has seed {(seed.Success ? seed.Groups[1].Value : "none")}, the client has {Level.Seed}");
            }

            if (Game.IsServer)
            {
                Finish(false, "the client process also runs a server");
            }

            yield break;
        }
    }
}
#endif
