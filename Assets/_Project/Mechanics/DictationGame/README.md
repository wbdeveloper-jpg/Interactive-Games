# DictationGame — Unity Package

## Folder Structure

```
DictationGame/
├── Scripts/
│   ├── Core/
│   │   ├── DictationRoundData.cs       ← ScriptableObject — one question
│   │   ├── DictationQuestionSet.cs     ← ScriptableObject — full question bank + session config
│   │   ├── DictationAudioManager.cs    ← Playback + replay counter
│   │   ├── DictationHintSystem.cs      ← 3-tier auto-generated hints
│   │   ├── DictationKeyboard.cs        ← Custom QWERTY, clones in-scene template
│   │   └── DictationGameManager.cs     ← Core loop, scoring, fuzzy match
│   └── Editor/
│       └── DictationSceneSetup.cs      ← Tools > Dictation Game > Create Full Scene
└── README.md
```

---

## Requirements — Install First

- **TextMeshPro** → Window > TextMeshPro > Import TMP Essentials
- **DOTween (free)** → Asset Store → import → Tools > DOTween Utility Panel > Setup

---

## Step 1 — Place Folder

Drop `DictationGame/` into:
```
Assets/MiniGames/DictationGame/
```

---

## Step 2 — Auto-Create Scene

```
Tools > Dictation Game > Create Full Scene
```

This builds the full Canvas, all panels, all managers, wires every Inspector reference.
The `KeyTemplate` is created **directly in the scene** (deactivated under KeyboardArea) — no prefab folder.

---

## Step 3 — Style the Key Template (in-scene)

In Hierarchy: `DictationCanvas > KeyboardArea > KeyTemplate`

- Change the `Image` color, sprite, size here
- Change the `Label` TMP font, size, color here
- The keyboard clones this GameObject at runtime for every key
- Special keys (Backspace, Space) use a slightly different color set via Inspector on `DictationKeyboard`

---

## Step 4 — Create Questions

**One question = one ScriptableObject:**
```
Right-click Project > Create > DictationGame > Round Data
```
Fill in:
- `Round Title` — shown in top bar
- `Difficulty` — Easy / Medium / Hard
- `Audio Clip` — drag your .mp3 / .wav
- `Answer Sentence` — the exact text to match (hints auto-generate from this)

---

## Step 5 — Create a Question Set

```
Right-click Project > Create > DictationGame > Question Set
```

Inspector fields:

| Field | Purpose |
|---|---|
| `All Questions` | Drag all your Round Data SOs here |
| `Total Rounds Per Session` | How many questions to play per session |
| `Easy Count` | Target easy questions to pick |
| `Medium Count` | Target medium questions to pick |
| `Hard Count` | Target hard questions to pick |

**Session building rules:**
- Picks from each difficulty bucket up to target count
- If a bucket runs short → fills remaining slots from other difficulties
- If total bank < requested → uses whatever is available
- Always shuffled fresh each session — order never repeats

---

## Step 6 — Assign Question Set

Select `DictationGameManager` in Hierarchy.
Drag your `QuestionSet` SO into the `Question Set` field. That's it — no individual round assignment needed.

---

## Integration with Parent Platform

Two static events you can subscribe to anywhere:

```csharp
// fires after each round ends
DictationGameManager.OnRoundComplete += (int roundScore) => { };

// fires when all rounds in the session are done
DictationGameManager.OnSessionComplete += (int totalScore) => { };
```

The **Continue →** button on the last round auto-changes label to **Finish →** and fires `OnSessionComplete`.
Wire that to your platform's scene loader or module controller.

---

## Scoring Reference

| Event | Deduction |
|---|---|
| Each replay used | -5 pts |
| Hint 1 | -5 pts |
| Hint 2 | -10 pts |
| Hint 3 | -15 pts |
| Wrong attempt | -10 pts |
| Close enough (1–2 char typo) | -5 pts |
| 3 failed attempts | Round score = 0 |

All values editable from DictationGameManager Inspector.

---

## Assets You Need

| Asset | Notes |
|---|---|
| Audio clips (.mp3/.wav) | One per question — spoken sentences |
| Background image | Assign to `Background` Image component |
| Replay icon images | 2 small images, assign to ReplayIcon_1 / ReplayIcon_2, or just leave as colored squares |

No other assets required to get a working demo.
