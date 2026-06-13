# SkyFall Game — Handoff for Next AI / Developer

## Latest state

Use the latest package:

`SkyFallGame_Fresh_Final_v5_1_CarrierYFix.zip`

This is the correct fresh base. The user should delete all older SkyFall/SkyCatch/MathSkyCatch folders before importing it.

Delete old folders if they exist:

```text
Assets/_Project/Mechanics/SkyFallGame
Assets/SkyFallGame
Assets/SkyCatchGame
Assets/MathSkyCatchGame
```

Then import only the latest fresh package.

Unity menu after import:

```text
Tools > SkyFall > Create Final Production Math Scene
```

---

## User preference

The user is a fresher/junior Unity developer under time pressure. Give practical production-ready Unity help without over-explaining.

Preferred output style:

1. Identify mechanic pattern.
2. Say whether reusable mechanics are possible.
3. Suggest fastest architecture.
4. Keep script count reasonable.
5. Use Inspector-driven settings.
6. Include editor menu builders.
7. Keep core mechanic reusable.
8. Content-specific logic should live in providers.
9. Do not suddenly rename architecture.
10. Give clean setup steps.

---

## Core architecture

The game is called **SkyFall**.

Important: SkyFall is the reusable core mechanic. It must not become a math-only manager.

Correct architecture:

```text
SkyFallGameManager = reusable core gameplay manager
SkyFallMathContentProvider = math-only content/rule generator
```

Future content should be added as new providers:

```text
SkyFallGrammarContentProvider
SkyFallScienceContentProvider
SkyFallPhonicsContentProvider
```

Do not rename the core manager to `MathSkyCatch`, `MathSkyFall`, etc.

---

## Fresh folder structure

```text
Assets/
└── SkyFallGame/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── SkyFallBasketDrag.cs
    │   │   ├── SkyFallContentProviderBase.cs
    │   │   ├── SkyFallDropData.cs
    │   │   ├── SkyFallFallingItem.cs
    │   │   ├── SkyFallFontThemeApplier.cs
    │   │   ├── SkyFallGameManager.cs
    │   │   ├── SkyFallImageGuidePanel.cs
    │   │   ├── SkyFallSafeAreaFitter.cs
    │   │   ├── SkyFallScreenFlowController.cs
    │   │   ├── SkyFallUiPanelAnimator.cs
    │   │   └── SkyFallUiTrailEmitter.cs
    │   └── Content/
    │       └── Math/
    │           └── SkyFallMathContentProvider.cs
    └── Editor/
        └── SkyFallSceneBuilder.cs
```

There should be no old patch file named:

```text
SkyFallFinalOverlaySceneTools.cs
```

If it exists, delete it. It was from older patches and caused compile errors.

---

## Current gameplay

Mechanic pattern:

```text
Falling Answer Catcher / SkyFall
```

Flow:

1. Flyer moves horizontally across the top.
2. It drops one item.
3. Player drags the basket left/right.
4. Player catches only correct objects based on the question.
5. Correct catch gives score.
6. Wrong catch gives penalty.
7. Game ends by timer or lives depending on selected mode.

Default spawning rule:

```text
Only one falling object active at a time.
Next object spawns only after current item is caught or missed.
```

Do not make multiple falling objects by default.

---

## Game-over modes

`SkyFallGameManager` supports:

```text
TimeLimited
LifeLimited
```

Time mode:

- Shows countdown timer.
- Ends when time reaches 0.

Life mode:

- Shows life icons.
- Lost lives fade/grey out.
- Ends when lives reach 0.
- Tracks survived time.

Top HUD should visually switch between timer and lives based on selected mode.

---

## Math provider

`SkyFallMathContentProvider` currently supports runtime generation.

Modes:

```text
CatchEvenNumbers
CatchOddNumbers
CatchEvenAnswers
CatchOddAnswers
CatchSelectedOperation
```

Operations:

```text
Plus
Minus
Multiply
Divide
```

Inspector settings include:

