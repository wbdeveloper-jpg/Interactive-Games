# Sentence Word Search - Project Handoff

## Purpose of This File

This file is a continuation handoff for the **Sentence Word Search** Unity mini-game mechanic.

Assume the previous ChatGPT conversation is lost. Continue development from this file.

The user is a junior/fresher Unity developer under time pressure. Give practical, production-friendly guidance. Keep explanations short and useful. Prefer reusable mini-game code, inspector-driven settings, and simple setup.

---

# 1. Current Game Mechanic

## Mechanic Name

`SentenceWordSearch`

## Pattern

This is a hybrid mechanic:

**Sentence Completion + Word Search Drag Selection**

Player sees a sentence with a blank, for example:

> The wind is _________.

The correct answer word is hidden inside a fixed alphabet grid. The player drags across letters to select the word.

---

# 2. Core Gameplay Rules

## Main Rule

The grid is **one fixed board** for the whole round.

Do **not** regenerate the alphabet grid every question.

The board should contain all selected answer words for the current round.

Example:

- Question 1 answer: `STRONG`
- Question 2 answer: `BRIGHT`
- Question 3 answer: `COLD`
- Question 4 answer: `KITTEN`
- Question 5 answer: `WATER`

All of these words are placed into the same board before gameplay starts.

For each question, the player must select only the current question answer.

---

# 3. Latest Version State

## Latest Working Direction

The project reached:

`v2.6 SCRIPT ONLY BOARD SETTINGS PATCH`

This patch came after:

- v2.3 review fixes
- v2.4 prefab-ready cell system
- v2.5 UI refinement layout
- v2.6 script-only patch to expose row/column settings on Manager again

## Important

The user has already started/refined UI design.

From this point onward:

**Do not regenerate the scene unless the user explicitly asks.**

Use script-only replacement patches whenever possible.

---

# 4. Current Import / Replacement Rule

If changing code now:

Replace only:

```text
Assets/_Project/Mechanics/SentenceWordSearch/Scripts/Runtime
```

Do not delete designed Canvas/UI.

Do not run scene builder again unless requested.

Do not replace user-refined prefabs unless the user asks.

---

# 5. Project Folder Structure

Expected structure:

```text
Assets/
└── _Project/
    └── Mechanics/
        └── SentenceWordSearch/
            ├── Prefabs/
            │   └── SentenceWordSearchCell.prefab
            ├── Scripts/
            │   ├── Runtime/
            │   │   ├── SentenceWordSearchManager.cs
            │   │   ├── SentenceWordSearchBoard.cs
            │   │   ├── SentenceWordSearchInputController.cs
            │   │   ├── SentenceWordSearchCell.cs
            │   │   ├── SentenceWordSearchUI.cs
            │   │   ├── SentenceWordSearchAudio.cs
            │   │   └── SentenceWordSearchQuestion.cs
            │   └── Editor/
            │       └── SentenceWordSearchSceneBuilder.cs
            └── Demo/
```

---

# 6. Dependencies

## Required

- Unity UI
- TextMeshPro
- DOTween

DOTween is mandatory. Do not use scripting define symbol workaround.

Use:

```csharp
using DG.Tweening;
```

directly where needed.

---

# 7. Current Architecture

## SentenceWordSearchManager

Main coordinator.

Responsibilities:

- Start game
- Select random questions from question bank
- Sync manager board settings into board
- Control question flow
- Validate selected word
- Score logic
- Timer
- Correct/incorrect flow
- Pause/resume
- Result handling

Important v2.6 note:

Board settings should be exposed on Manager again for convenience.

Expected Manager inspector section:

```text
Board Settings - Edit Here Or On Board
- Use Manager Board Settings
- Rows
- Columns
- Grid Padding
- Grid Spacing
- Difficulty
- Filler Alphabet
```

If `Use Manager Board Settings` is true, Manager should apply these values to `SentenceWordSearchBoard`.

---

## SentenceWordSearchBoard

Responsible for:

- Fixed grid generation
- Placing all selected answer words before gameplay starts
- Auto-fitting cell size inside fixed parent area
- Difficulty-based word direction placement
- Finding a cell at screen position
- Returning first/last cell of current answer for hint system

Must contain or support:

```csharp
FindCellAtScreenPosition(Vector2 screenPosition)
```

Reason: drag detection needs to work reliably even when UI cell raycasts are imperfect.

---

## SentenceWordSearchInputController

Responsible for:

- Pointer down
- Drag across cells
- Pointer up
- Building a straight path
- Showing preview overlay while dragging
- Sending selected word to Manager

Drag should work by checking screen position against board cells, not only `PointerEnter`.

Must support:

- Horizontal selection
- Vertical selection
- Diagonal selection
- Reverse selection if difficulty/setting allows

