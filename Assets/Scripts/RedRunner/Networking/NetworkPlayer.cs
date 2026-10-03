using Fomoxa.Unity;
using UnityEngine;

namespace RedRunner.Networking
{
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        private NetworkGame m_Game;

        public override void OnStartClient()
        {
            if (NetworkObject.IsOwner)
            {
                m_Game = FindFirstObjectByType<NetworkGame>();
                m_Game?.SetLocalPlayer(this);
            }
        }

        public override void OnStopClient()
        {
            m_Game?.ClearLocalPlayer(this);
            m_Game = null;
        }
    }
}
