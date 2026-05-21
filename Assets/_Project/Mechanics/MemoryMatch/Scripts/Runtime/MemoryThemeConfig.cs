using TMPro;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [CreateAssetMenu(
        fileName = "MemoryThemeConfig",
        menuName = "NG Education/Memory Match/Theme Config")]
    public sealed class MemoryThemeConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string themeId = "default_theme";
        [SerializeField] private string displayName = "Default Theme";

        [Header("Scene Background")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = Color.white;

        [Header("Title Area")]
        [SerializeField] private Sprite titleBackgroundSprite;
        [SerializeField] private Color titleBackgroundColor = new Color(1f, 1f, 1f, 0f);

        [Header("Instruction / Description Area")]
        [SerializeField] private Sprite instructionBackgroundSprite;
        [SerializeField] private Color instructionBackgroundColor = new Color(1f, 1f, 1f, 0f);

        [Header("Card Front")]
        [SerializeField] private Sprite cardFrontSprite;
        [SerializeField] private Color cardFrontColor = Color.white;

        [Header("Card Back")]
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private Color cardBackColor = new Color(0.2f, 0.45f, 0.9f, 1f);

        [Header("Card Text")]
        [SerializeField] private TMP_FontAsset cardFont;
        [SerializeField] private Color cardTextColor = Color.black;

        [Header("Header Text")]
        [SerializeField] private TMP_FontAsset headerFont;
        [SerializeField] private Color titleTextColor = Color.black;
        [SerializeField] private Color instructionTextColor = Color.black;

        [Header("Timer UI")]
        [SerializeField] private Sprite timerBackgroundSprite;
        [SerializeField] private Color timerBackgroundColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Sprite clockIconSprite;
        [SerializeField] private Color clockIconColor = Color.white;
        [SerializeField] private Color timerTextColor = Color.white;
        [SerializeField] private Color timerWarningTextColor = new Color(1f, 0.25f, 0.15f, 1f);
        [SerializeField] private AudioClip timerWarningTickingLoop;



        [Header("Hint UI")]
        [SerializeField] private Sprite hintBackgroundSprite;
        [SerializeField] private Color hintBackgroundColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Sprite hintIconSprite;
        [SerializeField] private Color hintIconColor = Color.white;
        [SerializeField] private Color hintTextColor = Color.white;
        [SerializeField] private Color hintDisabledTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color hintVisualColor = new Color(1f, 0.85f, 0.15f, 0.85f);

        [Header("Pause Overlay")]
        [SerializeField] private Sprite pauseOverlayBackgroundSprite;
        [SerializeField] private Color pauseOverlayBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Sprite pausePanelSprite;
        [SerializeField] private Color pausePanelColor = Color.white;
        [SerializeField] private Color pauseTitleColor = Color.black;
        [SerializeField] private Color pauseBodyColor = Color.black;

        [Header("Popup Panel Background")]
        [Tooltip("This is the popup box/panel background art, not the content illustration.")]
        [SerializeField] private Sprite popupPanelSprite;
        [SerializeField] private Color popupPanelColor = Color.white;

        [Header("Popup Text")]
        [SerializeField] private Color popupTitleColor = Color.black;
        [SerializeField] private Color popupBodyColor = Color.black;

        [Header("Popup Default Illustration / Character")]
        [Tooltip("Optional subject/theme illustration shown in popup when the matched pair has no specific Learning Image. Example: Maths teacher PNG.")]
        [SerializeField] private Sprite popupDefaultIllustrationSprite;

        [SerializeField] private Color popupDefaultIllustrationColor = Color.white;

        [Tooltip("If false, only pair-specific Learning Image will show.")]
        [SerializeField] private bool useDefaultIllustrationWhenPairImageMissing = true;

        public string ThemeId => themeId;
        public string DisplayName => displayName;

        public Sprite BackgroundSprite => backgroundSprite;
        public Color BackgroundColor => backgroundColor;

        public Sprite TitleBackgroundSprite => titleBackgroundSprite;
        public Color TitleBackgroundColor => titleBackgroundColor;

        public Sprite InstructionBackgroundSprite => instructionBackgroundSprite;
        public Color InstructionBackgroundColor => instructionBackgroundColor;

        public Sprite CardFrontSprite => cardFrontSprite;
        public Color CardFrontColor => cardFrontColor;

        public Sprite CardBackSprite => cardBackSprite;
        public Color CardBackColor => cardBackColor;

        public TMP_FontAsset CardFont => cardFont;
        public Color CardTextColor => cardTextColor;

        public TMP_FontAsset HeaderFont => headerFont;
        public Color TitleTextColor => titleTextColor;
        public Color InstructionTextColor => instructionTextColor;

        public Sprite TimerBackgroundSprite => timerBackgroundSprite;
        public Color TimerBackgroundColor => timerBackgroundColor;
        public Sprite ClockIconSprite => clockIconSprite;
        public Color ClockIconColor => clockIconColor;
        public Color TimerTextColor => timerTextColor;
        public Color TimerWarningTextColor => timerWarningTextColor;
        public AudioClip TimerWarningTickingLoop => timerWarningTickingLoop;

        public Sprite HintBackgroundSprite => hintBackgroundSprite;
        public Color HintBackgroundColor => hintBackgroundColor;
        public Sprite HintIconSprite => hintIconSprite;
        public Color HintIconColor => hintIconColor;
        public Color HintTextColor => hintTextColor;
        public Color HintDisabledTextColor => hintDisabledTextColor;
        public Color HintVisualColor => hintVisualColor;


        public Sprite PauseOverlayBackgroundSprite => pauseOverlayBackgroundSprite;
        public Color PauseOverlayBackgroundColor => pauseOverlayBackgroundColor;
        public Sprite PausePanelSprite => pausePanelSprite;
        public Color PausePanelColor => pausePanelColor;
        public Color PauseTitleColor => pauseTitleColor;
        public Color PauseBodyColor => pauseBodyColor;

        public Sprite PopupPanelSprite => popupPanelSprite;
        public Color PopupPanelColor => popupPanelColor;
        public Color PopupTitleColor => popupTitleColor;
        public Color PopupBodyColor => popupBodyColor;

        public Sprite PopupDefaultIllustrationSprite => popupDefaultIllustrationSprite;
        public Color PopupDefaultIllustrationColor => popupDefaultIllustrationColor;
        public bool UseDefaultIllustrationWhenPairImageMissing => useDefaultIllustrationWhenPairImageMissing;
    }
}