---

## SentenceWordSearchCell

Responsible for visual state of one alphabet cell.

The prefab should have this structure:

```text
SentenceWordSearchCell
├── SolvedOverlay
├── PreviewOverlay
├── HintRing
└── LetterText
```

Expected public methods include:

```csharp
Setup(...)
SetPreview(bool active)
SetSolved(bool active)
SetHint(bool active)
SetWrongFlash()
SetNormal()
```

Important:

- Same alphabet cell can be part of multiple hidden words.
- Correct solved overlay can stay on previously solved word.
- If a cell is reused in another word, it should still work.
- Preview overlay should be temporary while dragging.
- Solved overlay should remain after correct selection.

---

## SentenceWordSearchUI

Responsible for:

- Title / header
- Score text
- Timer text
- Question counter text
- Instruction line
- Sentence display
- Blank fill animation
- Score popup
- Panels: pause, result, how-to-play
- Applying primary and secondary fonts

Must support:

- Correct word flying/popping into sentence blank
- Sentence highlight/pulse while narration is playing
- If narration audio is missing, still do text pulse/read animation
- `+10` popup for correct
- `-1` popup for wrong, but score never below zero

---

## SentenceWordSearchAudio

Responsible for:

- BG music
- Correct SFX
- Wrong SFX
- Complete SFX
- Per-question narration audio

Must support:

- BG music slot
- Narration should play after correct word reaches/fills sentence blank
- Sentence pulse/highlight should continue while narration plays
- If narration clip is null, play text-only animation timing

---

## SentenceWordSearchQuestion

Question data model.

Expected fields:

```csharp
string sentenceWithBlank;
string answer;
Sprite questionSprite;
AudioClip narrationAudio;
```

Question bank should be editable from Inspector.

---

# 8. Question Selection Rule

Do not always take first 5 questions.

Use random selection from question bank:

```text
Question Bank -> randomly choose N questions -> build one board with selected answer words
```

Inspector should allow:

```text
Max Questions / Question Count
```

If question bank has fewer than requested, use available count safely.

---

# 9. Board Generation Rules

The board is fixed for selected questions.

## Difficulty Levels

A simple enum is acceptable:

```csharp
public enum SentenceWordSearchDifficulty
{
    Easy,
    Medium,
    Hard
}
```

Expected behavior:

```text
Easy:
- horizontal
- vertical

Medium:
- horizontal
- vertical
- diagonal

Hard:
- horizontal
- vertical
- diagonal
- reverse directions
```

Important:

Words should be randomized in direction and position.

If placement fails, retry.

If still failing, give warning and recommend larger row/column count.

---

# 10. Auto-Fit Grid Requirement

The alphabet grid parent has a fixed UI size.

The grid must not overflow the parent.

If rows/columns increase, cells should become smaller automatically.

Board should calculate cell size based on:

```text
Grid Parent Rect Size
Rows
Columns
Padding
Spacing
```

Then apply to GridLayoutGroup:

```text
constraint = FixedColumnCount
constraintCount = columns
cellSize = calculated size
spacing = grid spacing
padding = grid padding
```

---

# 11. UI Layout Requirement

The user liked the v2.5 layout.

Do not change layout structure unless asked.

## Header Row

- Pause button on left side
- Game title centered
- How To Play square button on right side

## Top Info Row

Should contain:

```text
Score | Instruction Line | Timer | Hint Button
```

Each info text should have a background card/panel.

Hint button should be in top info row.

## Question Area

Question count should not be in top info row.

Question count should be before/near the sentence.

Either:

```text
Question 1/5
The wind is _________.
```

or a dedicated `QuestionCounterText` above the sentence.

## Restart

Do not show restart button in gameplay bottom area.

Restart should exist inside:

- Pause panel
- Result panel

---

# 12. Cell Prefab Design Workflow

The user needs to refine UI manually.

There must be a real prefab asset:

```text
Assets/_Project/Mechanics/SentenceWordSearch/Prefabs/SentenceWordSearchCell.prefab
```

Designer should edit:

```text
Cell background
LetterText font / color / size
PreviewOverlay color
SolvedOverlay color
HintRing style
Button visual style
```

Do not rely only on hidden scene templates.

---

# 13. Fonts

The game should support two universal fonts:

```text
Primary Font
Secondary Font
```

Usage:

## Primary Font

Use for:

- Title
- Score
- Timer
- Progress/question count
- Buttons
- Score popup

## Secondary Font

Use for:

- Sentence
- Instruction line
- How-to-play text
- Description text

Fonts should be assignable from Inspector, likely in `SentenceWordSearchUI`.

---

# 14. Correct Answer Flow

When user selects correct word:

1. Show selected cells as correct/solved.
2. Show `+10` score popup near selected word.
3. Animate word popping/flying into sentence blank.
4. Fill the sentence blank with the answer.
5. Play narration audio if assigned.
6. While narration plays, sentence text should pulse/color highlight.
7. If no narration audio, still do short text pulse/read animation.
8. Then move to next question.

Important:

Do not switch to next question immediately after correct selection.

---

# 15. Wrong Answer Flow

When user selects wrong word:

1. Show wrong visual feedback briefly.
2. Show `-1` popup.
3. Reduce score by 1 only if score is above zero.
4. Score must never become negative.
5. Clear preview/wrong overlay.
6. Let player try again.

Do not use old generic "Correct!" feedback text.

---

# 16. Hint System

Simple hint system requested.

When user taps Hint:

- Pulse first alphabet cell of current answer
- Pulse last alphabet cell of current answer

This means Board must store placed word metadata:

```text
answer word
start cell
end cell
direction
path cells
```

Hint should not solve the word.

Hint should visually pulse the first and last letter.

Use DOTween pulse on `HintRing` or cell scale.

---

# 17. Scoring

Expected scoring:

```text
Correct selection: +10
Wrong selection: -1 popup
Actual score cannot go below 0
```

Score popup should appear visually near selection or center board area.

---

# 18. Audio

Slots expected:

```text
BG Music
Correct SFX
Wrong SFX
Complete SFX
Per-question narration audio
```

BG music should loop.

Narration audio should not start before word fill animation completes.

---

# 19. Current UI Design Theme

The user is refining UI based on this design direction:

```text
Soft pastel minimalistic theme with red as main accent color.
```

Suggested palette:

```text
Muted coral red
Soft blush pink
Warm off-white / cream
Pale peach
Dusty rose
Light beige
Warm dark gray text
Soft green for solved/correct state
Pale peach/yellow for selection state
```

Keep UI clean, minimal, child-friendly, and readable.

---

# 20. Important User Preference

The user wants:

- Fast practical output
- Reusable mini-game mechanics
- Production-level Unity C# code
- Simple setup
- Inspector-driven settings
- Editor tools when useful
- Dedicated manager names per mechanic
- TextMeshPro
- DOTween
- Low script count but not forced into one giant script
- No over-explaining
- Zip/script patches when possible

The user respects senior design decisions, but expects working code.

Avoid giving huge code before understanding the issue.

When UI is already designed, prefer script-only patches.

---

# 21. Known Mistakes From Previous Iterations

Avoid repeating these:

1. Do not regenerate board every question.
2. Do not hide row/column settings only inside Board component.
3. Do not remove prefab workflow.
4. Do not force everything into one script.
5. Do not switch to next question before narration/read animation finishes.
6. Do not always select first 5 questions.
7. Do not put restart in gameplay bottom area.
8. Do not remove hint button from top info row.
9. Do not require user to rerun scene builder after UI refinement.
10. Do not provide a package without checking method names match across scripts.

---

# 22. If Continuing Development

Start by asking the user what exact issue they are facing now.

If they report compile errors:

- Provide script-only replacement patch if UI is already done.
- Do not regenerate the scene.
- Keep public serialized fields backward-compatible if possible.
- Avoid renaming serialized fields unless necessary.

If they request a new version:

- Mention exactly what to replace.
- Say whether scene builder must be rerun or not.
- If script-only, clearly say:
  - replace Runtime scripts only
  - do not delete Canvas
  - do not replace prefab

---

# 23. Latest Expected Menu Item

If full scene generation is ever needed again, the editor tool menu should be something like:

```text
Mini Games > Sentence Word Search > Create V2.5 UI Refine Ready Scene
```

But after UI refinement, avoid using it.

---

# 24. Recommended Next Development Step

If continuing from current state, the most useful next tasks are:

1. Verify v2.6 runtime scripts compile.
2. Confirm Manager inspector shows row/column board settings.
3. Confirm existing UI references are still assigned.
4. Confirm designed cell prefab still works.
5. Test:
   - drag selection
   - correct flow
   - wrong flow
   - hint
   - narration timing
   - score popup
   - fixed board
   - random questions

---

# 25. Short Summary for Any AI

This is a Unity mini-game mechanic called `SentenceWordSearch`.

It is a fixed-board sentence-completion word search game. Randomly select N questions from a question bank, place all answers into one board, and ask the player to drag-select the answer for the current sentence. Keep UI layout from v2.5. The user already refined UI, so do not regenerate the scene. Continue with script-only patches unless explicitly asked. DOTween and TextMeshPro are required. The alphabet cell must be a real editable prefab. Row/column settings must be visible on the Manager. Correct flow must wait for `+10 popup -> word fly to blank -> sentence fill -> narration/text pulse -> next question`.

