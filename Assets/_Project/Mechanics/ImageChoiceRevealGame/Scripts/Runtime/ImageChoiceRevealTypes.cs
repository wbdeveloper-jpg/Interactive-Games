using UnityEngine;

namespace ImageChoiceRevealGame
{
    public enum ImageChoiceRevealMode { Normal = 0, Shadow = 1, Zoomed = 2, ZoomedShadow = 3 }
    public enum ImageChoiceHintMode { AutoByRevealMode = 0, ReduceOptionsToTwo = 1, ShadowReveal = 2, ZoomOut = 3 }
    public enum ImageChoiceOptionDisplayType { Image = 0, Text = 1 }
    public enum ImageChoiceOptionDisplayMode { UsePerOptionSetting = 0, ForceImage = 1, ForceText = 2 }
    public enum ImageChoiceQuestionRevealOverride { UseManagerDefault = 0, Normal = 1, Shadow = 2, Zoomed = 3, ZoomedShadow = 4 }

    public enum ImageChoiceLoadingStyle { Slider = 0, BlinkingDots = 1, SliderAndDots = 2 }

    [System.Serializable]
    public class ImageChoiceScoreSettings
    {
        [Min(0)] public int correctScore = 10;
        [Min(0)] public int wrongPenalty = 2;
        [Min(0)] public int hintPenalty = 0;
    }

    [System.Serializable]
    public class ImageChoiceTimerSettings
    {
        public bool useTimer = true;
        [Min(5f)] public float gameDurationSeconds = 60f;
    }

    [System.Serializable]
    public class ImageChoiceRevealSettings
    {
        [Header("Shadow")]
        [Range(0f, 1f)] public float shadowStartRevealAmount = 0f;
        [Range(0.05f, 1f)] public float shadowHintRevealStep = 0.35f;

        [Header("Zoom")]
        [Min(1f)] public float zoomStartScale = 2.2f;
        [Range(0.1f, 2f)] public float zoomHintStep = 0.45f;
    }

    [System.Serializable]
    public class ImageChoiceLoadingSettings
    {
        public bool showLoadingPanel = true;
        [Range(0.2f, 5f)] public float loadingDuration = 1.15f;
        public ImageChoiceLoadingStyle loadingStyle = ImageChoiceLoadingStyle.SliderAndDots;
    }

    [System.Serializable]
    public class ImageChoiceAnimationSettings
    {
        public bool useAnimations = true;
        [Header("Question")]
        [Range(0.05f, 1f)] public float questionEnterDuration = 0.28f;
        [Range(0.05f, 1f)] public float questionHintDuration = 0.35f;
        [Header("Options")]
        [Range(0.05f, 1f)] public float optionEnterDuration = 0.22f;
        [Range(0f, 0.25f)] public float optionStaggerDelay = 0.045f;
        [Range(0.05f, 1f)] public float optionRemoveDuration = 0.2f;
        [Header("Feedback")]
        [Range(0.1f, 1.5f)] public float feedbackDuration = 0.42f;
        [Range(0.2f, 2f)] public float scorePopupDuration = 0.75f;
        [Header("Panels")]
        [Range(0.05f, 1f)] public float panelFadeDuration = 0.22f;
    }

    [System.Serializable]
    public class ImageChoiceAudioSettings
    {
        [Header("SFX")]
        public AudioClip clickSfx;
        public AudioClip correctSfx;
        public AudioClip wrongSfx;
        public AudioClip hintSfx;
        public AudioClip gameCompleteSfx;
        [Header("Background Music")]
        public bool playBackgroundMusic = true;
        public bool loopBackgroundMusic = true;
        [Range(0f, 1f)] public float backgroundVolume = 0.35f;
        public AudioClip backgroundMusic;
    }
}
