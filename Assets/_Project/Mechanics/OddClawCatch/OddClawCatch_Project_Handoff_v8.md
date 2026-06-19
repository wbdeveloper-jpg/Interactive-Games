# Odd Claw Catch — Project Handoff

**Project:** Odd Claw Catch  
**Mechanic:** Fixed-pivot rotating claw catcher / timing-based educational answer game  
**Latest package at handoff:** `OddClawCatch_UnityPackage_v8_EasyModeButton.zip`  
**Handoff date:** 2026-06-18  
**Current owner/user:** MOON / Narayanagroup  
**Current project stage:** Scene layout is ready. Future updates should avoid layout regeneration unless explicitly requested.

---

## 1. Current project summary

This is a reusable Unity mini-game mechanic where:

1. A claw is fixed at the top of the screen.
2. The claw rotates left and right like a pendulum/aiming arm.
3. Answer objects are placed at the bottom/ground area.
4. The player taps/clicks anywhere to extend the claw along its current angle.
5. The claw catches an object only if its catch zone actually overlaps the object.
6. The caught object attaches to the claw head/grab socket.
7. The claw retracts to the pivot.
8. The game evaluates correct/wrong after the claw returns.
9. Correct catch increases score/wave.
10. Wrong catch or timeout reduces health.
11. Miss only plays a miss animation and lets the player retry the same question.
12. The game continues until health reaches zero.

This is **not** UFO sucking, tractor beam, rope-pull, or balloon movement. It is a **fixed-pivot rotating claw timing game**.

---

## 2. Latest package/version status

Latest clean package:

```text
OddClawCatch_UnityPackage_v8_EasyModeButton.zip
```

The package contains all previous gameplay fixes plus:

- 100-word English synonym/antonym bank.
- Easy mode runtime toggle methods.
- Optional `OddClawEasyModeButton` helper script.
- Editor helper to attach easy-mode toggle script to an existing button.
- No layout regeneration required for v7/v8 changes.

Important: The README inside the package may still have older top line text saying v6, but the package includes v7 and v8 sections and code.

---

## 3. Current user instruction / project rule

The user’s scene is already manually prepared and polished enough to continue.

**Do not modify layout unless explicitly requested.**

Future updates should be:

- Script-only when possible.
- Inspector-driven.
- Safe for the current scene.
- No `OddClawCatch_Root` regeneration unless user asks.
- No new UI hierarchy unless user explicitly asks.
- Do not reset designer-tuned RectTransforms.

The user may add their own buttons/UI objects manually, and scripts should support being wired from existing UI.

---

## 4. Main scripts

Runtime scripts:

```text
Assets/OddClawCatch/Scripts/Runtime/OddClawQuestionData.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawQuestionGeneratorBase.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawMathQuestionGenerator.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawSpriteQuestionGenerator.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawEnglishQuestionGenerator.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawItemView.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawController.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawAudioManager.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawFeedbackPopup.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawCatchManager.cs
Assets/OddClawCatch/Scripts/Runtime/OddClawEasyModeButton.cs
```

Editor scripts:

```text
Assets/OddClawCatch/Scripts/Editor/OddClawSceneBuilder.cs
Assets/OddClawCatch/Scripts/Editor/OddClawGeneratorAssetMenu.cs
Assets/OddClawCatch/Scripts/Editor/OddClawEnglishWordBankTools.cs
Assets/OddClawCatch/Scripts/Editor/OddClawEasyModeButtonTools.cs
```

---

## 5. Design philosophy for future changes

This project should be treated as **designer-owned layout + script-owned behavior**.

Correct rule:

```text
Designer-set RectTransform values are primary.
Code should animate from cached scene values.
Code should not silently force size/position.
Only override values when an Inspector field explicitly says so.
```

Avoid hardcoding:

```text
ClawArm height
ClawHead Y
Grabber Y
Grabbed object offset
Top bar size
Item area dimensions
Panel sizes
```

Prefer:

```text
Cache current scene values at Awake/Start.
Expose overrides in Inspector.
Use current RectTransform as baseline.
```

---

## 6. Claw behavior status

Important fixed behavior:

- Claw no longer scales down while grabbing.
- Claw arm/head should respect designer-set RectTransform values.
- `ClawArm` height should not reset at runtime.
- `ClawHeadGrabber` Y should not reset at runtime.
- Runtime caches designer values and returns to them after retract.
- Grabbed item alignment is inspector-driven.

Key inspector areas in `OddClawController`:

```text
Designer Layout Source
Grabbed Object Attach
Extend And Catch
Normal Claw Sprite
Grabbing Claw Sprite
```

Grab attach flow:

1. Use `GrabSocket` if assigned.
2. Apply global grabbed item offset/rotation/scale.
3. Apply per-item/template grab offset/rotation/scale if enabled.

Recommended future rule: if an item looks bad while being held, tune item/template grab offset first, not code.

---

## 7. Evaluation timing status

Final desired flow:

```text
Tap
→ Claw stops rotating
→ Claw extends
→ If object hit, claw closes / grabbing sprite shows
→ Object attaches to grab socket
→ Claw retracts to cached idle pose
→ Evaluate correct/wrong
→ Show popup
→ Color item green/red
→ Fade caught item away
→ Wait for evaluation sequence to finish
→ Start next question/wave
```

Miss flow:

```text
Tap
→ Extend
→ No catch
→ Miss animation/popup
→ Retract
→ Same question remains
→ Same objects remain
→ Player retries
```

Current intended behavior:

- Correct/wrong evaluation must fully finish before claw resumes.
- Popup and item fade must not overlap with next question spawn.
- Input/rotation should remain locked during evaluation.
- Miss should not reduce health by default.
- Miss should not change question by default.
- Miss should not reduce Bloom accuracy by default.

Inspector timing areas:

```text
OddClawCatchManager > Evaluation Flow
OddClawItemView > Evaluation Animation
OddClawCatchManager > Gameplay Rules
```

---

## 8. Top bar/UI status

The final requested top bar direction was:

Top row:

```text
Score    Question    Pause
```

Bottom row:

```text
HP label + HP slider    Wave number    Timer slider    Speed multiplier
```

Important notes:

- No separate time label needed because timer uses a slider.
- Wave number sits where old time label/empty space was.
- Pause button remains.
- Home button removed from local pause/result UI.
- Result panel should have only:
  - `CONTINUE`
  - `PLAY AGAIN`
- Pause panel should have only:
  - `RESUME`
  - `RESTART`

Speed text format should be multiplier style:

```text
1X
1.1X
1.5X
2X
```

---

## 9. Easy mode button status

v8 added script-only easy-mode support.

Public methods on `OddClawCatchManager`:

```csharp
EnableEasyMode()
DisableEasyMode()
ToggleEasyMode()
SetEasyModeEnabled(bool)
```

Helper script:

```text
OddClawEasyModeButton
```

Editor menu:

```text
Tools > Odd Claw Catch > Easy Mode Button > Attach Toggle Script To Selected Button
```

Usage:

1. User adds their own button to the ready scene.
2. Either wire the button `OnClick` directly to `OddClawCatchManager.ToggleEasyMode()`.
3. Or add `OddClawEasyModeButton` to the button.
4. Optional label can show:
   - `Easy: ON`
   - `Easy: OFF`

No layout regeneration required.

---

## 10. Question generator status

The manager uses one generator asset at a time.

Available generator types:

```text
OddClawMathQuestionGenerator
OddClawSpriteQuestionGenerator
OddClawEnglishQuestionGenerator
```

Generator asset creation:

```text
Project Window > Right Click > Create > Odd Claw Catch > Question Generators
```

or:

```text
Tools > Odd Claw Catch > Create Question Generator
```

Assign generator here:

```text
OddClawCatchManager > Question Generator
```

---

## 11. English word bank status

v7 added a 100-entry English synonym/antonym bank from the user-provided C# snippet.

Editor menus:

```text
Tools > Odd Claw Catch > English Word Bank > Create 100 Word English Generator Asset
Tools > Odd Claw Catch > English Word Bank > Replace Selected Generator With 100 Words
Tools > Odd Claw Catch > English Word Bank > Update Scene Manager Assigned Generator
Tools > Odd Claw Catch > English Word Bank > Create And Assign 100 Word English Generator To Scene Manager
```

Recommended for existing ready scene:

```text
Tools > Odd Claw Catch > English Word Bank > Update Scene Manager Assigned Generator
```

or select the English generator asset and run:

```text
Tools > Odd Claw Catch > English Word Bank > Replace Selected Generator With 100 Words
```

This does not rebuild the layout.

---

## 12. Bloom Reward System integration

Critical rules:

- Do not create Bloom UI.
- Do not create a custom reward panel to replace Bloom.
- Do not add RewardManager prefab to this scene.
- Do not instantiate RewardManager.
- RewardManager already exists in LoadingScene and persists through DontDestroyOnLoad.
- Access Bloom only through:

```csharp
RewardManager.Instance
```

Required namespaces:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;
```

`OddClawCatchManager` implements:

```csharp
IGameSceneCallbacks
```

Optionally:

```csharp
IGameAudioCallbacks
```

Required Bloom flow:

```text
Scene start
→ RewardManager.Instance.ShowPreGame(_skills)
→ Wait until RewardManager.Instance.IsPreGameComplete
→ Show local loading panel
→ Show How To Play panel
→ START clicked
→ Start gameplay
→ Game over
→ Show local result panel
→ Do NOT automatically open Bloom post-game
→ Only local CONTINUE opens Bloom post-game
→ Build GameEvaluationData
→ RewardManager.Instance.ShowPostGame(_skills, eval)
```

Skill list example in manager:

```csharp
private List<SkillEntry> _skills = new List<SkillEntry>
{
    new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.3f, accuracyWeight: 0.7f),
    new SkillEntry(BloomSkillType.Analyze, 75f, timeWeight: 0.5f, accuracyWeight: 0.5f),
};
```

Evaluation data:

```csharp
GameEvaluationData eval = new GameEvaluationData
{
    timeScore = timeScore,
    accuracyScore = accuracyScore,
    mistakeCount = mistakeCount,
    timeTaken = timeTaken
};
```

Rules:

```text
timeScore: normalized 0 to 1
accuracyScore: normalized 0 to 1
mistakeCount: raw
timeTaken: raw seconds
```

Callbacks:

```csharp
public void OnPlayAgain()
{
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

public void OnHome()
{
    SceneManager.LoadScene("Loader Scene");
}
```

Even though Home button is removed locally, `OnHome()` should remain for Bloom callback compatibility.

---

## 13. Scene builder status

Scene builder menu:

```text
Tools > Odd Claw Catch > Create Layout In Current Scene
```

Behavior:

- Creates layout inside currently active scene.
- Does not create a new Unity scene.
- If `OddClawCatch_Root` exists, it replaces only that generated root.
- Does not touch unrelated scene objects.

Current user has said scene is ready, so future developers should not use scene builder unless user asks.

---

## 14. Known risks / things to verify after import

Because Bloom Reward System is private to the user’s project, package could not be compile-tested against the exact Bloom assembly.

Verify in Unity Console:

```text
RewardSystem namespace exists
RewardManager.Instance exists
IGameSceneCallbacks method signatures match
IGameAudioCallbacks method signatures match if used
DOTween installed and setup
TextMeshPro installed/imported
```

If compile errors appear:

1. Fix Bloom interface signatures first.
2. Confirm DOTween namespace/imports.
3. Confirm TextMeshPro package.
4. Do not change gameplay architecture unless necessary.

---

## 15. User preferences and expectations

The user is a junior/fresher Unity developer with limited time.

They prefer:

- Production-level but simple setup.
- Inspector-driven fields.
- Low/medium script count.
- DOTween where useful.
- TextMeshPro.
- Editor menu tools.
- No over-engineering.
- Practical fixes that can be imported and used quickly.
- Designer-friendly scene behavior.
- No hidden hardcoded layout resets.
- Direct updated zip packages when requested.
- Script-only updates when their scene is already ready.

Tone/style for future help:

- Be direct.
- Take ownership of missed details.
- Do not only explain; implement when asked.
- Do not ask unnecessary clarification when intent is clear.
- Avoid regenerating or modifying layout unless requested.

---

## 16. Future-safe modification checklist

Before adding any feature, check:

```text
Will this require layout regeneration?
Can this be script-only?
Will it reset designer-tuned RectTransforms?
Can it be exposed in Inspector?
Can it be attached to an existing button/UI?
Does it preserve Bloom flow?
Does it avoid touching RewardManager prefab/UI?
Does it preserve miss-as-retry behavior?
Does it keep evaluation timing clean?
```

If yes to layout reset or hardcoded transform, redesign the change.

---

## 17. Suggested next possible improvements

Only if user asks:

1. Add a small runtime settings panel for:
   - Easy/Normal
   - Sound on/off
   - Vibration on/off
2. Add item rarity/skin support.
3. Add combo streak visual.
4. Add improved tutorial images.
5. Add generator mixer that randomly alternates Math/English/Sprite.
6. Add localization-ready question text.
7. Add object-specific catch socket presets.

Do not implement these unless requested.

---

## 18. Current final instruction from user

The user asked for a handoff file so any future AI/developer can continue from this stage without reading the full chat.

This file is that handoff. Treat it as the source of project context.
