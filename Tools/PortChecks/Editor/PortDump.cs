using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortDump
    {
        public static void Project()
        {
            var text = new StringBuilder();
            var scene = EditorSceneManager.OpenScene(PortSetup.PlayScene, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Dump(root.transform, 0, text, 3);
            }

            foreach (string path in new[]
            {
                "Assets/Prefabs/Blocks", "Assets/Prefabs/Enemies", "Assets/Prefabs/Collectables", "Assets/Prefabs",
            })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    text.AppendLine("PREFAB " + assetPath);
                    Dump(prefab.transform, 1, text, 2);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:TerrainGenerationSettings"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                text.AppendLine("SETTINGS " + assetPath);
                DumpFields(AssetDatabase.LoadMainAssetAtPath(assetPath), 1, text);
            }

            text.AppendLine($"fixedDeltaTime {Time.fixedDeltaTime} gravity2D {Physics2D.gravity}");
            for (int a = 0; a < 32; a++)
            {
                string name = LayerMask.LayerToName(a);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var ignored = Enumerable.Range(0, 32).Where(b => !string.IsNullOrEmpty(LayerMask.LayerToName(b)) && Physics2D.GetIgnoreLayerCollision(a, b)).Select(LayerMask.LayerToName);
                text.AppendLine($"LAYER {a} {name} ignores2D [{string.Join(",", ignored)}]");
            }

            var inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axes = inputManager.FindProperty("m_Axes");
            for (int i = 0; i < axes.arraySize; i++)
            {
                SerializedProperty axis = axes.GetArrayElementAtIndex(i);
                text.AppendLine($"AXIS {axis.FindPropertyRelative("m_Name").stringValue} +{axis.FindPropertyRelative("positiveButton").stringValue} -{axis.FindPropertyRelative("negativeButton").stringValue} alt+{axis.FindPropertyRelative("altPositiveButton").stringValue}");
            }

            System.IO.File.WriteAllText("port-dump.txt", text.ToString());
            Debug.Log("PORT-CHECK dump: PASS");
        }

        private static void Dump(Transform transform, int depth, StringBuilder text, int fieldDepth)
        {
            string indent = new string(' ', depth * 2);
            GameObject gameObject = transform.gameObject;
            text.AppendLine($"{indent}- {gameObject.name} active={gameObject.activeSelf} layer={LayerMask.LayerToName(gameObject.layer)} tag={gameObject.tag} pos={transform.localPosition} scale={transform.localScale}");
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    text.AppendLine($"{indent}    [missing script]");
                    continue;
                }

                text.AppendLine($"{indent}    [{component.GetType().Name}]");
                if (depth < fieldDepth && !(component is Transform))
                {
                    DumpFields(component, depth + 3, text);
                }
            }

            foreach (Transform child in transform)
            {
                Dump(child, depth + 1, text, fieldDepth);
            }
        }

        private static void DumpFields(Object target, int depth, StringBuilder text)
        {
            string indent = new string(' ', depth * 2);
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.GetIterator();
            bool enter = true;
            int count = 0;
            while (property.NextVisible(enter) && count < 60)
            {
                enter = false;
                if (property.name == "m_Script")
                {
                    continue;
                }

                count++;
                text.AppendLine($"{indent}{property.name} = {Value(property)}");
            }
        }

        private static string Value(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null ? "null" : property.objectReferenceValue.name + " (" + property.objectReferenceValue.GetType().Name + ")";
                case SerializedPropertyType.Float: return property.floatValue.ToString();
                case SerializedPropertyType.Integer: return property.intValue.ToString();
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Enum: return property.enumDisplayNames.Length > property.enumValueIndex && property.enumValueIndex >= 0 ? property.enumDisplayNames[property.enumValueIndex] : property.intValue.ToString();
                case SerializedPropertyType.Vector2: return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return property.vector3Value.ToString();
                case SerializedPropertyType.LayerMask: return property.intValue.ToString();
                default:
                    if (property.isArray)
                    {
                        var items = new StringBuilder("[");
                        for (int i = 0; i < property.arraySize && i < 40; i++)
                        {
                            items.Append(Value(property.GetArrayElementAtIndex(i))).Append(", ");
                        }

                        return items.Append("] n=").Append(property.arraySize).ToString();
                    }

                    return property.propertyType.ToString();
            }
        }
    }
}
