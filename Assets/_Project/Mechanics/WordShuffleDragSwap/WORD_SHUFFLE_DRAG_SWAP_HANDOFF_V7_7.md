# Word Shuffle Drag Swap — Developer / AI Handoff File

**Current handoff version:** V7.7 Script-Only Bloom Start Flow  
**Mechanic name:** Word Shuffle Drag Swap  
**Target Unity project path used by current developer:**  
`Assets/_Project/Mechanics/WordShuffleDragSwap/`

This file is for any Unity developer or AI continuing the project from the current state.

---

## 1. Game Summary

This is an educational drag-and-swap unscramble mini-game.

The player sees a question/prompt and shuffled tiles.  
They drag one tile onto another tile to swap their positions.  
The goal is to arrange the tiles into the correct answer.

The system supports:

1. **English word mode**
2. **Math large-number mode**
3. **General question-answer mode**

The main gameplay is already accepted and should not be rewritten unless necessary.

---

## 2. Current Core Mechanic

### Main Interaction

- Tiles are displayed in one row.
- Each tile contains one character:
  - English mode: letters
  - Math mode: digits
  - General mode: letters or digits from answer
- Player drags one tile over another tile.
- On drop, the two tiles swap positions.
- After every valid swap, the manager checks whether the current tile order matches the answer.

### Correct Answer Flow

When the player solves the answer:

1. Input is locked.
2. Tiles animate.
3. Tiles turn **blue**.
4. Score is awarded.
5. Next question starts after a short delay.

### Timeout / Failed Question Flow

When timer expires:

1. Tiles shake.
2. Tiles turn **red** briefly.
3. Tiles move into correct positions.
4. Tiles turn **blue** to show the correct answer.
5. Corrected answer punch animation plays.
6. Short hold.
7. Next question starts.

### Hint Flow

Hints are now **per question**, not total per game.

Hint count is based on answer length:

| Answer Length | Hints Available |
|---:|---:|
| 3 | 1 |
| 4 | 2 |
| 5 | 3 |
| 6 | 4 |
| 7+ | 5 max |

Each hint:

- Moves one incorrect character into its correct slot.
- Locks that hinted tile.
- Locked hinted tile turns **green**.
- Locked hinted tile cannot be dragged.
- Locked hinted tile cannot be swapped.
- Each hint used reduces that question score by 1 point.

Scoring:

```text
Base score per question = 10
Final score for question = 10 - hintsUsedThisQuestion
Minimum score should stay clamped safely if changed later.
```

Instruction/message text is reused to show hint score feedback, for example:

```text
Hint used: this answer is now worth 9/10 (-1 per hint).
```

---

## 3. Important Color Meaning

Keep this consistent:

| Color | Meaning |
|---|---|
| Neutral / white | Normal tile |
| Green | Hint locked tile |
| Blue | Correct / solved / corrected final answer |
| Red | Timeout / failed / mistake state |

Do not add checkmark symbols or extra characters to tiles.  
The user’s font may not support symbols. Use color only.

---

## 4. Game Modes

The manager has a mode selection system.  
All modes must output the same internal round data:

```text
questionText
answer
optional image/audio in future
```

The drag-swap mechanic should not care where the question came from.

---

### 4.1 English Word Mode

Uses `WordShuffleWordDatabase`.

- Database contains Grade 3–4 friendly simple words.
- A random word is selected.
- Word is shuffled.
- Question text can be something like:
  - “Arrange the correct word”
  - or whatever is configured in the manager/builder.

---

### 4.2 Math Large Number Mode

No manual question data needed.

Generates number questions at runtime.

Example:

```text
Question: Three Thousand Four Hundred and Twenty-five
Answer: 3425
```

Player unscrambles:

```text
3 4 2 5
```

to make:

```text
3425
```

Math options implemented/planned in current manager:

- Number length min/max
- International number style
- Indian number style
- Mixed style
- British grammar with “and”
- American grammar without “and”