- Min digit count.
- Max digit count.
- Allow plus/minus/multiply/divide.
- Allow 3-number equations.
- Correct item chance.
- Equation chance in number modes.
- Clean division handling.

Math generation should remain in the math provider, not in the core manager.

---

## Generated scene hierarchy

Main root:

```text
SkyFallCanvas_Final
└── SafeArea
    ├── BackgroundLayer
    ├── GameplayLayer
    ├── TrailFxLayer
    ├── HUDLayer
    └── OverlayLayer
```

Top HUD:

```text
TopHUDRoot
├── ScoreGroup        // left
├── QuestionGroup     // middle
├── ModeInfoGroup     // right: timer/lives
└── PauseButtonRoot   // same line
```

Overlay hierarchy:

```text
LoadingPanelRoot
└── LoadingCard

HowToPlayPanelRoot
└── HowToPlayCard

PausePanelRoot
└── PauseCard

ResultPanelRoot
└── ResultCard

FeedbackPanelRoot
└── FeedbackCard
```

Preserve root/card/content structure.

---

## Loading screen

Loading screen is intentionally simple:

```text
Large game name
Unity Slider loader
```

Runtime field is:

```csharp
Slider loadingSlider;
```

Do not use old field:

```csharp
loadingProgressFill
```

Old editor scripts using `loadingProgressFill` must be deleted.

---

## How-to-play images

Inspector location:

```text
Hierarchy:
SkyFallCanvas_Final
└── SafeArea
    └── OverlayLayer
        └── HowToPlayPanelRoot

Inspector component:
SkyFallImageGuidePanel
└── Guide Images
```

User can set any number of guide images.

---

## Fonts

Inspector location:

```text
Hierarchy:
SkyFallCanvas_Final

Component:
SkyFallFontThemeApplier
├── Primary Font
└── Secondary Font
```

Primary font should be used for:

- Question text.
- Game title.
- Button text.
- Result/Pause/How To Play titles.
- Falling item numbers/equations.

Secondary font should be used for:

- Score label/value.
- Timer label/value.
- Lives label.
- Result details.
- Guide page counter/helper text.

---

## Basket input rule

Basket must not teleport on play-area click.

Correct behavior:

```text
User must start click/touch on basket.
Then drag left/right.
Y axis is locked.
Movement is smooth.
```

Script:

```text
SkyFallBasketDrag.cs
```

Recommended defaults:

```text
Require Start On Basket = true
Use Smooth Movement = true
Follow Speed = 22
Lock Y Position = true
Side Padding = 20
```

---

## Responsive falling

Do not use raw pixel fall speed as the primary difficulty setting.

Correct logic:

```text
fallSpeed = distanceToBasketZone / targetReachTime
```

Reason:

- Phone landscape has short height.
- Tablet has tall height.
- Reaction time should stay consistent.

Inspector values:

```text
Easiest Reach Time
Hardest Reach Time
Minimum Fall Distance
Basket Catch Zone Y Offset
Reach Time Curve
```

Recommended defaults:

```text
Easiest Reach Time = 3.2
Hardest Reach Time = 1.35
Minimum Fall Distance = 350
Basket Catch Zone Y Offset = 20
```

---

## Latest carrier Y bug fix

Bug found: flyer moved out of screen on Y axis, sometimes Y became more than 2000.

Cause: carrier bob animation was feeding the bobbed Y back into `carrierBaseY`.

Fixed in latest package:

```text
SkyFallGame_Fresh_Final_v5_1_CarrierYFix.zip
```

Correct rule:

```text
carrierBaseY is locked when flyer starts a route.
Bobbing is visual-only around locked base Y.
Do not assign carrierBaseY from bobbed current Y every frame.
```

Do not reintroduce this bug.

---

## Flyer direction

Flyer should face movement direction.

Hierarchy:

```text
FlyingCarrier
└── CarrierDirectionVisual
```

Replace flyer art on `CarrierDirectionVisual`.

Recommended mode:

```text
Carrier Direction Visual Mode = FlipScaleX
```

