Emotion Timer Quiz - Clean Production Setup
==========================================

This package is rebuilt from the last stable version:
EmotionTimerQuiz_UnityPackage_Final_25Q_Loading_Fonts.zip

There are only two editor menu items:

1) Tools > Emotion Timer Quiz > Create Clean Scene
2) Tools > Emotion Timer Quiz > Create 25 Questions

What this version includes
--------------------------
- Stable visible OptionCardsRow with OptionCard_A, OptionCard_B, OptionCard_C
- No prefabs
- Scene-template only
- 25 sample questions
- Loading panel before How To Play
- Mandatory How To Play panel before gameplay
- DOTween UI polish
- Primary Font and Secondary Font fields
- Timer progress uses Unity Slider, not filled image
- Pause button on left side
- Next Round button on right side with 10-second auto-continue countdown
- Result panel shows score, questions played, correct, wrong, timed out, total time
- Progressive question count:
  - First completed play: 5 questions
  - Second completed play: 10 questions
  - Then 15, 20, 25 based on completed play count
- CanvasScaler + SafeArea support for mobile/tablet screens

How to install
--------------
1. Backup your current scene.
2. Replace your old Assets/EmotionTimerQuiz folder with this one.
3. Let Unity compile.
4. Run:
   Tools > Emotion Timer Quiz > Create Clean Scene
5. Press Play.

Expected hierarchy
------------------
EmotionTimerQuizCanvas
└── SafeAreaRoot
    ├── TopHUD
    ├── TimerProgressSlider
    ├── SituationCard
    ├── FeedbackTextHolder
    ├── OptionCardsRow
    │   ├── OptionCard_A
    │   ├── OptionCard_B
    │   └── OptionCard_C
    ├── PauseActionLeft
    └── NextRoundActionRight

How to add emotion images
-------------------------
1. Select EmotionTimerQuizCanvas > EmotionTimerQuizManager.
2. In Inspector, open Asset Registry.
3. For each character, assign sprites:
   - HAPPY
   - SAD
   - ANGRY
   - SCARED
   - CONFIDENT
   - EXCITED

Important rule:
Each question uses one target character. Wrong answers are automatically picked from the same character, so children cannot guess by character design.

How to add guide images
-----------------------
1. Select EmotionTimerQuizManager.
2. Open How To Play Guide.
3. Add sprites to Guide Images.
4. If an image is missing, the fallback text from Guide Fallback Texts will show.

How to reset the progressive level test
---------------------------------------
During testing, clear PlayerPrefs or change Completed Play Prefs Key in the manager.
The key defaults to:
EmotionTimerQuiz_CompletedPlayCount

DOTween
-------
This package uses DOTween.
Make sure DOTween is installed and setup in your project.
