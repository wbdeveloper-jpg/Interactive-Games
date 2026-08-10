#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeasurementMix.Editor
{
    [CustomEditor(typeof(MeasurementGameSettings))]
    public class MeasurementGameSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "Each profile controls practical measurement and conversion " +
                "questions. Select the mass/liquid units appropriate for the " +
                "class, enable decimal values only when required, and keep " +
                "Questions Per Run at five for the intended flow.",
                MessageType.Info);

            if (!GUILayout.Button("Restore Recommended Difficulty Profiles"))
                return;

            MeasurementGameSettings settings =
                (MeasurementGameSettings)target;
            Undo.RecordObject(settings, "Restore Measurement Difficulty Profiles");
            settings.ApplyRecommendedDefaults();
            EditorUtility.SetDirty(settings);
        }
    }
}
#endif
