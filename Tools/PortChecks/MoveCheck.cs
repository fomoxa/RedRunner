#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner.PortChecks
{
    public sealed class MoveCheck : PlayerCheck
    {
        protected override void Begin()
        {
            JoinFromStartScreen("127.0.0.1");
        }

        protected override IEnumerator Verify()
        {
            var player = Game.LocalPlayer;
            if (!player.IsPredicting)
            {
                Finish(false, "the client does not predict its own character");
                yield break;
            }

            CrossPlatformInputManager.SwitchActiveInputMethod(CrossPlatformInputManager.ActiveInputMethod.Touch);
            Vector3 start = player.transform.position;
            CrossPlatformInputManager.SetAxis("Horizontal", 1f);
            yield return new WaitForSecondsRealtime(2f);
            Vector3 ran = player.transform.position;
            if (ran.x - start.x < 5f || player.transform.localScale.x <= 0f)
            {
                Finish(false, $"running right for 2 s moved from {start} to {ran}, scale {player.transform.localScale}");
                yield break;
            }

            CrossPlatformInputManager.SetAxis("Horizontal", 0f);
            yield return new WaitForSecondsRealtime(1f);
            float groundY = player.transform.position.y;
            CrossPlatformInputManager.SetButtonDown("Jump");
            yield return null;
            yield return null;
            CrossPlatformInputManager.SetButtonUp("Jump");
            float highest = groundY;
            for (float waited = 0f; waited < 0.8f; waited += Time.unscaledDeltaTime)
            {
                highest = Mathf.Max(highest, player.transform.position.y);
                yield return null;
            }

            if (highest - groundY < 1.5f)
            {
                Finish(false, $"jumping rose from {groundY} to only {highest}");
                yield break;
            }

            CrossPlatformInputManager.SetAxis("Horizontal", -1f);
            yield return new WaitForSecondsRealtime(1f);
            CrossPlatformInputManager.SetAxis("Horizontal", 0f);
            if (player.transform.localScale.x >= 0f)
            {
                Finish(false, "running left did not turn the character");
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);
            float cameraGap = Mathf.Abs(CameraController.Singleton.transform.position.x - player.transform.position.x);
            int background = FindObjectsByType<BackgroundBlock>(FindObjectsSortMode.None).Length;
            if (cameraGap > 1f || background == 0)
            {
                Finish(false, $"camera {cameraGap} away from the player, {background} background blocks");
                yield break;
            }

            Finish(true, $"ran from {start.x:F2} to {ran.x:F2}, jumped {highest - groundY:F2}, {player.ReconcileCount} reconciles, camera {cameraGap:F2} away, {background} background blocks");
        }
    }
}
#endif
