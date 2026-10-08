#if UNITY_EDITOR
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Networking.Sessions;
using Fomoxa.Unity;
using UnityEditor;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class ConnectionCheck : MonoBehaviour
    {
        private const float TimeoutSeconds = 15f;

        private NetworkManager server;
        private NetworkManager client;
        private float startedAt;
        private bool finished;
        private bool started;

        private void Start()
        {
            server = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).FirstOrDefault();
            if (server == null)
            {
                Finish(false, "the Play scene has no NetworkManager");
                return;
            }

            if (!(server.TransportManager.Transport is UdpNetworkTransport))
            {
                Finish(false, $"the scene NetworkManager uses {server.TransportManager.Transport.GetType().Name}, not UDP");
                return;
            }

            server.ServerManager.StartConnection(0);
            var clientObject = new GameObject("Check Client");
            clientObject.AddComponent<UdpNetworkTransport>();
            client = clientObject.AddComponent<NetworkManager>();
            client.ClientManager.OnClientConnectionState += args => started |= args.State == ConnectionState.Started;
            client.ClientManager.StartConnection("127.0.0.1", server.ServerManager.Port);
            startedAt = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (finished || client == null)
            {
                return;
            }

            if (started)
            {
                Finish(true, $"client connected over UDP to port {server.ServerManager.Port}");
            }
            else if (Time.realtimeSinceStartup - startedAt > TimeoutSeconds)
            {
                Finish(false, $"client state {client.ClientManager.State}, server peers {server.ServerManager.PeerCount}");
            }
        }

        private void Finish(bool passed, string detail)
        {
            finished = true;
            Debug.Log($"PORT-CHECK {nameof(ConnectionCheck)}: {(passed ? "PASS" : "FAIL")} {detail}");
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
#endif
