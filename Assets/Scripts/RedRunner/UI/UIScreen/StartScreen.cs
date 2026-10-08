using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fomoxa.Networking.Sessions;
using RedRunner.Networking;

namespace RedRunner.UI
{
    public class StartScreen : UIScreen
    {
        [SerializeField]
        protected Button PlayButton = null;
        [SerializeField]
        protected Button HelpButton = null;
        [SerializeField]
        protected Button InfoButton = null;
        [SerializeField]
        protected Button ExitButton = null;
        [SerializeField]
        protected InputField AddressField = null;
        [SerializeField]
        protected NetworkGame Network = null;

        private void Start()
        {
            PlayButton.SetButtonAction(() =>
            {
                var uiManager = UIManager.Singleton;
                var InGameScreen = uiManager.UISCREENS.Find(el => el.ScreenInfo == UIScreenInfo.IN_GAME_SCREEN);
                if (InGameScreen != null)
                {
                    Network.Join(AddressField.text);
                    uiManager.OpenScreen(InGameScreen);
                    GameManager.Singleton.StartGame();
                }
            });
            Network.ClientStateChanged += Network_ClientStateChanged;

            ExitButton.SetButtonAction(() =>
            {
                GameManager.Singleton.ExitGame();
            });
        }
        private void OnDestroy()
        {
            Network.ClientStateChanged -= Network_ClientStateChanged;
        }

        private void Network_ClientStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Stopped && GameManager.Singleton.gameStarted)
            {
                GameManager.Singleton.EndGame();
                UIManager.Singleton.OpenScreen(this);
            }
        }

        public override void UpdateScreenStatus(bool open)
        {
            base.UpdateScreenStatus(open);
        }
    }
}