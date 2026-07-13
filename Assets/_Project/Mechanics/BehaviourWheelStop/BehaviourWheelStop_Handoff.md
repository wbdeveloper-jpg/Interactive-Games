# Behaviour Wheel Stop — Developer / AI Handoff

## Project Summary

**Game name:** Behaviour Wheel Stop  
**Unity type:** 2D educational mini-game, landscape mobile-friendly  
**Mechanic pattern:** Spin-wheel quiz timing game  
**Main idea:** Player reads a question, watches a circular wheel spin, then taps **STOP** when the correct answer reaches the fixed top pointer.

This handoff is for continuing the existing project without rebuilding the scene or losing manually adjusted UI.

---

## Developer Working Context

The current developer is a junior/fresher Unity developer with limited time. Keep future changes practical, inspector-driven, and code-only whenever possible. Avoid unnecessary scene regeneration because the UI has already been manually adjusted.

Preferred style:

- Fast reusable mini-game mechanics
- Clean production-level C#
- Low script count
- Inspector-driven settings
- TextMeshPro for all text
- DOTween is already installed, use it directly when needed
- Do **not** create DOTween key/setup/initializer code
- Avoid over-engineering
- Do not use `ExecuteAlways`
- Do not use `ExecuteInEditMode`
- Do not modify scene objects from `OnValidate`
- Safe Area must apply only in Play Mode
- Editor builder should only create UI when menu item is clicked
- Editor script must not create or modify `.cs` files

---

## Current Game Flow

```text
Bloom Pre-Game
→ Loading Panel
→ How To Play Panel
→ Gameplay
→ Local Result Card
→ Continue Button
→ Bloom Post-Game
```

Important Bloom rule:

```text
Do not place RewardManager in the game scene.
RewardManager already exists in LoadingScene and persists through DontDestroyOnLoad.
Always access through RewardManager.Instance.
```

The game manager implements:

```csharp
IGameSceneCallbacks
IGameAudioCallbacks
```

Callbacks:

```csharp
OnPlayAgain()
OnHome()
OnRewardScreenOpen()
```

---

## Current Modes

The project should keep only these 3 modes:

### 1. Behaviour Mode

Fixed behaviour game using 6 options:

```text
Caring
Selfish
Kind
Respectful
Ignorant
Protective
```

Important: Behaviour mode now uses a **shared icon list** so icons are assigned once, not per question.

Inspector location:

```text
BehaviourWheelQuestionBank
→ Behaviour Mode Shared Icons
```

There should be 6 entries:

```text
Caring
Selfish
Kind
Respectful
Ignorant
Protective
```

The developer only attaches sprite icons here.

### 2. General Mode

A single editable inspector question list.

Use this for:

```text
Science
EVS
GK
English
Any normal question-answer game
```

Inspector location:

```text
BehaviourWheelQuestionBank
→ General Mode Questions
```

Supports:

```text
3 to 6 options
optional option icons
questionText
correctAnswer
explanation
difficulty
quizMode metadata if present
```

Default content currently includes about 25 general science / parts-of-plants questions. The user will later replace or add their own questions.

### 3. Maths Mode

Runtime-generated questions, not manual questions.

Supports:

```text
Addition
Subtraction
Multiplication
Division
```

Important latest update:

- Multiplication uses real symbol: `×`
- Division uses real symbol: `÷`
- Multiplication has separate digit controls for left and right number.
- Division has separate digit controls for dividend and divisor.

Example:

```text
A × B
A ÷ B
```

A and B can have different digit lengths from inspector.

Recommended inspector groups:

```text
Runtime Math Generator
→ Multiplication Factor Sizes
→ Division Number Sizes
```

Example fields:

```text
Multiplication Left Min Digits
Multiplication Left Max Digits
Multiplication Right Min Digits
Multiplication Right Max Digits

Division Dividend Min Digits
Division Dividend Max Digits
Division Divisor Min Digits
Division Divisor Max Digits
```

Division should support whole-number answer generation if enabled.

---

## Wheel Visual Rules

This is the most important part of the game.

The wheel must look like a perfect circular pizza wheel.

### Wheel structure

```text
WheelRoot_Square
├── WheelVisualMesh_WheelGraphic
├── SliceContentRoot
│   ├── Slice_0_Content
│   │   ├── Icon
│   │   └── Label
│   ├── Slice_1_Content
│   └── ...
├── CenterCap_Image_Editable
└── OuterBorder / mesh border if present
```

Pointer is separate:

```text
CenterArea
├── WheelRoot_Square
└── FixedPointerImage
```

### Wheel visual implementation

