#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OddClawEnglishWordBankTools
{
    private const string GeneratorFolder = "Assets/OddClawCatch/Generated/QuestionGenerators";
    private const string DefaultAssetName = "OddClawEnglishQuestionGenerator_100Words.asset";

    [MenuItem("Tools/Odd Claw Catch/English Word Bank/Create 100 Word English Generator Asset")]
    public static void CreateEnglishGeneratorWith100Words()
    {
        OddClawEnglishQuestionGenerator generator = CreateGeneratorAsset();
        generator.ReplaceWordBankWithDefault100();
        EditorUtility.SetDirty(generator);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = generator;
        EditorGUIUtility.PingObject(generator);
        Debug.Log("Created English generator with " + generator.wordBank.Count + " words.");
    }

    [MenuItem("Tools/Odd Claw Catch/English Word Bank/Replace Selected Generator With 100 Words")]
    public static void ReplaceSelectedGeneratorWith100Words()
    {
        OddClawEnglishQuestionGenerator generator = Selection.activeObject as OddClawEnglishQuestionGenerator;
        if (generator == null)
        {
            EditorUtility.DisplayDialog("Odd Claw English Word Bank", "Select an OddClawEnglishQuestionGenerator asset first.", "OK");
            return;
        }

        ReplaceGeneratorBank(generator);
        Debug.Log("Updated selected English generator with " + generator.wordBank.Count + " words: " + AssetDatabase.GetAssetPath(generator));
    }

    [MenuItem("Tools/Odd Claw Catch/English Word Bank/Replace Selected Generator With 100 Words", true)]
    private static bool ValidateReplaceSelectedGeneratorWith100Words()
    {
        return Selection.activeObject is OddClawEnglishQuestionGenerator;
    }

    [MenuItem("Tools/Odd Claw Catch/English Word Bank/Update Scene Manager Assigned Generator")]
    public static void UpdateSceneManagerAssignedGenerator()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Odd Claw English Word Bank", "No OddClawCatchManager found in the active scene.", "OK");
            return;
        }

        OddClawEnglishQuestionGenerator generator = manager.questionGenerator as OddClawEnglishQuestionGenerator;
        if (generator == null)
        {
            EditorUtility.DisplayDialog("Odd Claw English Word Bank", "The current scene manager is not assigned to an English generator. Use Create And Assign 100 Word English Generator To Scene Manager instead.", "OK");
            return;
        }

        ReplaceGeneratorBank(generator);
        Debug.Log("Updated scene manager assigned English generator with " + generator.wordBank.Count + " words.");
    }

    [MenuItem("Tools/Odd Claw Catch/English Word Bank/Create And Assign 100 Word English Generator To Scene Manager")]
    public static void CreateAndAssignEnglishGeneratorToSceneManager()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Odd Claw English Word Bank", "No OddClawCatchManager found in the active scene.", "OK");
            return;
        }

        OddClawEnglishQuestionGenerator generator = CreateGeneratorAsset();
        generator.ReplaceWordBankWithDefault100();
        generator.mode = OddClawEnglishMode.MixedRandom;
        EditorUtility.SetDirty(generator);

        Undo.RecordObject(manager, "Assign Odd Claw English Generator");
        manager.questionGenerator = generator;
        EditorUtility.SetDirty(manager);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = generator;
        EditorGUIUtility.PingObject(generator);

        Debug.Log("Created and assigned English generator with " + generator.wordBank.Count + " words to scene manager.");
    }

    private static void ReplaceGeneratorBank(OddClawEnglishQuestionGenerator generator)
    {
        Undo.RecordObject(generator, "Replace Odd Claw English Word Bank");
        generator.ReplaceWordBankWithDefault100();
        EditorUtility.SetDirty(generator);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static OddClawEnglishQuestionGenerator CreateGeneratorAsset()
    {
        EnsureFolderExists();
        string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GeneratorFolder, DefaultAssetName));
        OddClawEnglishQuestionGenerator generator = ScriptableObject.CreateInstance<OddClawEnglishQuestionGenerator>();
        AssetDatabase.CreateAsset(generator, path);
        return generator;
    }

    private static void EnsureFolderExists()
    {
        EnsureFolder("Assets", "OddClawCatch");
        EnsureFolder("Assets/OddClawCatch", "Generated");
        EnsureFolder("Assets/OddClawCatch/Generated", "QuestionGenerators");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
