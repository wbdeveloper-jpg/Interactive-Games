# OddSuckMechanic — Developer / AI Handoff File

**Current working version:** V5.4.1 Strict Beam Catch Patch  
**Project type:** Unity UI-based 2D mini-game mechanic  
**Mechanic name:** OddSuckMechanic  
**Main manager:** `OddSuckManager`  
**Target user/developer:** Junior Unity developer under tight production timeline  
**Preferred style:** Fast reusable mini-game code, inspector-driven setup, low complexity, production-level structure.

---

## 1. Game Summary

OddSuckMechanic is an endless UFO-themed odd-one-out game.

The UFO moves automatically across the top of the screen. Objects appear on the ground. One object is different from the others. The player waits until the UFO beam is aligned over the odd object, then taps anywhere to pull it.

The game supports:

- Math/text odd-one-out mode
- Sprite/image odd-one-out mode
- Mixed random mode
- Endless waves
- Health bar
- Wave timer
- Increasing UFO speed
- Bloom Reward System integration
- How-to-play image navigation
- Loading panel
- UFO entry and exit animations
- Tractor-beam energy particles

---

## 2. Latest Stable Version

Use this version as the base for future work:

```text
OddSuckMechanic V5.4.1
```

Important meaning of V5.4.1:

- V5.4 created the final production-level scene structure.
- V5.4.1 added strict beam catch logic so empty taps do not pull nearby objects.

If continuing development, start from the **V5.4 scene structure** plus the **V5.4.1 OddSuckManager.cs patch**.

---

## 3. Recommended Unity Folder Location

```text
Assets/_Project/Mechanics/OddSuckMechanic
```

Expected structure:

```text
OddSuckMechanic/
  Scripts/
    Runtime/
      OddSuckManager.cs
      OddSuckItemView.cs
      OddSuckUfoAutoMover.cs
      OddSuckAudioManager.cs
      OddSuckUiParticleEmitter.cs
      OddSuckQuestionGeneratorBase.cs
      OddSuckMathQuestionGenerator.cs
      OddSuckSpriteCategoryQuestionGenerator.cs
      OddSuckFeedbackPopup.cs

    Editor/
      OddSuckSceneBuilder.cs

  README_Setup.md
```

---

## 4. Current Scene Builder

Use this menu for fresh scene generation:

```text
Tools > Odd Suck > Create V5.4 Production Structured Scene
```

After scene generation, apply the latest script-only patch if needed:

```text
V5.4.1 Strict Beam Catch Patch
```

Usually this means replacing only:

```text
Assets/_Project/Mechanics/OddSuckMechanic/Scripts/Runtime/OddSuckManager.cs
```

---

## 5. Scene Regeneration Rule

Do **not** regenerate the scene for small gameplay logic patches.

Regenerate only when:

- New UI objects are required
- Overlay panel structure changes
- New buttons or references are added
- New item templates are added
- Scene hierarchy must be rebuilt

Do not regenerate for:

- Gap tuning
- Beam catch tuning
- Score/health logic changes
- Speed tuning
- Timer tuning
- Audio logic changes
- Generator logic changes

The user has already done UI polish work, so avoid regeneration unless absolutely required.

---

## 6. Production UI Structure

Overlay panels should follow this hierarchy:

```text
PanelRoot
  OverlayDim
  PanelCard
    Header
    Body
    Footer
```

This should apply to:

```text
LoadingPanel
HowToPlayPanel
PausePanel
ResultPanel
```

Use layout groups inside cards where possible:

- `VerticalLayoutGroup` for card sections
- `HorizontalLayoutGroup` for footer buttons
- `ContentSizeFitter` only where needed

---

## 7. HUD Structure

Top bar should be clean and production-friendly:

```text
TopBar
  HealthGroup
    HealthLabel
    HealthSlider

  WaveTimerGroup
    WaveText
    TimerSlider

  SpeedText
  PauseButton
    PauseIcon
```

Rules:

- Health label should only say `Health`
- Do not show health number
- Timer should not have separate time text
- Wave text should show like `Wave 1`
- Pause button should be square
- Pause button should use an icon, not text

---

## 8. Core Gameplay Flow

Current intended flow:

```text
Bloom Pre-Game Panel
→ Loading Panel
→ How To Play Panel
→ UFO Entry Animation
→ Gameplay Starts
→ Health reaches zero
→ UFO Exit Animation
→ Result Panel
→ Continue Button
→ Bloom Post-Game Panel
```

Important:

- Gameplay must not start before Bloom pre-game completes.
- Local loading starts after Bloom pre-game.
- How-to-play starts after loading.
- UFO entry starts after how-to-play.
- Timer/controls start after UFO entry.
- Bloom post-game opens only when result panel Continue button is clicked.

---

## 9. Bloom Reward System Integration

The game integrates Bloom from V5 onward.

Critical rule:

```text
Do not place RewardManager in the game scene.
```

RewardManager already exists in the loading scene and persists through `DontDestroyOnLoad`.

