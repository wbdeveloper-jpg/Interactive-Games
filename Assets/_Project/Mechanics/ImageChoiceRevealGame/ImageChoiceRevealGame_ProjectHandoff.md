# Image Choice Reveal Game — Project Handoff

## Project Purpose

This Unity mini-game system is a reusable **Image Choice Reveal Quiz** mechanic.

The player sees a modified question image and chooses the correct image option.

Supported game styles:

- Normal image choice quiz
- Shadow/silhouette guessing
- Zoomed image guessing
- Zoomed + shadowed guessing
- Future image-to-image quiz variations

The system is designed for fast client delivery, easy reskinning, and scene-level customization.

---

## Current Latest Build State

Use this as the current working baseline:

```text
ImageChoiceRevealGame_RealBloomRewardIntegration.zip
```

Then apply this manager script patch:

```text
ImageChoiceRevealGameManager_ScorePopupColors.cs
```

Rename it to:

```text
ImageChoiceRevealGameManager.cs
```

And replace:

```text
Assets/ImageChoiceRevealGame/Runtime/ImageChoiceRevealGameManager.cs
```

This latest manager adds separate score popup colors for:

```text
Correct score popup
Wrong score popup
Hint cost popup
```

---

## Required Unity Packages

### Required

```text
TextMeshPro
DOTween
Bloom Reward System module
```

### TextMeshPro Setup

```text
Window > TextMeshPro > Import TMP Essential Resources
```

### DOTween Setup

```text
Tools > Demigiant > DOTween Utility Panel > Setup DOTween
```

### Bloom Reward System Setup

The Bloom Reward System must already exist in the project.

Important rule:

```text
Do not place RewardManager prefab in this game scene.
RewardManager prefab should exist once in LoadingScene.
It persists using DontDestroyOnLoad.
The game accesses it through RewardManager.Instance.
```

---

## Main Unity Menu

After importing scripts, create the game scene UI using:

```text
Tools > Image Choice Reveal > Create Scene Template UI
```

This creates:

```text
ImageChoiceRevealCanvas
ImageChoiceRevealGameManager
LoadingPanel
GameplayPanel
QuestionFrameImage
QuestionViewport_Masked
QuestionImage
OptionsParent_Grid
OptionButtonTemplate
PausePanel
HowToPlayPanel
ResultPanel
```

---

## Important Scene Template Rule

This project does **not** use option button prefabs anymore.

Instead it uses an inactive scene template:

```text
ImageChoiceRevealCanvas
└── Root
    └── GameplayPanel
        └── OptionsParent_Grid
            └── OptionButtonTemplate
```

Keep `OptionButtonTemplate` inactive.

Customize it directly in the scene.

At runtime, the manager clones this template and creates answer buttons automatically.

This avoids prefab storage problems and allows every scene to have a different option-card style.

---

## Current Script Structure

```text
Assets/
└── ImageChoiceRevealGame/
    ├── Runtime/
    │   ├── ImageChoiceRevealTypes.cs
    │   ├── ImageChoiceRevealQuestionData.cs
    │   ├── ImageChoiceRevealOptionButton.cs
    │   └── ImageChoiceRevealGameManager.cs
    │
    └── Editor/
        └── ImageChoiceRevealSceneCreator.cs
```

No prefab folder is required.

---

## Main Manager

Main script:

```text
ImageChoiceRevealGameManager.cs
```

It handles:

- Loading panel
- Optional how-to-play before gameplay
- Main gameplay loop
- Timer
- Question count
- Score
- Correct/wrong count
- Hint system
- Dynamic option count
- Result panel
- Bloom Reward System integration
- DOTween animation
- Audio
- Scene-template option button cloning

---

## Option Button Script

Script:

```text
ImageChoiceRevealOptionButton.cs
```

It handles:

- Displaying option sprite
- Button click callback
- Correct overlay
- Wrong overlay
- DOTween appear animation
- Correct pulse animation
- Wrong shake animation
- Hide-by-hint animation

The option button does not contain game rules.

Game rules stay inside:

```text
ImageChoiceRevealGameManager
```

---

## Question Data

Script:

```text
ImageChoiceRevealQuestionData.cs
```

Each question has:

```text
Question Name
Question Sprite
Correct Option Sprite
Distractor Sprites
Optional Question Audio
```

For most games:

```text
Question Sprite = Apple
Correct Option Sprite = Apple
Distractors = Banana, Mango, Ball
```

If `Question Sprite` is missing, the manager uses `Correct Option Sprite`.

If `Correct Option Sprite` is missing, the manager uses `Question Sprite`.

---

## Reveal Modes

Inspector field:

```text
Reveal Mode
```

Available modes:

```text
Normal
Shadow
Zoomed
ZoomedShadow
```

### Normal

Shows normal question image.

Recommended hint:

```text
Reduce Options To Two
```

### Shadow

Question image starts black/silhouette.

Hint reveals more color.

### Zoomed

Question image starts zoomed-in.

Hint zooms out.

### ZoomedShadow

Question image starts both zoomed-in and black.

Hint zooms out and reveals color together.

---

## Hint Modes

Inspector field:

```text
Hint Mode
```

Available:

```text
Auto By Reveal Mode
Reduce Options To Two
Shadow Reveal
Zoom Out
```

Recommended:

```text
Auto By Reveal Mode
```

Auto behavior:

```text
Shadow -> reveal more color
Zoomed -> zoom out
ZoomedShadow -> zoom out + reveal color
Normal -> reduce options to two
```

---

## Hint Score Cost

Current latest manager includes:

```text
Hint Score Cost
├── Hint Costs Score
└── Hint Cost Points
```

Recommended default:

```text
Hint Costs Score = false
Hint Cost Points = 5
```

If enabled, every hint subtracts the configured points.

Score never goes below zero.

---

## Score Popup Colors

Current latest manager includes:

```text
Score Popup Colors
├── Correct Score Popup Color
├── Wrong Score Popup Color
└── Hint Cost Popup Color
```

These control the color of:

```text
+10
-2
-5
```

No scene recreation needed to use this.

Just replace the manager script and assign colors in the Inspector.

---

## Layout Design

Current gameplay layout:

```text
Top slim info row:
Score | Instruction | Timer | Question Count | Hint

Center:
Large question frame

Below:
Feedback text and score popup

Lower area:
2x2 image options

Bottom-left:
Pause button

Bottom-right:
How To Play button
```

Large game title is not shown during gameplay.

Game title is shown on the loading panel instead.

---

## Loading Panel

Loading panel contains:

```text
Large game heading
Loading text
Slider loader
Blinking dots loader
```

Loading style options:

```text
Slider
BlinkingDots
SliderAndDots
```

Recommended:

```text
SliderAndDots
```

---

## How-To-Play Flow

Inspector option:

```text
How To Play Flow
└── Show How To Play Before Gameplay
```

If enabled:

```text
Bloom PreGame Panel
-> Loading Panel
-> How To Play Panel
-> Gameplay starts after player closes How To Play
```

Timer behavior:

```text
Before gameplay: timer has not started
During how-to-play: timer pauses
After closing: timer resumes
```

---

## Result Panel

Current result panel shows:

```text
Game Complete
Score
Correct count
Wrong count
Restart button
Continue button
```

Restart reloads the current scene.

Continue opens the Bloom Reward System post-game panel.

---

## Bloom Reward System Integration

The latest game manager directly uses:

```csharp
using RewardSystem;
```

The manager implements:

```csharp
IGameSceneCallbacks
IGameAudioCallbacks
```

Callbacks implemented:

```csharp
OnPlayAgain()
OnHome()
OnRewardScreenOpen()
```

### Pre-game Flow

The manager calls:

```csharp
RewardManager.Instance.ShowPreGame(bloomSkills);
```

Then waits:

```csharp
yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
```

Only after that, the loading panel and game flow continue.

### Post-game Flow

When the player presses `Continue` on the local result panel, the manager calls:

```csharp
RewardManager.Instance.ShowPostGame(bloomSkills, eval);
```

### Evaluation Data

The manager builds:

```text
timeScore
accuracyScore
mistakeCount
timeTaken
```

Calculation:

```text
accuracyScore = correctAnswerCount / plannedQuestionCount
mistakeCount = wrongAnswerCount
timeTaken = Time.time - gameplayStartTime
timeScore = 1 - (timeTaken / expectedMaxTime)
```

All normalized values are clamped between 0 and 1.

---

## Default Bloom Skills

Default skills:

```csharp
Remember = 100
Understand = 100
```

These are exposed in the Inspector as `Bloom Skills`.

Use first two skills by default unless client/game requirement changes.

---

## Home Scene

Inspector field:

```text
Home Scene Name
```

Default:

```text
Loader Scene
```

The manager uses this in:

```csharp
OnHome()
```

---

## Audio

Manager supports:

```text
Click SFX
Correct SFX
Wrong SFX
Hint SFX
Game Complete SFX
Background Music
Question Audio per question
```

When Bloom reward screen opens, manager stops:

```text
musicSource
questionAudioSource
```

Through:

```csharp
OnRewardScreenOpen()
```

---

## DOTween Animations

