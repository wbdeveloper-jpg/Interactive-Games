# Setup Guide - Tag Basket Sorter v12

## Create Rough Scene

Use:

`Tools > Tag Basket Sorter > Create Rough 5-Level UI`

This creates:

- Landing level page
- 5 level panels
- Locked/unlocked level buttons
- Gameplay page
- Top bar with large score/progress, center slider timer, hint counter, square pause button
- Hint button with remaining count like `3/3`
- Pause panel
- Result panel with Continue + Play Again only
- Auto-start How To Play panel
- First-level breathing tutorial overlay with background
- Basket title badge background behind every basket title
- Bloom pre-game/post-game integration hooks

## Final Layout Notes

This is intended as the final generated layout pass.

Generated basket hierarchy:

```text
Basket_Common_Noun
├── BasketTitleBackground
│   └── BasketTitle
├── PlacedItemsRoot
└── BasketFrontOverlay
```

Runtime layering forces this visual order:

```text
Basket image/base
PlacedItemsRoot
BasketFrontOverlay
BasketTitleBackground + BasketTitle
```

So dropped objects look inside the basket, and the title remains readable.

## Top Bar

Generated layout:

- Left: score and progress text
- Middle: Unity `Slider` timer
- Right: hint container with hint button + remaining hint count
- Far right: square pause button

Manager fields:

- `timerSlider`
- `timerSliderFillImage`
- `hintContainer`
- `hintCounterText`
- `hintButton`
- `pauseButton`

`timerText` is still available only for old/legacy scenes. New generated scenes do not use it.

## Bloom Integration

Do not place `RewardManager` in this game scene.

`RewardManager` should already exist once in `LoadingScene` and persist with `DontDestroyOnLoad`.

The manager implements:

```csharp
IGameSceneCallbacks
IGameAudioCallbacks
```

Callbacks included:

```csharp
OnPlayAgain() -> reloads current scene
OnHome() -> loads homeSceneName, default "Loader Scene"
OnRewardScreenOpen() -> stops BGM if enabled
```

## Add / Hide Levels

### Add a level

Duplicate one `Level_X_Panel` under `LevelPanelsRoot`.

Then select `TagBasketSorter_Canvas` and click:

`Refresh Levels And Level Buttons Now`

This creates/updates landing level buttons in Edit Mode.

### Hide a level

Select the level panel and disable:

`isLevelEnabled`

Hidden levels will not appear in landing buttons and will not count in progression.

## Hints

Each level has:

`maxHintsAllowed`

Default is 3.

Hint behavior:

- Start shows `3/3`.
- First new hint reduces it to `2/3`.
- Repeated taps on the same hint do not reduce the count again.
- The same object and basket pulse until that object is correctly placed.
- After the hinted object is placed, the next hint can select another object.
- Hint text message is hidden by default. Enable `showHintTextOverlay` only if you want text.

## Basket Feel

Each basket has:

- `placementRoot`
- `basketFrontOverlay`
- `titleBackgroundImage`
- organic placed item jitter
- organic placed item rotation

Designer/manual object positions are untouched before drop. Random position/rotation applies only inside basket after correct placement.

## Draggable Visual Mode

Each draggable item has:

`visualMode`

Options:

- `ImageOnly`
- `ImageAndLabel`

Default generated label color is red.

## Fonts

On `TagBasketSortGameManager`, assign:

- `primaryFont`
- `secondaryFont`

Then click:

`Apply Primary/Secondary Fonts To Texts`

This applies fonts to all TextMeshPro texts under the generated game canvas.

## Audio

Assign these on `TagBasketSortGameManager`:

- `backgroundMusicClip`
- `correctClip`
- `wrongClip`
- `hintClip`
- `clockWarningClip`
- `levelCompleteClip`
- `gameCompleteClip`
- `timeoutClip`

Clock warning plays once when remaining time is below `clockWarningTimePercent`, default 10%.

## Required Packages

- TextMeshPro
- DOTween
- Bloom Reward System


## v12 Patch
- First-level tutorial breathing animation now targets the full tutorial card/background, not only the text.
- Hint pulse animation now lasts longer by default using `hintPulseDuration = 0.45` and `hintPulseLoopCount = 4`.
- Editor builder assigns `tutorialBreathTarget` automatically when regenerating the scene.