Access it only through:

```csharp
RewardManager.Instance
```

`OddSuckManager` should implement:

```csharp
IGameSceneCallbacks
IGameAudioCallbacks
```

Expected callbacks:

```csharp
public void OnPlayAgain()
{
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

public void OnHome()
{
    SceneManager.LoadScene("Loader Scene");
}

public void OnRewardScreenOpen()
{
    // Stop local game music here.
}
```

Bloom pre-game call:

```csharp
RewardManager.Instance.ShowPreGame(_skills);
```

Wait before continuing:

```csharp
yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
```

Bloom post-game call should happen from result Continue button:

```csharp
RewardManager.Instance.ShowPostGame(_skills, eval);
```

---

## 10. Question Generator System

The mechanic should not use fixed rounds anymore.

`OddSuckManager` asks a generator for the next wave.

Current generators:

```text
OddSuckMathQuestionGenerator
OddSuckSpriteCategoryQuestionGenerator
```

Play modes:

```text
Math Only
Sprite Only
Mixed Random
```

Future generators can be added without changing the core game loop:

```text
Color generator
Shape generator
Word category generator
Audio clue generator
Image matching generator
```

---

## 11. Math/Text Mode

Text/math mode supports:

- Odd among even
- Even among odd
- Random odd/even
- Direct number
- Addition
- Subtraction
- Mixed expressions

Text/math mode uses separate item templates:

```text
OddSuckTextItemTemplate_Left
OddSuckTextItemTemplate_Center
OddSuckTextItemTemplate_Right
```

These are real scene templates and should have their own background image directly set on the template.

Do not use inspector sprite swapping for these backgrounds anymore.

Slot style logic:

```text
2 items → Left, Right
3 items → Left, Center, Right
4 items → Left, Center, Center, Right
5 items → Left, Left, Center, Right, Right
6 items → Left, Left, Center, Center, Right, Right
```

Purpose:

- Text boxes can look like they are dropped with different perspective angles.
- This gives better production polish without requiring separate prefabs for each object.

---

## 12. Sprite/Image Mode

Sprite-only/image mode must use a separate clean template:

```text
OddSuckImageItemTemplate
```

Rules:

- Do not use left/center/right box angle templates for image mode.
- Image mode should show icons clearly.
- Text should be hidden.
- Icon sprite should be visible.
- Template background alpha should usually be `0` if only the icon should show.

Expected image mode visual:

```text
Item background/template = invisible or clean card
Icon sprite = visible
Text = hidden
```

---

## 13. Item Spacing / Gap System

V5.3.2 added safe script-side gap controls.

Inspector path:

```text
OddSuckManager > Spawn Layout
```

Fields:

```text
Item X Padding
Item Spacing Multiplier
Minimum Item Gap
```

Meaning:

- `Item X Padding` controls side padding from the left/right edge.
- `Item Spacing Multiplier` increases/decreases spacing between objects.
- `Minimum Item Gap` prevents objects from becoming too close.

Recommended values:

```text
Item Spacing Multiplier = 1.15
Minimum Item Gap = 25
```

If objects still feel close:

```text
Item Spacing Multiplier = 1.25
Minimum Item Gap = 35
```

This patch does not require scene regeneration.

---

## 14. Tap / Pull Logic

Important latest fix from V5.4.1:

The game should not pull the nearest object when the beam is visually in empty space.

Current correct logic:

```text
Only pull an item if it is inside the beam catch zone.
```

Inspector path:

```text
OddSuckManager > Rules
```

Important fields:

```text
Use Beam Catch Zone
Beam Catch Zone Width
Alignment Tolerance
```

Recommended values:

```text
Use Beam Catch Zone = true
Beam Catch Zone Width = 60
Alignment Tolerance = 40
```

Meaning:

- `Beam Catch Zone Width` is now the real gameplay pull area.
- `Alignment Tolerance` is legacy/fallback behavior.
- If `Use Beam Catch Zone = true`, tune `Beam Catch Zone Width`, not `Alignment Tolerance`.

Expected behavior:

```text
Tap with beam over item → pulls item
Tap in empty beam area → no item pulled, wrong/no-target feedback plays
```

---

## 15. UFO Movement

The UFO moves automatically.

Features:

- Horizontal auto movement
- Some vertical drifting for realistic movement
- Hover/bob animation
- Direction flip when moving left/right
- Sprite-frame animation while moving
- Entry animation from top
- Exit animation toward random top direction

Exit animation should sometimes go:

```text
Top
Top-left
Top-right
```

If random exit causes issues, it can safely fall back to top-only.

---

## 16. Beam Energy Effect

The old UFO trail was removed because the client said UFOs should not emit trail while moving.

Current effect:

```text
Tractor-beam energy particles only when the beam/light is open.
```

Rules:

- No movement trail
- No Unity TrailRenderer
- No world ParticleSystem required
- Use UI pooled particle emitter
- Particles are active only inside the beam

