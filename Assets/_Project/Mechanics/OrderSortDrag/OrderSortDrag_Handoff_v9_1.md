# Order Sort Drag Mini-Game - Handoff Document

Use this document to continue development if the original chat is lost. Attach this file plus the latest code package to any AI/developer.

Latest package to continue from:
- OrderSortDrag_MiniGame_v9_1_CompileFix.zip

Latest known compile fix:
- v9 had CS0103 because currentScorePopupIndex was used but not declared.
- v9.1 fixed it by adding: private int currentScorePopupIndex; inside OrderSortDragManager.

## User working style

The user is a junior/fresher Unity developer under time pressure. Keep guidance practical and short.
They prefer reusable mini-game mechanics, inspector-driven settings, TextMeshPro, DOTween where useful, and editor menu tools that auto-create a rough scene.
Do not over-engineer. Do not create prefabs for this mechanic. Use scene templates generated inside the scene.
The user manually replaces sprites/UI art after the rough layout works.

## Game overview

Game name: Order Sort
Mechanic pattern: drag-and-drop ordering / sequencing game.

Player drags shuffled cards from a basket/source area into ordered slots, then presses Check.
System checks each slot, shows overlays, updates score, and shows result panel when finished.

Supports:
- Alphabetical A-Z
- Alphabetical Z-A
- Short to long
- Long to short
- Number small to large
- Number large to small
- Manual custom order

Strict content modes only:
- TextOnly: rectangular text cards.
- ImageOnly: square image cards with hidden Value used for sorting.
No mixed text+image mode.

## Expected folder structure

Assets/MiniGames/OrderSortDrag/
- Scripts/
  - OrderSortDragManager.cs
  - OrderSortDragItem.cs
  - OrderSortDropSlot.cs
  - OrderSortBankDropArea.cs
- Editor/
  - OrderSortDragSceneBuilder.cs

The user's project may use Assets/_Project/Mechanics/OrderSortDrag/. That is okay.

## Script roles

OrderSortDragManager.cs:
Main game manager. Handles question data, sorting, text/image mode, organic placement, responsive slots, timer, score, checking, DOTween feedback, result/how-to/pause panels, Bloom reward integration, and scene-template cloning.

OrderSortDragItem.cs:
Runtime draggable card. Handles pointer drag, current/previous slot, visible text/image, card face background, and text card random pastel color.

OrderSortDropSlot.cs:
Slot/drop target. Must use this production hierarchy:
SlotSceneTemplate
- SlotBackground
  - ItemHolder
  - IndexBadge
    - IndexText
  - ResultOverlay

Card goes into ItemHolder. ResultOverlay must stay above card.

OrderSortBankDropArea.cs:
Drop zone for returning cards to basket. Return can be enabled/disabled by inspector.

OrderSortDragSceneBuilder.cs:
Editor menu scene generator. No prefab assets. Creates scene templates under OrderSortSceneTemplates_DO_NOT_DELETE.

## Important design decisions

No prefab assets for this mechanic.
Use scene templates created in the scene.

Scene template root:
OrderSortSceneTemplates_DO_NOT_DELETE

Basket hierarchy:
BasketWrapper
- BasketBackground
  - CardAreaFrame
    - CardArea

Cards spawn into CardArea. BasketBackground and CardAreaFrame are intended for replacing with UI sprites.

Card hierarchy:
TextCardSceneTemplate / ImageObjectSceneTemplate
- CardFace
  - TextLabel   (TextOnly)
  - ObjectImage (ImageOnly)

Slot hierarchy:
SlotSceneTemplate
- SlotBackground
  - ItemHolder
  - IndexBadge
    - IndexText
  - ResultOverlay

Header hierarchy:
Header
- TitleBar
  - PauseButton
  - GameTitlePanel
    - GameTitleText
  - HowToPlayButton
- StatusBar
  - ScorePanel
  - TimePanel
  - InstructionPanel
  - ProgressPanel

ProgressPanel should show only if there is more than one runtime question.

## Current gameplay requirements

