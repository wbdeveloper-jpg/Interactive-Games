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

        [Header("Audio")]
        [SerializeField] private MemoryAudioConfig audioConfig;

        [Header("Fonts")]
        [Tooltip("Used for major headings: activity title, popup title, pause title, summary title.")]
        [SerializeField] private TMP_FontAsset headerFont;

        [Tooltip("Used for normal readable text: instructions, popup body, summary body, pause body.")]
        [SerializeField] private TMP_FontAsset bodyFont;

        [Tooltip("Used for compact UI text: timer, score, hint counter, guide step counter.")]
        [SerializeField] private TMP_FontAsset uiFont;

        [Tooltip("Used only for text shown inside memory cards.")]
        [SerializeField] private TMP_FontAsset cardFont;

        [Header("Scene Background")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = Color.white;

        [Header("Title Area")]
        [SerializeField] private Sprite titleBackgroundSprite;
        [SerializeField] private Color titleBackgroundColor = new Color(1f, 1f, 1f, 0f);

        [Header("Instruction / Description Area")]
        [SerializeField] private Sprite instructionBackgroundSprite;
        [SerializeField] private Color instructionBackgroundColor = new Color(1f, 1f, 1f, 0f);

        [Header("Header Text")]
        [SerializeField] private Color titleTextColor = Color.black;
        [SerializeField] private Color instructionTextColor = Color.black;

        [Header("Top Buttons - Pause")]
        [SerializeField] private Sprite pauseButtonBackgroundSprite;
        [SerializeField] private Color pauseButtonBackgroundColor = Color.white;
        [SerializeField] private Sprite pauseButtonIconSprite;
        [SerializeField] private Color pauseButtonIconColor = Color.white;

        [Header("Top Buttons - How To Play")]
        [SerializeField] private Sprite howToPlayButtonBackgroundSprite;
        [SerializeField] private Color howToPlayButtonBackgroundColor = Color.white;
        [SerializeField] private Sprite howToPlayButtonIconSprite;
        [SerializeField] private Color howToPlayButtonIconColor = Color.white;

        [Header("Card Front")]
        [SerializeField] private Sprite cardFrontSprite;
        [SerializeField] private Color cardFrontColor = Color.white;

        [Header("Card Back")]
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private Color cardBackColor = new Color(0.2f, 0.45f, 0.9f, 1f);

        [Header("Card Text")]
        [SerializeField] private Color cardTextColor = Color.black;

        [Header("Card State Visuals")]
        [SerializeField] private Sprite selectedVisualSprite;
        [SerializeField] private Color selectedVisualColor = new Color(0.2f, 0.6f, 1f, 0.9f);
        [SerializeField] private Sprite matchedVisualSprite;
        [SerializeField] private Color matchedVisualColor = new Color(0.2f, 0.9f, 0.35f, 1f);
        [SerializeField] private Sprite hintVisualSprite;
        [SerializeField] private Color hintVisualColor = new Color(1f, 0.85f, 0.15f, 0.85f);

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

        [Header("Score UI")]
        [SerializeField] private Sprite scoreBackgroundSprite;
        [SerializeField] private Color scoreBackgroundColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color scoreTextColor = Color.white;
        [SerializeField] private Color scorePositiveDeltaColor = new Color(0.25f, 1f, 0.25f, 1f);
        [SerializeField] private Color scoreNegativeDeltaColor = new Color(1f, 0.25f, 0.15f, 1f);

        [Header("Learning Popup Panel")]
        [Tooltip("This is the popup box/panel background art, not the content illustration.")]
        [SerializeField] private Sprite popupPanelSprite;
        [SerializeField] private Color popupPanelColor = Color.white;
        [SerializeField] private Color popupTitleColor = Color.black;
        [SerializeField] private Color popupBodyColor = Color.black;

        [Header("Learning Popup Progress")]
        [SerializeField] private Color popupProgressBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color popupProgressFillColor = new Color(0.3f, 0.65f, 1f, 1f);

        [Header("Popup Default Illustration / Character")]
        [Tooltip("Optional subject/theme illustration shown in popup when the matched pair has no specific Learning Image. Example: Maths teacher PNG.")]
        [SerializeField] private Sprite popupDefaultIllustrationSprite;
        [SerializeField] private Color popupDefaultIllustrationColor = Color.white;
        [Tooltip("If false, only pair-specific Learning Image will show.")]
        [SerializeField] private bool useDefaultIllustrationWhenPairImageMissing = true;

        [Header("Pause Overlay")]
        [SerializeField] private Sprite pauseOverlayBackgroundSprite;
        [SerializeField] private Color pauseOverlayBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Sprite pausePanelSprite;
        [SerializeField] private Color pausePanelColor = Color.white;
        [SerializeField] private Color pauseTitleColor = Color.black;
        [SerializeField] private Color pauseBodyColor = Color.black;

        [Header("Summary Overlay")]
        [SerializeField] private Sprite summaryOverlayBackgroundSprite;
        [SerializeField] private Color summaryOverlayBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Sprite summaryPanelSprite;
        [SerializeField] private Color summaryPanelColor = Color.white;
        [SerializeField] private Color summaryTitleColor = Color.black;
        [SerializeField] private Color summaryBodyColor = Color.black;
        [SerializeField] private Color summaryMetricsColor = Color.black;

        public string ThemeId => themeId;
        public string DisplayName => displayName;
        public MemoryAudioConfig AudioConfig => audioConfig;

        public TMP_FontAsset HeaderFont => headerFont;
        public TMP_FontAsset BodyFont => bodyFont != null ? bodyFont : headerFont;
        public TMP_FontAsset UIFont => uiFont != null ? uiFont : BodyFont;
        public TMP_FontAsset CardFont => cardFont != null ? cardFont : BodyFont;

        public Sprite BackgroundSprite => backgroundSprite;
        public Color BackgroundColor => backgroundColor;

        public Sprite TitleBackgroundSprite => titleBackgroundSprite;
        public Color TitleBackgroundColor => titleBackgroundColor;
        public Sprite InstructionBackgroundSprite => instructionBackgroundSprite;
        public Color InstructionBackgroundColor => instructionBackgroundColor;

        public Color TitleTextColor => titleTextColor;
        public Color InstructionTextColor => instructionTextColor;

        public Sprite PauseButtonBackgroundSprite => pauseButtonBackgroundSprite;
        public Color PauseButtonBackgroundColor => pauseButtonBackgroundColor;
        public Sprite PauseButtonIconSprite => pauseButtonIconSprite;
        public Color PauseButtonIconColor => pauseButtonIconColor;

        public Sprite HowToPlayButtonBackgroundSprite => howToPlayButtonBackgroundSprite;
        public Color HowToPlayButtonBackgroundColor => howToPlayButtonBackgroundColor;
        public Sprite HowToPlayButtonIconSprite => howToPlayButtonIconSprite;
        public Color HowToPlayButtonIconColor => howToPlayButtonIconColor;

        public Sprite CardFrontSprite => cardFrontSprite;
        public Color CardFrontColor => cardFrontColor;
        public Sprite CardBackSprite => cardBackSprite;
        public Color CardBackColor => cardBackColor;
        public Color CardTextColor => cardTextColor;

        public Sprite SelectedVisualSprite => selectedVisualSprite;
        public Color SelectedVisualColor => selectedVisualColor;
        public Sprite MatchedVisualSprite => matchedVisualSprite;
        public Color MatchedVisualColor => matchedVisualColor;
        public Sprite HintVisualSprite => hintVisualSprite;
        public Color HintVisualColor => hintVisualColor;

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

        public Sprite ScoreBackgroundSprite => scoreBackgroundSprite;
        public Color ScoreBackgroundColor => scoreBackgroundColor;
        public Color ScoreTextColor => scoreTextColor;
        public Color ScorePositiveDeltaColor => scorePositiveDeltaColor;
        public Color ScoreNegativeDeltaColor => scoreNegativeDeltaColor;

        public Sprite PopupPanelSprite => popupPanelSprite;
        public Color PopupPanelColor => popupPanelColor;
        public Color PopupTitleColor => popupTitleColor;
        public Color PopupBodyColor => popupBodyColor;
        public Color PopupProgressBackgroundColor => popupProgressBackgroundColor;
        public Color PopupProgressFillColor => popupProgressFillColor;

        public Sprite PopupDefaultIllustrationSprite => popupDefaultIllustrationSprite;
        public Color PopupDefaultIllustrationColor => popupDefaultIllustrationColor;
        public bool UseDefaultIllustrationWhenPairImageMissing => useDefaultIllustrationWhenPairImageMissing;

        public Sprite PauseOverlayBackgroundSprite => pauseOverlayBackgroundSprite;
        public Color PauseOverlayBackgroundColor => pauseOverlayBackgroundColor;
        public Sprite PausePanelSprite => pausePanelSprite;
        public Color PausePanelColor => pausePanelColor;
        public Color PauseTitleColor => pauseTitleColor;
        public Color PauseBodyColor => pauseBodyColor;

        public Sprite SummaryOverlayBackgroundSprite => summaryOverlayBackgroundSprite;
        public Color SummaryOverlayBackgroundColor => summaryOverlayBackgroundColor;
        public Sprite SummaryPanelSprite => summaryPanelSprite;
        public Color SummaryPanelColor => summaryPanelColor;
        public Color SummaryTitleColor => summaryTitleColor;
        public Color SummaryBodyColor => summaryBodyColor;
        public Color SummaryMetricsColor => summaryMetricsColor;
    }
}
