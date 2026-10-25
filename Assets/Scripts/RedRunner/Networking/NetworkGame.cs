using System;
using Fomoxa.Networking;
using Fomoxa.Networking.Sessions;
using Fomoxa.Unity;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEngine;

namespace RedRunner.Networking
{
    public sealed class NetworkGame : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        [SerializeField]
        private NetworkManager m_Manager;

        [SerializeField]
        private NetworkObject m_PlayerPrefab;

        [SerializeField]
        private Vector2 m_SpawnPoint = new Vector2(7.24f, 12.59f);

        [SerializeField]
        private ushort m_Port = DefaultPort;

        [SerializeField]
        private int m_ServerFrameRate = 60;

        public event Action<NetworkPlayer> LocalPlayerChanged;

        public event Action<ConnectionStateArgs> ClientStateChanged;

        public NetworkManager Manager => m_Manager;

        public NetworkPlayer LocalPlayer { get; private set; }

        public Vector2 SpawnPoint => m_SpawnPoint;

        public bool IsServer => m_Manager.ServerManager.State == ServerState.Started;

        private void Start()
        {
            m_Manager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            m_Manager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            string[] arguments = Environment.GetCommandLineArgs();
            ushort port = ReadPort(arguments);
            if (Array.IndexOf(arguments, "-server") >= 0)
            {
                Application.targetFrameRate = m_ServerFrameRate;
                StartServer(port);
            }
            else if (Array.IndexOf(arguments, "-host") >= 0)
            {
                Host(port);
            }
        }

        private void OnDestroy()
        {
            if (m_Manager != null && m_Manager.ServerManager != null)
            {
                m_Manager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
                m_Manager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            }
        }

        public void StartServer(ushort port)
        {
            m_Manager.ServerManager.StartConnection(port);
            Debug.Log($"[RedRunner] server listening on UDP port {m_Manager.ServerManager.Port}");
        }

        public void Host(ushort port)
        {
            StartServer(port);
            m_Manager.ClientManager.StartConnection("localhost", m_Manager.ServerManager.Port);
        }

        public void Join(string address)
        {
            Join(address, m_Port);
        }

        public void Join(string address, ushort port)
        {
            m_Manager.ClientManager.StartConnection(string.IsNullOrWhiteSpace(address) ? "localhost" : address.Trim(), port);
        }

        public void Leave()
        {
            m_Manager.ClientManager.StopConnection();
            if (IsServer)
            {
                m_Manager.ServerManager.StopConnection();
            }
        }

        internal void SetLocalPlayer(NetworkPlayer player)
        {
            LocalPlayer = player;
            if (CameraController.Singleton != null)
            {
                CameraController.Singleton.Follow(player != null ? player.transform : null);
            }

            if (TerrainGenerator.Singleton != null)
            {
                TerrainGenerator.Singleton.Follow(player != null ? player.Character : null);
            }
            LocalPlayerChanged?.Invoke(player);
        }

        internal void ClearLocalPlayer(NetworkPlayer player)
        {
            if (LocalPlayer == player)
            {
                SetLocalPlayer(null);
            }
        }

        private void OnRemoteConnectionState(ConnectionStateArgs args)
        {
            if (args.State != ConnectionState.Started)
            {
                return;
            }

            NetworkObject player = Instantiate(m_PlayerPrefab, m_SpawnPoint, Quaternion.identity);
            m_Manager.ServerManager.Spawn(player, args.PeerId);
            Debug.Log($"[RedRunner] spawned a player for peer {args.PeerId} at {m_SpawnPoint}");
        }

        private void OnClientConnectionState(ConnectionStateArgs args)
        {
            ClientStateChanged?.Invoke(args);
        }

        private ushort ReadPort(string[] arguments)
        {
            int index = Array.IndexOf(arguments, "-port");
            if (index >= 0 && index + 1 < arguments.Length && ushort.TryParse(arguments[index + 1], out ushort port))
            {
                return port;
            }

            return m_Port;
        }
    }
}
