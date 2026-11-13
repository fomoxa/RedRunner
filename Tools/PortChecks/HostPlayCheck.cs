#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using RedRunner.UI;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class HostPlayCheck : PlayerCheck
    {
        protected override void Begin()
        {
            Game.Host(0);
        }

        protected override IEnumerator Verify()
        {
            try
            {
                JoinFromStartScreen("localhost");
            }
            catch (Exception exception)
            {
                Finish(false, $"Play on the host threw {exception.GetType().Name}: {exception.Message}");
                yield break;
            }

            yield return null;
            UIScreen inGame = UIManager.Singleton.UISCREENS.First(screen => screen.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN);
            if (!inGame.IsOpen || !GameManager.Singleton.gameStarted || Game.Manager.ServerManager.PeerCount != 1)
            {
                Finish(false, $"after Play the in-game screen is open {inGame.IsOpen}, game started {GameManager.Singleton.gameStarted}, peers {Game.Manager.ServerManager.PeerCount}");
            }
        }
    }
}
#endif
