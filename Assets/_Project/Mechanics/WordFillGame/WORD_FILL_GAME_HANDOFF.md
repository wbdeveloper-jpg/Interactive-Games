# Word Fill Game Project Handoff

Save this file in the Unity project root. It is meant to let another AI/developer continue the project if the original chat context is lost.

---

## 1. Project Status

Project name:

**Word Fill Game / Affirmation Words Game**

Current working version:

**V5 - How To Play version**

Current state:

- Base mechanic is working.
- Rough UI is working.
- Manual rough graphics placement has started / mostly done.
- The game has timer, score, hint penalty, narration, pause panel, result panel, background music support, DOTween polish, and How To Play intro.
- Continue from the V5 How To Play version.
- Do not rebuild from scratch unless absolutely required.

Important note:

The later message about “Base Version Review / V2 Feature Update” was sent to the wrong chat and should be ignored unless the user explicitly asks to revisit it.

---

## 2. Developer Context

The user is a junior/fresher Unity developer under time pressure.

Preferred development style:

- Keep guidance practical and short.
- Do not over-explain.
- Use reusable code with flexible layouts.
- Client wants fast games.
- Client can recognize repeated visible mechanics, so code can be reusable but layouts/scenes should feel different.
- Use inspector-driven data.
- Use TextMeshPro.
- Use DOTween for polished UI animation.
- Use dedicated manager names for this game type, not generic names.
- Example: `WordFillAudioManager`, not `AudioManager`.
- Use editor menu tools to auto-create rough working Canvas/UI.
- After rough system works, user manually places and styles graphics.
- Avoid over-engineering.
- Keep script count low but production-safe.

---

## 3. Game Pattern

Pattern:

**Image clue + missing word spelling game**

Flow:

```text
How To Play panel appears
Student clicks Continue
Timer starts
Student sees clue image
Student sees incomplete affirmation sentence
Student taps letter tiles
Correct answer gives score
Correct full line is shown and narrated
Next question loads
After required correct answers or timer ends, result panel appears
```

Example:

```text
Clue image: Aanya showing courage
Word line: I am b _ _ _ _
Answer: brave
Completed narration line: I am brave.
```

---

## 4. Game Content

Chapter:

**Chapter 2 – Humpty Dumpty Had a Great Fall**

Theme:

**List of Affirmation Words**

Characters:

- Aanya
- Rishi

Character info was mentioned as coming from Bridge Course, with Sonia Ma’am and Akash Sir to be contacted if exact references are needed.

---

## 5. Word List

Use these exact words:

```text
brave
blissful
creative
fulfilled
grateful
mindful
peaceful
zealous
```

Narration lines:

```text
I am brave.
I am blissful.
I am creative.
I am fulfilled.
I am grateful.
I am mindful.
I am peaceful.
I am zealous.
```

Blank count reference:

```text
brave     = I am b _ _ _ _
blissful  = I am b _ _ _ _ _ _ _
creative  = I am c _ _ _ _ _ _ _
fulfilled = I am f _ _ _ _ _ _ _ _
grateful  = I am g _ _ _ _ _ _ _
mindful   = I am m _ _ _ _ _ _
peaceful  = I am p _ _ _ _ _ _ _
zealous   = I am z _ _ _ _ _ _
```

Important:

```text
fulfilled = 9 letters total = f + 8 blanks
```

---

## 6. Current Script Structure

Current V5 structure:

```text
Assets/WordFillGame/Scripts/
 ├── WordQuestion.cs
 ├── LetterTile.cs
 ├── WordFillGameController.cs
 ├── WordFillUIAnimator.cs
 ├── WordFillAudioManager.cs
 ├── WordFillHowToPlayStep.cs
 └── WordFillHowToPlayPanel.cs

Assets/WordFillGame/Editor/
 └── WordFillSceneBuilder.cs
```

Current editor menu:

```text
Tools > Word Fill Game > Create V5 How To Play Scene UI
```

---

## 7. Important Prefab

Letter tile prefab default location:

```text
Assets/WordFillGame/Prefabs/LetterTilePrefab.prefab
```

This prefab can be moved anywhere inside `Assets`, but if moved, reassign it in:

```text
WordFillGameController > Letter Tile Prefab
```

Safe to edit on prefab:

- background sprite
- button image
- text font
- text color
- text size
- pressed/highlighted colors