Current preferred default from user:

```text
Number style: Mixed Indian / International
Grammar: BritishAnd
```

British examples:

```text
150  = One Hundred and Fifty
1005 = One Thousand and Five
3425 = Three Thousand Four Hundred and Twenty-five
```

Digit repeat rule:

```text
For answer length n, no single digit should repeat more than n - 2 times.
```

Example:

```text
5 digit answer = same digit max 3 times
4 digit answer = same digit max 2 times
3 digit answer = same digit max 1 time
```

---

### 4.3 General Question Mode

Uses `WordShuffleQuestionDatabase`.

Each entry contains:

```text
questionText
answer
```

Examples:

```text
Question: Which animal says meow?
Answer: CAT
```

```text
Question: What is 12 + 8?
Answer: 20
```

This mode is intended for any subject where the developer/designer manually assigns the question and answer.

---

## 5. Current UI State

The UI has been heavily adjusted by the user manually.  
Future updates should avoid regenerating or overwriting the user’s edited UI unless asked.

### Important Current UI Principles

- Landscape 1920 x 1080 base.
- Responsive layout.
- No generated images.
- Background is intentionally empty so designer can add art later.
- Tile is based on a scene template, not a prefab asset.
- Top HUD is styled with larger mobile-readable controls.
- Mode badge was removed.
- Timer fill/bar was removed.
- Timer remains as a clean timer text/card.
- Hint button, score card, round progress, pause button exist in top HUD.
- Main cards are larger because game has less content.
- Dynamic tile sizing supports short and long answers.

---

## 6. Current HUD / Top Bar

Current top bar should contain:

- Round circular progress UI
- Score block
- Timer text/card only
- Hint button with hint count
- Pause button

Removed intentionally:

- Mode badge
- Timer fill
- Timer bar

Do not re-add those unless specifically requested.

---

## 7. Circular Round Progress

There is a circular round progress UI in the top bar.

Purpose:

- Shows current round visually.
- Keeps round info compact and premium.
- It is UI-only and should not affect gameplay.

If the developer changes round count logic, keep circular progress synced to:

```text
currentRound / roundsPerGame
```

---

## 8. Dynamic Tile Sizing

Current dynamic sizing behavior:

- Short answers should get larger tiles.
- Long answers should shrink to fit.
- Layout must not break for 13–14 character answers.
- Tile sizing is visual only and must not affect drag-swap indexing.

Expected behavior:

```text
CAT       -> large tiles
APPLE     -> large tiles
SCHOOL    -> medium-large tiles
ELEPHANT  -> medium tiles
13-14 chars -> smaller tiles, still fits
```

Core rule:

```text
availableWidth = slotParentWidth - safePadding
tileSize = calculated based on answerLength
tileSize = clamped between min and max
```

Also update:

- slot RectTransform size
- tile RectTransform size
- tile text font size
- spacing between tiles

Do not convert mechanic to free-placement.  
It should stay index-based drag-swap.

---

## 9. Scene Tile Template System

The tile is not meant to be a prefab asset anymore.

Scene builder creates a hidden scene template:

```text
GamePanel
└── SceneTemplates_DO_NOT_DELETE
    └── LetterTileSceneTemplate_DO_NOT_DELETE
```

Runtime tiles are cloned from this scene template.

Designer workflow:

1. Enable `SceneTemplates_DO_NOT_DELETE`.
2. Edit `LetterTileSceneTemplate_DO_NOT_DELETE` directly in scene.
3. Disable `SceneTemplates_DO_NOT_DELETE`.
4. Runtime uses the edited scene template.

This gives the designer direct control inside the scene.

---

## 10. Fonts

The game uses two fonts:

- Primary font
- Secondary font

Manager has global font slots:

```text
Primary Font
Secondary Font
Apply Global Fonts On Awake
```

Font assignment intention:

