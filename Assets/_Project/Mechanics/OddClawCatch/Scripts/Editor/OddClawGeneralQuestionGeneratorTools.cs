#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OddClawGeneralQuestionGeneratorTools
{
    private const string GeneratorFolder =
        "Assets/OddClawCatch/Generated/QuestionGenerators";
    private const string DefaultAssetName =
        "OddClawGeneralQuestionGenerator.asset";

    [MenuItem("Tools/Odd Claw Catch/General Question Mode/Create Generator Asset")]
    public static void CreateGeneratorAsset()
    {
        OddClawGeneralQuestionGenerator generator = CreateAsset();
        SelectAsset(generator);
        Debug.Log(
            "Created Odd Claw General Question Generator: "
            + AssetDatabase.GetAssetPath(generator));
    }

    [MenuItem("Tools/Odd Claw Catch/General Question Mode/Create And Assign To Open Scene Manager")]
    public static void CreateAndAssignToOpenSceneManager()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw General Question Mode",
                "No OddClawCatchManager was found in the open scene.",
                "OK");
            return;
        }

        OddClawGeneralQuestionGenerator generator = CreateAsset();

        Undo.RecordObject(manager, "Assign Odd Claw General Question Generator");
        manager.questionGenerator = generator;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        AssetDatabase.SaveAssets();

        SelectAsset(generator);
        Debug.Log(
            "Created and assigned Odd Claw General Question Generator to "
            + manager.gameObject.name
            + ": "
            + AssetDatabase.GetAssetPath(generator));
    }

    private static OddClawGeneralQuestionGenerator CreateAsset()
    {
        EnsureFolder("Assets", "OddClawCatch");
        EnsureFolder("Assets/OddClawCatch", "Generated");
        EnsureFolder("Assets/OddClawCatch/Generated", "QuestionGenerators");

        string path = AssetDatabase.GenerateUniqueAssetPath(
            GeneratorFolder + "/" + DefaultAssetName);
        OddClawGeneralQuestionGenerator generator =
            ScriptableObject.CreateInstance<OddClawGeneralQuestionGenerator>();

        AssetDatabase.CreateAsset(generator, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return generator;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void SelectAsset(Object asset)
    {
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
#endif
