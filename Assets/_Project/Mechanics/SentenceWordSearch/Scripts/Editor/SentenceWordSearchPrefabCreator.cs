#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SentenceWordSearchPrefabCreator
{
    public const string RootPath = "Assets/_Project/Mechanics/SentenceWordSearch";
    public const string PrefabFolder = RootPath + "/Prefabs";
    public const string CellPrefabPath = PrefabFolder + "/SentenceWordSearchCell.prefab";

    [MenuItem("Mini Games/Sentence Word Search/Create Missing Cell Prefab")]
    public static SentenceWordSearchCell CreateMissingCellPrefab()
    {
        EnsureFolders();

        SentenceWordSearchCell existing = AssetDatabase.LoadAssetAtPath<SentenceWordSearchCell>(CellPrefabPath);

        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            Debug.Log("Sentence Word Search Cell prefab already exists. Selected existing prefab.");
            return existing;
        }

        GameObject root = new GameObject("SentenceWordSearchCell", typeof(RectTransform));

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100f, 100f);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(1f, 0.96f, 0.96f, 1f);

        Button button = root.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        SentenceWordSearchCell cell = root.AddComponent<SentenceWordSearchCell>();
        cell.backgroundImage = bg;
        cell.button = button;

        Image solved = CreateOverlay(root.transform, "SolvedOverlay", new Color(0.45f, 0.86f, 0.55f, 0.55f));
        Image preview = CreateOverlay(root.transform, "PreviewOverlay", new Color(0.95f, 0.35f, 0.32f, 0.45f));
        Image hint = CreateOverlay(root.transform, "HintRing", new Color(0.95f, 0.22f, 0.22f, 0.95f));

        GameObject textObject = new GameObject("LetterText", typeof(RectTransform));
        textObject.transform.SetParent(root.transform, false);
        Stretch(textObject.GetComponent<RectTransform>());

        TextMeshProUGUI letterText = textObject.AddComponent<TextMeshProUGUI>();
        letterText.text = "A";
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.fontSize = 42f;
        letterText.fontStyle = FontStyles.Bold;
        letterText.color = new Color(0.22f, 0.18f, 0.18f, 1f);
        letterText.raycastTarget = false;

        cell.solvedOverlayImage = solved;
        cell.previewOverlayImage = preview;
        cell.hintRingImage = hint;
        cell.letterText = letterText;

        solved.raycastTarget = false;
        preview.raycastTarget = false;
        hint.raycastTarget = false;

        solved.gameObject.SetActive(false);
        preview.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SentenceWordSearchCell created = prefab.GetComponent<SentenceWordSearchCell>();
        Selection.activeObject = prefab;

        Debug.Log("Created Sentence Word Search Cell prefab at: " + CellPrefabPath);
        return created;
    }

    public static SentenceWordSearchCell LoadOrCreateCellPrefab()
    {
        EnsureFolders();

        SentenceWordSearchCell cell = AssetDatabase.LoadAssetAtPath<SentenceWordSearchCell>(CellPrefabPath);

        if (cell != null)
            return cell;

        return CreateMissingCellPrefab();
    }

    private static Image CreateOverlay(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "_Project");
        CreateFolderIfMissing("Assets/_Project", "Mechanics");
        CreateFolderIfMissing("Assets/_Project/Mechanics", "SentenceWordSearch");
        CreateFolderIfMissing(RootPath, "Prefabs");
        CreateFolderIfMissing(RootPath, "Scripts");
        CreateFolderIfMissing(RootPath + "/Scripts", "Runtime");
        CreateFolderIfMissing(RootPath + "/Scripts", "Editor");
        CreateFolderIfMissing(RootPath, "Demo");
    }

    private static void CreateFolderIfMissing(string parent, string folder)
    {
        string path = parent + "/" + folder;

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }
}

#endif
