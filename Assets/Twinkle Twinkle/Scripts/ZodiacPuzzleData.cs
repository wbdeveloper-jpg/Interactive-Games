using UnityEngine;

[CreateAssetMenu(menuName = "Zodiac Puzzle/Zodiac Puzzle Data", fileName = "ZodiacPuzzleData")]
public class ZodiacPuzzleData : ScriptableObject
{
    [Header("Identity")]
    public ZodiacSign sign;
    [Tooltip("Optional. Leave empty to use the enum display name.")]
    public string displayNameOverride;
    [TextArea(2, 4)] public string descriptionOverride;

    [Header("Puzzle Art")]
    [Tooltip("Full puzzle image shown before shuffling. Optional but recommended.")]
    public Sprite fullPuzzleSprite;
    [Tooltip("Puzzle pieces in correct slot order. Piece 0 belongs to Slot 0, etc.")]
    public Sprite[] pieceSprites;

    [Header("Result Art")]
    public Sprite resultSprite;

    [Header("Constellation Reveal")]
    [Tooltip("Prefab with ConstellationAnimator attached. Unique for each zodiac.")]
    public GameObject constellationPrefab;

    [Header("Gameplay")]
    [Min(1f)] public float timeLimitSeconds = 60f;

    public string DisplayName
    {
        get { return string.IsNullOrWhiteSpace(displayNameOverride) ? sign.GetDisplayName() : displayNameOverride; }
    }

    public string Description
    {
        get { return string.IsNullOrWhiteSpace(descriptionOverride) ? sign.GetDefaultDescription() : descriptionOverride; }
    }
}
