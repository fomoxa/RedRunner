using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class ScreenshotTaker : MonoBehaviour
    {
        private const int Width = 1280;
        private const int Height = 720;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-screenshot") >= 0)
            {
                new GameObject(nameof(ScreenshotTaker)).AddComponent<ScreenshotTaker>();
            }
        }

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string path = Argument(arguments, "-screenshot");
            float after = float.Parse(Argument(arguments, "-screenshot-after") ?? "5", CultureInfo.InvariantCulture);
            yield return new WaitForSecondsRealtime(after);
            yield return new WaitForEndOfFrame();
            var target = new RenderTexture(Width, Height, 24);
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(camera => camera.enabled).OrderBy(camera => camera.depth))
            {
                RenderTexture previous = camera.targetTexture;
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = previous;
            }

            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"PORT-SHOT wrote {path}");
            if (Array.IndexOf(arguments, "-screenshot-quit") >= 0)
            {
                Application.Quit();
            }
        }

        private static string Argument(string[] arguments, string name)
        {
            int index = Array.IndexOf(arguments, name);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
        }
    }
}