Normal mode:

```text
Tap → Beam opens → Beam energy particles play → Item pulls → Beam closes → Particles stop
```

Easy mode:

```text
Beam always open → Beam energy particles can stay active
```

---

## 17. Health and Timer

Health and timer use Unity `Slider` components.

Health:

- Green/current health slider updates immediately.
- Yellow/damage slider follows after a small delay.
- On last damage, wait for yellow damage animation to finish before UFO exit/result.

Timer:

- Each wave has a time limit.
- Wave time decreases gradually.
- Minimum wave time should stay at 15 seconds.
- Timeout reduces health like a wrong answer.

---

## 18. Audio

`OddSuckAudioManager` should support:

- SFX for correct
- SFX for wrong
- SFX for tap/pull
- SFX for UI clicks if needed
- Background music clip
- Music volume
- Loop background music
- Play music when gameplay starts
- Stop music on game over / Bloom reward screen

Bloom audio callback:

```csharp
public void OnRewardScreenOpen()
{
    oddSuckAudioManager.StopMusic();
}
```

---

## 19. How-To-Play System

How-to-play panel should support multiple guide images.

Inspector field:

```text
OddSuckManager > How To Play > How To Images
```

Buttons:

```text
Previous
Next
Start
```

Optional UI:

```text
Step Counter: 1 / 4
```

Behavior:

- If images are assigned, show images.
- If no images are assigned, show fallback text.
- Player must finish/close how-to before UFO entry and gameplay.

Recommended guide image count:

```text
3 or 4 images
```

Suggested image topics:

```text
1. Find the odd object
2. Wait for the UFO
3. Tap to pull
4. Be careful with health and time
```

---

## 20. Loading Panel

Loading panel should appear after Bloom pre-game and before How-To-Play.

Structure:

```text
LoadingPanelRoot
  OverlayDim
  PanelCard
    Header
      GameNameText
    Body
      LoadingSlider
    Footer
```

The loading slider is fake/local loading, not actual Unity async loading unless future developer adds that.

---

## 21. Result Panel

Result panel should appear after game over animation.

Buttons:

```text
Play Again
Continue
```

Important:

- `Continue` should open Bloom post-game.
- Bloom handles Play Again / Home callbacks after its post-game screen.

Result flow:

```text
Health reaches zero
→ yellow health damage animation finishes
→ UFO flies back to space
→ Result panel opens
→ Continue button opens Bloom post-game
```

---

## 22. Coding Rules for Future Developers / AI

Follow these rules strictly:

1. Do not over-engineer.
2. Keep the mechanic reusable but simple.
3. Use inspector fields for gameplay tuning.
4. Use dedicated class names with `OddSuck` prefix.
5. Do not rename existing public serialized fields unless migration is handled.
6. Avoid scene regeneration for script-only changes.
7. Preserve the V5.4 production hierarchy.
8. Keep TextMeshPro for text.
9. Use DOTween for UI animation/polish.
10. Explicitly use `UnityEngine.Random`, never plain `Random`.

Correct random usage:

```csharp
UnityEngine.Random.Range(min, max);
UnityEngine.Random.value;
```

Avoid:

```csharp
Random.Range(min, max);
Random.value;
```

Reason: avoids ambiguity between `UnityEngine.Random` and `System.Random`.

---

## 23. Known User Preferences

The main developer/user prefers:

- Practical solutions
- Low explanation
- Production-ready scripts
- Simple setup
- Editor menu scene creation
- Inspector-driven tuning
- Reusable mechanics
- No unnecessary abstract systems
- Clear folder structure
- Dedicated manager names
- Minimal script count when possible
- TextMeshPro
- DOTween
- No full scene regeneration unless necessary

---

## 24. Things to Avoid

Avoid these mistakes:

- Do not silently change scene hierarchy in a script-only patch.
- Do not add new required references without saying scene regeneration is needed.
- Do not create RewardManager in this scene.
- Do not make image mode use text angled box templates.
- Do not pull nearest object if beam is empty.
- Do not use UFO movement trail; beam effect only.
- Do not show health numbers unless explicitly requested.
- Do not make pause button wide with text.
- Do not place overlay UI elements randomly under panel root.

---

## 25. Future Safe Improvements

Safe script-only improvements:

- More question generators
- Better scoring formula
- Difficulty tuning
- More feedback animations
- More audio hooks
- Speed curve tuning
- Better timeout penalty logic

Scene regeneration likely required for:

- New overlay panel structure
- New HUD layout
- New buttons
- New templates
- New major UI references

---

## 26. Final Current Status

The game is close to final production polish.

The biggest completed decisions are:

- Endless gameplay instead of fixed rounds
- Bloom integrated
- Production panel structure added
- Separate text templates and image template added
- Strict beam catch logic added
- Health/timer sliders working
- How-to image navigation added
- Beam particles replace UFO movement trail
- BGM support restored

Future work should protect the current structure and only make focused changes.

