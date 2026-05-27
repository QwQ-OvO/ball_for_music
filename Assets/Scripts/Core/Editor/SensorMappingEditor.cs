#if UNITY_EDITOR
using InformationString.Core.Config;
using UnityEditor;
using UnityEngine;

namespace InformationString.Core.Editor
{
    [CustomEditor(typeof(SensorMapping))]
    public sealed class SensorMappingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Mock 默认区间（100–199 等）仅适用于 MockSensorInput。\n" +
                "实机 1k/2.2k/4.7k/10k 电阻请点下方按钮一键填入实测区间。",
                MessageType.Info);

            if (GUILayout.Button("Apply Hardware Resistor Calibration (实机 4 档阻值)"))
            {
                ApplyTo((SensorMapping)target);
            }
        }

        [MenuItem("InformationString/Apply Hardware SensorMapping Calibration")]
        private static void ApplyFromMenu()
        {
            var mapping = Selection.activeObject as SensorMapping;
            if (mapping == null)
            {
                EditorUtility.DisplayDialog(
                    "SensorMapping",
                    "请先在 Project 窗口选中 Sensor Mapping_Default（或任意 SensorMapping 资源）。",
                    "OK");
                return;
            }

            ApplyTo(mapping);
        }

        private static void ApplyTo(SensorMapping mapping)
        {
            Undo.RecordObject(mapping, "Apply Hardware Resistor Calibration");
            mapping.ApplyHardwareResistorCalibration();
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();
            Debug.Log("[SensorMapping] Applied hardware 1k/2.2k/4.7k/10k calibration.", mapping);
        }
    }
}
#endif