```text
Primary Font    -> body/question text
Secondary Font  -> title/top bar/buttons/feedback/tiles
```

There is also a manual context option:

```text
Apply Global Fonts Now
```

Do not add unsupported symbols like checkmarks into TMP text.

---

## 11. Audio / BGM

A background music slot has been added.

Current BGM-related fields:

```text
Background Music Source
Background Music
Background Music Volume
```

Expected behavior:

- BGM loops during gameplay/local game screens.
- BGM should stop when Bloom reward screen opens.
- This is handled through `IGameAudioCallbacks.OnRewardScreenOpen()`.

---

## 12. Bloom Reward System Integration

Bloom integration is currently working but with a corrected start flow in V7.7.

### Critical Rule

Do not place `RewardManager` in the game scene.

The RewardManager prefab already exists in the LoadingScene and persists with `DontDestroyOnLoad`.

Always access it through:

```csharp
RewardManager.Instance
```

---

### Required Namespaces

The manager should include:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;
```

---

### Interfaces

Only the game manager should implement these:

```csharp
IGameSceneCallbacks
IGameAudioCallbacks
```

Only one class in the scene should implement `IGameSceneCallbacks`.

---

### Bloom Skill List

A skill list is defined once in the manager and reused for pre and post Bloom panels.

Example:

```csharp
private List<SkillEntry> _skills = new List<SkillEntry>
{
    new SkillEntry(BloomSkillType.Remember, 100f),
    new SkillEntry(BloomSkillType.Understand, 50f),
};
```

Do not create different lists for pre and post.

---

## 13. Current Bloom Flow — V7.7

The latest requested flow:

```text
Scene starts
↓
Bloom pre-game panel appears immediately
↓
Wait until RewardManager.Instance.IsPreGameComplete
↓
Local Start panel appears
↓
Player clicks Start
↓
How To Play overlay appears
↓
Player continues
↓
Gameplay starts
```

Important:

- Bloom pre-game should not wait for local Start button.
- Pressing local Start should not open Bloom pre-game again.
- Timer/controls/gameplay must not start before Bloom pre-game is complete.

---

## 14. Result / Bloom Post-Game Flow

At game end:

1. Local result panel appears.
2. Result panel has a Continue button.
3. Continue button opens Bloom post-game panel.
4. Post-game uses same `_skills` list.
5. Evaluation data is passed to Bloom.

Current intended result data:

```csharp
GameEvaluationData eval = new GameEvaluationData
{
    timeScore = timeScore,             // normalized 0 to 1
    accuracyScore = accuracyScore,     // normalized 0 to 1
    mistakeCount = mistakeCount,       // raw count
    timeTaken = timeTaken              // raw seconds
};
```

Accuracy example:

```csharp
accuracyScore = totalQuestions > 0
    ? Mathf.Clamp01((float)correctCount / totalQuestions)
    : 0f;
