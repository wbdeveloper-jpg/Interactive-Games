using TMPro;
using UnityEngine;

public enum GridAdventureFontRole
{
    Primary,
    Secondary
}

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class GridAdventureTextFontRole : MonoBehaviour
{
    [Tooltip("Primary = titles/buttons/headers. Secondary = body/clue/counters/item labels.")]
    public GridAdventureFontRole fontRole = GridAdventureFontRole.Secondary;
}
