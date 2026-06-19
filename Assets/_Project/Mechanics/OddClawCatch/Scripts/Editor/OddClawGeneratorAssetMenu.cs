#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OddClawGeneratorAssetMenu
{
    private const string GeneratorFolder = "Assets/OddClawCatch/Generated/QuestionGenerators";

    [MenuItem("Tools/Odd Claw Catch/Create Question Generator/Math Generator")]
    public static void CreateMathGeneratorFromTools()
    {
        CreateGeneratorAsset<OddClawMathQuestionGenerator>("OddClawMathQuestionGenerator.asset");
    }

    [MenuItem("Tools/Odd Claw Catch/Create Question Generator/English Word Generator")]
    public static void CreateEnglishGeneratorFromTools()
    {
        OddClawEnglishQuestionGenerator generator = CreateGeneratorAsset<OddClawEnglishQuestionGenerator>("OddClawEnglishQuestionGenerator.asset");
        generator.ReplaceWordBankWithDefault100();
        EditorUtility.SetDirty(generator);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Odd Claw Catch/Create Question Generator/Sprite Category Generator")]
    public static void CreateSpriteGeneratorFromTools()
    {
        CreateGeneratorAsset<OddClawSpriteQuestionGenerator>("OddClawSpriteQuestionGenerator.asset");
    }

    private static T CreateGeneratorAsset<T>(string defaultFileName) where T : ScriptableObject
    {
        EnsureFolderExists();

        string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GeneratorFolder, defaultFileName));
        T asset = ScriptableObject.CreateInstance<T>();

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log("Created Odd Claw question generator: " + path);
        return asset;
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