---

## Falling item hierarchy

```text
FallingItemPrefab
├── CatchHitBox
└── VisualRoot
    ├── FallingTrailAnchor
    └── OuterCard
        └── InnerCard
            ├── ItemIcon
            └── ItemText
```

Why:

- Visual card can be wide for long equations.
- CatchHitBox stays fixed/narrow.
- Wide cards should not make gameplay too easy.

Adaptive layout supports:

- Small tile for short numbers.
- Medium tile for 3-digit numbers.
- Wide card for equations.
- Two-line card for long equations.

---

## Current animations

Already included:

- Falling object starts small and grows.
- Correct item moves into basket, shrinks, fades.
- Wrong item rejects upward, shrinks, fades.
- Basket bounces on correct catch.
- Basket shakes on wrong catch.
- Feedback text floats/fades.
- Score pulses.
- Timer pulses when low.
- Result panel pop.
- Flyer soft bob.
- UI trails for flyer, falling item, and basket.

No DOTween dependency. It should compile without DOTween.

---

## Trails

`SkyFallUiTrailEmitter.cs` uses UI Image particles because the game is Canvas/UI-based.

Trail anchors:

```text
CarrierDirectionVisual
└── FlyerTrailAnchor

FallingItemPrefab
└── VisualRoot
    └── FallingTrailAnchor

BasketVisual
└── BasketTrailAnchor
```

Theme-neutral. Works for plane, witch, bird, cloud, rocket, etc.

---

## Audio

`SkyFallGameManager` has:

```text
SFX Source
Music Source
Background Music
Correct Clip
Wrong Clip
Game Over Clip
Drop Clip
Play Background Music On Scene Start
Music Volume
```

Background music support is included.

---

## Recommended setup for user

1. Delete old folders.
2. Import `SkyFallGame_Fresh_Final_v5_1_CarrierYFix.zip`.
3. Create empty Unity scene.
4. Run:

```text
Tools > SkyFall > Create Final Production Math Scene
```

5. Select `SkyFallCanvas_Final`.
6. Assign Primary Font and Secondary Font.
7. Select `HowToPlayPanelRoot`.
8. Assign guide images.
9. Select `SkyFallGameManager`.
10. Assign audio if available.
11. Press Play.

---

## Neutral how-to-play prompt

For one reusable guide image across many games:

```text
clean casual mobile game tutorial illustration, neutral colorful theme, soft gradient background, rounded UI elements, premium 2D mobile game art, modern educational casual game style, child-friendly but not babyish, polished interface, high readability, minimal clutter, bright focal highlights, simple visual instruction, reusable across different game genres, no specific festival or character theme, no watermark.

Create a single landscape how-to-play guide image for a generic casual mobile game. Show a top instruction bar, a clean gameplay area, example target objects, a player-controlled object or touch interaction area, a hand icon showing drag/tap interaction, one example correct action with a "+10" popup, one wrong action with a "-5" popup, and a top HUD with score, instruction, and timer or lives. Make the composition very clear, simple, and reusable across different educational and casual mini-games. Add only one heading: "HOW TO PLAY".
```

---

## Mistakes to avoid

Do not:

- Rename SkyFall core into math-specific scripts.
- Move math generation into `SkyFallGameManager`.
- Use tap-to-teleport basket.
- Spawn multiple falling items by default.
- Use raw pixel speed as difficulty.
- Use wide visual card for catch detection.
- Keep old editor patch files.
- Use `loadingProgressFill`.
- Let flyer bob modify route base Y.
- Overdo loading screen.
- Create theme-specific builder unless user asks.

---

## Likely next work

Potential next tasks:

1. Grammar content provider.
2. ScriptableObject level packs.
3. Theme-specific skin replacement.
4. Better mute/settings UI.
5. Button click SFX.
6. More level difficulty presets.

Best path for grammar:

```text
Assets/SkyFallGame/Scripts/Content/Grammar/SkyFallGrammarContentProvider.cs
```

Core manager should stay reusable.