Do not remove:

```text
Button component
Image component
LetterTile script
Child TMP Text
```

---

## 8. DOTween Requirement

DOTween is required.

Current code directly uses:

```csharp
using DG.Tweening;
```

No scripting define symbol is needed.

If compile errors appear, install/import DOTween first.

---

## 9. Features Already Implemented

### Logic

- Random question order
- Inspector-driven questions
- Inspector-driven question count per round
- Inspector-driven max timer
- Score system
- Hint penalty
- Wrong attempt count
- Result panel
- Play again

### Timer

- Starts only after How To Play Continue.
- Pauses during correct feedback and narration.
- Pauses during pause panel.
- Pauses when How To Play is opened during gameplay.
- Timer warning animation/sound can run near the end.

### Hint

- Hint line hidden first.
- Hint button in top bar.
- Hint reveals clue text.
- Hint reduces score by 5.
- Hint has attention animation if unused.
- Hint animation stops after use.

### Audio

Dedicated audio manager:

```text
WordFillAudioManager
```

Audio slots:

```text
Button Tap Clip
Letter Tap Clip
Correct Clip
Wrong Clip
Hint Open Clip
Timer Tick Clip
Time Up Clip
Game Complete Clip
Panel Open Clip
Background Music Clip
```

Each question has:

```text
Completed Line Narration
```

### Narration

After correct answer:

```text
+10 Correct popup
Full completed line appears
Narration audio plays
Text color/pulse animation plays while line is being read
Next question loads
```

If narration is missing, fallback text animation plays using fallback narration duration.

### Pause

- Pause button in top bar.
- Pause overlay panel.
- Continue button resumes game.
- Background music pauses/resumes.

### How To Play

Startup How To Play panel appears every round.

Behavior:

```text
How To Play panel opens
Timer is not running
Student sees instruction text above image
Student uses Prev / Next
Student clicks Continue
Game starts
Timer starts
```

Top bar also has:

```text
How? button
```

Clicking it during gameplay:

```text
Pauses timer
Pauses input
Pauses music
Opens How To Play panel
Continue resumes game
```

How To Play steps are inspector-driven:

```text
Instruction Text
Instruction Image
```

Default steps:

```text
1. Look carefully at the picture.
2. Tap the letter tiles to complete the missing word.
3. Use Hint if you need help, but it reduces your score.
```

---

## 10. Recommended Inspector Settings

On `WordFillGameController`:

```text
Game Heading = Affirmation Words
Game Objective Line = Fill in the missing letters to complete the affirmation.
Questions Per Round = 5
Max Time Seconds = 60
Random Question Order = true
Show How To Play On Round Start = true
Timer Warning Seconds = 10
Hint Penalty Points = 5
Fallback Narration Duration = 1.2
```

On `WordFillAudioManager`:

```text
Music Volume = 0.25 to 0.35
SFX Volume = 0.8 to 1
Narration Volume = 1
Play Music On Round Start = true
```

---

## 11. Manual UI Reference Checklist

If wiring scripts to custom UI, scene needs these objects/references.

Main objects:

```text
WordFillGameController
WordFillUIAnimator
WordFillAudioManager
HowToPlayPanel
LetterTilePrefab
```

Top bar:

```text
GameHeadingText
ScoreText
TimerText
HowToPlayButton
HintButton
PauseButton
FeedbackText
```

Game area:

```text
GameObjectiveText
ClueImage
HiddenHintText
WordText
```

`HiddenHintText` needs:

```text
CanvasGroup
```

Letter area:

```text
LetterButtonParent
BackspaceButton
ClearButton
```

`LetterButtonParent` usually needs:

```text
GridLayoutGroup
```

Popups / overlays:

```text
CenterFeedbackText
PausePanel
CompletePanel
HowToPlayPanel
```

`CenterFeedbackText` needs:

```text
CanvasGroup
```

PausePanel:

```text
PauseTitleText
ContinueButton
```

CompletePanel:

```text
CompleteTitleText
CompleteBodyText
PlayAgainButton
```

HowToPlayPanel:

```text
PanelRoot
TitleText
InstructionText
InstructionImage
PageText
PreviousButton
NextButton
ContinueButton
Steps list
```

---

## 12. Result Panel

No star rating is used.

Reason:

The user will integrate with a global reward system later.

Result panel should show raw performance only:

```text
Correct Answers
Wrong Attempts
Hints Used
Hint Penalty
Final Score
Time Used
```

---

## 13. Visual Direction

Preferred final look:

- Soft pastel children’s educational style
- Violet/lavender as theme color
- Premium storybook-learning look
- Rounded panels
- Soft shadows
- Cream / white / lilac / pastel violet palette
- Child-friendly readable typography
- Large clear word line
- Big tappable letter tiles
- Not too busy

Palette direction:

```text
Primary: pastel violet / lavender
Secondary: lilac
Support: cream, white, blush pink
Accent: soft yellow
Success: mint green
Wrong feedback: soft coral, not harsh red
```

---

## 14. Clue Image Asset Strategy

The user wants:

```text
One common background
+
Transparent clue character sprites
```

In Unity:

```text
Common background image stays same
ClueImage changes per question with transparent character/situation sprite
```

This keeps the game consistent and fast.

---

## 15. Common Background Prompt

Use this for the shared background:

```text
Create a soft pastel educational storybook background for a children’s word learning game. Use a gentle pastel violet theme with lavender, lilac, cream, and soft pink accents. The background should feel clean, premium, calm, and child-friendly. Keep it simple and not busy, with a soft classroom or learning-card atmosphere, subtle decorative shapes, soft gradients, and lots of empty center space for placing character clue sprites. No characters, no text, no UI, no watermark.
```

---

## 16. Common Character Sprite Style Line

Use this in all clue sprite prompts:

```text
Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, Indian child character, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

---

## 17. Clue Sprite Prompts

### Aanya – Brave

```text
A young Indian schoolgirl named Aanya showing courage while facing a challenge, standing confidently with a brave and determined expression, strong posture, child-friendly pose. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Rishi – Blissful

```text
A young Indian schoolboy named Rishi feeling extremely happy and blissful, smiling joyfully with a bright cheerful expression, showing pure happiness and delight. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Aanya – Creative

```text
A young Indian schoolgirl named Aanya writing something imaginative and original, looking inspired and creative, holding a notebook or paper and pencil, expressing imagination and originality. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Rishi – Fulfilled

```text
A young Indian schoolboy named Rishi looking fully satisfied and fulfilled after doing something good or working hard, calm proud smile, peaceful happy expression, confident relaxed posture. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Aanya – Grateful

```text
A young Indian schoolgirl named Aanya feeling grateful and thankful toward Rishi, warm thankful expression, gentle smile, one hand on chest or hands joined in thanks, kind emotional pose. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Aanya – Mindful

```text
A young Indian schoolgirl named Aanya looking mindful and aware, calm thoughtful expression, peaceful focused pose, showing awareness and attention. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Rishi – Peaceful

```text
A young Indian schoolboy named Rishi sitting peacefully, calm and relaxed with a soft smile, serene posture, expressing peace and inner calm. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

### Aanya – Zealous

```text
A young Indian schoolgirl named Aanya full of energy and enthusiasm, lively pose, excited cheerful expression, showing eagerness, passion, and active spirit. Soft pastel children’s educational illustration, premium storybook style, clean polished 2D art, consistent recurring character design, rounded friendly features, soft shading, violet/lavender accent colors, isolated character sprite, transparent background, no text, no watermark, no scene background.
```

---

## 18. What To Do Next

Recommended next steps:

```text
1. Keep the current V5 scene as the working base.
2. Finish clue sprite generation.
3. Assign final clue sprites into question data.
4. Assign narration audio clips.
5. Assign final SFX and background music.
6. Finalize manual UI graphics.
7. Test complete 5-question loop.
8. Build/test on target device.
```

---

## 19. Short Prompt For Future AI

Paste this to continue with another AI:

```text
You are a senior Unity developer and designer helping a junior/fresher Unity developer under time pressure. Continue from the existing Word Fill Game V5 How To Play Unity project. Do not rebuild from scratch unless required. Keep answers short, practical, and production-focused. The game is a reusable image-clue word-fill mechanic for affirmation words. It already has a WordFillGameController, WordFillUIAnimator, WordFillAudioManager, WordFillHowToPlayPanel, LetterTilePrefab, DOTween UI polish, timer, scoring, hint penalty, narration, pause panel, result panel, and How To Play intro. Help only with final setup, UI polish, bug fixes, clue image prompts, and Unity inspector wiring.
```
