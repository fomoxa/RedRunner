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
    public sealed class HostCheck : PlayerCheck
    {
        protected override void Begin()
        {
            Game.Host(0);
        }

        protected override IEnumerator Verify()
        {
            if (!Game.IsServer || Game.Manager.ServerManager.PeerCount != 1)
            {
                Finish(false, $"host server {Game.IsServer}, peers {Game.Manager.ServerManager.PeerCount}");
            }

            yield break;
        }
    }
}
#endif
