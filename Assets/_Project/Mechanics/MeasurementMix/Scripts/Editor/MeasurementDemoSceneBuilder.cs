#if UNITY_EDITOR
using UnityEditor;

namespace MeasurementMix.Editor
{
    /// <summary>
    /// Compatibility entry point for the first package version.
    /// It intentionally never creates, opens or saves a scene.
    /// </summary>
    public static class MeasurementDemoSceneBuilder
    {
        [MenuItem("Tools/Measurement Mix/Build UI In Current Open Scene")]
        public static void BuildInCurrentOpenScene()
        {
            MeasurementGameUIBuilder.BuildRoughUI();
        }

        // Retained for code that called the old method directly. Despite the
        // legacy method name, it only adds UI to the active scene.
        public static void CreateDemoScene()
        {
            MeasurementGameUIBuilder.BuildRoughUI();
        }
    }
}
#endif