```

Time score example:

```csharp
timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));
```

---

## 15. Bloom Callbacks

The manager should implement:

```csharp
public void OnPlayAgain()
{
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

```csharp
public void OnHome()
{
    SceneManager.LoadScene("Loader Scene");
}
```

Optional Android mediator call may be added later if project uses it:

```csharp
// UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
```

For audio:

```csharp
public void OnRewardScreenOpen()
{
    // Stop BGM/audio here.
}
```

---

## 16. Overlay Panel Structure

Overlay panels should use professional hierarchy:

```text
StartOverlayPanel
└── StartMainCard

PauseOverlayPanel
└── PauseMainCard

HowToPlayOverlayPanel
└── HowToPlayMainCard

ResultOverlayPanel
└── ResultMainCard
```

Avoid flat messy overlay structures in future updates.

Pause menu should contain:

- Resume
- How To Play
- Restart
- Home/Quit if needed

How To Play can be opened from pause without restarting gameplay.

---

## 17. Start / HTP Flow

Local UI flow after Bloom pre-game:

```text
Start panel visible
↓
Player clicks Start
↓
How To Play panel opens by default
↓
Player clicks Continue
↓
Gameplay starts
```

Pause HTP flow:

```text
Pause opened
↓
Click How To Play
↓
HTP opens
↓
Closing HTP returns to pause/game without restarting
```

---

## 18. Scoring Rules

Current scoring:

```text
Base score per question = 10
Hint penalty = -1 per used hint in that question
Timeout/failure = 0 for that question
Correct without hint = 10
Correct with 1 hint = 9
Correct with 2 hints = 8
```

Hints reset every question based on answer length.

Track at least:

```text
currentScore
correctCount
mistakeCount
hintsUsedThisQuestion
roundScoreAvailable
```

For Bloom accuracy:

- Correct solved questions count as correct.
- Timeout should count as mistake.
- Wrong/incomplete timeout should affect `mistakeCount`.

---

## 19. Timer

The game has a per-question timer.

Current UI:

- Text timer only.
- No timer fill.
- No timer bar.

Timer should pause when:

- Pause panel is open.
- Game is not active.
- Bloom pre-game is active.
- Bloom post-game is active.
- Before actual gameplay starts.

Timer can continue during hint animation unless specifically changed later.

---

## 20. Current Script List

Expected folder:

```text
Assets/_Project/Mechanics/WordShuffleDragSwap/
```

Scripts:

```text
Scripts/
├── WordShuffleDragSwapManager.cs
├── WordShuffleLetterTile.cs
├── WordShuffleWordDatabase.cs
└── WordShuffleQuestionDatabase.cs
```

Editor:

```text
Editor/
└── WordShuffleSceneBuilder.cs
```

Possible helper scripts/classes depending on latest package:

```text
Circular progress UI helper / mesh component
```

Do not add many scripts unless there is a clear reason.  
The user prefers low script count and dedicated mechanic-specific names.

---

## 21. Main Manager Responsibilities

`WordShuffleDragSwapManager.cs` currently owns:

- Game mode selection
- Round generation
- English word selection
- Math number generation
- General question selection
- Timer
- Score
- Hints
- Tile spawn/despawn
- Tile order tracking
- Correct check
- Timeout reveal
- Result panel
- Bloom pre/post integration
- BGM
- Pause / HTP / start flow
- Font application
- Dynamic tile sizing

This is acceptable for this mini-game because the user wants low script count and fast delivery.

---

## 22. Tile Script Responsibilities

`WordShuffleLetterTile.cs` should handle only tile-level behavior:

- Begin drag
- Drag
- End drag/drop
- Locked state
- Basic visual state hooks
- Pointer blocking if locked
- Calling back into manager for swaps

It should not own scoring, questions, timer, Bloom, or round flow.

---

## 23. Databases

### Word Database

`WordShuffleWordDatabase.cs`

Holds a list of English words.

Used by English mode.

### General Question Database

`WordShuffleQuestionDatabase.cs`

Holds question-answer pairs.

Used by General mode.

Fields should stay simple and inspector-editable.

---

## 24. Scene Builder

`WordShuffleSceneBuilder.cs` creates rough working UI.

Important note:

The user has already manually worked on UI.  
Future script-only patches should not force them to regenerate the scene unless needed.

Builder menu names from previous versions included:

```text
Tools > Word Shuffle Drag Swap > Create Complete Scene - V7.5 Bloom Minimal HUD
Tools > Word Shuffle Drag Swap > Create Complete Scene - V7.4 Dynamic Tile Sizing
Tools > Word Shuffle Drag Swap > Create Complete Scene - V7.3 Larger Responsive UI
```

Current user likely uses their manually edited scene plus patched manager script.

If adding UI in builder later, do not overwrite manual scene unless user asks.

---

## 25. Current Latest Patch Chain

Important version history:

```text
V6.2  - Professional overlays and HTP before gameplay
V7    - Landscape UI layout with circular progress
V7.1  - Hint button visual update
V7.2  - Full HUD style update
V7.3  - Larger responsive UI
V7.4  - Dynamic tile sizing
V7.4.1 - Compile fix for fittedWidth variable scope
V7.5  - Minimal HUD cleanup + Bloom + BGM
V7.6  - Script-only per-question hints + hint score penalty
V7.7  - Script-only Bloom pre-panel appears immediately at scene start
```

Latest code state should include V7.7 manager on top of prior current project.

---

## 26. Known Recently Fixed Error

Error:

```text
CS0136: A local or parameter named 'fittedWidth' cannot be declared in this scope
```

Fix:

```text
Renamed fallback variable to legacyFittedWidth.
```

Do not reintroduce duplicate local variable names inside nested scopes.

---

## 27. Important User Preferences

The user is a junior/fresher Unity developer with limited time.

They prefer:

- Practical guidance
- Short direct setup instructions
- Reusable mini-game mechanics
- Inspector-driven settings
- TextMeshPro
- DOTween
- Low script count
- Editor menu scene generation
- Scene template over prefab when they need direct UI styling
- No unnecessary rewrites
- No changes to working gameplay while doing UI/script-only updates
- Dedicated script names, not generic managers

Avoid:

- Over-engineering
- Huge architecture changes
- Rebuilding UI when user asks for script-only
- Adding unsupported symbols to fonts
- Adding generated images/assets
- Changing mechanics while doing UI changes

---

## 28. Safe Future Update Rules

Before changing anything, classify update as one of:

```text
Script-only logic patch
UI/layout-only patch
Scene builder patch
Mechanic patch
Bloom integration patch
```

Then only touch required files.

If user says:

```text
script only
```

Only touch manager/tile scripts.  
Do not alter scene builder or UI hierarchy.

If user says:

```text
UI only
```

Do not alter scoring, hints, Bloom, timer, round flow, or gameplay.

If user says:

```text
main gameplay is fine
```

Do not rewrite drag-swap or tile logic.

---

## 29. Suggested Next Improvements

Only do these if asked:

1. Better result stats:
   - score
   - correct
   - timeout/mistake count
   - hints used
   - accuracy
2. Optional per-mode HTP text
3. Difficulty presets:
   - Easy: short words, more time
   - Normal: medium words
   - Hard: longer words/numbers
4. Optional audio per event:
   - tile pick
   - swap
   - hint
   - correct
   - timeout
5. Better Bloom skill weighting per mode:
   - English: Remember + Understand
   - Math number: Understand + Apply
   - General: configurable

---

## 30. Quick QA Checklist

Before delivering any update:

- Game compiles without console errors.
- Bloom pre-panel appears immediately on scene start.
- Local Start panel appears only after Bloom pre-panel is complete.
- Start opens HTP before gameplay.
- Timer starts only when gameplay starts.
- Drag-swap works.
- Hint count resets per question.
- Hint count follows answer length.
- Hint locked tiles turn green and cannot be dragged/swapped.
- Hint reduces that question’s score.
- Correct player solve turns tiles blue.
- Timeout flow: red shake -> arrange -> blue reveal -> next.
- Result Continue opens Bloom post-game.
- BGM stops when Bloom reward screen opens.
- No mode badge in HUD.
- No timer fill/bar in HUD.
- Dynamic tile sizing works for short and long answers.
- Long answers do not go off-screen.

---

## 31. Final Advice for Next Developer / AI

The game is already in a good playable state.

Do not rebuild the whole system.  
Continue with small controlled patches.

Most important rule:

```text
Question source can change.
UI can change.
But drag-swap index-based mechanic should remain stable.
```

When adding features, preserve the internal round data pattern:

```text
RoundData
{
    questionText
    answer
}
```

Then the same mechanic can continue supporting English, math, and general subjects.
