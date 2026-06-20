using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum GridAdventureItemDisplayMode
{
    ImageOnly,
    ImageAndLabel
}

[CreateAssetMenu(fileName = "GridAdventureLevel", menuName = "Grid Adventure/Level Data")]
public class GridAdventureLevelData : ScriptableObject
{
    [Header("Level")]
    public string levelTitle = "Grid Adventure";
    [Min(1)] public int columns = 3;
    [Min(1)] public int rows = 3;
    public string basketTitle = "ITEM BASKET";

    [Header("Question Selection")]
    [Tooltip("If there are more questions than grid cells, the manager picks a random set for this round.")]
    public bool randomizeWhenMoreThanGridCells = true;

    [Tooltip("0 = fresh random each play. Any positive value gives repeatable random selection for testing.")]
    public int randomSeed = 0;

    [Tooltip("Default visual mode for basket question cards.")]
    public GridAdventureItemDisplayMode itemDisplayMode = GridAdventureItemDisplayMode.ImageAndLabel;

    [Header("Cells / Questions")]
    [Tooltip("You can add more than 9 questions. For a 3x3 grid, only 9 are used per round.")]
    public List<GridAdventureItemData> items = new List<GridAdventureItemData>();

    public int GridCapacity
    {
        get { return Mathf.Max(1, columns) * Mathf.Max(1, rows); }
    }

    public int TotalItems
    {
        get { return items == null ? 0 : items.Count; }
    }

    public GridAdventureItemData GetItemForCoordinate(string coordinate)
    {
        if (items == null || string.IsNullOrEmpty(coordinate)) return null;

        string cleanCoordinate = coordinate.Trim();
        for (int i = 0; i < items.Count; i++)
        {
            GridAdventureItemData item = items[i];
            if (item == null || string.IsNullOrEmpty(item.gridCoordinate)) continue;

            if (string.Equals(item.gridCoordinate.Trim(), cleanCoordinate, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }
}

[Serializable]
public class GridAdventureItemData
{
    [Tooltip("Unique item key. Example: leaf, apple, tiger.")]
    public string itemId;

    [Tooltip("Optional label shown on the basket card when using Image + Label mode.")]
    public string displayName;

    [Tooltip("Optional authoring coordinate. Runtime can remap selected random questions to the active grid order.")]
    public string gridCoordinate = "A1";

    [Header("Clue")]
    [TextArea(2, 4)]
    [FormerlySerializedAs("hintText")]
    public string clueText;

    [Header("Visual")]
    public Sprite sprite;

    [Header("Legacy Optional Audio")]
    [Tooltip("Kept only for backward compatibility. The Read Hint button was removed.")]
    public AudioClip hintAudioClip;
}
