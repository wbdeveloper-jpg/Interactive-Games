# Reward System — Complete Setup Guide

## Scripts Overview

| Script | Type | Purpose |
|--------|------|---------|
| `BloomSkillData.cs` | ScriptableObject | Static data per Bloom skill |
| `RewardDataModels.cs` | Plain C# | Data classes: SkillEntry, GameEvaluationData, SkillResult |
| `IGameSceneCallbacks.cs` | Interface | Contract for Play Again / Home in each game scene |
| `ScoreCalculator.cs` | Static Utility | Pure score calculation logic |
| `BloomCardPre.cs` | MonoBehaviour | Pre-game card UI + animation |
| `BloomCardPost.cs` | MonoBehaviour | Post-game card UI + medal + stars animation |
| `PreGamePanel.cs` | MonoBehaviour | Panel 1 controller with countdown |
| `PostGamePanel.cs` | MonoBehaviour | Panel 2 controller with reveal sequence |
| `InfoPanel.cs` | MonoBehaviour | Panel 3 with modal system |
| `InfoSkillButton.cs` | MonoBehaviour | Skill selector button in Info Panel |
| `RewardManager.cs` | Singleton MB | Central controller — use this from game scenes |
| `LoadingSceneManager.cs` | MonoBehaviour | Entry point / scene routing |
| `ExampleGameManager.cs` | Template | Copy this into each game scene |

---

## Step 1 — Create ScriptableObject Assets

1. In Project window, right-click → **Create → RewardSystem → BloomSkillData**
2. Create **6 assets**, one per skill:
   - `BloomSkill_Remember`
   - `BloomSkill_Understand`
   - `BloomSkill_Apply`
   - `BloomSkill_Analyze`
   - `BloomSkill_Evaluate`
   - `BloomSkill_Create`
3. For each asset fill in: `skillType`, `skillName`, `icon` (Sprite), `definition`, `details`

---

## Step 2 — Build Prefabs

### BloomCardPre Prefab
```
BloomCardPre (GameObject)
  ├── Add BloomCardPre.cs
  ├── Add CanvasGroup
  ├── Icon (UI Image)
  └── SkillName (TextMeshProUGUI)
```
Assign icon and skillNameText references in inspector.

### BloomCardPost Prefab
```
BloomCardPost (GameObject)
  ├── Add BloomCardPost.cs
  ├── Background (Image) — medal bg swapped at runtime
  ├── Icon (Image)
  ├── SkillName (TextMeshProUGUI)
  ├── ScoreText (TextMeshProUGUI)
  └── StarsContainer
        ├── Star1
        │     ├── StarOuter (Image — always visible, dim)
        │     └── StarInner (Image — glowing, shown by script)
        ├── Star2 (same)
        └── Star3 (same)
```
Assign all references in BloomCardPost inspector.

### InfoSkillButton Prefab
```
InfoSkillButton (Button + InfoSkillButton.cs)
  ├── Icon (Image)
  └── Name (TextMeshProUGUI)
```

---

## Step 3 — Build RewardManager Canvas Hierarchy

```
RewardManager (GameObject — DontDestroyOnLoad)
  ├── Add RewardManager.cs
  └── Canvas (Screen Space - Camera, SortOrder 999, Scale with Screen Size)
        ├── Panel_PreGame (Add PreGamePanel.cs + CanvasGroup)
        │     ├── Heading (TextMeshProUGUI)
        │     ├── CardHolder (HorizontalLayoutGroup or GridLayout)
        │     └── BtnInfo (Button — eye icon)
        │
        ├── Panel_PostGame (Add PostGamePanel.cs + CanvasGroup)
        │     ├── Heading (TextMeshProUGUI)
        │     ├── CardHolder (HorizontalLayoutGroup or GridLayout)
        │     ├── BtnPlayAgain (Button)
        │     ├── BtnHome (Button)
        │     └── BtnInfo (Button — eye icon)
        │
        └── Panel_Info (Add InfoPanel.cs + CanvasGroup)
              ├── SkillButtonHolder (HorizontalLayoutGroup)
              ├── BtnClose / BtnGotIt (Button)
              └── Modal_SkillDetail (GameObject)
                    ├── ModalIcon (Image)
                    ├── ModalHeading (TextMeshProUGUI)
                    ├── ModalDefinition (TextMeshProUGUI)
                    ├── ModalDetails (TextMeshProUGUI)
                    └── BtnModalGotIt (Button)
```

---

## Step 4 — Inspector Assignments on RewardManager

| Field | Assign |
|-------|--------|
| allSkillData (list) | All 6 BloomSkillData assets |
| preGamePanel | Panel_PreGame |
| postGamePanel | Panel_PostGame |
| infoPanel | Panel_Info |
| mainCanvas | The Canvas child |
| timeWeight | 0.4 (default) |
| accuracyWeight | 0.6 (default) |
| silverThreshold | 0.4 (default) |
| goldThreshold | 0.7 (default) |

On **PostGamePanel** inspector:
- bronzeSprite, silverSprite, goldSprite — assign your medal background sprites

---

## Step 5 — Loading Scene Setup

1. Create a scene named `LoadingScene` (or your name)
2. Place the **RewardManager prefab** in this scene
3. Add `LoadingSceneManager.cs` to a GameObject
4. Assign `rewardManagerPrefab` as fallback
5. Set `defaultScene` to your first game scene name for editor testing
6. Make `LoadingScene` **Build Index 0**

---

## Step 6 — Each Game Scene Setup

1. Copy `ExampleGameManager.cs`, rename it to `YourGameManager.cs`
2. Implement `IGameSceneCallbacks`:
   ```csharp
   public class ColorFillingManager : MonoBehaviour, IGameSceneCallbacks
   {
       public void OnPlayAgain() => SceneManager.LoadScene(...);
       public void OnHome()      => SceneManager.LoadScene("LoadingScene");
   }
   ```
3. Define skills for this game:
   ```csharp
   private List<SkillEntry> _skills = new()
   {
       new SkillEntry(BloomSkillType.Remember,   100f),
       new SkillEntry(BloomSkillType.Understand,  50f),
   };
   ```
4. Call on Start:
   ```csharp
   RewardManager.Instance.ShowPreGame(_skills);
   ```
5. Call on game over:
   ```csharp
   RewardManager.Instance.ShowPostGame(_skills, evalData);
   ```

---

## Score Calculation Reference

```
combinedScore    = (timeScore × timeWeight) + (accuracyScore × accuracyWeight)
finalScore       = combinedScore × skill.maxScore
normalizedScore  = finalScore / skill.maxScore   // always 0.0 – 1.0

Stars:  >= 0.75 → 3 stars | >= 0.45 → 2 stars | else → 1 star
Medal:  >= goldThreshold → Gold | >= silverThreshold → Silver | else → Bronze
```

All thresholds and weights are inspector variables on RewardManager — tune without code changes.

---

## What Each Game Scene MUST Provide

| Requirement | Where |
|-------------|-------|
| Implement `IGameSceneCallbacks` | On any MonoBehaviour in the scene |
| Call `ShowPreGame(skills)` on Start | Your game manager |
| Call `ShowPostGame(skills, eval)` on game over | Your game manager |
| Pre-calculate `timeScore` (0-1) | Your game logic |
| Pre-calculate `accuracyScore` (0-1) | Your game logic |

That is all. The reward system handles everything else internally.