Organic basket:
Cards should be scattered organically in the basket, not placed as a strict grid, when organic placement is selected.

Return to basket:
Inspector option: Allow Return To Basket.
If enabled, user can drag placed cards back to basket.
If disabled, user cannot return them; show feedback message.

Swap:
Placed cards can swap. How-to-play should mention this.

Timer:
Timer is based on seconds per object multiplied by selected object count.

Object count:
Inspector controls how many objects/items are used per question.
The selected items should be random, not serial.

Scoring:
Correct slot: default +10.
Wrong slot: default -20.
Empty/skipped slot: 0.

Checking feedback:
1. User presses Check.
2. Each slot flashes checking color.
3. Correct goes to correct color.
4. Wrong goes to wrong color.
5. Empty goes to empty color.
6. All overlays stay visible after checking.
7. Score text updates live.
8. Score popup appears around middle of scene, not on the card.
9. Next/Result appears after evaluation.

Multi-question:
Do not remove multiple questions.
If only one runtime question, hide progress UI.
If more than one runtime question, show progress such as Question 1/3 or Round 1/3.
Question Limit = 0 means use all questions.

Text card colors:
TextOnly cards support random pastel card-face colors.
Inspector fields:
- Use Random Text Card Colors
- Text Card Pastel Colors
Five pastel colors are enough.

## Bloom Reward System integration

RewardManager already exists in LoadingScene and persists with DontDestroyOnLoad.
Do NOT add RewardManager prefab to this game scene.
Use RewardManager.Instance only.
Use namespace: using RewardSystem;

OrderSortDragManager must implement IGameSceneCallbacks.
If audio must stop on reward screen, also implement IGameAudioCallbacks.

Required callbacks:
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
    // Stop this game's BGM/audio here if needed.
}

Default Bloom skills:
private List<SkillEntry> _skills = new List<SkillEntry>
{
    new SkillEntry(BloomSkillType.Remember, 100f),
    new SkillEntry(BloomSkillType.Understand, 100f),
};

Flow:
1. Show Bloom pre-game.
2. Wait for IsPreGameComplete.
3. Show how-to-play.
4. Start gameplay only after pre-game is complete and how-to-play is closed/started.
5. Start timer only when gameplay begins.
6. Show result panel after game completes.
7. Continue button opens Bloom post-game.

Evaluation values must be normalized 0 to 1, not raw percentages.

## UI theme

Theme: soft pastel minimalistic.
Primary color: #FFDAB4.
Support: soft cream, warm white, muted peach, beige, dusty pink, soft brown.
Avoid dark/neon/arcade/clutter.

The user will replace final sprites manually. Do not generate artwork unless asked.

## Known fixes already made

- Overlay was hidden behind card. Fixed by using ItemHolder and keeping ResultOverlay above card.
- Return-to-basket did not work on basket cards. Fixed by forwarding/handling drops properly.
- Result panel not showing. Fixed by forcing popup panels above gameplay and showing after final evaluation.
- Score popup hidden on cards. Fixed by moving popup to center anchor.
- v9 compile error currentScorePopupIndex. Fixed in v9.1 by declaring the field.

## Cautions for future AI/developer

Do not:
- Reintroduce mixed text+image mode.
- Add prefab assets.
- Remove organic basket.
- Put score popup back on cards.
- Hide progress forever; show it only for multiple questions.
- Remove Bloom integration.
- Add RewardManager prefab.
- Start timer before Bloom pre-game completes.
- Enable gameplay input under overlay panels.
- Generate sprites unless user asks.

When updating:
- Prefer code-only changes if user already has a designed scene.
- If new references are needed, add inspector fields and fallback auto-find logic.
- Provide full updated ZIP, not just changed scripts.
- Keep explanations short and practical.

## Recommended continuation package

Start from:
OrderSortDrag_MiniGame_v9_1_CompileFix.zip

When handing off, attach:
1. This handoff document.
2. The latest package ZIP.
3. Screenshot of current Unity hierarchy/Inspector if user has manually edited UI.
4. Any current console errors.
