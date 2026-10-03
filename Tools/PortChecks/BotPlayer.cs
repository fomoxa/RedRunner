using System;
using System.Collections;
using System.Globalization;
using RedRunner.Networking;
using RedRunner.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.PortChecks
{
    public sealed class BotPlayer : MonoBehaviour
    {
        private const float JumpEvery = 1.3f;
        private const float StartScreenDelay = 3.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-bot") >= 0)
            {
                new GameObject(nameof(BotPlayer)).AddComponent<BotPlayer>();
            }
        }

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string address = Argument(arguments, "-bot") ?? "127.0.0.1";
            float seconds = float.Parse(Argument(arguments, "-bot-seconds") ?? "45", CultureInfo.InvariantCulture);
            yield return new WaitForSecondsRealtime(StartScreenDelay);
            NetworkGame game = FindFirstObjectByType<NetworkGame>();
            StartScreen startScreen = FindObjectsByType<StartScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            startScreen.GetComponentInChildren<InputField>(true).text = address;
            startScreen.transform.Find("Play Button").GetComponent<Button>().onClick.Invoke();
            while (game.LocalPlayer == null)
            {
                yield return null;
            }

            Debug.Log($"PORT-BOT joined as object {game.LocalPlayer.NetworkObject.ObjectId}");
            CrossPlatformInputManager.SwitchActiveInputMethod(CrossPlatformInputManager.ActiveInputMethod.Touch);
            CrossPlatformInputManager.SetAxis("Horizontal", 1f);
            float startedAt = Time.realtimeSinceStartup;
            float nextJump = startedAt + JumpEvery;
            bool dead = false;
            while (Time.realtimeSinceStartup - startedAt < seconds)
            {
                if (game.LocalPlayer != null && game.LocalPlayer.IsDead != dead)
                {
                    dead = game.LocalPlayer.IsDead;
                    Debug.Log(dead ? $"PORT-BOT died of {game.LocalPlayer.Character.DeathCause}" : "PORT-BOT respawned");
                }

                if (Time.realtimeSinceStartup > nextJump)
                {
                    CrossPlatformInputManager.SetButtonDown("Jump");
                    yield return null;
                    yield return null;
                    CrossPlatformInputManager.SetButtonUp("Jump");
                    nextJump = Time.realtimeSinceStartup + JumpEvery;
                }

                yield return null;
            }

            Debug.Log("PORT-BOT leaving");
            game.Leave();
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private static string Argument(string[] arguments, string name)
        {
            int index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length && !arguments[index + 1].StartsWith("-") ? arguments[index + 1] : null;
        }
    }
}
