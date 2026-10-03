using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RedRunner.PortChecks
{
    public static class PortInspect
    {
        public static void AddressField()
        {
            EditorSceneManager.OpenScene(PortSetup.PlayScene, OpenSceneMode.Single);
            var text = new StringBuilder();
            InputField field = Object.FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            text.AppendLine($"field text '{field.text}' textComponent {field.textComponent?.name} interactable {field.interactable}");
            foreach (Text label in field.GetComponentsInChildren<Text>(true))
            {
                var rect = (RectTransform)label.transform;
                text.AppendLine($"{label.name} active {label.gameObject.activeInHierarchy} enabled {label.enabled} text '{label.text}' font {label.font?.name} size {label.fontSize} color {label.color} rect {rect.rect} overflow {label.verticalOverflow} material {label.material?.name}");
            }

            Text sample = GameObject.Find("Help Window").GetComponentInChildren<Text>(true);
            text.AppendLine($"sample {sample.name} font {sample.font?.name} size {sample.fontSize} color {sample.color} material {sample.material?.name}");
            Debug.Log("PORT-CHECK inspect:\n" + text);
        }
    }
}