Use custom UI mesh / `MaskableGraphic`, not rectangular UI panels.

Each slice is a real radial wedge built with triangle fan vertices:

```text
center point = rect center
outer radius = min(width, height) / 2
slice angle = 360 / optionCount
startAngle = sliceIndex * sliceAngle
endAngle = startAngle + sliceAngle
```

Use enough arc segments per slice for smooth curved edges.

The wheel currently supports dynamic slice count:

```text
3 options = 3 slices
4 options = 4 slices
5 options = 5 slices
6 options = 6 slices
```

Max should stay capped at 6.

### Pointer

Pointer stays fixed at the top.

```text
Pointer angle = 90 degrees
Pointer does not rotate
WheelRoot rotates
```

### Selection logic

Do not select by button index.

Use wheel rotation angle:

```csharp
selectedAngle = NormalizeAngle(pointerAngle - wheelRotation);
sliceIndex = Mathf.FloorToInt(selectedAngle / sliceAngle);
selectedAnswer = options[sliceIndex];
```

Visual slice order and detection order must match exactly.

---

## Slice Label / Icon Behaviour

Latest accepted behaviour:

```text
WheelRoot rotates
SliceContentRoot rotates with WheelRoot
Label and Icon rotate naturally with the wheel as printed content
Label and Icon do NOT counter-rotate
Label and Icon do NOT try to stay screen-horizontal
No extra axis spinning
```

The user specifically disliked the previous “keep labels horizontal while wheel spins” behaviour because it created too much motion and confusion.

### Current layout target

- Label is placed farther outward in the wider part of the slice.
- Icon is placed closer to the center side.
- Label box is wide enough to avoid ellipsis dots.
- Text should not end with `...`.
- Math numbers may need bigger font size.

Recommended values:

```text
Label Radius Multiplier = 0.66
Icon Radius Multiplier = 0.42
Label Width Multiplier = 0.68 or higher
Label Height Multiplier = 0.15
Icon Size Multiplier = 0.15
```

### Icon toggle

There is a global icon toggle:

```text
WheelRoot_Square
→ BehaviourWheelSpinner
→ Slice Content Layout
→ Show Icons
```

If OFF:

```text
All icons hidden
Labels still visible
Assigned sprites are ignored
```

There is also:

```text
Label Radius Without Icons Multiplier
```

Use when icons are OFF to center text nicely.

---

## Stop Behaviour

STOP must be instant.

No braking / smooth deceleration after click.

When player taps STOP:

```text
Wheel stops immediately
No further wheel movement
Then feedback animation plays
Then next question loads after delay
```

The current setting should default to:

```text
Instant Stop = ON
```

---

## Feedback Panel Rules

There is a feedback popup/card after each question.

Important correction:

- Correct/Wrong colour must apply to the **inner feedback card image**, not the full panel overlay.
- The outer `FeedbackPanel` background should not change colour.

Inspector should expose:

```text
BehaviourWheelUI
→ Feedback Style
→ Feedback Background Image
→ Correct Feedback Color
→ Wrong Feedback Color
→ Feedback Text Color
→ Make Feedback Text Bold
```

Expected defaults:

```text
Correct Feedback Color = green
Wrong Feedback Color = red
Feedback Text Color = white
Make Feedback Text Bold = true
```

Timing:

```text
BehaviourWheelGameManager
→ Round Settings
→ Feedback Duration
→ Minimum Feedback Duration
```

Recommended:

```text
Minimum Feedback Duration = 1.8 to 2.2 seconds
```

---

## Top Bar Polish

Question counter and score need editable background images.

Expected objects/slots:

```text
QuestionCounterBg_ImageSlot
ScoreBg_ImageSlot
```

Inspector / hierarchy:

```text
GameplayPanel > TopBar > QuestionCounterBg_ImageSlot
GameplayPanel > TopBar > ScoreBg_ImageSlot
BehaviourWheelUI > Top Bar
```

---

## Center Cap

The center cap should be editable, not hard-decided by code.

Current expected object:

```text
WheelRoot_Square > CenterCap_Image_Editable
```

User can:

```text
Change Image color
Assign center cap sprite
Resize from RectTransform
```

The wheel graphic may still have mesh center cap colour, but the editable image is preferred for final art.

---

## Fonts

Two-font system is required.

Inspector component:

```text
BehaviourWheelFontTheme
```

Fields:

```text
Primary Font
Secondary Font
```

Apply fonts to every TMP text in the scene.

Do not leave some TMP text using default font.

---

## Audio

Audio was added as a dedicated controller.

Script:

```text
BehaviourWheelAudioController.cs
```

Inspector clip slots should include:

```text
Background Music
Button Click
Stop Wheel
Correct
Wrong
Feedback Popup
Result
Pause Open
Panel Open
```

The developer will assign clips manually.

When Bloom post-game opens, game BGM should stop through:

```csharp
OnRewardScreenOpen()
```

---

## How To Play Images

The game needs 3 full-screen image pages. User requested:

- No “How to Play” heading
- Large readable text
- Full-screen tutorial images
- Should represent the generalized game, not only behaviour mode
- Should visually match the actual game UI style

Generated concept pages:

### Page 1

Theme:

```text
Read the question carefully.
Look at the answer choices on the wheel.
```

Example:

```text
Which part of a plant absorbs water?
Root / Leaf / Stem / Flower / Fruit / Seed
```

### Page 2

Theme:

```text
Watch the wheel spin.
Wait for the correct answer to come near the pointer.
```

Example:

```text
What is 3 × 4?
12 / 10 / 7 / 9 / 8 / 15
```

### Page 3

Theme:

```text
Tap STOP at the right time.
Stop the wheel when the correct answer reaches the pointer.
```

Example:

```text
The King ignores the elderly people. What behaviour is this?
Correct! +10
```

HTP panel supports image per page:

```text
BehaviourWheelUI
→ How To Play Pages
→ Page Title / Description / Image Sprite
```

For final use, assign the generated images to the page image slots.

---

## Important Script List

Required structure:

```text
Mechanics/BehaviourWheelStop/Scripts/Runtime/
```

Runtime scripts:

```text
BehaviourWheelGameManager.cs
BehaviourWheelSpinner.cs
BehaviourWheelSlice.cs
BehaviourWheelQuestionData.cs
BehaviourWheelQuestionBank.cs
BehaviourWheelUI.cs
BehaviourWheelPausePanel.cs
BehaviourWheelResultPanel.cs
BehaviourWheelFontTheme.cs
BehaviourWheelResponsiveLayout.cs
BehaviourWheelSafeArea.cs
BehaviourWheelWheelGraphic.cs
BehaviourWheelAudioController.cs
```

Editor script:

```text
Mechanics/BehaviourWheelStop/Scripts/Editor/
BehaviourWheelStopSceneBuilder.cs
```

Editor menu:

```text
Tools > Behaviour Wheel Stop > Create Rough UI
```

Do not use this menu on the manually adjusted scene unless intentionally rebuilding UI.

---

## Current Important Code Updates Already Requested

The latest correct combined state should include:

1. Shared behaviour icons code
2. Feedback card colour fix
3. White bold feedback text
4. Longer feedback duration
5. Math multiplication/division separate digit controls
6. Proper `×` and `÷` symbols
7. Icon toggle in spinner
8. Editable top counter/score backgrounds
9. Editable center cap image
10. Instant stop behaviour
11. No label/icon counter-rotation

If a future zip/update is made, ensure these are not accidentally lost.

---

## Scene Safety Notes

The user manually adjusted the UI. Future updates should preferably be code-only.

Avoid:

```text
Scene rebuild
Auto-regenerating Canvas
Moving existing RectTransforms in code unexpectedly
Changing hierarchy names unless needed
Resetting serialized inspector data
```

If a scene update is unavoidable, clearly warn first.

---

## Suggested Future Improvements

Possible future changes that are safe if done code-only:

- Add inspector max label font size for math mode
- Add per-mode wheel colour palettes
- Add per-mode instruction text
- Add optional correct slice glow
- Add option to show correct answer after wrong
- Add per-mode BGM
- Add result medal animation before Bloom post-game

---

## Quick QA Checklist

Before handing to client:

- Behaviour mode shows 6 shared icons from one list.
- General mode questions can be edited from Inspector.
- Math mode generates questions correctly.
- Multiplication displays `×`.
- Division displays `÷`.
- STOP freezes wheel instantly.
- Pointer stays fixed.
- Selected answer matches slice under pointer.
- Feedback card changes green/red, not full overlay.
- Feedback text is white and bold.
- Result screen shows score/correct/wrong.
- Continue opens Bloom post-game.
- Play Again reloads scene.
- Home loads `Loader Scene`.
- BGM stops when Bloom reward screen opens.
- HTP images are assigned and readable.
- No `ExecuteAlways`, no `OnValidate` scene mutation.

---

## Final Note

This mechanic is now a reusable wheel-quiz framework.

Use:

```text
Behaviour mode = fixed 6 behaviour words
General mode = any editable question-answer game
Maths mode = runtime generated arithmetic
```

Keep the wheel visual system untouched unless there is a real bug, because the current accepted wheel behaviour is working well.