Current animations:

```text
Question enter animation
Option stagger appear animation
Correct option pulse
Wrong option shake
Score popup float/fade
Hint shadow reveal
Hint zoom out
Hint zoom + shadow reveal
Hint option removal
Panel pop/fade
Loading transition
```

Keep animations minimal and clean.

Avoid over-animating educational games.

---

## Project Fonts

Manager includes:

```text
Project Fonts
├── Primary Font
├── Secondary Font
├── Font Apply Root
├── Primary Font Texts
└── Apply Fonts On Awake
```

Use:

```text
Primary Font = title / important headings
Secondary Font = normal UI text
```

This avoids assigning fonts manually one by one.

---

## Question Frame Structure

Current question image structure:

```text
GameplayPanel
└── QuestionFrameImage
    └── QuestionViewport_Masked
        └── QuestionImage
```

Customize:

```text
QuestionFrameImage
```

as the visible frame.

Keep:

```text
QuestionViewport_Masked
```

for zoom clipping.

---

## Common Setup Steps For New Scene

1. Import scripts.
2. Install TMP + DOTween.
3. Make sure Bloom Reward System exists in LoadingScene.
4. Open target scene.
5. Run:

```text
Tools > Image Choice Reveal > Create Scene Template UI
```

6. Select:

```text
ImageChoiceRevealGameManager
```

7. Assign:

```text
Game Heading
Game Instruction
Questions
Reveal Mode
Hint Mode
Timer
Audio
Fonts
Bloom Skills
```

8. Customize:

```text
OptionButtonTemplate
QuestionFrameImage
Colors
Layout
```

9. Press Play.

---

## Safe Future Update Rules

### Do not recreate the scene for script-only changes

If future changes only affect logic, replace:

```text
ImageChoiceRevealGameManager.cs
```

or another runtime script.

Do not run the scene creator again unless the UI layout structure needs to be rebuilt.

### Do not add RewardManager to game scene

RewardManager belongs only in LoadingScene.

Game scene uses:

```text
RewardManager.Instance
```

### Keep one callback manager

Only one class in the scene should implement:

```text
IGameSceneCallbacks
```

Currently:

```text
ImageChoiceRevealGameManager
```

does this.

Do not add another object implementing the same callbacks unless the reward system guide changes.

### Keep OptionButtonTemplate inactive

The manager duplicates it at runtime.

### Keep Bloom skills consistent

The same skill list is passed to:

```text
ShowPreGame
ShowPostGame
```

---

## If Something Breaks

### DOTween compile error

Install/setup DOTween.

### RewardSystem compile error

Bloom Reward System module is missing or namespace differs.

Check:

```csharp
using RewardSystem;
```

and confirm the module contains:

```text
RewardManager
SkillEntry
BloomSkillType
GameEvaluationData
IGameSceneCallbacks
IGameAudioCallbacks
```

### Options do not show

Check:

```text
OptionButtonTemplate
OptionsParent_Grid
Question data sprites
```

### Timer starts too early

Check:

```text
Show How To Play Before Gameplay
RewardManager.Instance.IsPreGameComplete
```

### Continue button does nothing

Check:

```text
Use Bloom Reward System = true
RewardManager.Instance exists
Bloom Reward System is loaded from LoadingScene
```

---

## Latest Change Log Summary

### Base mechanic
Created reusable image-choice reveal quiz.

### Multi-mode reveal
Added Normal, Shadow, Zoomed, ZoomedShadow.

### UI upgrade
Removed large gameplay heading, moved title to loading panel.

### Scene template
Removed prefab workflow and added inactive scene OptionButtonTemplate.

### Fonts/frame
Added primary/secondary fonts and question frame parent image.

### How-to-play flow
Added optional how-to-play before gameplay and timer pause while how-to-play is open.

### Real Bloom integration
Added direct RewardSystem integration using RewardManager.Instance.

### Result upgrade
Added score, correct count, wrong count, Continue button.

### Score popup color patch
Added correct/wrong/hint cost score popup colors.

---

## Current Recommended Next Improvements

Only if needed later:

1. Add per-question text/audio clue mode.
2. Add blur reveal mode.
3. Add outline reveal mode.
4. Add sprite category auto-random distractor pools.
5. Add optional final local review screen before Bloom reward.
6. Add UI skin ScriptableObject if many visual themes are needed.

Do not add these unless client requires them.

---

## Senior Notes

This project is now a strong reusable base for fast mini-game delivery.

Best workflow:

```text
Same code
Different scene layout
Different sprites
Different option template
Different reveal mode
Different heading/instruction
```

This keeps production fast while making the visible game feel different for clients.
